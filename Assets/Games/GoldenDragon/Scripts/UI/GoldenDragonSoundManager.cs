using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldenDragonSoundManager : MonoBehaviour
{
    #region Variables

    public static GoldenDragonSoundManager Instance;

    [SerializeField] private GoldenDragonGameSettings soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource spinMusicSource;
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
    }

    public void StopMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            musicSource.clip = sound.audioClip;
            musicSource.Stop();
        }
    }
    public void PlaySpinMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            spinMusicSource.clip = sound.audioClip;
            spinMusicSource.volume = sound.volume;
            spinMusicSource.pitch = sound.pitch;
            spinMusicSource.loop = true;
            spinMusicSource.Play();
        }
    }
    public void StopSpinMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            spinMusicSource.clip = sound.audioClip;
            spinMusicSource.Stop();
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
    }

    public void StopWinMusic(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            winSource.clip = sound.audioClip;
            winSource.Stop();
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
    public void StopSFX()
    {
        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }
    #endregion
}
