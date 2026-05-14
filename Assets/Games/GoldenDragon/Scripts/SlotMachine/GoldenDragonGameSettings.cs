using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum GoldenDragonSlotType
{
    GoldenDragon, GoldenPhoenix, CopperCoin, DragonTurtle, GoldenLion, GoldenFish, RedEnvelope, Ace, King, Queen, Jack, Ten
}

[System.Serializable]
public struct GoldenDragonSlotResource
{
    public GoldenDragonSlotType type;
    public int slotTypeIndex;
}
public enum GoldenDragonSpinType
{
    Single, All
}

[System.Serializable]
public class GoldenDragonSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public GoldenDragonSpinType startSpin;
    [EnumToggleButtons] public GoldenDragonSpinType endSpin;
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
public class GoldenDragonSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion

[CreateAssetMenu(menuName = "Settings/Golden Dragon", fileName = "Golden Dragon")]
public class GoldenDragonGameSettings : ScriptableObject
{
    public delegate void GoldenDragonSettingsEvents();
    public static event GoldenDragonSettingsEvents UpdateLayout;
    public static event GoldenDragonSettingsEvents UpdateScale;

    [TabGroup("Slot Resources")]
    [TableList]
    public List<GoldenDragonSlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<GoldenDragonSoundItem> soundItems;


    [TabGroup("Spin Settings")]
    [HideLabel]
    public GoldenDragonSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(0, 600)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 0f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-100, 100)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 0f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 600)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 0;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;


    public GoldenDragonSoundItem GetSound(string soundName)
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