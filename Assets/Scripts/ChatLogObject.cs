using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogObject : MonoBehaviour
{
    [SerializeField] private Image chatBoxImage;
    [SerializeField] private Sprite systemBoxSprite;
    [SerializeField] private Image senderSprite;
    [SerializeField] private TextMeshProUGUI senderName;
    [SerializeField] private TextMeshProUGUI message;

    public void Init(chatLogs log)
    {
        if (log == null) return;

        // [핵심 진단] 타입이 숫자로 어떻게 들어오는지 확인 (0: Player, 1: System, 2: Other)
        Debug.Log($"[ChatLogObject] Init - Name: {log.senderName}, Type: {log.senderType} ({(int)log.senderType})");

        // 1. 기본 데이터 설정
        if (senderName != null) senderName.text = log.senderName ?? "";
        if (message != null) message.text = log.message ?? "";
        if (senderSprite != null) 
        {
            senderSprite.sprite = log.senderSprite;
            senderSprite.gameObject.SetActive(log.senderSprite != null && log.senderType != SenderType.System);
        }

        // 2. 레이아웃 컴포넌트 참조
        var rootLayout = GetComponent<HorizontalLayoutGroup>();
        var contentColumnTransform = transform.Find("ContentColumn");
        var contentColumn = contentColumnTransform != null ? contentColumnTransform.GetComponent<RectTransform>() : null;
        var columnLayout = contentColumn != null ? contentColumn.GetComponent<VerticalLayoutGroup>() : null;

        if (rootLayout != null)
        {
            // 리셋 및 초기화
            ResetLayout(rootLayout, columnLayout);

            // 타입별 설정
            switch (log.senderType)
            {
                case SenderType.System:
                    ApplySystemLayout(rootLayout, columnLayout);
                    break;
                case SenderType.Player:
                    ApplyPlayerLayout(rootLayout, columnLayout);
                    break;
                case SenderType.Other:
                default:
                    ApplyOtherLayout(rootLayout, columnLayout);
                    break;
            }

            // 3. 레이아웃 강제 갱신
            if (contentColumn != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentColumn);
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }

    private void ResetLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        root.reverseArrangement = false;
        root.childAlignment = TextAnchor.UpperLeft;
        
        if (chatBoxImage != null)
        {
            chatBoxImage.rectTransform.localScale = Vector3.one;
        }
        
        if (message != null)
        {
            message.rectTransform.localScale = Vector3.one;
            message.alignment = TextAlignmentOptions.Left;
        }

        if (column != null)
        {
            column.childAlignment = TextAnchor.UpperLeft;
        }

        if (senderName != null)
        {
            senderName.alignment = TextAlignmentOptions.Left;
        }
    }

    private void ApplySystemLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        if (chatBoxImage != null && systemBoxSprite != null) chatBoxImage.sprite = systemBoxSprite;
        root.childAlignment = TextAnchor.UpperCenter;
        if (column != null) column.childAlignment = TextAnchor.UpperCenter;
        if (senderName != null) senderName.alignment = TextAlignmentOptions.Center;
        if (message != null) message.alignment = TextAlignmentOptions.Center;
    }

    private void ApplyPlayerLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        root.reverseArrangement = true; 
        root.childAlignment = TextAnchor.UpperRight;
        if (column != null) column.childAlignment = TextAnchor.UpperRight;
        if (senderName != null) senderName.alignment = TextAlignmentOptions.Right;

        if (chatBoxImage != null)
        {
            chatBoxImage.rectTransform.localScale = new Vector3(-1, 1, 1);
            if (message != null)
            {
                if (message.transform.IsChildOf(chatBoxImage.transform))
                {
                    message.rectTransform.localScale = new Vector3(-1, 1, 1);
                }
                message.alignment = TextAlignmentOptions.Right;
            }
        }
    }

    private void ApplyOtherLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        // ResetLayout에서 이미 기본값으로 설정됨
    }
}
