using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
public enum FruitMarySlotType
{
    PINEAPPLE, GRAPES, WATERMELON, LEMON, WILD, BELL, BAR, SCATTER, CHERRIES, BANANA, BONUS
}

public enum FruitMarySpinType
{
    Single, All
}



[System.Serializable]
public struct FruitMarySlotResource
{
    public FruitMarySlotType type;
    [PreviewField] public Sprite background;

}

[System.Serializable]
public class FruitMarySpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public FruitMarySpinType startSpin;
    [EnumToggleButtons] public FruitMarySpinType endSpin;
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
[System.Serializable]
public class FruitMarySoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

[CreateAssetMenu(menuName = "Settings/Fruit Mary", fileName = "FruitMary")]
public class FruitMaryGameSettings : ScriptableObject
{
    public delegate void FruitMarySettingsEvents();
    public static event FruitMarySettingsEvents UpdateLayout;
    public static event FruitMarySettingsEvents UpdateScale;
    //public static event SettingsEvents OnSavePaylines;

    [TabGroup("Resources")]
    [TableList]
    //[SerializeField]
    public List<FruitMarySlotResource> resourcesList;

    [TabGroup("Sound")]
    [TableList]
    public List<FruitMarySoundItem> soundItems;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public FruitMarySpinSettings spinSettings;


    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(1, 300)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 1f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-100, 100)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 1f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-600, 600)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 1;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;

    public FruitMarySoundItem GetSound(string soundName)
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
