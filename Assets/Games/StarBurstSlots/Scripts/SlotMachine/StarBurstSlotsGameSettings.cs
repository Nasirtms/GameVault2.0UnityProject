using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings

public enum StarBurstSlotsSlotType
{
    Redgem, Yellowgem, Purplegem, Bluegem, Seven, Greengem, Bar, Wild
}



[System.Serializable]
public struct StarBurstSlotsSlotResource
{
    public StarBurstSlotsSlotType type;
    public int index;
    public string winAnimationBoolName;
    public string IdleAnimationBoolName;
}

#endregion

#region Spin Settings

public enum StarBurstSlotsSpinType
{
    Single, All
}

[System.Serializable]
public class StarBurstSlotsSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public StarBurstSlotsSpinType startSpin;
    [EnumToggleButtons] public StarBurstSlotsSpinType endSpin;
    [MinMaxSlider(0f, 2f, true)] public Vector2 delayAmongReels;
    public bool useSameAcceleration;
    public bool useSameSpeed;

    [Title("Boundaries")]
    [Range(0, 0.5f)] public float minClamp;
    public float topBoundary;
    public float bottomBoundary;
    [Title("Spinning")]
    [MinMaxSlider(10f, 500f, true)] public Vector2 startSpeed = new Vector2(10f, 10f);
    [MinMaxSlider(0.1f, 5f, true)] public Vector2 acceleration = new Vector2(0.1f, 0.1f);
    [MinMaxSlider(0, 1000, true)] public Vector2 speedRange = new Vector2(0f, 0f);
}

#endregion

#region Sound Settings

[System.Serializable]
public class StarBurstSlotsSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings

[CreateAssetMenu(menuName = "Settings/StarBurstSlots", fileName = "New_Settings")]
public class StarBurstSlotsGameSettings : ScriptableObject
{
    public delegate void StarBurstSlotsSettingsEvents();
    public static event StarBurstSlotsSettingsEvents UpdateLayout;
    public static event StarBurstSlotsSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<StarBurstSlotsSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<StarBurstSlotsSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public StarBurstSlotsSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(-300, 300)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 1f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-60, 60)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 1f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 600)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 1;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;

    public StarBurstSlotsSoundItem GetSound(string soundName)
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

    #endregion
}
