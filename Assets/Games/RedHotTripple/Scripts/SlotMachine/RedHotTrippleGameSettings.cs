using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum RedHotTrippleSlotType
{
    Empty, WhiteSeven, BlueSeven, SingleRedHot, DoubleSevenRedHot, TripleSevenRedHot, WildSymbol, BonusSymbol, Two, Three, Four, Five, Six, Seven
}
public enum RedHotTrippleSpinDirection
{
    Downwards, Upwards, Random
}
public enum RedHotTrippleSpinType
{
    Single, All
}

[System.Serializable]
public struct RedHotTrippleSlotResource
{
    public RedHotTrippleSlotType type;
    public string slotAnimationBool;
    public int slotTypeIndex;
}

[System.Serializable]
public class RedHotTrippleSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public RedHotTrippleSpinType startSpin;
    [EnumToggleButtons] public RedHotTrippleSpinType endSpin;
    [EnumToggleButtons] public RedHotTrippleSpinDirection spinDirection;
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
public class RedHotTrippleSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}
#endregion

[CreateAssetMenu(menuName = "Settings/RedHotTripple", fileName = "RedHotTripple")]
public class RedHotTrippleGameSettings : ScriptableObject
{
    public delegate void RedHotTrippleSettingsEvents();
    public static event RedHotTrippleSettingsEvents UpdateLayout;
    public static event RedHotTrippleSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<RedHotTrippleSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<RedHotTrippleSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public RedHotTrippleSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(0, 500)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 1f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-1500, 0)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 1f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 0)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 1;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;

    public RedHotTrippleSoundItem GetSound(string soundName)
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
    private void OnUpdateLayout()
    {
        UpdateLayout?.Invoke();
    }

    private void OnUpdateScale()
    {
        UpdateScale?.Invoke();
    }
}