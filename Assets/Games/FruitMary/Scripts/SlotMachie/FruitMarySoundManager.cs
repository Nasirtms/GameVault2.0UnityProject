using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitMarySoundManager : MonoBehaviour
{
    #region Variables

    public static FruitMarySoundManager Instance;

    [SerializeField] private FruitMaryGameSettings soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource winSource;
    private bool isSoundMute = false;
    private bool isMusicMute = false;

    public bool IsSoundMute() => isSoundMute;
    public bool IsMusicMute() => isMusicMute;

    #endregion

    #region

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;

        if (musicSource == null || sfxSource == null)
        {
            Debug.LogError("Please assign both MusicSource and SFXSource in the SoundManager.");
        }
    }

    #endregion

    #region Sound Manager

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

    public void PlayWinMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            winSource.clip = sound.audioClip;
            winSource.volume = sound.volume;
            winSource.pitch = sound.pitch;
            winSource.loop = true;
            winSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music '{soundName}' not found.");
        }
    }

    public void StopWinMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            winSource.clip = sound.audioClip;
            winSource.Stop();
        }
        else
        {
            Debug.LogWarning($"Music '{soundName}' not found.");
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
        winSource.mute = mute;
        isMusicMute = mute;
    }

    #endregion
}