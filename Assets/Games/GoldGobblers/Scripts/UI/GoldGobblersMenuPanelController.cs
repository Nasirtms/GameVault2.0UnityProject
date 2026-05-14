using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GoldGobblersMenuPanelController : MonoBehaviour
{
    [Header("Music & Sound Sprite")]
    [SerializeField] private Image soundButtonImage;
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    public void SoundActive(bool soundActive)
    {
        SoundManager.Instance.MuteSFX(soundActive);

        if (soundActive)
        {
            soundButtonImage.sprite = soundOffSprite;
        }
        else
        {
            soundButtonImage.sprite = soundOnSprite;
        }
    }

    public void MusicActive(bool musicActive)
    {
        SoundManager.Instance.MuteMusic(musicActive);

        if (musicActive)
        {
            musicButtonImage.sprite = musicOffSprite;
        }
        else
        {
            musicButtonImage.sprite = musicOnSprite;
        }
    }

}
