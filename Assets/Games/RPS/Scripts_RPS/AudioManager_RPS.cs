using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager_RPS : MonoBehaviour
{
    [SerializeField] private GameSettings_RPS soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource winMusicSource;
    [SerializeField] private AudioSource wheelSpinSource;

    private bool isSoundMute = false;
    private bool isMusicMute = false;

    private static AudioManager_RPS instance;
    public static AudioManager_RPS Instance => instance;

    public bool IsSoundMute() => isSoundMute;
    public bool IsMusicMute() => isMusicMute;



    private void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    public void PlayMusic(string soundName)
    {
        if (isMusicMute) return;

        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }

        var sound = soundData.GetSound(soundName);

        if (sound != null)
        {
            if (musicSource.clip == sound.audioClip && musicSource.isPlaying)
            {
                return; // Already playing this music
            }
            musicSource.clip = sound.audioClip;
            musicSource.volume = sound.volume;
            musicSource.pitch = sound.pitch;
            musicSource.loop = true;
            musicSource.Play();
        }
        else
        {
        }
    }

    public void StopMusic(string soundName)
    {
        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            musicSource.clip = sound.audioClip;
            musicSource.Stop();
        }
        else
        {
        }
    }
    public void WinPlayMusic(string soundName)
    {
        if (isMusicMute) return;

        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }

        var sound = soundData.GetSound(soundName);

        if (sound != null)
        {
            if (winMusicSource.clip == sound.audioClip && winMusicSource.isPlaying)
            {
                return; // Already playing this music
            }
            winMusicSource.clip = sound.audioClip;
            winMusicSource.volume = sound.volume;
            winMusicSource.pitch = sound.pitch;
            winMusicSource.loop = true;
            winMusicSource.Play();
        }
        else
        {
        }
    }

    public void WinStopMusic(string soundName)
    {
        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            winMusicSource.clip = sound.audioClip;
            winMusicSource.Stop();
        }
        else
        {
        }
    }
    public void WheelPlayMusic(string soundName)
    {
        if (isMusicMute) return;

        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }

        var sound = soundData.GetSound(soundName);

        if (sound != null)
        {
            if (wheelSpinSource.clip == sound.audioClip && wheelSpinSource.isPlaying)
            {
                return; // Already playing this music
            }
            wheelSpinSource.clip = sound.audioClip;
            wheelSpinSource.volume = sound.volume;
            wheelSpinSource.pitch = sound.pitch;
            wheelSpinSource.loop = true;
            wheelSpinSource.Play();
        }
        else
        {
        }
    }

    public void WheelStopMusic(string soundName)
    {
        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            wheelSpinSource.clip = sound.audioClip;
            wheelSpinSource.Stop();
        }
        else
        {
        }
    }
    public void PlaySFX(string soundName)
    {
        if (isSoundMute) return;

        if (soundData == null)
        {
            soundData = UIManager_RPS.Instance.settings;
        }

        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            sfxSource.volume = sound.volume;
            sfxSource.pitch = sound.pitch;
            sfxSource.PlayOneShot(sound.audioClip);
        }
        else
        {
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
