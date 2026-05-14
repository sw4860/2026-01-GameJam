using UnityEngine;

public class CursorManage : MonoBehaviour
{
    [SerializeField] private Texture2D hover;
    [SerializeField] private Texture2D original;

    public void SetHoverCursor()
    {
        if (hover != null)
        {
            Cursor.SetCursor(hover, Vector2.zero, CursorMode.Auto);
        }
    }
    
    public void SetDefaultCursor()
    {
        Cursor.SetCursor(original, Vector2.zero, CursorMode.Auto);
    }

    private void OnMouseEnter()
    {
        SetHoverCursor();
    }

    private void OnMouseExit()
    {
        SetDefaultCursor();
    }
}
