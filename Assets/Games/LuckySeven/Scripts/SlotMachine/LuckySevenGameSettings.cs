using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum LuckySevenSlotType
{
    Empty, OrangeSeven, BlueSeven, SilverSeven, SingleBlueBar, DoubleSilverBar, TripleOrangeBar, wildSymbol
}


[System.Serializable]
public struct LuckySevenSlotResource
{
    public LuckySevenSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum LuckySevenSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum LuckySevenSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class LuckySevenSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public LuckySevenSpinMode startSpin;
    [EnumToggleButtons] public LuckySevenSpinMode endSpin;
    [EnumToggleButtons] public LuckySevenSpinDirection spinDirection;

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
public class LuckySevenSlotSettings
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
public class LuckySevenSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/LuckySeven", fileName = "LuckySeven")]
public class LuckySevenGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<LuckySevenSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<LuckySevenSoundItem> soundItems;
    [TabGroup("Spin Settings")]
    [HideLabel]
    public LuckySevenSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public LuckySevenSlotSettings slotSettings;
    public LuckySevenSoundItem GetSound(string soundName)
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