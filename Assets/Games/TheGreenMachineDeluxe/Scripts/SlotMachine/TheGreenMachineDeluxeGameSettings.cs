using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

#region Slot Settings

public enum TheGreenMachineDeluxeSlotType
{
    Empty, Dollar1, Dollar2, Dollar5, Dollar10, Dollar20, Dollar50, Dollar100, Dollar500, Dollar1000, FREE_SPINS_1, FREE_SPINS_3, FREE_SPINS_5, MINI, MINOR, MAJOR, MEGA, GRAND
}

public enum TheGreenMachineDeluxeSlotCategory
{
    Empty, Cash, FreeSpin, Jackpot
}

[System.Serializable]
public struct TheGreenMachineDeluxeSlotResource
{
    public TheGreenMachineDeluxeSlotType type;
    public TheGreenMachineDeluxeSlotCategory category;
    [PreviewField] public Sprite frameImage;
    [PreviewField] public Sprite winImage;
    [PreviewField] public Sprite textImage;
}

#endregion

#region Spin Settings

public enum TheGreenMachineDeluxeSpinType
{
    Single, All
}

[System.Serializable]
public class TheGreenMachineDeluxeSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public TheGreenMachineDeluxeSpinType startSpin;
    [EnumToggleButtons] public TheGreenMachineDeluxeSpinType endSpin;
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
public class TheGreenMachineDeluxeSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

#region Game Settings

[CreateAssetMenu(menuName = "Settings/The Green Machine Deluxe", fileName = "New_Settings")]
public class TheGreenMachineDeluxeGameSettings : ScriptableObject
{
    public delegate void TheGreenMachineDeluxeSettingsEvents();
    public static event TheGreenMachineDeluxeSettingsEvents UpdateLayout;
    public static event TheGreenMachineDeluxeSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<TheGreenMachineDeluxeSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<TheGreenMachineDeluxeSoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public TheGreenMachineDeluxeSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(-300, 300)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 1f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-60, 60)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 1f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 600)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 1;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;

    public TheGreenMachineDeluxeSoundItem GetSound(string soundName)
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
