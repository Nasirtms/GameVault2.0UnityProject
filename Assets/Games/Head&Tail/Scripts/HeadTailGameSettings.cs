using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

#region Sound Settings

[System.Serializable]
public class HeadTailSoundItem
{
    public string soundName;
    public AudioClip audioClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
}

#endregion


[CreateAssetMenu(menuName = "Settings/HeadTail", fileName = "Head and Tail")]
public class HeadTailGameSettings : ScriptableObject
{
    [TabGroup("Sound")]
    [TableList]
    public List<HeadTailSoundItem> soundItems;

    [TabGroup("BetOptions")]
    [TableList]
    public List<float> betOptions = new(){ 1.00f, 2.00f, 3.00f, 4.00f, 5.00f, 6.00f, 7.00f, 8.00f, 9.00f, 10.00f, 20.00f };
    [Min(0)] public float startIndex = 0;

    public HeadTailSoundItem GetSound(string soundName)
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
