using UnityEngine;
using UnityEngine.UI;

public class MemoryRestoredPopup : MonoBehaviour
{
    public static MemoryRestoredPopup Instance { get; private set; }

    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Color overlayColor = new Color(0.02f, 0.03f, 0.05f, 0.72f);
    [SerializeField] private Color titleColor = new Color(0.75f, 0.95f, 1f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.95f, 0.95f, 0.90f, 1f);
    [SerializeField] private float visibleSeconds = 2.8f;

    private GameObject overlayObject;
    private Text titleText;
    private Text bodyText;
    private float hideAt;

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
        if (overlayObject != null && overlayObject.activeSelf && Time.time >= hideAt)
        {
            Hide();
        }
    }

    public void Show(string memoryTitle, string description)
    {
        EnsureUi();

        if (titleText != null)
        {
            titleText.text = "MEMORY RESTORED";
        }

        if (bodyText != null)
        {
            bodyText.text = string.IsNullOrWhiteSpace(memoryTitle)
                ? description
                : memoryTitle + "\n" + description;
        }

        if (overlayObject != null)
        {
            overlayObject.SetActive(true);
            overlayObject.transform.SetAsLastSibling();
            hideAt = Time.time + visibleSeconds;
        }
    }

    private void Hide()
    {
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
            GameObject canvasObject = new GameObject("Memory Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        overlayObject = new GameObject("Memory Restored Popup");
        overlayObject.transform.SetParent(targetCanvas.transform, false);

        Image overlay = overlayObject.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = false;

        RectTransform overlayRect = overlay.rectTransform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", overlayObject.transform, 44, FontStyle.Bold, titleColor, new Vector2(0.12f, 0.52f), new Vector2(0.88f, 0.68f));
        titleText.alignment = TextAnchor.MiddleCenter;

        bodyText = CreateText("Description", overlayObject.transform, 26, FontStyle.Italic, bodyColor, new Vector2(0.16f, 0.36f), new Vector2(0.84f, 0.52f));
        bodyText.alignment = TextAnchor.UpperCenter;
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle style, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }
}
