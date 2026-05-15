using UnityEngine;
using UnityEngine.UI;

public class ItemAcquiredPopup : MonoBehaviour
{
    public static ItemAcquiredPopup Instance { get; private set; }

    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Color panelColor = new Color(0.08f, 0.07f, 0.05f, 0.94f);
    [SerializeField] private Color titleColor = new Color(1f, 0.85f, 0.42f, 1f);
    [SerializeField] private Color bodyColor = new Color(0.96f, 0.90f, 0.78f, 1f);
    [SerializeField] private float visibleSeconds = 2.2f;

    private GameObject panelObject;
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
        if (panelObject != null && panelObject.activeSelf && Time.time >= hideAt)
        {
            Hide();
        }
    }

    public void Show(string itemName, string description)
    {
        EnsureUi();

        if (titleText != null)
        {
            titleText.text = "[" + itemName + "를 획득했다.]";
        }

        if (bodyText != null)
        {
            bodyText.text = string.IsNullOrWhiteSpace(description) ? "어딘가에 사용할 수 있을 것 같다." : description;
        }

        if (panelObject != null)
        {
            panelObject.SetActive(true);
            panelObject.transform.SetAsLastSibling();
            hideAt = Time.time + visibleSeconds;
        }
    }

    private void Hide()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }

    private void EnsureUi()
    {
        if (panelObject != null)
        {
            return;
        }

        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }

        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("Notification Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        panelObject = new GameObject("Item Acquired Popup");
        panelObject.transform.SetParent(targetCanvas.transform, false);

        Image panel = panelObject.AddComponent<Image>();
        panel.color = panelColor;
        panel.raycastTarget = false;

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.26f, 0.66f);
        panelRect.anchorMax = new Vector2(0.74f, 0.90f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        titleText = CreateText("Title", panelObject.transform, 21, FontStyle.Bold, titleColor, new Vector2(0f, 0.54f), Vector2.one);
        titleText.rectTransform.offsetMin = new Vector2(26f, 2f);
        titleText.rectTransform.offsetMax = new Vector2(-26f, -8f);

        bodyText = CreateText("Description", panelObject.transform, 18, FontStyle.Normal, bodyColor, Vector2.zero, new Vector2(1f, 0.58f));
        bodyText.rectTransform.offsetMin = new Vector2(26f, 12f);
        bodyText.rectTransform.offsetMax = new Vector2(-26f, -4f);
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle style, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 13;
        text.resizeTextMaxSize = fontSize;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }
}
