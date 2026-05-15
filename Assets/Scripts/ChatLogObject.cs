using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogObject : MonoBehaviour
{
    [SerializeField] private RectTransform backRt;
    [SerializeField] private Image chatBoxImage;
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Image senderSprite;
    [SerializeField] private TextMeshProUGUI senderName;
    [SerializeField] private TextMeshProUGUI message;

    public void Init(chatLogs log)
    {
        if (log == null) return;

        // 1. 이름 및 메시지 설정
        if (senderName != null) senderName.text = log.senderName ?? "";
        if (message != null) message.text = log.message ?? "";

        // 2. 스프라이트 설정
        if (senderSprite != null) 
        {
            senderSprite.sprite = log.senderSprite;
            // 시스템 메시지가 아니고 스프라이트가 있을 때만 활성화
            senderSprite.gameObject.SetActive(log.senderSprite != null && log.senderType != SenderType.System);
        }

        // 3. 레이아웃 및 정렬 설정
        var rootLayout = GetComponent<HorizontalLayoutGroup>();
        var contentColumn = transform.Find("ContentColumn");
        var columnLayout = contentColumn != null ? contentColumn.GetComponent<VerticalLayoutGroup>() : null;

        if (rootLayout != null)
        {
            if (log.senderType == SenderType.System)
            {
                // 시스템 메시지: 중앙 정렬
                if (chatBoxImage != null && boxSprite != null) chatBoxImage.sprite = boxSprite;
                if (chatBoxImage != null) chatBoxImage.rectTransform.localScale = Vector3.one;
                
                rootLayout.childAlignment = TextAnchor.UpperCenter;
                if (columnLayout != null) columnLayout.childAlignment = TextAnchor.UpperCenter;
                if (senderName != null) senderName.alignment = TextAlignmentOptions.Center;
                if (message != null)
                {
                    message.alignment = TextAlignmentOptions.Center;
                    message.rectTransform.localScale = Vector3.one;
                }
            }
            else if (log.senderType == SenderType.Player)
            {
                // 플레이어: 우측 정렬 및 말풍선 반전
                if (chatBoxImage != null)
                {
                    chatBoxImage.rectTransform.localScale = new Vector3(-1, 1, 1);
                    // 텍스트가 말풍선 자식이라면 같이 반전되므로 다시 뒤집어줌
                    if (message != null)
                    {
                        message.alignment = TextAlignmentOptions.Right;
                        message.rectTransform.localScale = new Vector3(-1, 1, 1);
                    }
                }
                rootLayout.childAlignment = TextAnchor.UpperRight;
                if (columnLayout != null) columnLayout.childAlignment = TextAnchor.UpperRight;
                if (senderName != null) senderName.alignment = TextAlignmentOptions.Right;
                if (senderSprite != null) senderSprite.transform.SetAsLastSibling();
            }
            else
            {
                // 타인: 좌측 정렬 (기본값)
                if (chatBoxImage != null)
                {
                    chatBoxImage.rectTransform.localScale = Vector3.one;
                    if (message != null)
                    {
                        message.alignment = TextAlignmentOptions.Left;
                        message.rectTransform.localScale = Vector3.one;
                    }
                }
                rootLayout.childAlignment = TextAnchor.UpperLeft;
                if (columnLayout != null) columnLayout.childAlignment = TextAnchor.UpperLeft;
                if (senderName != null) senderName.alignment = TextAlignmentOptions.Left;
                if (senderSprite != null) senderSprite.transform.SetAsFirstSibling();
            }

            // 레이아웃 즉시 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}
