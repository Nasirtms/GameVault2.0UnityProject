using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public enum DayOfDeadSlotType
{
    BlueDiamond, Dog, Drum, Maracas, FemaleCharacter, GreenClub, Guitar, RedHeart, MaleCharacter, PurpleSpade, Wild, ExpandingWild, ElderlyFemale, FreeGameWild, Scatter
}

public enum DayOfDeadSpinMode
{
    SpinAll, SpinOneByOne
}

public enum DayOfDeadSpinDirection
{
    Down, Up, Random  
}

[System.Serializable]
public class DayOfDeadSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public DayOfDeadSpinMode startSpin;
    [EnumToggleButtons] public DayOfDeadSpinMode endSpin;
    [EnumToggleButtons] public DayOfDeadSpinDirection spinDirection;

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
public class DayOfDeadSlotSettings
{
    public float MoveSpeed;
    public float TopYPosition;
    public float BottomYPosition;

    public float MinSpinDuration;
    public float MaxSpinDuration;

    public float SymbolScaleX;
    public float SymbolScaleY;
}

[System.Serializable]
public struct DayOfDeadSlotResource
{
    public DayOfDeadSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

#region Sound Settings

[System.Serializable]
public class DayOfDeadSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

[CreateAssetMenu(menuName = "Settings/DayOfDead", fileName = "Day Of Dead")]
public class DayOfDeadGameSettings : ScriptableObject
{
    [TabGroup("Slot Resources")]
    [TableList]
    public List<DayOfDeadSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<DayOfDeadSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public DayOfDeadSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public DayOfDeadSlotSettings slotSettings;

    public DayOfDeadSoundItem GetSound(string soundName)
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
