using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishGameSoundManager : MonoBehaviour
{
    public static FishGameSoundManager instance;

    public AudioSource generalAudioSource;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public void PlayOneShot(AudioClip clip, float volumeScale = 1)
    {
        generalAudioSource.PlayOneShot(clip, volumeScale);
    }
}
