using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InteractableObject : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("--- Requirements ---")]
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private bool consumeItemOnSuccess = false;
    [SerializeField] private bool interactOnlyOnce = true;

    [Header("--- Success Actions ---")]
    [SerializeField] private ChatData successChat;
    [SerializeField] private WindowData windowToOpen;
    [SerializeField] private ItemData itemToGive;
    [SerializeField] private string globalEventId;
    public UnityEvent OnSuccess;

    [Header("--- Examine / Fail Actions ---")]
    [SerializeField] private ChatData examineChat;
    [TextArea(2, 3)]
    [SerializeField] private string wrongItemText = "It doesn't seem to work with this.";

    [Header("--- Visual & State ---")]
    [SerializeField] private GameObject toggleObject;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private bool disableColliderOnSuccess = false;
    [SerializeField] private bool disableObjectOnSuccess = false;

    private SpriteRenderer sr;
    private Color originColor;
    private bool isInteracted = false;
    private Collider2D col;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (sr) originColor = sr.color;
        if (toggleObject != null) toggleObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData) => Interact();
    public void OnPointerEnter(PointerEventData eventData) => SetHover(true);
    public void OnPointerExit(PointerEventData eventData) => SetHover(false);

    private void SetHover(bool active)
    {
        if (isInteracted && interactOnlyOnce) 
        {
            SetCursor(defaultCursor);
            return;
        }

        if (sr) sr.color = active ? new Color(1f, 0.9f, 0.6f) : originColor;
        SetCursor(active ? hoverCursor : defaultCursor);
    }

    private void SetCursor(Texture2D tex)
    {
        Cursor.SetCursor(tex, Vector2.zero, CursorMode.Auto);
    }

    public void Interact()
    {
        if (isInteracted && interactOnlyOnce) return;

        var selectedItem = Inventory.Instance?.GetSelectedItem();

        // 1. Using an item
        if (selectedItem != null)
        {
            if (requiredItem != null && selectedItem.Id == requiredItem.Id)
            {
                if (consumeItemOnSuccess) Inventory.Instance.RemoveCollectedItem(selectedItem);
                StartSuccessSequence();
            }
            else
            {
                if (!string.IsNullOrEmpty(wrongItemText))
                    DialoguePopup.Instance?.Show(wrongItemText);
            }
            return;
        }

        // 2. Direct click (Examine or simple interaction)
        if (requiredItem == null)
        {
            StartSuccessSequence();
        }
        else
        {
            if (examineChat != null) ChatLogManager.Instance?.StartChat(examineChat);
            else DialoguePopup.Instance?.Show("It's locked or needs something.");
        }
    }

    private void StartSuccessSequence()
    {
        isInteracted = true;
        if (sr) sr.color = originColor;

        if (successChat != null && ChatLogManager.Instance != null)
        {
            if (windowToOpen != null) WindowManager.Instance?.OpenWindow(windowToOpen);
            ChatLogManager.Instance.OnChatComplete.AddListener(ExecuteActions);
            ChatLogManager.Instance.StartChat(successChat);
        }
        else
        {
            if (windowToOpen != null) WindowManager.Instance?.OpenWindow(windowToOpen);
            ExecuteActions();
        }
    }

    private void ExecuteActions()
    {
        if (ChatLogManager.Instance != null)
            ChatLogManager.Instance.OnChatComplete.RemoveListener(ExecuteActions);

        // Give Item
        if (itemToGive != null)
        {
            Inventory.PickupItem pickup = new Inventory.PickupItem { itemData = itemToGive };
            Inventory.Instance?.AddItem(pickup);
        }

        // Visual State Toggle
        if (toggleObject != null) toggleObject.SetActive(true);
        if (disableColliderOnSuccess && col != null) col.enabled = false;
        
        // Trigger Events
        if (!string.IsNullOrEmpty(globalEventId)) EventManager.Instance?.TriggerEvent(globalEventId);
        OnSuccess?.Invoke();

        if (disableObjectOnSuccess) gameObject.SetActive(false);
    }

    public void ResetInteraction()
    {
        isInteracted = false;
        if (col != null) col.enabled = true;
        if (toggleObject != null) toggleObject.SetActive(false);
        gameObject.SetActive(true);
    }
}
