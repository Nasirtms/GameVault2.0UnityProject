using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum UltimateFireLinkRoute66SlotType
{
    Wild, Can, Dice, Map, A, K, Q, J, Ten, Nine, Freegames, Mini, Minor, Major, Mega
}

[System.Serializable]
public struct UltimateFireLinkRoute66SlotResource
{
    public UltimateFireLinkRoute66SlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum UltimateFireLinkRoute66SpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}
public enum UltimateFireLinkRoute66SpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class UltimateFireLinkRoute66SpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public UltimateFireLinkRoute66SpinMode startSpin;
    [EnumToggleButtons] public UltimateFireLinkRoute66SpinMode endSpin;
    [EnumToggleButtons] public UltimateFireLinkRoute66SpinDirection spinDirection;

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
public class UltimateFireLinkRoute66SlotSettings
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
public class UltimateFireLinkRoute66SoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/UltimateFireLink Route66", fileName = "UltimateFireLinkRoute66")]
public class UltimateFireLinkRoute66GameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<UltimateFireLinkRoute66SlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<UltimateFireLinkRoute66SoundItem> soundItems;
    [TabGroup("Spin Settings")]
    [HideLabel]
    public UltimateFireLinkRoute66SpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public UltimateFireLinkRoute66SlotSettings slotSettings;
    public UltimateFireLinkRoute66SoundItem GetSound(string soundName)
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