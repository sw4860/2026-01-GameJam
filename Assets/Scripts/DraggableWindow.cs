using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggableWindow : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    private Canvas canvas;
    private RectTransform windowTransform;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        windowTransform = GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        windowTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
        ClampPos();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        windowTransform.SetAsLastSibling();
    }

    private void ClampPos()
    {
        RectTransform parentRT = windowTransform.parent as RectTransform;
        if (parentRT == null) return;

        Vector2 pos = windowTransform.anchoredPosition;

        Rect parentRect = parentRT.rect;
        Rect windowRect = windowTransform.rect;

        float minX = parentRect.xMin - windowRect.xMin;
        float maxX = parentRect.xMax - windowRect.xMax;
        float minY = parentRect.yMin - windowRect.yMin;
        float maxY = parentRect.yMax - windowRect.yMax;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        windowTransform.anchoredPosition = pos;
    }
}
