using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum StickyPiggySlotType
{
    A, K, Q, J, Ten, Keys, Newspaper, Gentleman, Lady, Robber, Diva, Bonus, PiggyWildX2, PiggyWildX3
}

[System.Serializable]
public struct StickyPiggySlotResource
{
    public StickyPiggySlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum StickyPiggySpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum StickyPiggySpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class StickyPiggySpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public StickyPiggySpinMode startSpin;
    [EnumToggleButtons] public StickyPiggySpinMode endSpin;
    [EnumToggleButtons] public StickyPiggySpinDirection spinDirection;

    public float SpinSpeed;
    public float ReelStopDelay;
    public float ReelStartDelay;
    public float SlowDownDuration;

    public bool ClampToTopPosition;
    //[SerializeField, Range(0f, 1f)] private float spinClampFrequency;
    //public float SpinClampStrength;

    [Range(0f, 1f)] public float WindUpAmount;
    [Range(0f, 2f)] public float WindUpDuration;
    [Range(0f, 2f)] public float ClampDuration;
}
[System.Serializable]
public class StickyPiggySlotSettings
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
public class StickyPiggySoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/StickyPiggy", fileName = "StickyPiggy")]
public class StickyPiggyGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<StickyPiggySlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<StickyPiggySoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public StickyPiggySpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public StickyPiggySlotSettings slotSettings;

    public StickyPiggySoundItem GetSound(string soundName)
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