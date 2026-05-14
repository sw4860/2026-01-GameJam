using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChatLogManager : MonoBehaviour
{
    public static ChatLogManager Instance { get; private set; }
    
    [Header("References")]
    public GameObject MessagePrefab;
    public RectTransform messageContent;
    public ScrollRect messageScrollRect;
    public Button nextButton;
    
    [Header("Data")]
    public ChatData currentChatData;
    public int chatIndex = 0;

    void Awake()
    {
        Instance = this;
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
        
        ShowNextChat();
    }

    public void ShowNextChat()
    {
        if (currentChatData == null || chatIndex >= currentChatData.chatLogs.Length)
        {
            Debug.Log("End of chat");
            return;
        }

        AddChatLog(chatIndex);
        chatIndex++;
    }

    public void AddChatLog(int index)
    {
        if (currentChatData == null || index < 0 || index >= currentChatData.chatLogs.Length)
            return;

        string content = currentChatData.chatLogs[index].message;

        GameObject messageObj = Instantiate(MessagePrefab, messageContent);
        messageObj.TryGetComponent<ChatLogObject>(out var chatLogObject);
        chatLogObject.Init(currentChatData, index);

        StartCoroutine(UpdateChatUI());
    }

    IEnumerator UpdateChatUI()
    {
        yield return null;

        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent);
        Canvas.ForceUpdateCanvases();

        messageScrollRect.verticalNormalizedPosition = 0f;
    }
}
