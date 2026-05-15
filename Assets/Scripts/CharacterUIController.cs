using UnityEngine;
using UnityEngine.UI;

public class CharacterUIController : MonoBehaviour
{
    public static CharacterUIController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image characterImage;
    [SerializeField] private Animator characterAnimator;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    public void SetTrigger(string triggerName)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(triggerName);
        }
    }
    
    public void SetSprite(Sprite newSprite)
    {
        if (characterImage != null)
        {
            characterImage.sprite = newSprite;
        }
    }

    public void PlayWakeUp() => SetTrigger("WakeUp");
    public void PlayGlitch() => SetTrigger("Glitch");
    public void PlayIdle() => SetTrigger("Idle");
}
