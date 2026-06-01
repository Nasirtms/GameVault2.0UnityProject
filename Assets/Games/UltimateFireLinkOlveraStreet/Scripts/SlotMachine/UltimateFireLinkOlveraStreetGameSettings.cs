using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum UltimateFireLinkOlveraStreetSlotType
{
    Wild, Skull, Guitar, TacoPlatter, A, K, Q, J, Ten, Nine, Freegames, Mini, Minor, Major, Mega
}

[System.Serializable]
public struct UltimateFireLinkOlveraStreetSlotResource
{
    public UltimateFireLinkOlveraStreetSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum UltimateFireLinkOlveraStreetSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum UltimateFireLinkOlveraStreetSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class UltimateFireLinkOlveraStreetSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public UltimateFireLinkOlveraStreetSpinMode startSpin;
    [EnumToggleButtons] public UltimateFireLinkOlveraStreetSpinMode endSpin;
    [EnumToggleButtons] public UltimateFireLinkOlveraStreetSpinDirection spinDirection;

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
public class UltimateFireLinkOlveraStreetSlotSettings
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
public class UltimateFireLinkOlveraStreetSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/UltimateFireLink OlveraStreet", fileName = "UltimateFireLinkOlveraStreet")]
public class UltimateFireLinkOlveraStreetGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<UltimateFireLinkOlveraStreetSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<UltimateFireLinkOlveraStreetSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public UltimateFireLinkOlveraStreetSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public UltimateFireLinkOlveraStreetSlotSettings slotSettings;

    public UltimateFireLinkOlveraStreetSoundItem GetSound(string soundName)
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