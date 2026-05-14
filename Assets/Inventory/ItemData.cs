using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "RE:MEMO/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField] private string displayName;
    [TextArea] [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    public string Id => id;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
}
