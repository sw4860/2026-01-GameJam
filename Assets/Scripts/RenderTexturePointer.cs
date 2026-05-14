using UnityEngine;
using UnityEngine.UI;

public class RenderTexturePointer : MonoBehaviour
{
    public static RenderTexturePointer Instance;
    public Camera renderCamera;
    public RawImage renderArea;

    public void Awake()
    {
        Instance = this;
    }

    public bool TryGetWorldPositionInRenderArea(Vector2 screenPoint, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
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
                float depth = renderCamera.orthographic ? 10f : renderCamera.nearClipPlane + 0.1f;
                Vector3 viewportPoint = new Vector3(normalizedPoint.x, normalizedPoint.y, depth);
                worldPosition = renderCamera.ViewportToWorldPoint(viewportPoint);
                return true;
            }
        }

        return false;
    }

    public Vector3 GetWorldPositionInRenderArea(Vector2 point)
    {
        if (TryGetWorldPositionInRenderArea(point, out Vector3 worldPos))
        {
            return worldPos;
        }
        return Vector3.zero;
    }
}
