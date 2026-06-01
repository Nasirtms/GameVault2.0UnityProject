using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#region Slot Settings
public enum LifeOfLuxurySlotType
{
    GoldBar, SilverBar, BronzeBar, MoneyClip, Watch, Car, Ring, Jet, Yacht, GoldCoin, Wild
}

[System.Serializable]
public struct LifeOfLuxurySlotResource
{
    public LifeOfLuxurySlotType slotType;
    public string baseGameAnimationBool;
    public string freeGameAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum LifeOfLuxurySpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum LifeOfLuxurySpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class LifeOfLuxurySpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public LifeOfLuxurySpinMode startSpin;
    [EnumToggleButtons] public LifeOfLuxurySpinMode endSpin;
    [EnumToggleButtons] public LifeOfLuxurySpinDirection spinDirection;

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
public class LifeOfLuxurySlotSettings
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
public class LifeOfLuxurySoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/LifeOfLuxury", fileName = "LifeOfLuxury")]
public class LifeOfLuxuryGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<LifeOfLuxurySlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<LifeOfLuxurySoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public LifeOfLuxurySpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public LifeOfLuxurySlotSettings slotSettings;

    public LifeOfLuxurySoundItem GetSound(string soundName)
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            foreach (var sound in soundItems)
            {
                if (sound.soundName == soundName)
                    return sound;
            }
            Debug.LogWarning($"Sound '{soundName}' not found in SoundData.");
            return null;
        }
        return null;
    }
    #endregion
}