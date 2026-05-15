using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InteractableObject : MonoBehaviour, IPointerDownHandler
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

    // 커서 변경은 이제 CursorManage와 RenderTexturePointer가 전담합니다.
    // 여기서는 색상 변경 등 순수 시각적 효과만 남깁니다.
    public void SetHighlight(bool active)
    {
        if (isInteracted && interactOnlyOnce) return;
        if (sr) sr.color = active ? new Color(1f, 0.9f, 0.6f) : originColor;
    }

    public void OnPointerDown(PointerEventData eventData) => Interact();

    public void Interact()
    {
        if (isInteracted && interactOnlyOnce) return;

        var selectedItem = Inventory.Instance?.GetSelectedItem();

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
        CursorManage.ResetToDefault(); // 상호작용 성공 시 커서 리셋

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

        if (itemToGive != null)
        {
            Inventory.PickupItem pickup = new Inventory.PickupItem { itemData = itemToGive };
            Inventory.Instance?.AddItem(pickup);
        }

        if (toggleObject != null) toggleObject.SetActive(true);
        if (disableColliderOnSuccess && col != null) col.enabled = false;
        
        if (!string.IsNullOrEmpty(globalEventId)) EventManager.Instance?.TriggerEvent(globalEventId);
        OnSuccess?.Invoke();

        if (disableObjectOnSuccess) gameObject.SetActive(false);
    }
}
