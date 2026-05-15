using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChatLogManager : MonoBehaviour
{
    public static ChatLogManager Instance { get; private set; }
    
    [Header("References")]
    public GameObject MessagePanel;
    public GameObject MessagePrefab;
    public RectTransform messageContent;
    public ScrollRect messageScrollRect;
    public Button nextButton;
    
    [Header("Data")]
    public ChatData currentChatData;
    public int chatIndex = 0;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnChatComplete;
    public UnityEngine.Events.UnityAction<string> OnActionTriggered;

    void Awake()
    {
        Instance = this;
        if (OnChatComplete == null) OnChatComplete = new UnityEngine.Events.UnityEvent();
    }

    void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextChat);
        }
    }

    public void StartChat(ChatData data)
    {
        currentChatData = data;
        chatIndex = 0;
        
        for (int i = messageContent.childCount - 1; i >= 0; i--)
        {
            Destroy(messageContent.GetChild(i).gameObject);
        }
        
        if (MessagePanel != null)
        {
            MessagePanel.SetActive(true);
            MessagePanel.GetComponent<RectTransform>().SetAsLastSibling();
        }
        ShowNextChat();
    }

    public void ShowNextChat()
    {
        if (currentChatData == null || chatIndex >= currentChatData.chatLogs.Length)
        {
            Debug.Log("End of chat");
            OnChatComplete?.Invoke();
            return;
        }

        AddChatLog(chatIndex);
        
        string tag = currentChatData.chatLogs[chatIndex].actionTag;
        if (!string.IsNullOrEmpty(tag))
        {
            OnActionTriggered?.Invoke(tag);
        }

        chatIndex++;
    }

    public void AddChatLog(int index)
    {
        if (currentChatData == null || index < 0 || index >= currentChatData.chatLogs.Length)
            return;

        GameObject messageObj = Instantiate(MessagePrefab, messageContent);
        if (messageObj.TryGetComponent<ChatLogObject>(out var chatLogObject))
        {
            chatLogObject.Init(currentChatData.chatLogs[index]);
        }

        StartCoroutine(UpdateChatUI());
    }

    IEnumerator UpdateChatUI()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent);
        Canvas.ForceUpdateCanvases();
        if (messageScrollRect != null) messageScrollRect.verticalNormalizedPosition = 0f;
    }
}
