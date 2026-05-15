using UnityEngine;
using UnityEngine.EventSystems;

public class FixOnOffWindowObject : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private GameObject targetWindow;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetWindow == null) return;

        bool willBeActive = !targetWindow.activeSelf;
        targetWindow.SetActive(willBeActive);

        if (willBeActive)
        {
            targetWindow.transform.SetAsLastSibling();
        }
    }

    public void TogglePanel()
    {
        targetWindow.SetActive(!targetWindow.activeSelf);
    }
}
