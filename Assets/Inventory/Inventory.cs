using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
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

        [Tooltip("Optional full popup title. Example: [열쇠를 획득했다.]")]
        public string acquiredText;

        public GameObject sceneObject;
        public Sprite icon;
        public bool hideObjectOnPickup = true;

        public string Id => !string.IsNullOrWhiteSpace(id) ? id : itemData != null ? itemData.Id : string.Empty;
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName : itemData != null ? itemData.DisplayName : string.Empty;
        public string Description => !string.IsNullOrWhiteSpace(description) ? description : itemData != null ? itemData.Description : string.Empty;
        public string AcquiredText => !string.IsNullOrWhiteSpace(acquiredText) ? acquiredText : BuildDefaultAcquiredText();
        public Sprite Icon => icon != null ? icon : itemData != null ? itemData.Icon : null;

        public void SetFallbackIcon(Sprite fallbackIcon)
        {
            if (icon == null)
            {
                icon = fallbackIcon;
            }
        }

        private string BuildDefaultAcquiredText()
        {
            string itemName = DisplayName;
            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = Id;
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = "아이템";
            }

            return "[" + itemName + "을(를) 획득했다.]";
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
        if (!Application.isPlaying)
        {
            ApplyMemoryRoomEditorLayout();
            return;
        }

        mainCamera = Camera.main;
        inventoryCanvas = GetInventoryCanvas();
        EnsureMemoryRoomClickTargets();
        EnsurePickupItemVisuals();
        EnsureRuntimeManagers();
        CreateDragPreviewIcon();
        EnsurePuzzleClickTargets();
        SetBagOpen(false);
        RefreshSlots();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyMemoryRoomEditorLayout();
            EnsureMemoryRoomClickTargets();
            EnsurePickupItemVisuals();
        }
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

    private void ApplyMemoryRoomEditorLayout()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }

        GameObject roomLayout = GameObject.Find("Room Layout");
        if (roomLayout != null)
        {
            roomLayout.SetActive(true);
        }

        HideLegacyRoomObject("Long Table Top");
        HideLegacyRoomObject("Long Table Front");
        HideLegacyRoomObject("Table Left Leg");
        HideLegacyRoomObject("Table Right Leg");
        HideLegacyRoomObject("Wall Shelf");
        HideLegacyRoomObject("Floor");
        HideLegacyRoomObject("Back Shadow");
        HideLegacyRoomObject("Safe Image");

        GameObject backWall = GameObject.Find("Back Wall");
        if (backWall == null)
        {
            backWall = new GameObject("Back Wall");
        }

        if (roomLayout != null)
        {
            backWall.transform.SetParent(roomLayout.transform, true);
        }

        SpriteRenderer renderer = backWall.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backWall.AddComponent<SpriteRenderer>();
        }

        Sprite roomSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Inventory/Sprites/Memory01/Memory01_Background_Square_Tight.png");
        if (roomSprite != null)
        {
            renderer.sprite = roomSprite;
        }

        backWall.SetActive(true);
        backWall.transform.position = new Vector3(0f, 0f, 0f);
        backWall.transform.localRotation = Quaternion.identity;
        backWall.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = -30;
        renderer.color = Color.white;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 2.7f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.86f, 0.91f, 0.98f, 1f);
        }

        EditorUtility.SetDirty(gameObject);
        EditorUtility.SetDirty(backWall);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void HideLegacyRoomObject(string objectName)
    {
#if UNITY_EDITOR
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            target.SetActive(false);
            EditorUtility.SetDirty(target);
        }
