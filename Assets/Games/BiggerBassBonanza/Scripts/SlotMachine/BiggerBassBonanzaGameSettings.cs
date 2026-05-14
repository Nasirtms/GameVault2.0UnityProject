using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public enum BiggerBassBonanzaSlotType
{
    Wild, Scatter, Boat, FishingRod, Bait, FishingBox, GoldenFish, BigFish, MediumFish, SmallFish, VerySmallFish, Ace, King, Queen, Jack, Ten
}

public enum BiggerBassBonanzaSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}

public enum BiggerBassBonanzaSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class BiggerBassBonanzaSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public BiggerBassBonanzaSpinMode startSpin;
    [EnumToggleButtons] public BiggerBassBonanzaSpinMode endSpin;
    [EnumToggleButtons] public BiggerBassBonanzaSpinDirection spinDirection;

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
public class BiggerBassBonanzaSlotSettings
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
public struct BiggerBassBonanzaSlotResource
{
    public BiggerBassBonanzaSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

#region Sound Settings

[System.Serializable]
public class BiggerBassBonanzaSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

[CreateAssetMenu(menuName = "Settings/Bigger Bass Bonanza", fileName = "BiggerBassBonanza")]
public class BiggerBassBonanzaGameSettings : ScriptableObject
{
    [TabGroup("Slot Resources")]
    [TableList]
    public List<BiggerBassBonanzaSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<BiggerBassBonanzaSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public BiggerBassBonanzaSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public BiggerBassBonanzaSlotSettings slotSettings;
    public BiggerBassBonanzaSoundItem GetSound(string soundName)
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            foreach (var sound in soundItems)
            {
                if (sound.soundName == soundName)
                    return sound;
            }

            //Debug.LogWarning($"Sound '{soundName}' not found in SoundData.");
            return null;
        }
        return null;
    }
}
