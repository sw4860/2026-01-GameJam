using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public class PickupItem
    {
        [Tooltip("Optional shared item data. Scene fields below override this when filled.")]
        public ItemData itemData;

        [Tooltip("Stable item id used by puzzles. Example: key, diary, file")]
        public string id;

        [Tooltip("Item name shown in the acquired popup. Example: 열쇠")]
        public string displayName;

        [Tooltip("Description shown under the acquired popup title.")]
        [TextArea] public string description;

        public GameObject sceneObject;
        public Sprite icon;
        public bool hideObjectOnPickup = true;

        public string Id => !string.IsNullOrWhiteSpace(id) ? id : itemData != null ? itemData.Id : string.Empty;
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : itemData != null ? itemData.DisplayName : string.Empty;
        public string Description => !string.IsNullOrWhiteSpace(description) ? description : itemData != null ? itemData.Description : string.Empty;
        public Sprite Icon => icon != null ? icon : itemData != null ? itemData.Icon : null;

        public void SetFallbackIcon(Sprite fallbackIcon)
        {
            if (icon == null)
            {
                icon = fallbackIcon;
            }
        }
    }

    [System.Serializable]
    public class InventorySlot
    {
        public Image background;
        public Image icon;
    }

    [Header("Scene Items")]
    [SerializeField] private List<PickupItem> pickupItems = new List<PickupItem>();

    [Header("Scene Slots")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Bag Window")]
    [SerializeField] private Image bagButton;
    [SerializeField] private GameObject bagWindow;
    [SerializeField] private List<InventorySlot> bagSlots = new List<InventorySlot>();

    [Header("Colors")]
    [SerializeField] private Color emptySlotColor = new Color(0.62f, 0.55f, 0.40f, 0.95f);
    [SerializeField] private Color selectedSlotColor = new Color(0.96f, 0.88f, 0.50f, 1f);
    [SerializeField] private Color bagWindowColor = new Color(0.12f, 0.10f, 0.09f, 0.94f);

    private readonly List<PickupItem> collectedItems = new List<PickupItem>();

    private Camera mainCamera;
    private Canvas inventoryCanvas;
    private Image dragPreviewIcon;
    private PickupItem draggedItem;
    private Vector3 dragOffset;
    private Vector2 dragStartScreenPosition;
    private SpriteRenderer draggedSpriteRenderer;
    private bool draggedSpriteWasEnabled;
    private int selectedIndex = -1;
    private bool bagOpen;
    private bool cabinetOpen;
    private bool safeOpen;
    private ClickableObject hoveredClickableObject;

    private const float ClickPickupDistance = 8f;

    private void Awake()
    {
        mainCamera = Camera.main;
        inventoryCanvas = GetInventoryCanvas();
        EnsureRuntimeManagers();
        CreateDragPreviewIcon();
        EnsurePuzzleClickTargets();
        SetBagOpen(false);
        RefreshSlots();
    }

    private void Update()
    {
        if (Mouse.current == null)
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        UpdateHover(screenPosition);

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            SetBagOpen(!bagOpen);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (TryToggleBag(screenPosition))
            {
                return;
            }

            if (bagOpen && TrySelectBagSlot(screenPosition))
            {
                return;
            }

            if (TrySelectSlot(screenPosition))
            {
                return;
            }

            if (TryUseSelectedItem(screenPosition))
            {
                return;
            }

            if (TryInteractWithClickedObject(screenPosition))
            {
                return;
            }

            BeginDrag(screenPosition);
        }

        if (draggedItem != null && Mouse.current.leftButton.isPressed)
        {
            Drag(screenPosition);
        }

        if (draggedItem != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag(screenPosition);
        }
    }

    public bool HasItem(string itemId)
    {
        return collectedItems.Exists(item => item.Id == itemId);
    }

    public PickupItem GetSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= collectedItems.Count)
        {
            return null;
        }

        return collectedItems[selectedIndex];
    }

    public void ClearSelectedItem()
    {
        selectedIndex = -1;
        RefreshSlots();
        RefreshBagWindow();
    }

    private void SetBagOpen(bool open)
    {
        bagOpen = open;
        if (bagWindow != null)
        {
            bagWindow.SetActive(open);
        }

        RefreshBagWindow();
    }

    private void EnsurePuzzleClickTargets()
    {
        EnsureCollider("Small Cabinet");
        EnsureCollider("Cabinet Door");
        EnsureCollider("Safe Image");

        ConfigureClickableObject(
            "Safe Image",
            "safe",
            "금고",
            "단단히 잠긴 금고다. 열쇠 구멍이 있다.",
            "key",
            "이걸로는 금고가 열리지 않는다.",
            "찰칵. 금고가 열렸다. 안쪽에서 오래된 기억 조각이 떠오른다.",
            "",
            "safe_open",
            true,
            "금고는 이미 열려 있다."
        );

    }

    private void EnsureRuntimeManagers()
    {
        if (GameState.Instance == null && FindFirstObjectByType<GameState>() == null)
        {
            gameObject.AddComponent<GameState>();
        }

        if (DialoguePopup.Instance == null && FindFirstObjectByType<DialoguePopup>() == null)
        {
            gameObject.AddComponent<DialoguePopup>();
        }

        if (ItemAcquiredPopup.Instance == null && FindFirstObjectByType<ItemAcquiredPopup>() == null)
        {
            gameObject.AddComponent<ItemAcquiredPopup>();
        }

        if (MemoryRestoredPopup.Instance == null && FindFirstObjectByType<MemoryRestoredPopup>() == null)
        {
            gameObject.AddComponent<MemoryRestoredPopup>();
        }

        if (EndingSequence.Instance == null && FindFirstObjectByType<EndingSequence>() == null)
        {
            gameObject.AddComponent<EndingSequence>();
        }
    }

    private void EnsureCollider(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null || target.GetComponent<Collider2D>() != null)
        {
            return;
        }

        target.AddComponent<BoxCollider2D>();
    }

    private void ConfigureClickableObject(
        string objectName,
        string objectId,
        string displayName,
        string examineText,
        string requiredItemId = "",
        string wrongItemText = "",
        string useSuccessText = "",
        string stateFlagOnExamine = "",
        string stateFlagOnUse = "",
        bool consumeRequiredItem = false,
        string afterStateText = "")
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            return;
        }

        ClickableObject clickableObject = target.GetComponent<ClickableObject>();
        if (clickableObject != null)
        {
            return;
        }

        clickableObject = target.AddComponent<ClickableObject>();

        clickableObject.Configure(
            objectId,
            displayName,
            examineText,
            requiredItemId,
            wrongItemText,
            useSuccessText,
            stateFlagOnExamine,
            stateFlagOnUse,
            consumeRequiredItem,
            afterStateText
        );
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        GameObject clickedObject = GetClickedObject(screenPosition);
        if (clickedObject == null)
        {
            return;
        }

        PickupItem item = FindPickupItem(clickedObject);
        if (item == null || collectedItems.Contains(item) || item.sceneObject == null)
        {
            return;
        }

        draggedItem = item;
        dragStartScreenPosition = screenPosition;
        dragOffset = item.sceneObject.transform.position - GetWorldPosition(screenPosition, item.sceneObject.transform.position.z);
        draggedSpriteRenderer = item.sceneObject.GetComponentInChildren<SpriteRenderer>();
        if (item.Icon == null && draggedSpriteRenderer != null)
        {
            item.SetFallbackIcon(draggedSpriteRenderer.sprite);
        }
        if (draggedSpriteRenderer != null)
        {
            draggedSpriteWasEnabled = draggedSpriteRenderer.enabled;
            draggedSpriteRenderer.enabled = false;
        }

        if (dragPreviewIcon != null)
        {
            dragPreviewIcon.sprite = item.Icon;
            dragPreviewIcon.color = draggedSpriteRenderer != null ? draggedSpriteRenderer.color : Color.white;
            dragPreviewIcon.enabled = item.Icon != null;
            dragPreviewIcon.transform.SetAsLastSibling();
            dragPreviewIcon.rectTransform.sizeDelta = GetDraggedItemScreenSize(draggedSpriteRenderer);
            dragPreviewIcon.rectTransform.position = screenPosition;
        }
    }

    private void Drag(Vector2 screenPosition)
    {
        if (draggedItem == null || draggedItem.sceneObject == null)
        {
            return;
        }

        draggedItem.sceneObject.transform.position = GetWorldPosition(screenPosition, draggedItem.sceneObject.transform.position.z) + dragOffset;

        if (dragPreviewIcon != null && dragPreviewIcon.enabled)
        {
            dragPreviewIcon.rectTransform.position = screenPosition;
        }
    }

    private void EndDrag(Vector2 screenPosition)
    {
        PickupItem releasedItem = draggedItem;
        SpriteRenderer releasedRenderer = draggedSpriteRenderer;
        bool releasedRendererWasEnabled = draggedSpriteWasEnabled;
        draggedItem = null;
        draggedSpriteRenderer = null;
        draggedSpriteWasEnabled = false;

        if (dragPreviewIcon != null)
        {
            dragPreviewIcon.enabled = false;
        }

        if (releasedItem == null)
        {
            return;
        }

        bool clickedWithoutDragging = Vector2.Distance(dragStartScreenPosition, screenPosition) <= ClickPickupDistance;
        bool shouldAddToInventory = clickedWithoutDragging || IsPointerOverAnySlot(screenPosition);
        if (shouldAddToInventory)
        {
            AddItem(releasedItem);
            return;
        }

        if (releasedRenderer != null)
        {
            releasedRenderer.enabled = releasedRendererWasEnabled;
        }
    }

    private bool TryUseSelectedItem(Vector2 screenPosition)
    {
        PickupItem selectedItem = GetSelectedItem();
        if (selectedItem == null)
        {
            return false;
        }

        GameObject clickedObject = GetClickedObject(screenPosition);
        if (clickedObject == null)
        {
            return false;
        }

        ClickableObject clickableObject = GetClickableObject(clickedObject);
        if (clickableObject != null && !string.IsNullOrWhiteSpace(clickableObject.RequiredItemId))
        {
            bool correctItem = clickableObject.CanUseItem(selectedItem.Id);
            bool handled = clickableObject.TryUseItem(selectedItem.Id);
            if (!handled)
            {
                return false;
            }

            if (correctItem)
            {
                if (clickableObject.ObjectId == "safe")
                {
                    safeOpen = true;
                    OpenSafeVisual();
                }

                if (clickableObject.ConsumeRequiredItem)
                {
                    RemoveCollectedItem(selectedItem);
                }
            }

            return true;
        }

        if (IsSafe(clickedObject))
        {
            UseItemOnSafe(selectedItem);
            return true;
        }

        if (IsCabinet(clickedObject))
        {
            UseItemOnCabinet(selectedItem);
            return true;
        }

        return false;
    }

    private bool TryInteractWithClickedObject(Vector2 screenPosition)
    {
        GameObject clickedObject = GetClickedObject(screenPosition);
        if (clickedObject == null)
        {
            return false;
        }

        ClickableObject clickableObject = GetClickableObject(clickedObject);
        return clickableObject != null && clickableObject.Examine();
    }

    private void UpdateHover(Vector2 screenPosition)
    {
        GameObject clickedObject = GetClickedObject(screenPosition);
        ClickableObject newHover = clickedObject != null ? GetClickableObject(clickedObject) : null;
        if (hoveredClickableObject == newHover)
        {
            return;
        }

        if (hoveredClickableObject != null)
        {
            hoveredClickableObject.SetHover(false);
        }

        hoveredClickableObject = newHover;
        if (hoveredClickableObject != null)
        {
            hoveredClickableObject.SetHover(true);
        }
    }

    private ClickableObject GetClickableObject(GameObject clickedObject)
    {
        if (clickedObject == null)
        {
            return null;
        }

        return clickedObject.GetComponentInParent<ClickableObject>();
    }

    private bool IsSafe(GameObject clickedObject)
    {
        return IsObjectOrChildNamed(clickedObject, "Safe Image");
    }

    private bool IsCabinet(GameObject clickedObject)
    {
        return IsObjectOrChildNamed(clickedObject, "Small Cabinet") ||
               IsObjectOrChildNamed(clickedObject, "Cabinet Door");
    }

    private bool IsObjectOrChildNamed(GameObject clickedObject, string objectName)
    {
        Transform current = clickedObject.transform;
        while (current != null)
        {
            if (current.name == objectName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void UseItemOnCabinet(PickupItem selectedItem)
    {
        if (cabinetOpen)
        {
            Debug.Log("The cabinet is already open.");
            return;
        }

        if (selectedItem.Id != "key")
        {
            Debug.Log(selectedItem.DisplayName + " does not fit here.");
            return;
        }

        cabinetOpen = true;
        OpenCabinetVisual();
        RemoveCollectedItem(selectedItem);
        Debug.Log("The key turns. The cabinet opens.");
    }

    private void UseItemOnSafe(PickupItem selectedItem)
    {
        if (safeOpen)
        {
            Debug.Log("The safe is already open.");
            return;
        }

        if (selectedItem.Id != "key")
        {
            Debug.Log(selectedItem.DisplayName + " does not open the safe.");
            return;
        }

        safeOpen = true;
        OpenSafeVisual();
        RemoveCollectedItem(selectedItem);
        Debug.Log("The key turns. The safe opens.");
    }

    private void OpenSafeVisual()
    {
        GameObject safe = GameObject.Find("Safe Image");
        if (safe == null)
        {
            return;
        }

        SpriteRenderer renderer = safe.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = new Color(0.48f, 0.50f, 0.48f, 1f);
        }

        // Position and size stay untouched; only the color changes to show the opened state.
    }

    private void OpenCabinetVisual()
    {
        GameObject door = GameObject.Find("Cabinet Door");
        if (door != null)
        {
            SpriteRenderer renderer = door.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0.18f, 0.12f, 0.09f, 1f);
            }

            door.transform.localPosition += new Vector3(0.28f, 0f, 0f);
        }

        GameObject cabinet = GameObject.Find("Small Cabinet");
        if (cabinet != null)
        {
            SpriteRenderer renderer = cabinet.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0.18f, 0.13f, 0.10f, 1f);
            }
        }
    }

    private GameObject GetClickedObject(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return null;
        }

        Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit2D = Physics2D.OverlapPoint(worldPosition);
        if (hit2D != null)
        {
            return hit2D.gameObject;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit3D))
        {
            return hit3D.collider.gameObject;
        }

        return null;
    }

    private Vector3 GetWorldPosition(Vector2 screenPosition, float worldZ)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return Vector3.zero;
        }

        float distanceFromCamera = Mathf.Abs(worldZ - mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera));
    }

    private PickupItem FindPickupItem(GameObject clickedObject)
    {
        foreach (PickupItem item in pickupItems)
        {
            if (item == null || item.sceneObject == null)
            {
                continue;
            }

            if (clickedObject == item.sceneObject || clickedObject.transform.IsChildOf(item.sceneObject.transform))
            {
                return item;
            }
        }

        return null;
    }

    private Canvas GetInventoryCanvas()
    {
        foreach (InventorySlot slot in slots)
        {
            if (slot != null && slot.background != null)
            {
                return slot.background.GetComponentInParent<Canvas>();
            }
        }

        return null;
    }

    private void CreateDragPreviewIcon()
    {
        if (inventoryCanvas == null)
        {
            return;
        }

        GameObject preview = new GameObject("Drag Preview Icon");
        preview.transform.SetParent(inventoryCanvas.transform, false);

        dragPreviewIcon = preview.AddComponent<Image>();
        dragPreviewIcon.raycastTarget = false;
        dragPreviewIcon.preserveAspect = true;
        dragPreviewIcon.enabled = false;

        RectTransform rectTransform = dragPreviewIcon.rectTransform;
        rectTransform.sizeDelta = new Vector2(36f, 36f);
    }

    private Vector2 GetDraggedItemScreenSize(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || mainCamera == null)
        {
            return new Vector2(36f, 36f);
        }

        Bounds bounds = spriteRenderer.bounds;
        Vector3 min = mainCamera.WorldToScreenPoint(bounds.min);
        Vector3 max = mainCamera.WorldToScreenPoint(bounds.max);

        float width = Mathf.Abs(max.x - min.x);
        float height = Mathf.Abs(max.y - min.y);

        return new Vector2(
            Mathf.Clamp(width, 24f, 140f),
            Mathf.Clamp(height, 24f, 140f)
        );
    }

    private void AddItem(PickupItem item)
    {
        if (item == null || collectedItems.Contains(item))
        {
            return;
        }

        if (collectedItems.Count >= slots.Count)
        {
            Debug.Log("Inventory is full.");
            return;
        }

        if (item.Icon == null && item.sceneObject != null)
        {
            SpriteRenderer spriteRenderer = item.sceneObject.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                item.SetFallbackIcon(spriteRenderer.sprite);
            }
        }

        collectedItems.Add(item);
        selectedIndex = collectedItems.Count - 1;
        GameState.Instance?.SetFlag("item_" + item.Id);

        if (item.hideObjectOnPickup && item.sceneObject != null)
        {
            item.sceneObject.SetActive(false);
        }

        string itemName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.Id : item.DisplayName;
        ItemAcquiredPopup.Instance?.Show(itemName, item.Description);

        RefreshSlots();
        RefreshBagWindow();
    }

    private void RemoveCollectedItem(PickupItem item)
    {
        int removedIndex = collectedItems.IndexOf(item);
        if (removedIndex < 0)
        {
            return;
        }

        collectedItems.RemoveAt(removedIndex);
        if (selectedIndex >= collectedItems.Count)
        {
            selectedIndex = collectedItems.Count - 1;
        }

        if (selectedIndex < 0)
        {
            selectedIndex = -1;
        }

        RefreshSlots();
        RefreshBagWindow();
    }

    private bool TrySelectSlot(Vector2 screenPosition)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].background == null)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(slots[i].background.rectTransform, screenPosition, null))
            {
                continue;
            }

            selectedIndex = i < collectedItems.Count && selectedIndex != i ? i : -1;
            RefreshSlots();
            RefreshBagWindow();
            return true;
        }

        return false;
    }

    private bool TryToggleBag(Vector2 screenPosition)
    {
        if (bagButton == null)
        {
            return false;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(bagButton.rectTransform, screenPosition, null))
        {
            return false;
        }

        SetBagOpen(!bagOpen);
        return true;
    }

    private bool TrySelectBagSlot(Vector2 screenPosition)
    {
        for (int i = 0; i < bagSlots.Count; i++)
        {
            if (bagSlots[i] == null || bagSlots[i].background == null)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(bagSlots[i].background.rectTransform, screenPosition, null))
            {
                continue;
            }

            selectedIndex = i < collectedItems.Count && selectedIndex != i ? i : -1;
            RefreshSlots();
            RefreshBagWindow();
            return true;
        }

        return false;
    }

    private bool IsPointerOverAnySlot(Vector2 screenPosition)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].background == null)
            {
                continue;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(slots[i].background.rectTransform, screenPosition, null))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            bool hasItem = i < collectedItems.Count;
            bool selected = i == selectedIndex;

            if (slots[i].background != null)
            {
                slots[i].background.color = selected ? selectedSlotColor : emptySlotColor;
            }

            if (slots[i].icon != null)
            {
                slots[i].icon.enabled = hasItem && collectedItems[i].Icon != null;
                slots[i].icon.sprite = hasItem ? collectedItems[i].Icon : null;
                slots[i].icon.preserveAspect = true;
            }
        }

        if (bagButton != null)
        {
            bagButton.color = bagOpen ? selectedSlotColor : emptySlotColor;
        }
    }

    private void RefreshBagWindow()
    {
        if (bagWindow == null)
        {
            return;
        }

        Image windowImage = bagWindow.GetComponent<Image>();
        if (windowImage != null)
        {
            windowImage.color = bagWindowColor;
        }

        for (int i = 0; i < bagSlots.Count; i++)
        {
            if (bagSlots[i] == null)
            {
                continue;
            }

            bool hasItem = i < collectedItems.Count;
            bool selected = i == selectedIndex;

            if (bagSlots[i].background != null)
            {
                bagSlots[i].background.color = selected ? selectedSlotColor : emptySlotColor;
            }

            if (bagSlots[i].icon != null)
            {
                bagSlots[i].icon.enabled = hasItem && collectedItems[i].Icon != null;
                bagSlots[i].icon.sprite = hasItem ? collectedItems[i].Icon : null;
                bagSlots[i].icon.preserveAspect = true;
            }
        }
    }
}
