using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings

public enum WolfMoonLinkSlotType
{
    Empty, Seven, BarSingle, BarDouble, BarTriple, Wolf, Owl, Chipmunk, WildX1, WildX2, Jackpot
}

[System.Serializable]
public struct WolfMoonLinkSlotResource
{
    public WolfMoonLinkSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

#endregion

#region Spin Settings

public enum WolfMoonLinkSpinMode
{
    SpinAll, SpinOneByOne
}

public enum WolfMoonLinkSpinDirection
{
    Down, Up, Random
}

[System.Serializable]
public class WolfMoonLinkSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public WolfMoonLinkSpinMode startSpin;
    [EnumToggleButtons] public WolfMoonLinkSpinMode endSpin;
    [EnumToggleButtons] public WolfMoonLinkSpinDirection spinDirection;

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
public class WolfMoonLinkSlotSettings
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
public class WolfMoonLinkSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings

[CreateAssetMenu(menuName = "Settings/WolfMoonLink", fileName = "WolfMoonLink")]
public class WolfMoonLinkGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<WolfMoonLinkSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<WolfMoonLinkSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public WolfMoonLinkSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public WolfMoonLinkSlotSettings slotSettings;

    public WolfMoonLinkSoundItem GetSound(string soundName)
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
}

#endregion