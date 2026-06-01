using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#region Slot Settings
public enum BuffaloXtraReelPowerSlotType
{
    Ace, Queen, King, Jack, Nine, Ten, Bison, Eagle, Lion, Wolf, Deer, WildX2, WildX3, Scatter
}

[System.Serializable]
public struct BuffaloXtraReelPowerSlotResource
{
    public BuffaloXtraReelPowerSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum BuffaloXtraReelPowerSpinMode
{
    SpinAll, SpinOneByOne
}

public enum BuffaloXtraReelPowerSpinDirection
{
    Down, Up, Random
}

[System.Serializable]
public class BuffaloXtraReelPowerSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public BuffaloXtraReelPowerSpinMode startSpin;
    [EnumToggleButtons] public BuffaloXtraReelPowerSpinMode endSpin;
    [EnumToggleButtons] public BuffaloXtraReelPowerSpinDirection spinDirection;

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
public class BuffaloXtraReelPowerSlotSettings
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
public class BuffaloXtraReelPowerSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/BuffaloXtraReelPower", fileName = "BuffaloXtraReelPower")]
public class BuffaloXtraReelPowerGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<BuffaloXtraReelPowerSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<BuffaloXtraReelPowerSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public BuffaloXtraReelPowerSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public BuffaloXtraReelPowerSlotSettings slotSettings;

    public BuffaloXtraReelPowerSoundItem GetSound(string soundName)
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