using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [System.Serializable]
    public class PickupItem
    {
        public ItemData itemData;
        public string id;
        public string displayName;
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
            if (icon == null) icon = fallbackIcon;
        }
    }

    [Header("Dynamic Slot Settings")]
    [SerializeField] private GameObject slotPrefab;
    
    [Header("Main Quick Slots")]
    [SerializeField] private int mainSlotCount = 5;
    [SerializeField] private Transform mainSlotParent;
    private List<InventorySlotUI> mainSlots = new List<InventorySlotUI>();

    [Header("Bag Window")]
    [SerializeField] private Image bagButton;
    [SerializeField] private GameObject bagWindow;
    [SerializeField] private int bagSlotCount = 20;
    [SerializeField] private Transform bagSlotParent;
    private List<InventorySlotUI> bagSlots = new List<InventorySlotUI>();

    [Header("Colors")]
    [SerializeField] private Color emptySlotColor = new Color(0.62f, 0.55f, 0.40f, 0.95f);
    [SerializeField] private Color selectedSlotColor = new Color(0.96f, 0.88f, 0.50f, 1f);
    [SerializeField] private Color bagWindowColor = new Color(0.12f, 0.10f, 0.09f, 0.94f);

    [Header("Scene Items")]
    [SerializeField] private List<PickupItem> pickupItems = new List<PickupItem>();

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
    public bool cabinetOpen;
    public bool safeOpen;

    private const float ClickPickupDistance = 8f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCamera = Camera.main;
        InitializeSlots();
        
        inventoryCanvas = GetInventoryCanvas();
        EnsureRuntimeManagers();
        CreateDragPreviewIcon();
        EnsurePuzzleClickTargets();
        SetBagOpen(false);
        RefreshSlots();
    }

    private void InitializeSlots()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("Inventory: Slot Prefab is missing!");
            return;
        }

        CreateSlotsInParent(mainSlotCount, mainSlotParent, mainSlots);
        CreateSlotsInParent(bagSlotCount, bagSlotParent, bagSlots);
    }

    private void CreateSlotsInParent(int count, Transform parent, List<InventorySlotUI> list)
    {
        if (parent == null) return;

        foreach (Transform child in parent) Destroy(child.gameObject);
        list.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, parent);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = slotObj.AddComponent<InventorySlotUI>();
            list.Add(slotUI);
        }
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        Vector2 screenPosition = Mouse.current.position.ReadValue();

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            SetBagOpen(!bagOpen);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (TryToggleBag(screenPosition)) return;
            if (bagOpen && TrySelectBagSlot(screenPosition)) return;
            if (TrySelectSlot(screenPosition)) return;
            BeginDrag(screenPosition);
        }

        if (draggedItem != null && Mouse.current.leftButton.isPressed) Drag(screenPosition);
        if (draggedItem != null && Mouse.current.leftButton.wasReleasedThisFrame) EndDrag(screenPosition);
    }

    public bool HasItem(string itemId) => collectedItems.Exists(item => item.Id == itemId);
    
    public PickupItem GetSelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= collectedItems.Count) return null;
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
        if (bagWindow != null) bagWindow.SetActive(open);
        RefreshBagWindow();
    }

    private void RefreshSlots()
    {
        UpdateSlotVisuals(mainSlots);
        if (bagButton != null) bagButton.color = bagOpen ? selectedSlotColor : emptySlotColor;
    }

    private void RefreshBagWindow()
    {
        if (bagWindow == null) return;
        Image windowImage = bagWindow.GetComponent<Image>();
        if (windowImage != null) windowImage.color = bagWindowColor;
        UpdateSlotVisuals(bagSlots);
    }

    private void UpdateSlotVisuals(List<InventorySlotUI> slotList)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (slotList[i] == null) continue;
            bool hasItem = i < collectedItems.Count;
            bool selected = i == selectedIndex;

            if (slotList[i].background != null)
                slotList[i].background.color = selected ? selectedSlotColor : emptySlotColor;

            if (slotList[i].icon != null)
            {
                slotList[i].icon.enabled = hasItem && collectedItems[i].Icon != null;
                slotList[i].icon.sprite = hasItem ? collectedItems[i].Icon : null;
                slotList[i].icon.preserveAspect = true;
            }
        }
    }

    private bool TrySelectSlot(Vector2 screenPosition) => TrySelectFromList(screenPosition, mainSlots);
    private bool TrySelectBagSlot(Vector2 screenPosition) => TrySelectFromList(screenPosition, bagSlots);

    private bool TrySelectFromList(Vector2 screenPosition, List<InventorySlotUI> slotList)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (slotList[i] == null || slotList[i].background == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(slotList[i].background.rectTransform, screenPosition, null))
            {
                selectedIndex = (i < collectedItems.Count && selectedIndex != i) ? i : -1;
                RefreshSlots();
                RefreshBagWindow();
                return true;
            }
        }
        return false;
    }

    private bool IsPointerOverAnySlot(Vector2 screenPosition)
    {
        foreach (var slot in mainSlots)
        {
            if (slot != null && slot.background != null && RectTransformUtility.RectangleContainsScreenPoint(slot.background.rectTransform, screenPosition, null))
                return true;
        }
        foreach (var slot in bagSlots)
        {
            if (slot != null && slot.background != null && RectTransformUtility.RectangleContainsScreenPoint(slot.background.rectTransform, screenPosition, null))
                return true;
        }
        return false;
    }

    public void AddItem(PickupItem item)
    {
        if (item == null || collectedItems.Contains(item)) return;
        int capacity = Mathf.Max(mainSlotCount, bagSlotCount);
        if (collectedItems.Count >= capacity) return;

        if (item.Icon == null && item.sceneObject != null)
        {
            SpriteRenderer sr = item.sceneObject.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) item.SetFallbackIcon(sr.sprite);
        }

        collectedItems.Add(item);
        selectedIndex = collectedItems.Count - 1;
        StaticValues.SetFlag("item_" + item.Id);

        if (item.hideObjectOnPickup && item.sceneObject != null) item.sceneObject.SetActive(false);
        ItemAcquiredPopup.Instance?.Show(string.IsNullOrWhiteSpace(item.DisplayName) ? item.Id : item.DisplayName, item.Description);

        RefreshSlots();
        RefreshBagWindow();
    }

    public void RemoveCollectedItem(PickupItem item)
    {
        int idx = collectedItems.IndexOf(item);
        if (idx < 0) return;
        collectedItems.RemoveAt(idx);
        if (selectedIndex >= collectedItems.Count) selectedIndex = collectedItems.Count - 1;
        if (selectedIndex < 0) selectedIndex = -1;
        RefreshSlots();
        RefreshBagWindow();
    }

    private bool TryToggleBag(Vector2 screenPosition)
    {
        if (bagButton == null) return false;
        if (!RectTransformUtility.RectangleContainsScreenPoint(bagButton.rectTransform, screenPosition, null)) return false;
        SetBagOpen(!bagOpen);
        return true;
    }

    private GameObject GetClickedObject(Vector2 screenPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return null;
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit != null) return hit.gameObject;
        return null;
    }

    private Vector3 GetWorldPosition(Vector2 screenPosition, float worldZ)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        float dist = Mathf.Abs(worldZ - mainCamera.transform.position.z);
        return mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, dist));
    }

    private PickupItem FindPickupItem(GameObject clickedObject)
    {
        foreach (var item in pickupItems)
        {
            if (item?.sceneObject == null) continue;
            if (clickedObject == item.sceneObject || clickedObject.transform.IsChildOf(item.sceneObject.transform)) return item;
        }
        return null;
    }

    private Canvas GetInventoryCanvas()
    {
        if (mainSlotParent != null) return mainSlotParent.GetComponentInParent<Canvas>();
        if (bagSlotParent != null) return bagSlotParent.GetComponentInParent<Canvas>();
        return null;
    }

    private void CreateDragPreviewIcon()
    {
        if (inventoryCanvas == null) return;
        GameObject preview = new GameObject("Drag Preview Icon");
        preview.transform.SetParent(inventoryCanvas.transform, false);
        dragPreviewIcon = preview.AddComponent<Image>();
        dragPreviewIcon.raycastTarget = false;
        dragPreviewIcon.preserveAspect = true;
        dragPreviewIcon.enabled = false;
        dragPreviewIcon.rectTransform.sizeDelta = new Vector2(36f, 36f);
    }

    private Vector2 GetDraggedItemScreenSize(SpriteRenderer sr)
    {
        if (sr == null || mainCamera == null) return new Vector2(36f, 36f);
        Bounds b = sr.bounds;
        Vector3 min = mainCamera.WorldToScreenPoint(b.min);
        Vector3 max = mainCamera.WorldToScreenPoint(b.max);
        return new Vector2(Mathf.Clamp(Mathf.Abs(max.x - min.x), 24f, 140f), Mathf.Clamp(Mathf.Abs(max.y - min.y), 24f, 140f));
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        GameObject clicked = GetClickedObject(screenPosition);
        if (clicked == null) return;
        PickupItem item = FindPickupItem(clicked);
        if (item == null || collectedItems.Contains(item) || item.sceneObject == null) return;

        draggedItem = item;
        dragStartScreenPosition = screenPosition;
        dragOffset = item.sceneObject.transform.position - GetWorldPosition(screenPosition, item.sceneObject.transform.position.z);
        draggedSpriteRenderer = item.sceneObject.GetComponentInChildren<SpriteRenderer>();
        if (item.Icon == null && draggedSpriteRenderer != null) item.SetFallbackIcon(draggedSpriteRenderer.sprite);
        if (draggedSpriteRenderer != null) { draggedSpriteWasEnabled = draggedSpriteRenderer.enabled; draggedSpriteRenderer.enabled = false; }

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
        if (draggedItem?.sceneObject == null) return;
        draggedItem.sceneObject.transform.position = GetWorldPosition(screenPosition, draggedItem.sceneObject.transform.position.z) + dragOffset;
        if (dragPreviewIcon != null && dragPreviewIcon.enabled) dragPreviewIcon.rectTransform.position = screenPosition;
    }

    private void EndDrag(Vector2 screenPosition)
    {
        PickupItem released = draggedItem;
        SpriteRenderer sr = draggedSpriteRenderer;
        bool wasEnabled = draggedSpriteWasEnabled;
        draggedItem = null; draggedSpriteRenderer = null; draggedSpriteWasEnabled = false;
        if (dragPreviewIcon != null) dragPreviewIcon.enabled = false;
        if (released == null) return;

        bool isClick = Vector2.Distance(dragStartScreenPosition, screenPosition) <= ClickPickupDistance;
        if (isClick || IsPointerOverAnySlot(screenPosition)) AddItem(released);
        else if (sr != null) sr.enabled = wasEnabled;
    }

    private void EnsureRuntimeManagers()
    {
        if (DialoguePopup.Instance == null) gameObject.AddComponent<DialoguePopup>();
        if (ItemAcquiredPopup.Instance == null) gameObject.AddComponent<ItemAcquiredPopup>();
        if (MemoryRestoredPopup.Instance == null) gameObject.AddComponent<MemoryRestoredPopup>();
        if (EndingSequence.Instance == null) gameObject.AddComponent<EndingSequence>();
    }

    private void EnsurePuzzleClickTargets()
    {
        EnsureCollider("Small Cabinet"); EnsureCollider("Cabinet Door"); EnsureCollider("Safe Image");
        ConfigureClickableObject("Safe Image", "safe", "금고", "잠긴 금고다.", "key", "안 열린다.", "금고가 열렸다.", "", "safe_open", true, "이미 열려 있다.");
    }

    private void EnsureCollider(string name) { GameObject t = GameObject.Find(name); if (t != null && t.GetComponent<Collider2D>() == null) t.AddComponent<BoxCollider2D>(); }

    private void ConfigureClickableObject(string name, string id, string dName, string ex, string req = "", string wr = "", string succ = "", string fEx = "", string fUse = "", bool cons = false, string aft = "")
    {
        GameObject t = GameObject.Find(name); if (t == null || t.GetComponent<ClickableObject>() != null) return;
        t.AddComponent<ClickableObject>().Configure(id, dName, ex, req, wr, succ, fEx, fUse, cons, aft);
    }

    public void OpenSafeVisual() { GameObject s = GameObject.Find("Safe Image"); if (s?.GetComponent<SpriteRenderer>() != null) s.GetComponent<SpriteRenderer>().color = new Color(0.48f, 0.5f, 0.48f); }
    public void OpenCabinetVisual() { 
        GameObject d = GameObject.Find("Cabinet Door"); if (d?.GetComponent<SpriteRenderer>() != null) { d.GetComponent<SpriteRenderer>().color = new Color(0.18f, 0.12f, 0.09f); d.transform.localPosition += new Vector3(0.28f, 0, 0); }
        GameObject c = GameObject.Find("Small Cabinet"); if (c?.GetComponent<SpriteRenderer>() != null) c.GetComponent<SpriteRenderer>().color = new Color(0.18f, 0.13f, 0.1f);
    }
}
