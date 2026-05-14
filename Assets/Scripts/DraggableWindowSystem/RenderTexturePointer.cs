using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RenderTexturePointer : MonoBehaviour
{
    public static RenderTexturePointer Instance;
    public Camera renderCamera;
    public RawImage renderArea;

    private CursorManage lastHoveredCursor;

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
                CursorManage cm = hit.collider.GetComponent<CursorManage>();
                if (cm != null)
                {
                    if (lastHoveredCursor != cm)
                    {
                        if (lastHoveredCursor != null) lastHoveredCursor.SetDefaultCursor();
                        cm.SetHoverCursor();
                        lastHoveredCursor = cm;
                    }
                    return;
                }
            }
        }

        if (lastHoveredCursor != null)
        {
            lastHoveredCursor.SetDefaultCursor();
            lastHoveredCursor = null;
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
