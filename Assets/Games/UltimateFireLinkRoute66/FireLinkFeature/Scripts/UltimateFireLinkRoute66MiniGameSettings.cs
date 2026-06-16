using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;


#region Slot Settings
public enum UltimateFireLinkRoute66MiniGameSlotType
{
    Wild, Corn, OrangeJuice, Trumpet, A, K, Q, J, Ten, Nine, Freegames, Mini, Minor, Major, Mega
}

[System.Serializable]
public struct UltimateFireLinkRoute66MiniGameSlotResource
{
    public UltimateFireLinkRoute66MiniGameSlotType slotType;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum UltimateFireLinkRoute66MiniGameSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum UltimateFireLinkRoute66MiniGameSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class UltimateFireLinkRoute66MiniGameSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public UltimateFireLinkRoute66MiniGameSpinMode startSpin;
    [EnumToggleButtons] public UltimateFireLinkRoute66MiniGameSpinMode endSpin;
    [EnumToggleButtons] public UltimateFireLinkRoute66MiniGameSpinDirection spinDirection;

    //public float SpinSpeed;
    public float MoveSpeed;
    public float ReelStopDelay;
    public float ReelStartDelay;
    public float SlowDownDuration;
    public bool ClampToTopPosition;
    public float ManualDelta = 0.016f; //seconds per time (Best range: 0.010 – 0.016)

    [Range(0f, 1f)] public float WindUpAmount;
    [Range(0.1f, 2f)] public float WindUpDuration;
    [Range(0.1f, 2f)] public float ClampDuration;
}
[System.Serializable]
public class UltimateFireLinkRoute66MiniGameSlotSettings
{
    public float TopYPosition;
    public float BottomYPosition;
    public float SlotShiftStepDelay = 0.03f;
    public float MinSpinDuration;
    public float MaxSpinDuration;

    public float SymbolScaleX;
    public float SymbolScaleY;
}
#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/UltimateFireLinkRoute66MiniGame", fileName = "UltimateFireLinkRoute66MiniGame")]
public class UltimateFireLinkRoute66MiniGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<UltimateFireLinkRoute66MiniGameSlotResource> slotResources;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public UltimateFireLinkRoute66MiniGameSpinSettings spinSettings;
    [TabGroup("Slot Settings")]
    [HideLabel]
    public UltimateFireLinkRoute66MiniGameSlotSettings slotSettings;

    #endregion
}