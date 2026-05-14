using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class DraggableWindow : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler
{
    [SerializeField] private RectTransform dragHandle;
    [SerializeField] private Vector2 windowSize;

    private Canvas canvas;
    private RectTransform rt;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        rt = GetComponent<RectTransform>();
        rt.sizeDelta = windowSize;
    }

    public void OnPointerDown(PointerEventData eventData) => rt.SetAsLastSibling();

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragHandle && !RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera))
            eventData.pointerDrag = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.delta / canvas.scaleFactor;

        rt.anchoredPosition += delta;

        Clamp();
    }

    private void Clamp()
    {
        RectTransform p = rt.parent as RectTransform;
        if (!p) return;

        Vector2 pos = rt.anchoredPosition;
        Rect pr = p.rect;
        Rect wr = rt.rect;

        pos.x = Mathf.Clamp(pos.x, pr.xMin - wr.xMin, pr.xMax - wr.xMax);
        pos.y = Mathf.Clamp(pos.y, pr.yMin - wr.yMin, pr.yMax - wr.yMax);
        rt.anchoredPosition = pos;
    }
}
