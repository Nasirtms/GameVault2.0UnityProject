using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;


#region Slot Settings
public enum UltimateFireLinkRiverWalkMiniGameSlotType
{
    Wild, Barrel, OrangeJuice, CowboyHat, A, K, Q, J, Shoes, Belt, Freegames, Mini, Minor, Major, Mega
}

[System.Serializable]
public struct UltimateFireLinkRiverWalkMiniGameSlotResource
{
    public UltimateFireLinkRiverWalkMiniGameSlotType slotType;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum UltimateFireLinkRiverWalkMiniGameSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum UltimateFireLinkRiverWalkMiniGameSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class UltimateFireLinkRiverWalkMiniGameSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public UltimateFireLinkRiverWalkMiniGameSpinMode startSpin;
    [EnumToggleButtons] public UltimateFireLinkRiverWalkMiniGameSpinMode endSpin;
    [EnumToggleButtons] public UltimateFireLinkRiverWalkMiniGameSpinDirection spinDirection;

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
public class UltimateFireLinkRiverWalkMiniGameSlotSettings
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
[CreateAssetMenu(menuName = "Settings/UltimateFireLinkRiverWalkMiniGame", fileName = "UltimateFireLinkRiverWalkMiniGame")]
public class UltimateFireLinkRiverWalkMiniGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<UltimateFireLinkRiverWalkMiniGameSlotResource> slotResources;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public UltimateFireLinkRiverWalkMiniGameSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public UltimateFireLinkRiverWalkMiniGameSlotSettings slotSettings;

    #endregion
}