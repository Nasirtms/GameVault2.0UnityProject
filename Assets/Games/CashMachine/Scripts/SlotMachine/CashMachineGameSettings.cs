using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

public enum CashMachineSlotType
{
    One, Zero, DoubleZero, Two, Five, Ten, Blank
}

public enum CashMachineSpinType
{
    Single, All
}

[System.Serializable]
public struct CashMachineSlotResource
{
    public CashMachineSlotType type;
    [PreviewField] public Sprite icon;
    [PreviewField] public Sprite paylineIcon;
}

[System.Serializable]
public class CashMachineSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public CashMachineSpinType startSpin;
    [EnumToggleButtons] public CashMachineSpinType endSpin;
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
#region Sound Settings

[System.Serializable]
public class CashMachineSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

[CreateAssetMenu(menuName = "Settings/Cash Machine", fileName = "Cash Machine")]
public class CashMachineGameSettings : ScriptableObject
{
    public delegate void CashMachineSettingsEvents();
    public static event CashMachineSettingsEvents UpdateLayout;
    public static event CashMachineSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<CashMachineSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<CashMachineSoundItem> soundItems;


    [TabGroup("Spin Settings")]
    [HideLabel]
    public CashMachineSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(0, 600)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 0f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-150, 150)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 0f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-800, 0)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 0;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;


    public CashMachineSoundItem GetSound(string soundName)
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
