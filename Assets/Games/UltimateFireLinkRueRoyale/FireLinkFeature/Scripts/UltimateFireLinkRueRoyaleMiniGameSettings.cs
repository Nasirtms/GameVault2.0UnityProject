using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;


#region Slot Settings
public enum UltimateFireLinkRueRoyaleMiniGameSlotType
{
    Wild, Corn, OrangeJuice, Trumpet, A, K, Q, J, Ten, Nine, Freegames, Mini, Minor, Major, Mega
}

[System.Serializable]
public struct UltimateFireLinkRueRoyaleMiniGameSlotResource
{
    public UltimateFireLinkRueRoyaleMiniGameSlotType slotType;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum UltimateFireLinkRueRoyaleMiniGameSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum UltimateFireLinkRueRoyaleMiniGameSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class UltimateFireLinkRueRoyaleMiniGameSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public UltimateFireLinkRueRoyaleMiniGameSpinMode startSpin;
    [EnumToggleButtons] public UltimateFireLinkRueRoyaleMiniGameSpinMode endSpin;
    [EnumToggleButtons] public UltimateFireLinkRueRoyaleMiniGameSpinDirection spinDirection;

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
public class UltimateFireLinkRueRoyaleMiniGameSlotSettings
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
[CreateAssetMenu(menuName = "Settings/UltimateFireLinkRueRoyaleMiniGame", fileName = "UltimateFireLinkRueRoyaleMiniGame")]
public class UltimateFireLinkRueRoyaleMiniGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<UltimateFireLinkRueRoyaleMiniGameSlotResource> slotResources;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public UltimateFireLinkRueRoyaleMiniGameSpinSettings spinSettings;
    [TabGroup("Slot Settings")]
    [HideLabel]
    public UltimateFireLinkRueRoyaleMiniGameSlotSettings slotSettings;

    #endregion
}