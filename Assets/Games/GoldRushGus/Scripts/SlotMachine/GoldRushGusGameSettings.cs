using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum GoldRushGusSlotType
{
   Ace, King, Queen, Jack, Ten, Nine,  Wild, Key, FreeSpin, Necklace, Jar, Serpent, Eagle, Idol
}

[System.Serializable]
public struct GoldRushGusSlotResource
{
    public GoldRushGusSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum GoldRushGusSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum GoldRushGusSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class GoldRushGusSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public GoldRushGusSpinMode startSpin;
    [EnumToggleButtons] public GoldRushGusSpinMode endSpin;
    [EnumToggleButtons] public GoldRushGusSpinDirection spinDirection;

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
public class GoldRushGusSlotSettings
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
public class GoldRushGusSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/GoldRushGus", fileName = "GoldRushGus")]
public class GoldRushGusGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<GoldRushGusSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<GoldRushGusSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public GoldRushGusSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public GoldRushGusSlotSettings slotSettings;

    public GoldRushGusSoundItem GetSound(string soundName)
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