using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogObject : MonoBehaviour
{
    [SerializeField] private RectTransform backRt;
    [SerializeField] private Image chatBoxImage;
    [SerializeField] private Sprite BoxSprite;
    [SerializeField] private Image senderSprite;
    [SerializeField] private TextMeshProUGUI senderName;
    [SerializeField] private TextMeshProUGUI message;

    public void Init(ChatData chatData, int index)
    {
        var log = chatData.chatLogs[index];

        if (senderSprite != null) 
        {
            senderSprite.sprite = log.senderSprite;
            senderSprite.gameObject.SetActive(log.senderSprite != null && log.senderType != SenderType.System);
        }
        
        if (senderName != null) senderName.text = log.senderName ?? "";
        if (message != null) message.text = log.message ?? "";

        var rootLayout = GetComponent<HorizontalLayoutGroup>();
        var contentColumn = transform.Find("ContentColumn");
        var columnLayout = contentColumn != null ? contentColumn.GetComponent<VerticalLayoutGroup>() : null;

        if (rootLayout != null)
        {
            if (log.senderType == SenderType.System)
            {
                chatBoxImage.sprite = BoxSprite;
                rootLayout.childAlignment = TextAnchor.UpperCenter;
                if (columnLayout != null) columnLayout.childAlignment = TextAnchor.UpperCenter;
                if (senderName != null) senderName.alignment = TextAlignmentOptions.Center;
            }
            else if (log.senderType == SenderType.Player)
            {
                chatBoxImage.GetComponent<RectTransform>().localScale = new (-1, 1, 1);
                rootLayout.childAlignment = TextAnchor.UpperRight;
                if (columnLayout != null) columnLayout.childAlignment = TextAnchor.UpperRight;
                if (senderName != null) senderName.alignment = TextAlignmentOptions.Right;
                if (senderSprite != null) senderSprite.transform.SetAsLastSibling();
            }
            else
            {
                rootLayout.childAlignment = TextAnchor.UpperLeft;
                if (columnLayout != null) columnLayout.childAlignment = TextAnchor.UpperLeft;
                if (senderName != null) senderName.alignment = TextAlignmentOptions.Left;
                if (senderSprite != null) senderSprite.transform.SetAsFirstSibling();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
