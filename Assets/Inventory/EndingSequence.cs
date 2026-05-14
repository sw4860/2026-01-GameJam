using UnityEngine;
using UnityEngine.UI;

public class EndingSequence : MonoBehaviour
{
    public static EndingSequence Instance { get; private set; }

    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private string finalMessage = "나는 나를 지우지 않기로 했다.";
    [SerializeField] private float fadeSeconds = 2f;

    private GameObject overlayObject;
    private Image overlayImage;
    private Text messageText;
    private bool playing;
    private float startedAt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureUi();
        Hide();
    }

    private void Update()
    {
        if (!playing || overlayImage == null)
        {
            return;
        }

        float t = Mathf.Clamp01((Time.time - startedAt) / fadeSeconds);
        overlayImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, t);
        if (messageText != null)
        {
            messageText.color = new Color(0.95f, 0.92f, 0.84f, t);
        }
    }

    public void PlayEnding(string message = "")
    {
        EnsureUi();
        if (messageText != null && !string.IsNullOrWhiteSpace(message))
        {
            messageText.text = message;
        }
        else if (messageText != null)
        {
            messageText.text = finalMessage;
        }

        overlayObject.SetActive(true);
        overlayObject.transform.SetAsLastSibling();
        startedAt = Time.time;
        playing = true;
    }

    private void Hide()
    {
        playing = false;
        if (overlayObject != null)
        {
            overlayObject.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (overlayObject != null)
        {
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("Ending Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        overlayObject = new GameObject("Ending Overlay");
        overlayObject.transform.SetParent(targetCanvas.transform, false);

        overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        overlayImage.raycastTarget = false;

        RectTransform overlayRect = overlayImage.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Final Message");
        textObject.transform.SetParent(overlayObject.transform, false);
        messageText = textObject.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 30;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = new Color(0.95f, 0.92f, 0.84f, 0f);
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform textRect = messageText.rectTransform;
        textRect.anchorMin = new Vector2(0.12f, 0.40f);
        textRect.anchorMax = new Vector2(0.88f, 0.60f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}
