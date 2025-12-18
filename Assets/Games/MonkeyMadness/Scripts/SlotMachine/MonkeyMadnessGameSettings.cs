using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum MonkeyMadnessSlotType
{
    Empty, Monkey, Bongo, Parrot, Banana, Pineapple, Coconut
}

public enum MonkeyMadnessSpinType
{
    Single, All
}

[System.Serializable]
public struct MonkeyMadnessSlotResource
{
    public MonkeyMadnessSlotType type;
    [PreviewField] public Sprite normalImage;
    [PreviewField] public Sprite glowImage;
    [PreviewField] public Sprite blurredImage;
}

[System.Serializable]
public class MonkeyMadnessSpinSettings
{
    [Title("Reel Spinning")]
    [EnumToggleButtons] public MonkeyMadnessSpinType startSpin;
    [EnumToggleButtons] public MonkeyMadnessSpinType endSpin;
    [MinMaxSlider(0f, 2f, true)] public Vector2 delayAmongReels;
    public bool useSameAcceleration;
    public bool useSameSpeed;

    [Title("Boundaries")]
    [Range(-10f, 10f)] public float minClamp;
    public float topBoundary;
    public float bottomBoundary;
    [Title("Spinning")]
    [MinMaxSlider(10f, 500f, true)] public Vector2 startSpeed = new Vector2(10f, 10f);
    [MinMaxSlider(0.1f, 5f, true)] public Vector2 acceleration = new Vector2(0.1f, 0.1f);
    [MinMaxSlider(0, 1000, true)] public Vector2 speedRange = new Vector2(0f, 0f);
}

[CreateAssetMenu(menuName = "Settings/Monkey Madness", fileName = "MonkeyMadness")]
public class MonkeyMadnessGameSettings : ScriptableObject
{
    public delegate void MonkeyMadnessSettingsEvents();
    public static event MonkeyMadnessSettingsEvents UpdateLayout;
    public static event MonkeyMadnessSettingsEvents UpdateScale;

    [TabGroup("Resources")]
    [TableList]
    public List<MonkeyMadnessSlotResource> resourcesList;

    [System.Serializable]
    public class SoundItem
    {
        public string soundName;
        public AudioClip audioClip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
    }
    public SoundItem[] sounds;

    public SoundItem GetSound(string soundName)
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            foreach (var sound in sounds)
            {
                if (sound.soundName == soundName)
                    return sound;
            }

            Debug.LogWarning($"Sound '{soundName}' not found in SoundData.");
            return null;
        }
        return null;
    }

    [TabGroup("Spin Settings")]
    [HideLabel]
    public MonkeyMadnessSpinSettings spinSettings;

    [Title("Slot Holder")]
    [TabGroup("Layout")][LabelText("Horizontal Spacing")][Range(0, 600)][OnValueChanged("OnUpdateLayout")] public float horizontalLayout = 0f;

    [Title("Reel")]
    [TabGroup("Layout")][LabelText("Vertical Spacing")][Range(-150, 0)][OnValueChanged("OnUpdateLayout")] public float verticalLayout = 0f;
    [TabGroup("Layout")][LabelText("Padding Top")][Range(-800, 0)][OnValueChanged("OnUpdateLayout")] public int paddingTop = 0;

    [Title("Slot")]
    [TabGroup("Layout")][LabelText("Scale")][Range(0.5f, 2f)][OnValueChanged("OnUpdateScale")] public float slotScale = 1f;

    private void OnUpdateLayout()
    {
        UpdateLayout?.Invoke();
    }

    private void OnUpdateScale()
    {
        UpdateScale?.Invoke();
    }

}
