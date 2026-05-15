using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogObject : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image chatBoxImage;
    [SerializeField] private Sprite systemBoxSprite;
    [SerializeField] private Image senderSprite;
    [SerializeField] private TextMeshProUGUI senderName;
    [SerializeField] private TextMeshProUGUI message;

    public void Init(chatLogs log)
    {
        if (log == null) return;

        // 1. 기본 데이터 세팅
        if (senderName != null) senderName.text = log.senderName ?? "";
        if (message != null) message.text = log.message ?? "";
        if (senderSprite != null) 
        {
            senderSprite.sprite = log.senderSprite;
            senderSprite.gameObject.SetActive(log.senderSprite != null && log.senderType != SenderType.System);
        }

        // 2. 레이아웃 엔진 참조
        var rootLayout = GetComponent<HorizontalLayoutGroup>();
        var contentColumnTransform = transform.Find("ContentColumn");
        var contentColumn = contentColumnTransform != null ? contentColumnTransform.GetComponent<RectTransform>() : null;
        var columnLayout = contentColumn != null ? contentColumn.GetComponent<VerticalLayoutGroup>() : null;

        if (rootLayout != null)
        {
            // [중요] 레이아웃 강제 설정 리셋
            rootLayout.childForceExpandWidth = false;
            rootLayout.childControlWidth = true;
            rootLayout.reverseArrangement = false;

            if (columnLayout != null)
            {
                columnLayout.childForceExpandWidth = false;
                columnLayout.childControlWidth = true;
            }

            // 타입별 비주얼 적용
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

            // 3. 레이아웃 즉시 갱신 (하위에서 상위로)
            if (contentColumn != null) LayoutRebuilder.ForceRebuildLayoutImmediate(contentColumn);
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }

    private void ApplySystemLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        if (chatBoxImage != null && systemBoxSprite != null) chatBoxImage.sprite = systemBoxSprite;
        
        root.childAlignment = TextAnchor.UpperCenter;
        if (column != null) column.childAlignment = TextAnchor.UpperCenter;
        
        if (senderName != null) senderName.alignment = TextAlignmentOptions.Center;
        if (message != null)
        {
            message.alignment = TextAlignmentOptions.Center;
            message.rectTransform.localScale = Vector3.one;
        }
        if (chatBoxImage != null) chatBoxImage.rectTransform.localScale = Vector3.one;
    }

    private void ApplyPlayerLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        // 오른쪽 정렬 설정
        root.reverseArrangement = true; 
        root.childAlignment = TextAnchor.UpperRight;
        
        if (column != null) column.childAlignment = TextAnchor.UpperRight;
        if (senderName != null) senderName.alignment = TextAlignmentOptions.Right;

        if (chatBoxImage != null)
        {
            // 말풍선 반전
            chatBoxImage.rectTransform.localScale = new Vector3(-1, 1, 1);
            
            if (message != null)
            {
                // 글자가 뒤집히는 것 방지
                if (message.transform.IsChildOf(chatBoxImage.transform))
                    message.rectTransform.localScale = new Vector3(-1, 1, 1);
                else
                    message.rectTransform.localScale = Vector3.one;

                // [가독성 개선] 말풍선 안의 글자는 좌측 정렬이 더 깔끔합니다. 
                // 필요하다면 Right로 변경 가능합니다.
                message.alignment = TextAlignmentOptions.Left; 
            }
        }
    }

    private void ApplyOtherLayout(HorizontalLayoutGroup root, VerticalLayoutGroup column)
    {
        // 왼쪽 정렬 설정 (기본값)
        root.reverseArrangement = false;
        root.childAlignment = TextAnchor.UpperLeft;
        
        if (column != null) column.childAlignment = TextAnchor.UpperLeft;
        if (senderName != null) senderName.alignment = TextAlignmentOptions.Left;

        if (chatBoxImage != null) chatBoxImage.rectTransform.localScale = Vector3.one;
        if (message != null)
        {
            message.rectTransform.localScale = Vector3.one;
            message.alignment = TextAlignmentOptions.Left;
        }
    }
}
