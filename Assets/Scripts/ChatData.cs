using UnityEngine;

[System.Serializable]
public class chatLogs
{
    public string senderName;
    public Sprite senderSprite;
    public string message;
    public float interval;
    public bool isPlayer;
    public bool isSystem;
}

[CreateAssetMenu(fileName = "ChatData", menuName = "Scriptable Objects/ChatData")]
public class ChatData : ScriptableObject
{
    public chatLogs[] chatLogs;
    //public int eventId;
}
