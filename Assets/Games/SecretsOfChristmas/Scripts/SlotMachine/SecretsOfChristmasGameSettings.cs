using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#region Slot Settings
public enum SecretsOfChristmasSlotType
{
    Ace, King, Queen, Jack, Ten, Bell, Candle, House, Milk, Sock, Wild, Scatter
}

[System.Serializable]
public struct SecretsOfChristmasSlotResource
{
    public SecretsOfChristmasSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum SecretsOfChristmasSpinMode
{
    SpinAll, SpinOneByOne
}

public enum SecretsOfChristmasSpinDirection
{
    Down, Up, Random
}

[System.Serializable]
public class SecretsOfChristmasSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public SecretsOfChristmasSpinMode startSpin;
    [EnumToggleButtons] public SecretsOfChristmasSpinMode endSpin;
    [EnumToggleButtons] public SecretsOfChristmasSpinDirection spinDirection;

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
public class SecretsOfChristmasSlotSettings
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
public class SecretsOfChristmasSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/SecretsOfChristmas", fileName = "SecretsOfChristmas")]
public class SecretsOfChristmasGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<SecretsOfChristmasSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<SecretsOfChristmasSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public SecretsOfChristmasSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public SecretsOfChristmasSlotSettings slotSettings;

    public SecretsOfChristmasSoundItem GetSound(string soundName)
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