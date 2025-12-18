using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

[CreateAssetMenu(menuName = "Settings/Super ball Keno", fileName = "SuperBallKeno")]
public class SuperBallGameSettings : ScriptableObject
{
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

            return null;
        }
        return null;
    }
}
