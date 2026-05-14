using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyMadnessSoundManager : MonoBehaviour
{
    [SerializeField] private MonkeyMadnessGameSettings soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource reelStopSource;


    private bool isSoundMute = false;
    private bool isMusicMute = false;

    private static MonkeyMadnessSoundManager instance;
    public static MonkeyMadnessSoundManager Instance => instance;

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
            soundData = MonkeyMadnessSlotMachine.Instance.settings;
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
    }


    public bool IsMusicPlaying(string soundName)
    {
        return musicSource.isPlaying && musicSource.clip != null && musicSource.clip.name == soundName;
    }
    public void StopMusic(string soundName)
    {
        if (soundData == null)
        {
            soundData = MonkeyMadnessSlotMachine.Instance.settings;
        }
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            musicSource.clip = sound.audioClip;
            musicSource.Stop();
        }
    }
    public void PlayReelStopSFX(string soundName)
    {
        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            reelStopSource.clip = sound.audioClip;
            reelStopSource.volume = sound.volume;
            reelStopSource.pitch = sound.pitch;
            reelStopSource.loop = false;
            reelStopSource.Play();
        }
    }
    public void StopReelStopSFX()
    {
        if (reelStopSource.isPlaying)
            reelStopSource.Stop();
    }

    public void PlaySFX(string soundName)
    {
        if (isSoundMute) return;

        if (soundData == null)
        {
            soundData = MonkeyMadnessSlotMachine.Instance.settings;
        }

        var sound = soundData.GetSound(soundName);
        if (sound != null)
        {
            sfxSource.volume = sound.volume;
            sfxSource.pitch = sound.pitch;
            sfxSource.PlayOneShot(sound.audioClip);
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
