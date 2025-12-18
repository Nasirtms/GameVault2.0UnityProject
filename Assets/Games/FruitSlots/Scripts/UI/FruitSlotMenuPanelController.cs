using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FruitSlotMenuPanelController : MonoBehaviour
{
    [Header("Panel Settings")]
    //[SerializeField] private RectTransform panelTransform;
    [SerializeField] private float slideDuration = 0.3f;

    private Vector2 visiblePosition = new Vector2(-960f, 0.5f);
    private Vector2 hiddenPosition;

    //[Header("Background Click Area")]
    //[SerializeField] private GameObject transparentBackgroundOverlay;
    //[SerializeField] private Button transparentBackgroundButton;
    //[SerializeField] private Button menuPanelButton;

    [Header("Music & Sound Sprite")]
    [SerializeField] private Image soundButtonImage;
    [SerializeField] private Image musicButtonImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;

    private Coroutine slideMenuPanel;

    private void Start()
    {
        //transparentBackgroundOverlay.SetActive(false);
        //transparentBackgroundButton.onClick.AddListener(() => ShowMenuPanel(false));
        //menuPanelButton.onClick.AddListener(() => ShowMenuPanel(false));

        //// Set initial hidden position based on panel width
        //float panelWidth = panelTransform.rect.width;
        //hiddenPosition = new Vector2(-panelWidth - 960f, 0.5f);

        //// Initialize panel off-screen
        //panelTransform.anchoredPosition = hiddenPosition;
    }

    private void OnDestroy()
    {
        //transparentBackgroundButton?.onClick.RemoveAllListeners();
        //menuPanelButton?.onClick.RemoveAllListeners();
    }

    //public void ShowMenuPanel(bool show)
    //{
    //    if (slideMenuPanel != null) StopCoroutine(slideMenuPanel);

    //    if (show)
    //    {
    //        slideMenuPanel = StartCoroutine(SlidePanel(visiblePosition));
    //        transparentBackgroundOverlay.SetActive(true);
    //    }
    //    else
    //    {
    //        slideMenuPanel = StartCoroutine(SlidePanel(hiddenPosition));
    //        transparentBackgroundOverlay.SetActive(false);
    //    }
    //}

    //IEnumerator SlidePanel(Vector2 targetPosition)
    //{
    //    Vector2 start = panelTransform.anchoredPosition;
    //    float elapsed = 0;

    //    while (elapsed < slideDuration)
    //    {
    //        elapsed += Time.deltaTime;
    //        float t = Mathf.Clamp01(elapsed / slideDuration);
    //        panelTransform.anchoredPosition = Vector2.Lerp(start, targetPosition, t);
    //        yield return null;
    //    }

    //    panelTransform.anchoredPosition = targetPosition;
    //}

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

