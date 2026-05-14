using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class TheGreenMachineDeluxeSoundManager : MonoBehaviour
{
    public static TheGreenMachineDeluxeSoundManager Instance;

    [SerializeField] private TheGreenMachineDeluxeGameSettings soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource spinMusicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource winSource;

    private bool isSoundMute = false;
    private bool isMusicMute = false;

    public bool IsSoundMute() => isSoundMute;
    public bool IsMusicMute() => isMusicMute;

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

    public void MuteMusic(bool mute)
    {
        musicSource.mute = mute;
        isMusicMute = mute;
    }

    public void MuteSFX(bool mute)
    {
        sfxSource.mute = mute;
        isSoundMute = mute;
        winSource.mute = mute;
        spinMusicSource.mute = mute;
    }
}