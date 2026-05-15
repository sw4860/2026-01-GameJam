using System.Collections;
using UnityEngine;

public class OpeningSequenceController : MonoBehaviour
{
    public static OpeningSequenceController Instance { get; private set; }

    [Header("Character Reference")]
    [SerializeField] private CharacterUIController girlUI;

    [Header("Dialogue Data")]
    [SerializeField] private ChatData openingChat;

    [Header("Glitch UI Animator")]
    [SerializeField] private Animator glitchAnimator;

    [Header("Windows")]
    [SerializeField] private WindowData memoryWindowData;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(PlayOpeningSequence());
    }

    private IEnumerator PlayOpeningSequence()
    {
        yield return new WaitForSeconds(0.5f);

        if (girlUI != null)
        {
            girlUI.gameObject.SetActive(true);
            yield return null;
        }

        if (girlUI != null)
        {
            girlUI.PlayWakeUp();
            yield return new WaitForSeconds(2.0f); 
        }

        if (openingChat != null && ChatLogManager.Instance != null)
        {
            ChatLogManager.Instance.OnActionTriggered += HandleChatAction;
            ChatLogManager.Instance.OnChatComplete.AddListener(OnChatEnded);
            ChatLogManager.Instance.StartChat(openingChat);
        }
    }

    private void HandleChatAction(string tag)
    {
        switch (tag)
        {
            case "glitch":
                if (glitchAnimator != null) glitchAnimator.SetTrigger("PlayGlitch");
                if (girlUI != null) girlUI.PlayGlitch();
                break;

            case "open_memory":
                WindowManager.Instance?.OpenWindow(memoryWindowData);
                break;
        }
    }

    private void OnChatEnded()
    {
        if (ChatLogManager.Instance != null)
        {
            ChatLogManager.Instance.OnActionTriggered -= HandleChatAction;
            ChatLogManager.Instance.OnChatComplete.RemoveListener(OnChatEnded);
        }
        Debug.Log("Opening Sequence Complete");
    }
}
