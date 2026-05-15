using System.Collections.Generic;
using UnityEngine;

public static class StaticValues
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    public static bool HasFlag(string flagId)
    {
        return !string.IsNullOrWhiteSpace(flagId) && flags.Contains(flagId);
    }

    public static void SetFlag(string flagId, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(flagId)) return;
        
        if (value)
        {
            if (flags.Add(flagId))
            {
                Debug.Log("State unlocked: " + flagId);
            }
        }
        else
        {
            if (flags.Remove(flagId))
            {
                Debug.Log("State locked: " + flagId);
            }
        }
    }

    // Properties for backward compatibility and static access
    public static bool isFindPicture { get => HasFlag("isFindPicture"); set => SetFlag("isFindPicture", value); }
    public static bool isInvestCloth { get => HasFlag("isInvestCloth"); set => SetFlag("isInvestCloth", value); }
    public static bool isGetChest { get => HasFlag("isGetChest"); set => SetFlag("isGetChest", value); }
    public static bool isGetKey { get => HasFlag("isGetKey"); set => SetFlag("isGetKey", value); }
    public static bool isGetDialogue { get => HasFlag("isGetDialogue"); set => SetFlag("isGetDialogue", value); }
    public static bool isOpenDrawer { get => HasFlag("isOpenDrawer"); set => SetFlag("isOpenDrawer", value); }
    public static bool isConfirmDeleteHistory { get => HasFlag("isConfirmDeleteHistory"); set => SetFlag("isConfirmDeleteHistory", value); }
    public static bool isRecoverAllMemory { get => HasFlag("isRecoverAllMemory"); set => SetFlag("isRecoverAllMemory", value); }
    public static bool isEnding { get => HasFlag("isEnding"); set => SetFlag("isEnding", value); }
}
