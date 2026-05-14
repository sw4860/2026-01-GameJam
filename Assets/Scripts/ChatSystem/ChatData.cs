using UnityEngine;

public enum SenderType
{
    Player,
    System,
    Other
}

[System.Serializable]
public class chatLogs
{
    public string senderName;
    public Sprite senderSprite;
    public SenderType senderType;
    public string message;
    public float interval;
}

[CreateAssetMenu(fileName = "ChatData", menuName = "ChatData")]
public class ChatData : ScriptableObject
{
    public chatLogs[] chatLogs;
    //public int eventId;
}
