using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image background;
    public Image icon;

    // This makes it easier for the Inventory script to find components if not assigned
    private void OnValidate()
    {
        if (background == null) background = GetComponent<Image>();
        if (icon == null && transform.childCount > 0) icon = transform.GetChild(0).GetComponent<Image>();
    }
}
