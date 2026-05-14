using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

public enum AtomicMeltdownSlotType
{
    Empty, Wild10x, Wild5x, Wild3x, Wild2x, Blue7, Fiery7, Atomic7, SingleBAR, DoubleBAR, TripleBAR
}

public enum AtomicMeltdownSpinType
{
    Single, All
}

public enum AtomicMeltdownSpinDirection
{
    Downwards, Upwards, Random
}

[System.Serializable]
public struct AtomicMeltdownSlotResource
{
    public AtomicMeltdownSlotType type;
    [PreviewField] public Sprite icon;
    [PreviewField] public Sprite multiplier;
}

[System.Serializable]
public class AtomicMeltdownSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public AtomicMeltdownSpinType startSpin;
    [EnumToggleButtons] public AtomicMeltdownSpinType endSpin;
    [EnumToggleButtons] public AtomicMeltdownSpinDirection spinDirection;
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
public class AtomicMeltdownSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion
[CreateAssetMenu(menuName = "Settings/Atomic Meltdown", fileName = "AtomicMeltdown")]
public class AtomicMeltdownGameSettings : ScriptableObject
{
    public delegate void AtomicMeltdownSettingsEvents();
    public static event AtomicMeltdownSettingsEvents UpdateLayout;
    public static event AtomicMeltdownSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<AtomicMeltdownSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<AtomicMeltdownSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public AtomicMeltdownSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(0, 500)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 1f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-100, 0)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 1f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 0)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 1;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;


    public AtomicMeltdownSoundItem GetSound(string soundName)
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
    private void OnUpdateLayout()
    {
        UpdateLayout?.Invoke();
    }

    private void OnUpdateScale()
    {
        UpdateScale?.Invoke();
    }

}
