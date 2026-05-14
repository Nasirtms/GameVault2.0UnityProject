using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class QuickHitVolcanoSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Details
    private int index;
    [HideInInspector] public QuickHitVolcanoSlotType type;
    [HideInInspector] public QuickHitVolcanoSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private QuickHitVolcanoReelScript _parent;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Image _slotIcon;
    private Image _wildIcon;

    [SerializeField] private RectTransform slotTransform;
    [SerializeField] private RectTransform wildTransform;
    private bool slotActive = true;

    [Header("Animation Settings")]
    public float scaleAmount = 1.2f;
    public float animationDuration = 0.5f;
    public float animationPause = 1f;
    public float delayBetween = 0.2f;
    private UIShiny uiShiny;

    private Tween animationTween;
    #endregion

    #region Unity Methods

    private void Start()
    {
        if (QuickHitVolcanoSlotMachine.Instance != null)
            QuickHitVolcanoSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (QuickHitVolcanoSlotMachine.Instance != null)
            QuickHitVolcanoSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void Initialize(QuickHitVolcanoReelScript parentReel, int index)
    {
        this._parent = parentReel;
        this.index = index;

        _rectTransform = GetComponent<RectTransform>();
        _slotIcon = transform.GetChild(0).GetComponent<Image>();
        _wildIcon = transform.GetChild(1).GetComponent<Image>();
        uiShiny = _slotIcon.gameObject.GetComponent<UIShiny>();
        _canvas = GetComponent<Canvas>();

        _canvas.overrideSorting = false;

        do
        {
            GetRandom();
        }
        while (this.type == QuickHitVolcanoSlotType.QuickHitWild);
    }

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Slot Switching

    public void SetType(QuickHitVolcanoSlotResource newType)
    {
        this.currentResource = newType;
        this._slotIcon.sprite = newType.background;
        this.type = newType.type;

    }

    public void GetRandom()
    {
        var random = QuickHitVolcanoSlotMachine.Instance.settings.resourcesList[Random.Range(0, QuickHitVolcanoSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
    }

    #endregion

    #region Slot Animation

    [ContextMenu("Start Animation")]
    public void PlayAnimation()
    {
        StopAnimation(); // reset any existing tweens

        //animationTween = CreateLoopTween(_slotIcon, 0f);
        if (type == QuickHitVolcanoSlotType.Wild)
        {
            animationTween = StartWildAnimation(scaleAmount, animationDuration / 3f, animationDuration / 3f, animationPause / 2.2f, animationPause / 3f);
        }
        else
        {
            uiShiny.enabled = true;
            //Feedback-01 Deepak 17/10/2025               
            animationTween = CreateTweenWithPause(_slotIcon, scaleAmount, animationDuration / 9f, animationPause);
            //Feedback-01 Deepak 17/10/2025       changed-->reduced animationDuration by 9f, previous was not reduced by any number
        }
    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        uiShiny.enabled = false;

        animationTween?.Kill(); animationTween = null;

        if (_slotIcon) _slotIcon.rectTransform.localScale = Vector3.one;

        slotTransform.gameObject.SetActive(true);
        wildTransform.gameObject.SetActive(false);
        slotTransform.localScale = Vector3.one;
        wildTransform.localScale = Vector3.one;
        slotTransform.localEulerAngles = Vector3.zero;
        wildTransform.localEulerAngles = Vector3.zero;
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private Tween CreateLoopTween(Image target, float startDelay)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        return target.rectTransform
            .DOScale(scaleAmount, animationDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo) // infinite loop
            .SetDelay(startDelay);
    }

    private Tween CreateTweenWithPause(Image target, float scale, float duration, float pause)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        var rect = target.rectTransform;

        uiShiny = target.gameObject.GetComponent<UIShiny>();

        if (uiShiny != null)
        {
            uiShiny.enabled = false;
            uiShiny.Stop();
        }

        Sequence sequence = DOTween.Sequence();

        sequence.Append(rect.DOScale(scale, duration).SetEase(Ease.OutSine));

        sequence.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.enabled = true;
                uiShiny.Play();
            }
        });

        sequence.AppendInterval(pause);

        sequence.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.Stop();
                uiShiny.enabled = false;
                //Debug.Log("Shiny - Disabled");
            }
        });

        sequence.Append(rect.DOScale(1f, duration).SetEase(Ease.InSine));

        sequence.AppendInterval(pause * 0.5f);

        sequence.SetLoops(-1, LoopType.Restart);

        return sequence;
    }

    //public Tween StartWildAnimation(float targetScale, float scaleDuration, float scaleDownDuration, float pauseDuration, float flipDuration)
    //{
    //    // Ensure starting state
    //    slotTransform.gameObject.SetActive(true);
    //    wildTransform.gameObject.SetActive(false);
    //    slotTransform.localScale = Vector3.one;
    //    wildTransform.localScale = Vector3.one;
    //    slotTransform.localEulerAngles = Vector3.zero;
    //    wildTransform.localEulerAngles = Vector3.zero;

    //    Sequence seq = DOTween.Sequence();

    //    // Scale Up
    //    seq.Append(slotTransform.DOScale(Vector3.one * targetScale, scaleDuration));
    //    seq.Join(wildTransform.DOScale(Vector3.one * targetScale, scaleDuration));

    //    // Pause
    //    seq.AppendInterval(pauseDuration);

    //    // Flip halfway (rotate 90 on Y)
    //    seq.AppendCallback(() =>
    //    {
    //        RectTransform active = slotActive ? slotTransform : wildTransform;
    //        RectTransform inactive = slotActive ? wildTransform : slotTransform;

    //        active.DORotate(new Vector3(0, 90, 0), flipDuration).OnComplete(() =>
    //        {
    //            active.gameObject.SetActive(false);
    //            inactive.gameObject.SetActive(true);
    //            inactive.localEulerAngles = new Vector3(0, 90, 0); // Start from backside
    //            inactive.DORotate(Vector3.zero, flipDuration);
    //        });
    //    });

    //    // Pause after flip
    //    seq.AppendInterval(pauseDuration);

    //    // Scale Down
    //    seq.Append(slotTransform.DOScale(Vector3.one, scaleDownDuration));
    //    seq.Join(wildTransform.DOScale(Vector3.one, scaleDownDuration));

    //    seq.SetLoops(-1, LoopType.Yoyo);

    //    return seq;
    //}

    public Tween StartWildAnimation(
    float targetScale,
    float scaleDuration,
    float scaleDownDuration,
    float pauseDuration,
    float flipDuration)
    {
        // Ensure starting state
        slotTransform.gameObject.SetActive(true);
        wildTransform.gameObject.SetActive(false);
        slotTransform.localScale = Vector3.one;
        wildTransform.localScale = Vector3.one;
        slotTransform.localEulerAngles = Vector3.zero;
        wildTransform.localEulerAngles = Vector3.zero;

        // Track which one is active
        bool slotActive = true;

        Sequence seq = DOTween.Sequence();

        // Scale Up
        seq.Append(slotTransform.DOScale(Vector3.one * targetScale, scaleDuration));
        seq.Join(wildTransform.DOScale(Vector3.one * targetScale, scaleDuration));

        // Pause
        seq.AppendInterval(pauseDuration);

        // Flip halfway (rotate 90 on Y)
        seq.AppendCallback(() =>
        {
            RectTransform active = slotActive ? slotTransform : wildTransform;
            RectTransform inactive = slotActive ? wildTransform : slotTransform;

            active.DORotate(new Vector3(0, 90, 0), flipDuration).OnComplete(() =>
            {
                active.gameObject.SetActive(false);
                inactive.gameObject.SetActive(true);
                inactive.localEulerAngles = new Vector3(0, 90, 0); // start flipped
                inactive.DORotate(Vector3.zero, flipDuration);
            });
        });

        // Pause after flip
        seq.AppendInterval(pauseDuration);

        // Scale Down
        seq.Append(slotTransform.DOScale(Vector3.one, scaleDownDuration));
        seq.Join(wildTransform.DOScale(Vector3.one, scaleDownDuration));

        // At the end of each cycle, swap which one is active
        seq.AppendCallback(() =>
        {
            slotActive = !slotActive;
        });

        // Loop entire animation from start each time
        seq.SetLoops(-1, LoopType.Restart);

        return seq;
    }

    public void SetSortingLayer(int layer, bool enable)
    {
        _canvas.overrideSorting = enable;
        _canvas.sortingOrder = layer;
    }

    #endregion
}
