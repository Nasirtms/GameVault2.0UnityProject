using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum WildBuffaloSlotType
{
    Ace, Queen, King, Jack, Nine, Ten, Bison, Bear, Lion, Wolf, Eagle, Wild, Bonus, FreeGameSlot
}

[System.Serializable]
public struct WildBuffaloSlotResource
{
    public WildBuffaloSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum WildBuffaloSpinMode
{
    SpinAll, SpinOneByOne
}

public enum WildBuffaloSpinDirection
{
    Down, Up, Random
}

[System.Serializable]
public class WildBuffaloSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public WildBuffaloSpinMode startSpin;
    [EnumToggleButtons] public WildBuffaloSpinMode endSpin;
    [EnumToggleButtons] public WildBuffaloSpinDirection spinDirection;

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
public class WildBuffaloSlotSettings
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
public class WildBuffaloSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/WildBuffalo", fileName = "WildBuffalo")]
public class WildBuffaloGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<WildBuffaloSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<WildBuffaloSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public WildBuffaloSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public WildBuffaloSlotSettings slotSettings;

    public WildBuffaloSoundItem GetSound(string soundName)
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
}
#endregion