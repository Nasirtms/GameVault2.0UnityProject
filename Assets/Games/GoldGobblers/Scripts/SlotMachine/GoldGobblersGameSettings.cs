using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public enum GoldGobblersSlotType
{
    Wild, WildX2, WildX3, WildX5, Chest, Scale, Map, Helmet, Pickaxes, Ace, King, Queen, Jack, Ten, MineEntrance, RedGem, GreenGem, BlueGem
}

public enum GoldGobblersSpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}

public enum GoldGobblersSpinDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]
public class GoldGobblersSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public GoldGobblersSpinMode startSpin;
    [EnumToggleButtons] public GoldGobblersSpinMode endSpin;
    [EnumToggleButtons] public GoldGobblersSpinDirection spinDirection;

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
public class GoldGobblersSlotSettings
{
    public float MoveSpeed;
    public float TopYPosition;
    public float TopYPositionRedFreeSpin;
    public float BottomYPosition;

    public float MinSpinDuration;
    public float MaxSpinDuration;

    public float SymbolScaleX;
    public float SymbolScaleY;
}

[System.Serializable]
public struct GoldGobblersSlotResource
{
    public GoldGobblersSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

#region Sound Settings

[System.Serializable]
public class GoldGobblersSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

[CreateAssetMenu(menuName = "Settings/Gold Gobblers", fileName = "GoldGobblers")]
public class GoldGobblersGameSettings : ScriptableObject
{
    [TabGroup("Slot Resources")]
    [TableList]
    public List<GoldGobblersSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<GoldGobblersSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public GoldGobblersSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public GoldGobblersSlotSettings slotSettings;

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

    public GoldGobblersSoundItem GetSound(string soundName)
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
