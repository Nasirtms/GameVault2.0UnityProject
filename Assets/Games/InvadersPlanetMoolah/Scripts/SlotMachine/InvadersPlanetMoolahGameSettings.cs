using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings
public enum InvadersPlanetMoolahSlotType
{
    Barn, Trailer, Truck, Chickens, Milk, Mailbox, Outhouse, Grandpa, Grandma, Dog, Cowgirl, Man, Jackpot, Wild
}

[System.Serializable]
public struct InvadersPlanetMoolahSlotResource
{
    public InvadersPlanetMoolahSlotType slotType;
    public string slotAnimationBool;
    public int slotTypeIndex;
}
#endregion

#region Spin Settings
public enum InvadersPlanetMoolahSpinMode
{
    SpinAll,
    SpinOneByOne
}

public enum InvadersPlanetMoolahSpinDirection
{
    Down,
    Up,
    Random
}

[System.Serializable]
public class InvadersPlanetMoolahSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public InvadersPlanetMoolahSpinMode startSpin;
    [EnumToggleButtons] public InvadersPlanetMoolahSpinMode endSpin;
    [EnumToggleButtons] public InvadersPlanetMoolahSpinDirection spinDirection;

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
public class InvadersPlanetMoolahSlotSettings
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
public class InvadersPlanetMoolahSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}
#endregion

#region Game Settings
[CreateAssetMenu(menuName = "Settings/InvadersPlanetMoolah", fileName = "InvadersPlanetMoolah")]
public class InvadersPlanetMoolahGameSettings : ScriptableObject
{
    [TabGroup("Resources")]
    [TableList]
    public List<InvadersPlanetMoolahSlotResource> slotResources;

    [TabGroup("Sound")]
    [TableList]
    public List<InvadersPlanetMoolahSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public InvadersPlanetMoolahSpinSettings spinSettings;

    [TabGroup("Slot Settings")]
    [HideLabel]
    public InvadersPlanetMoolahSlotSettings slotSettings;

    public InvadersPlanetMoolahSoundItem GetSound(string soundName)
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