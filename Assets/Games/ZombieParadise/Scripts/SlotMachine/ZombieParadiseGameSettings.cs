using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public enum ZombieParadiseSlotType
{
    Scatter, Wild, Zombie, Skull, Spider, Brain, Candle, Ace, King, Queen, Jack, Ten, Nine
}

public enum ZombieParadiseSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}

public enum ZombieParadiseSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class ZombieParadiseSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public ZombieParadiseSpinMode startSpin;
    [EnumToggleButtons] public ZombieParadiseSpinMode endSpin;
    [EnumToggleButtons] public ZombieParadiseSpinDirection spinDirection;

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
public class ZombieParadiseSlotSettings
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
public struct ZombieParadiseSlotResource
{
    public ZombieParadiseSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

#region Sound Settings

[System.Serializable]
public class ZombieParadiseSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

[CreateAssetMenu(menuName = "Settings/Zombie Paradise", fileName = "ZombieParadise")]
public class ZombieParadiseGameSettings : ScriptableObject
{
    [TabGroup("Slot Resources")]
    [TableList]
    public List<ZombieParadiseSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<ZombieParadiseSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public ZombieParadiseSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public ZombieParadiseSlotSettings slotSettings;

    //[Button("Spin Slot Machine", ButtonStyle.Box)]
    //public void SpinSlotMachine()
    //{
    //    if (ZombieParadiseSlotMachine.Instance != null)
    //    {
    //        ZombieParadiseSlotMachine.Instance.SpinFromContextMenu();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("No SlotMachine instance found in scene! Make sure a SlotMachine is active.");
    //    }
    //}

    //[Button("Stp Slot Machine", ButtonStyle.Box)]
    //public void StopSlotMachine()
    //{
    //    if (ZombieParadiseSlotMachine.Instance != null)
    //    {
    //        ZombieParadiseSlotMachine.Instance.StopFromContextMenu();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("No SlotMachine instance found in scene! Make sure a SlotMachine is active.");
    //    }
    //}

    public ZombieParadiseSoundItem GetSound(string soundName)
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
