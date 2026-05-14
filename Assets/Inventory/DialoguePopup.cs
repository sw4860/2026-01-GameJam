using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialoguePopup : MonoBehaviour
{
    public static DialoguePopup Instance { get; private set; }

    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Color panelColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
    [SerializeField] private Color textColor = new Color(0.96f, 0.90f, 0.78f, 1f);
    [SerializeField] private Color nameColor = new Color(1f, 0.82f, 0.42f, 1f);

    private GameObject panelObject;
    private Text nameText;
    private Text bodyText;
    private string[] lines = new string[0];
    private int currentLineIndex;
    private int shownFrame = -1;
    private System.Action onComplete;

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
        if (panelObject == null || !panelObject.activeSelf || Mouse.current == null)
        {
            return;
        }

        if (Time.frameCount != shownFrame && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    public void Show(string message)
    {
        ShowDialogue("", message);
    }

    public void ShowDialogue(string speakerName, params string[] dialogueLines)
    {
        ShowDialogue(speakerName, dialogueLines, null);
    }

    public void ShowDialogue(string speakerName, string[] dialogueLines, System.Action completed)
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            return;
        }

        lines = dialogueLines;
        currentLineIndex = 0;
        onComplete = completed;
        EnsureUi();

        if (nameText != null)
        {
            bool hasName = !string.IsNullOrWhiteSpace(speakerName);
            nameText.gameObject.SetActive(hasName);
            nameText.text = speakerName;
        }

        SetCurrentLineText();

        if (panelObject != null)
        {
            panelObject.SetActive(true);
            panelObject.transform.SetAsLastSibling();
            shownFrame = Time.frameCount;
        }
    }

    private void SetCurrentLineText()
    {
        if (bodyText != null)
        {
            bodyText.text = lines[currentLineIndex];
        }
    }

    private void ShowNextLine()
    {
        currentLineIndex++;
        if (currentLineIndex < lines.Length)
        {
            SetCurrentLineText();
            shownFrame = Time.frameCount;
            return;
        }

        System.Action completed = onComplete;
        Hide();
        completed?.Invoke();
    }

    public void Hide()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }

        lines = new string[0];
        currentLineIndex = 0;
        onComplete = null;
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
            GameObject canvasObject = new GameObject("Dialogue Canvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        panelObject = new GameObject("Dialogue Popup");
        panelObject.transform.SetParent(targetCanvas.transform, false);

        Image panel = panelObject.AddComponent<Image>();
        panel.color = panelColor;
        panel.raycastTarget = false;

        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.22f, 0.035f);
        panelRect.anchorMax = new Vector2(0.78f, 0.155f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject nameObject = new GameObject("Name");
        nameObject.transform.SetParent(panelObject.transform, false);
        nameText = nameObject.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = nameColor;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform nameRect = nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.60f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(18f, 0f);
        nameRect.offsetMax = new Vector2(-18f, -4f);

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(panelObject.transform, false);
        bodyText = textObject.AddComponent<Text>();
        bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bodyText.fontSize = 17;
        bodyText.alignment = TextAnchor.MiddleLeft;
        bodyText.color = textColor;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform textRect = bodyText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = new Vector2(1f, 0.68f);
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, 0f);
    }
}
