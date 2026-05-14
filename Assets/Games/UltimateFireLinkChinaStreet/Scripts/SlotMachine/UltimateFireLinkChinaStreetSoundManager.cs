using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkChinaStreetSoundManager : MonoBehaviour
{
    #region variables

    public static UltimateFireLinkChinaStreetSoundManager Instance;

    [SerializeField] private UltimateFireLinkChinaStreetGameSettings soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource spinMusicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource winTextSource;

    private bool isSoundMute = false;
    private bool isMusicMute = false;

    public bool IsSoundMute() => isSoundMute;
    public bool IsMusicMute() => isMusicMute;

    #endregion

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
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
    public void SpinPlayMusic(string soundName)
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

    public void SpinStopMusic(string soundName)
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

    public void PlayWinText(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            winTextSource.clip = sound.audioClip;
            winTextSource.volume = sound.volume;
            winTextSource.pitch = sound.pitch;
            winTextSource.loop = true;
            winTextSource.Play();
        }
    }

    public void StopWinText(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            winTextSource.clip = sound.audioClip;
            winTextSource.Stop();
        }
    }

    public void MuteSFX(bool mute)
    {
        sfxSource.mute = mute;
        isSoundMute = mute;
    }
    public void StopSFX()
    {
        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }
    }
    public void MuteMusic(bool mute)
    {
        musicSource.mute = mute;
        isMusicMute = mute;
    }

    public void MuteText(bool mute)
    {
        winTextSource.mute = mute;
    }
}