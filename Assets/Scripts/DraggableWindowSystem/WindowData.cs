using UnityEngine;

[CreateAssetMenu(fileName = "NewWindowData", menuName = "WindowData")]
public class WindowData : ScriptableObject
{
    [Header("Window Info")]
    public string windowTitle;
    
    [Header("Content")]
    public Sprite contentImage;
    [TextArea(5, 10)]
    public string contentDescription;

    [Header("Settings")]
    public Vector2 preferredSize = new Vector2(400, 500);
}
