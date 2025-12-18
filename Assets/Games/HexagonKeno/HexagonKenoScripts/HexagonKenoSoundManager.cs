using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
public enum KenoSound
{
    BetChange,
    DrawComplete,
    BallDraw,
    Hit,
    Draw,
    HelpExitButton,
    HelpPanelButton,
    LastBallDraw,
    DrawEnd,
    NumberSelect
}
public class HexagonKenoSoundManager : MonoBehaviour
{
    [SerializeField] private HexagonKenoGameSettings soundData;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource reelStopSource;


    private bool isSoundMute = false;
    private bool isMusicMute = false;

    private static HexagonKenoSoundManager instance;
    public static HexagonKenoSoundManager Instance => instance;

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
            soundData = HexagonKeno.Instance.settings;
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


    public bool IsMusicPlaying(string soundName)
    {
        return musicSource.isPlaying && musicSource.clip != null && musicSource.clip.name == soundName;
    }
    public void StopMusic(string soundName)
    {
        if (soundData == null)
        {
            soundData = HexagonKeno.Instance.settings;
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
            soundData = HexagonKeno.Instance.settings;
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