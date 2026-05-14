using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindowContentUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image contentImage;
    [SerializeField] private TMP_Text descriptionText;
    
    private DraggableWindow draggableWindow;
    private RectTransform rectTransform;

    private void Awake()
    {
        draggableWindow = GetComponent<DraggableWindow>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(WindowData data)
    {
        if (titleText != null) titleText.text = data.windowTitle;
        if (contentImage != null)
        {
            contentImage.sprite = data.contentImage;
            contentImage.preserveAspect = true;
        }
        if (descriptionText != null) descriptionText.text = data.contentDescription;

        if (draggableWindow != null)
        {
            draggableWindow.SetSize(data.preferredSize);
        }
    }

    public void CloseWindow()
    {
        Destroy(gameObject);
    }
}
