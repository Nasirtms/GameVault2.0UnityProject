#if UNITY_EDITOR
#define ENABLE_DEBUG_LOGGING
#endif

using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


public enum _ZombieParadiseSlotType
{
    Vampire, Skull, Spider, Brain, Candle, Ace, King, Queen, Jack, Ten, Nine, Scatter
}

// Enum for spin modes
public enum SpinMode
{
    SpinAll,        // All reels start spinning at the same time
    SpinOneByOne    // Reels start spinning one by one with delays
}

// Enum for reel spinning direction
public enum ReelDirection
{
    Down,           // Reels spin downward (current behavior)
    Up,             // Reels spin upward (reverse behavior)
    Random          // Each reel randomly chooses up or down direction
}

[System.Serializable]

public class _ZombieParadiseSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public SpinMode startSpin;
    [EnumToggleButtons] public SpinMode endSpin;
    [EnumToggleButtons] public ReelDirection reelDirection;
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
    
    [Space(5)] public bool EnableDebugLogging;
}

[System.Serializable]
public class _ZombieParadise_Reel_Slot_Settings
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
public struct _ZombieParadiseSlotResource
{
    public _ZombieParadiseSlotType type;
    [PreviewField] public Sprite symbol;
    // You could add type or background later if needed
}


[CreateAssetMenu(menuName = "Settings/Zombie Paradise Setting", fileName = "ZombieParadise_Setting")]
public class SlotMachineSettings : ScriptableObject
{


    [TabGroup("Resources")]
    [TableList]
    public List<_ZombieParadiseSlotResource> symbolSprites;



    [TabGroup("Spin Settings")]
    [HideLabel]
    public _ZombieParadiseSpinSettings spinSettings;

   


    [TabGroup("Slot Settings")]
    [HideLabel]
    public _ZombieParadise_Reel_Slot_Settings slotSettings;


    
    [Button("Spin Slot Machine", ButtonStyle.Box)]
    public void SpinSlotMachine()
    {
        if (SlotMachine.Instance != null)
        {
            SlotMachine.Instance.SpinFromContextMenu();
        }
        else
        {
            Debug.LogWarning("No SlotMachine instance found in scene! Make sure a SlotMachine is active.");
        }
    }

    [Button("Stp Slot Machine", ButtonStyle.Box)]
    public void StopSlotMachine()
    {
        if (SlotMachine.Instance != null)
        {
            SlotMachine.Instance.StopFromContextMenu();
        }
        else
        {
            Debug.LogWarning("No SlotMachine instance found in scene! Make sure a SlotMachine is active.");
        }
    }



}