#endif
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

    private void EnsureMemoryRoomClickTargets()
    {
        EnsureMemoryRoomClickTarget(
            "Click Desk",
            "Click Drawer",
            "drawer",
            "서랍",
            "잠겨 있는 서랍이다. 열쇠 구멍이 보인다.",
            new Vector2(1.55f, -0.48f),
            new Vector2(0.44f, 0.55f),
            "key",
            "이걸로는 서랍을 열 수 없다.",
            "딸깍. 서랍이 열렸다. 안에서 로그 파일이 나왔다.",
            "drawer_open"
        );

        EnsureMemoryRoomClickTarget(
            "Click Wardrobe",
            "Click Wardrobe",
            "wardrobe",
            "옷장",
            "옷장 안에는 가족사진 속 원피스와 닮은 옷이 걸려 있다.",
            new Vector2(2.55f, -0.05f),
            new Vector2(0.88f, 2.15f)
        );

        EnsureMemoryRoomClickTarget(
            "Click Bed",
            "Click Bed",
            "bed",
            "침대",
            "작은 아이가 오래 누워 있었던 듯한 침대다.",
            new Vector2(-0.55f, 0.05f),
            new Vector2(1.45f, 0.98f)
        );

        EnsureMemoryRoomClickTarget(
            "Click Window",
            "Click Window",
            "window",
            "창문",
            "창밖의 빛이 조용히 방 안으로 들어온다.",
            new Vector2(1.18f, 1.12f),
            new Vector2(1.18f, 0.62f)
        );

        EnsureMemoryRoomClickTarget(
            "Click Bookshelf",
            "Click Bookshelf",
            "bookshelf",
            "책장",
            "책장에는 낡은 책과 기억의 흔적들이 가지런히 남아 있다.",
            new Vector2(-1.92f, 0.05f),
            new Vector2(0.82f, 2.15f)
        );
    }

    private void EnsureMemoryRoomClickTarget(
        string oldName,
        string objectName,
        string objectId,
        string displayName,
        string examineText,
        Vector2 position,
        Vector2 size,
        string requiredItemId = "",
        string wrongItemText = "",
        string useSuccessText = "",
        string stateFlagOnUse = "")
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null && !string.IsNullOrWhiteSpace(oldName))
        {
            target = GameObject.Find(oldName);
        }

        if (target == null)
        {
            target = new GameObject(objectName);
        }

        target.name = objectName;
        target.transform.position = new Vector3(position.x, position.y, 0f);
        target.transform.localScale = Vector3.one;

        BoxCollider2D collider = target.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = target.AddComponent<BoxCollider2D>();
        }

        collider.isTrigger = true;
        collider.size = size;
        collider.offset = Vector2.zero;

        ClickableObject clickableObject = target.GetComponent<ClickableObject>();
        if (clickableObject == null)
        {
            clickableObject = target.AddComponent<ClickableObject>();
        }

        clickableObject.Configure(
            objectId,
            displayName,
            examineText,
            requiredItemId,
            wrongItemText,
            useSuccessText,
            "",
            stateFlagOnUse,
            false,
            stateFlagOnUse == "drawer_open" ? "서랍은 이미 열려 있다." : ""
        );
    }

    private void EnsurePickupItemVisuals()
    {
        foreach (PickupItem item in pickupItems)
        {
            if (item == null || item.sceneObject == null)
            {
                continue;
            }

            if (item.Id == "file" && !IsStateFlagSet("drawer_open"))
            {
                item.sceneObject.SetActive(false);
                continue;
            }

            item.sceneObject.SetActive(true);

            foreach (SpriteRenderer spriteRenderer in item.sceneObject.GetComponentsInChildren<SpriteRenderer>(true))
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 15);

                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }
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

                if (clickableObject.ObjectId == "drawer")
                {
                    RevealPickupItem("file");
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
        Collider2D[] hit2DObjects = Physics2D.OverlapPointAll(worldPosition);
        foreach (Collider2D hit2D in hit2DObjects)
        {
            if (FindPickupItem(hit2D.gameObject) != null)
            {
                return hit2D.gameObject;
            }
        }

        Collider2D bestClickable = null;
        float bestClickableArea = float.MaxValue;
        foreach (Collider2D hit2D in hit2DObjects)
        {
            if (hit2D.GetComponentInParent<ClickableObject>() != null)
            {
                Bounds bounds = hit2D.bounds;
                float area = bounds.size.x * bounds.size.y;
                if (area < bestClickableArea)
                {
                    bestClickable = hit2D;
                    bestClickableArea = area;
                }
            }
        }

        if (bestClickable != null)
        {
            return bestClickable.gameObject;
        }

        if (hit2DObjects.Length > 0)
        {
            return hit2DObjects[0].gameObject;
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

        ItemAcquiredPopup.Instance?.Show(item.AcquiredText, item.Description);

        RefreshSlots();
        RefreshBagWindow();
    }

    private bool IsStateFlagSet(string flagName)
    {
        return !string.IsNullOrWhiteSpace(flagName) &&
               GameState.Instance != null &&
               GameState.Instance.HasFlag(flagName);
    }

    private void RevealPickupItem(string itemId)
    {
        PickupItem item = pickupItems.Find(candidate => candidate != null && candidate.Id == itemId);
        if (item == null || item.sceneObject == null || collectedItems.Contains(item))
        {
            return;
        }

        item.sceneObject.SetActive(true);
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
            if (slots[i] == null || slots[i].background == null || !slots[i].background.gameObject.activeInHierarchy)
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
        if (bagButton == null || !bagButton.gameObject.activeInHierarchy)
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
            if (bagSlots[i] == null || bagSlots[i].background == null || !bagSlots[i].background.gameObject.activeInHierarchy)
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
            if (slots[i] == null || slots[i].background == null || !slots[i].background.gameObject.activeInHierarchy)
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
