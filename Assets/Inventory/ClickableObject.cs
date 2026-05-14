using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    [SerializeField] private string objectId;
    [SerializeField] private string displayName;
    [SerializeField] private string speakerName;
    [SerializeField] private string examineText;
    [TextArea] [SerializeField] private string[] examineLines;
    [SerializeField] private string afterStateText;
    [SerializeField] private string requiredItemId;
    [SerializeField] private string wrongItemText;
    [SerializeField] private string useSuccessText;
    [TextArea] [SerializeField] private string[] useSuccessLines;
    [SerializeField] private string stateFlagOnExamine;
    [SerializeField] private string stateFlagOnUse;
    [SerializeField] private bool consumeRequiredItem;
    [SerializeField] private Color hoverColor = new Color(1f, 0.92f, 0.65f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color defaultColor;
    private bool hovering;

    public string ObjectId => objectId;
    public string RequiredItemId => requiredItemId;
    public bool ConsumeRequiredItem => consumeRequiredItem;

    private void Awake()
    {
        CacheRenderer();
    }

    public void Configure(
        string newObjectId,
        string newDisplayName,
        string newExamineText,
        string newRequiredItemId = "",
        string newWrongItemText = "",
        string newUseSuccessText = "",
        string newStateFlagOnExamine = "",
        string newStateFlagOnUse = "",
        bool newConsumeRequiredItem = false,
        string newAfterStateText = "")
    {
        objectId = newObjectId;
        displayName = newDisplayName;
        examineText = newExamineText;
        requiredItemId = newRequiredItemId;
        wrongItemText = newWrongItemText;
        useSuccessText = newUseSuccessText;
        stateFlagOnExamine = newStateFlagOnExamine;
        stateFlagOnUse = newStateFlagOnUse;
        consumeRequiredItem = newConsumeRequiredItem;
        afterStateText = newAfterStateText;
        CacheRenderer();
    }

    public void SetHover(bool active)
    {
        if (hovering == active)
        {
            return;
        }

        hovering = active;
        CacheRenderer();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? hoverColor : defaultColor;
        }
    }

    public bool Examine()
    {
        string message = GetExamineMessage();
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(stateFlagOnExamine))
        {
            GameState.Instance?.SetFlag(stateFlagOnExamine);
        }

        bool showingAfterState = !string.IsNullOrWhiteSpace(stateFlagOnUse) &&
                                 GameState.Instance != null &&
                                 GameState.Instance.HasFlag(stateFlagOnUse) &&
                                 !string.IsNullOrWhiteSpace(afterStateText);

        if (!showingAfterState && examineLines != null && examineLines.Length > 0)
        {
            DialoguePopup.Instance?.ShowDialogue(GetSpeakerName(), examineLines);
        }
        else
        {
            DialoguePopup.Instance?.ShowDialogue(GetSpeakerName(), message);
        }

        return true;
    }

    public bool CanUseItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(requiredItemId) && itemId == requiredItemId;
    }

    public bool TryUseItem(string itemId)
    {
        if (!CanUseItem(itemId))
        {
            if (!string.IsNullOrWhiteSpace(wrongItemText))
            {
                DialoguePopup.Instance?.Show(wrongItemText);
                return true;
            }

            return false;
        }

        if (!string.IsNullOrWhiteSpace(stateFlagOnUse))
        {
            GameState.Instance?.SetFlag(stateFlagOnUse);
        }

        if (useSuccessLines != null && useSuccessLines.Length > 0)
        {
            DialoguePopup.Instance?.ShowDialogue(GetSpeakerName(), useSuccessLines);
        }
        else if (!string.IsNullOrWhiteSpace(useSuccessText))
        {
            DialoguePopup.Instance?.ShowDialogue(GetSpeakerName(), useSuccessText);
        }

        return true;
    }

    private string GetSpeakerName()
    {
        if (!string.IsNullOrWhiteSpace(speakerName))
        {
            return speakerName;
        }

        return displayName;
    }

    private string GetExamineMessage()
    {
        if (!string.IsNullOrWhiteSpace(stateFlagOnUse) &&
            GameState.Instance != null &&
            GameState.Instance.HasFlag(stateFlagOnUse) &&
            !string.IsNullOrWhiteSpace(afterStateText))
        {
            return afterStateText;
        }

        if (!string.IsNullOrWhiteSpace(examineText))
        {
            return examineText;
        }

        return string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    }

    private void CacheRenderer()
    {
        if (spriteRenderer != null)
        {
            return;
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }
}
