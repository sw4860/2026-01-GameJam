using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RenderTexturePointer : MonoBehaviour
{
    public static RenderTexturePointer Instance;
    public Camera renderCamera;
    public RawImage renderArea;

    private CursorManage lastHoveredCursor;
    private InteractableObject lastHoveredInteractable;

    public void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        UpdateViewportCursor();
    }

    private void UpdateViewportCursor()
    {
        if (renderCamera == null || renderArea == null || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (TryGetViewportPoint(mousePos, out Vector3 viewportPoint))
        {
            Ray ray = renderCamera.ViewportPointToRay(viewportPoint);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray);
            
            if (hit.collider != null)
            {
                // 1. 커서 컴포넌트 확인
                CursorManage cm = hit.collider.GetComponent<CursorManage>();
                if (cm != null && lastHoveredCursor != cm)
                {
                    cm.SetHoverCursor();
                    lastHoveredCursor = cm;
                }

                // 2. 상호작용 컴포넌트 하이라이트 확인
                InteractableObject io = hit.collider.GetComponent<InteractableObject>();
                if (io != null && lastHoveredInteractable != io)
                {
                    if (lastHoveredInteractable != null) lastHoveredInteractable.SetHighlight(false);
                    io.SetHighlight(true);
                    lastHoveredInteractable = io;
                }
                
                // 찾은 게 있다면 여기서 리턴 (리셋 방지)
                if (cm != null || io != null) return;
            }
        }

        // 아무것도 히트되지 않았거나 RenderTexture 밖으로 나갔을 때 리셋
        ClearLastHovered();
    }

    private void ClearLastHovered()
    {
        if (lastHoveredCursor != null)
        {
            CursorManage.ResetToDefault();
            lastHoveredCursor = null;
        }

        if (lastHoveredInteractable != null)
        {
            lastHoveredInteractable.SetHighlight(false);
            lastHoveredInteractable = null;
        }
    }

    private bool TryGetViewportPoint(Vector2 screenPoint, out Vector3 viewportPoint)
    {
        viewportPoint = Vector3.zero;
        if (renderArea == null) return false;
        RectTransform rt = renderArea.rectTransform;
        Canvas canvas = renderArea.canvas;
        Camera uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, uiCamera, out Vector2 local))
        {
            Vector2 normalizedPoint = new Vector2(
                (local.x - rt.rect.x) / rt.rect.width,
                (local.y - rt.rect.y) / rt.rect.height
            );

            if (normalizedPoint.x >= 0 && normalizedPoint.x <= 1 && normalizedPoint.y >= 0 && normalizedPoint.y <= 1)
            {
                viewportPoint = new Vector3(normalizedPoint.x, normalizedPoint.y, 0);
                return true;
            }
        }
        return false;
    }

    public bool TryGetWorldPositionInRenderArea(Vector2 screenPoint, out Vector3 worldPosition)
    {
        if (TryGetViewportPoint(screenPoint, out Vector3 viewportPoint))
        {
            float depth = renderCamera.orthographic ? 10f : renderCamera.nearClipPlane + 0.1f;
            viewportPoint.z = depth;
            worldPosition = renderCamera.ViewportToWorldPoint(viewportPoint);
            return true;
        }
        worldPosition = Vector3.zero;
        return false;
    }
}
