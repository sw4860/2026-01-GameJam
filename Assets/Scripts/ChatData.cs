using UnityEngine;

public enum SenderType
{
    Player = 0,
    System = 1,
    Other = 2
}

[System.Serializable]
public class chatLogs
{
    public SenderType senderType;
    public string senderName;
    public Sprite senderSprite;
    [TextArea(3, 10)] public string message;
    public float interval;
    public string actionTag;
}

[CreateAssetMenu(fileName = "ChatData", menuName = "ChatData")]
public class ChatData : ScriptableObject
{
    public chatLogs[] chatLogs;
}
