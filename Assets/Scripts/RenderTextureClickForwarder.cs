using UnityEngine;
using UnityEngine.EventSystems;

public class RenderTextureClickForwarder : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (RenderTexturePointer.Instance == null) return;

        if (RenderTexturePointer.Instance.TryGetWorldPositionInRenderArea(eventData.position, out Vector3 worldPos))
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            
            if (hit != null)
            {
                ExecuteEvents.Execute(hit.gameObject, eventData, ExecuteEvents.pointerDownHandler);
                //Debug.Log($"Clicked on: {hit.gameObject.name} at {worldPos}");
            }
        }
    }
}
