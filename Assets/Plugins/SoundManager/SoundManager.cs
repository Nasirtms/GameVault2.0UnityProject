using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private SoundData soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private bool isSoundMute = false;
    private bool isMusicMute = false;

    private static SoundManager instance;
    public static SoundManager Instance => instance;

    public bool IsSoundMute () => isSoundMute;
    public bool IsMusicMute () => isMusicMute;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (musicSource == null || sfxSource == null)
        {
            Debug.LogError("Please assign both MusicSource and SFXSource in the SoundManager.");
        }
    }

    public void PlayMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            musicSource.clip = sound.audioClip;
            musicSource.volume = sound.volume;
            musicSource.pitch = sound.pitch;
            musicSource.loop = true; 
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music '{soundName}' not found.");
        }
    }

    public void StopMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            musicSource.clip = sound.audioClip;
            musicSource.Stop();
        }
        else
        {
            Debug.LogWarning($"Music '{soundName}' not found.");
        }
    }

    public void PlaySFX(string soundName)
    {
        if (!string.IsNullOrEmpty(soundName))
        {
            var sound = soundData.GetSound(soundName);
            if (sound != null)
            {
                sfxSource.clip = sound.audioClip;
                sfxSource.volume = sound.volume;
                sfxSource.pitch = sound.pitch;
                sfxSource.loop = false;
                sfxSource.PlayOneShot(sound.audioClip);
            }
            else
            {
                Debug.LogWarning($"SFX '{soundName}' not found.");
            }
        }
    }

    public void MuteSFX(bool mute)
    {
        sfxSource.mute = mute;
        isSoundMute = mute;
    }

    public void MuteMusic(bool mute)
    {
        musicSource.mute = mute;
        isMusicMute = mute;
    }
}