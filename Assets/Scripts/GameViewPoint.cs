using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(RawImage))]
public class GameViewport : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    
    private RawImage displayImage;
    private RenderTexture renderTexture;
    private RectTransform rectTransform;
    private Vector2 lastSize;

    void Awake()
    {
       displayImage = GetComponent<RawImage>();
       rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
       UpdateViewportSize();
    }

    void Update()
    {
       if (rectTransform.rect.size != lastSize)
       {
       UpdateViewportSize();
       }
    }

    private void UpdateViewportSize()
    {
       if (worldCamera == null) return;
       int width = Mathf.RoundToInt(rectTransform.rect.width);
       int height = Mathf.RoundToInt(rectTransform.rect.height);
       if (width <= 0 || height <= 0) return;
       if (renderTexture != null)
       {
           worldCamera.targetTexture = null;
           displayImage.texture = null;
           renderTexture.Release();
           Destroy(renderTexture);
       }
       renderTexture = new RenderTexture(width, height, 32)
       {
           filterMode = FilterMode.Bilinear
       };
       worldCamera.targetTexture = renderTexture;
       displayImage.texture = renderTexture;
       lastSize = rectTransform.rect.size;
       
       Debug.Log($"Viewport Resized: {width}x{height}");
    }
    
    private void OnDestroy()
    {
       if (renderTexture != null)
       {
           renderTexture.Release();
           Destroy(renderTexture);
       }
    }
}