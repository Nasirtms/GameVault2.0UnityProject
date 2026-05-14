using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum CashVaultSlotType
{
    Ace, King, Queen, Jack, Wild, Ten, Ship, Car, Jet, Sphere, Cash, Ring, Scatter, Watch
}

[System.Serializable]
public struct CashVaultSlotResource
{
    public CashVaultSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum CashVaultSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum CashVaultSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class CashVaultSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public CashVaultSpinMode startSpin;
    [EnumToggleButtons] public CashVaultSpinMode endSpin;
    [EnumToggleButtons] public CashVaultSpinDirection spinDirection;

    public float SpinSpeed;
    public float ReelStopDelay;
    public float ReelStartDelay;
    public float SlowDownDuration;

    public bool ClampToTopPosition;
    [SerializeField, Range(0f, 1f)] private float spinClampFrequency;
    public float SpinClampStrength;

    [Range(0f, 1f)] public float WindUpAmount;
    [Range(0.1f, 2f)] public float WindUpDuration;
    [Range(0.1f, 2f)] public float ClampDuration;
}
[System.Serializable]
public class CashVaultSlotSettings
{
    public float MoveSpeed;
    public float TopYPosition;
    public float BottomYPosition;

    public float MinSpinDuration;
    public float MaxSpinDuration;

    public float SymbolScaleX;
    public float SymbolScaleY;
}
#endregion

#region Sound Settings

[System.Serializable]
public class CashVaultSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/CashVault", fileName = "Cash Vault")]
public class CashVaultGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<CashVaultSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<CashVaultSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public CashVaultSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public CashVaultSlotSettings slotSettings;

    public CashVaultSoundItem GetSound(string soundName)
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            foreach (var sound in soundItems)
            {
                if (sound.soundName == soundName)
                    return sound;
            }
            return null;
        }
        return null;
    }
    #endregion
}