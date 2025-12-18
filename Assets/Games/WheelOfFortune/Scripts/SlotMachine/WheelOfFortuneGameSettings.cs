using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

public enum WheelOfFortuneSlotType
{
    Empty, Wheel2x, Wheel3x, WheelWild, SevenRed, Cherry, DoubleBAR,  SingleBAR, TripleBAR, Jackpot, SpinCash
}
public enum WheelOfFortuneSpinDirection
{
    Downwards, Upwards, Random
}
public enum WheelOfFortuneSpinType
{
    Single, All
}

[System.Serializable]
public struct WheelOfFortuneSlotResource
{
    public WheelOfFortuneSlotType type;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

[System.Serializable]
public class WheelOfFortuneSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public WheelOfFortuneSpinType startSpin;
    [EnumToggleButtons] public WheelOfFortuneSpinType endSpin;
    [EnumToggleButtons] public WheelOfFortuneSpinDirection spinDirection;
    [MinMaxSlider(0f, 2f, true)] public Vector2 delayAmongReels;
    public bool useSameAcceleration;
    public bool useSameSpeed;

    [Title("Boundaries")]
    [Range(0, 0.5f)] public float minClamp;
    public float topBoundary;
    public float middleBoundary;
    public float bottomBoundary;
    [Title("Spinning")]
    [MinMaxSlider(10f, 500f, true)] public Vector2 startSpeed = new Vector2(10f, 10f);
    [MinMaxSlider(0.1f, 5f, true)] public Vector2 acceleration = new Vector2(0.1f, 0.1f);
    [MinMaxSlider(0, 1000, true)] public Vector2 speedRange = new Vector2(0f, 0f);
}
#region Sound Settings

[System.Serializable]
public class WheelOfFortuneSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}
#endregion

[CreateAssetMenu(menuName = "Settings/WheelOfFortune", fileName = "WheelOfFortune")]
public class WheelOfFortuneGameSettings : ScriptableObject
{
    public delegate void WheelOfFortuneSettingsEvents();
    public static event WheelOfFortuneSettingsEvents UpdateLayout;
    public static event WheelOfFortuneSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<WheelOfFortuneSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<WheelOfFortuneSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public WheelOfFortuneSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(0, 500)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 1f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-1500, 0)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 1f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 0)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 1;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;

    public WheelOfFortuneSoundItem GetSound(string soundName)
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
    private void OnUpdateLayout()
    {
        UpdateLayout?.Invoke();
    }

    private void OnUpdateScale()
    {
        UpdateScale?.Invoke();
    }
}
