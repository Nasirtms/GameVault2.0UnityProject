using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CrazySevenMenuPanelController : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private RectTransform panelTransform;
    
    [Header("Background Click Area")]
    [SerializeField] private GameObject transparentBackgroundOverlay;
    [SerializeField] private Button transparentBackgroundButton;
    [SerializeField] private Button menuPanelButton;


    [Header("Music & Sound Sprite")]
    [SerializeField] private Image soundButtonImage;
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    private Coroutine slideMenuPanel;
    private bool isMenuPanelOpen = false;
    private void Start()
    {
        transparentBackgroundOverlay.SetActive(false);
        transparentBackgroundButton.onClick.AddListener(() => ShowMenuPanel());
        menuPanelButton.onClick.AddListener(() => ShowMenuPanel());

        float panelWidth = panelTransform.rect.width;
        panelTransform = panelTransform.GetComponent<RectTransform>();
        panelTransform.localScale = new Vector3(1, 0, 1);
        transparentBackgroundButton.transform.DORotate(new Vector3(0, 0, 0), 0.3f, RotateMode.Fast);
    }

    private void OnDestroy()
    {
        transparentBackgroundButton?.onClick.AddListener(()=> set());
        menuPanelButton?.onClick.RemoveAllListeners();
    }

    public void set()
    {
        transparentBackgroundOverlay.SetActive(false);
    }
    public void ShowMenuPanel()
    {

        if (transparentBackgroundOverlay != null)
        {
            transparentBackgroundButton.transform.DORotate(new Vector3(0, 0, 180f), 0.3f, RotateMode.Fast);
            if (transparentBackgroundOverlay.transform.localScale.y == 1)
            {
                transparentBackgroundButton.transform.DORotate(new Vector3(0, 0, 0), 0.3f, RotateMode.Fast);
                transparentBackgroundOverlay.transform.DOScaleY(0f, 0.2f)
                    .SetEase(Ease.InBack);
                transparentBackgroundOverlay.SetActive(false);
                return;
            }

            transparentBackgroundOverlay.transform.localScale = new Vector3(1, 0, 1);
            transparentBackgroundOverlay.SetActive(true);

            Sequence bounceSequence = DOTween.Sequence();
            bounceSequence.Append(transparentBackgroundOverlay.transform
                .DOScaleY(0.1f, 0.1f).SetEase(Ease.OutQuad));  // quick jump to 0.1
            bounceSequence.Append(transparentBackgroundOverlay.transform
                .DOScaleY(1f, 0.25f).SetEase(Ease.OutBack));

        }
        else
        {
            transparentBackgroundOverlay.SetActive(false);
        }
    }

    public void SoundActive(bool soundActive)
    {
        CrazySevenSoundManager.Instance.MuteSFX(soundActive);
        
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
        CrazySevenSoundManager.Instance.MuteMusic(musicActive);

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
