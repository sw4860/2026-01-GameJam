using UnityEngine;

public class CursorManage : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Vector2 hotSpot = Vector2.zero;

    private static Texture2D defaultCursor;
    private static CursorManage currentActive;

    public void SetHoverCursor()
    {
        if (hoverCursor != null)
        {
            currentActive = this;
            Cursor.SetCursor(hoverCursor, hotSpot, CursorMode.Auto);
        }
    }
    
    public static void ResetToDefault()
    {
        currentActive = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    // 일반 3D/2D 오브젝트용 (RenderTexture 밖의 월드 객체)
    private void OnMouseEnter()
    {
        // RenderTexturePointer가 처리하지 않는 일반 월드 객체일 경우에만 작동
        if (RenderTexturePointer.Instance == null) SetHoverCursor();
    }

    private void OnMouseExit()
    {
        if (RenderTexturePointer.Instance == null && currentActive == this) ResetToDefault();
    }
}
