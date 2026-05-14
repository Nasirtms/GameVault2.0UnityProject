using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;


#region Slot Settings
public enum UltimateFireLinkChinaStreetMiniGameSlotType
{
    Wild, DimSum, GreenTeapot, FortuneCookie, Ace, King, Queen, Jack, Ten, Nine, FreeGames, FireLinkMini, FireLinkMinor, FireLinkMajor, FireLinkMega, FireLink50x, FireLink100x
}

[System.Serializable]
public struct UltimateFireLinkChinaStreetMiniGameSlotResource
{
    public UltimateFireLinkChinaStreetMiniGameSlotType slotType;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum UltimateFireLinkChinaStreetMiniGameSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum UltimateFireLinkChinaStreetMiniGameSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class UltimateFireLinkChinaStreetMiniGameSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public UltimateFireLinkChinaStreetMiniGameSpinMode startSpin;
    [EnumToggleButtons] public UltimateFireLinkChinaStreetMiniGameSpinMode endSpin;
    [EnumToggleButtons] public UltimateFireLinkChinaStreetMiniGameSpinDirection spinDirection;

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
public class UltimateFireLinkChinaStreetMiniGameSlotSettings
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
[CreateAssetMenu(menuName = "Settings/UltimateFireLinkChinaStreetMiniGame", fileName = "UltimateFireLinkChinaStreetMiniGame")]
public class UltimateFireLinkChinaStreetMiniGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<UltimateFireLinkChinaStreetMiniGameSlotResource> slotResources;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public UltimateFireLinkChinaStreetMiniGameSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public UltimateFireLinkChinaStreetMiniGameSlotSettings slotSettings;

    #endregion
}