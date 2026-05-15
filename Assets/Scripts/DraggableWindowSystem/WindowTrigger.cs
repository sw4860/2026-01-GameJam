using UnityEngine;
using UnityEngine.EventSystems;


[RequireComponent(typeof(Collider2D))]
public class WindowTrigger : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private WindowData windowData;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (windowData != null && WindowManager.Instance != null)
        {
            WindowManager.Instance.OpenWindow(windowData);
        }
    }
}
