using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;


#region Slot Settings
public enum CashVaultMiniGameSlotType
{
    Sphere, Empty
}

[System.Serializable]
public struct CashVaultMiniGameSlotResource
{
    public CashVaultMiniGameSlotType slotType;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum CashVaultMiniGameSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum CashVaultMiniGameSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class CashVaultMiniGameSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public CashVaultMiniGameSpinMode startSpin;
    [EnumToggleButtons] public CashVaultMiniGameSpinMode endSpin;
    [EnumToggleButtons] public CashVaultMiniGameSpinDirection spinDirection;

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
public class CashVaultMiniGameSlotSettings
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
[CreateAssetMenu(menuName = "Settings/CashVaultMiniGame", fileName = "CashVaultMiniGame")]
public class CashVaultMiniGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<CashVaultMiniGameSlotResource> slotResources;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public CashVaultMiniGameSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public CashVaultMiniGameSlotSettings slotSettings;

    #endregion
}