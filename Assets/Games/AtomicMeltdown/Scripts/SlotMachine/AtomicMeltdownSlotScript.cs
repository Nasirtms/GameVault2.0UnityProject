using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class AtomicMeltdownSlotScript : MonoBehaviour
{
    #region Variables

    [Header("Symbol References")]
    public Image atomicImage;
    public Image meltdownImage;
    public Image multiplierImage;

    [Header("Animation Settings")]
    public float scaleAmount = 1.1f;
    public float animationDuration = 0.5f;
    public float delayBetween = 0.1f;
    private UIShiny uiShiny;
    public float animationPause = 0.75f;

    private Tween atomicTween;
    private Tween meltdownTween;
    private Tween multiplierTween;
    private Tween animationTween;
    // Slot Details
    private int index;
    [HideInInspector] public AtomicMeltdownSlotType type;
    [HideInInspector] public AtomicMeltdownSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private Image _background;
    private Image icon;
    private AtomicMeltdownReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (AtomicMeltdownSlotMachine.Instance != null)
            AtomicMeltdownSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (AtomicMeltdownSlotMachine.Instance != null)
            AtomicMeltdownSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    private Image Background
    {
        get
        {
            if (_background == null) _background = GetComponent<Image>();
            return _background;
        }
    }

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(AtomicMeltdownReelScript parentReel, int index)
    {
        this.index = index;
        this._parent = parentReel;
        _rectTransform = GetComponent<RectTransform>();
        icon = transform.GetChild(0).GetComponent<Image>();
        GetRandom();
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Slot Switching

    public void SetType(AtomicMeltdownSlotResource newType)
    {
        currentResource = newType;
        icon.sprite = newType.icon;
        type = newType.type;

        bool isWild = IsWild(type);

        if (isWild)
        {
            if (atomicImage) atomicImage.gameObject.SetActive(true);
            if (meltdownImage) meltdownImage.gameObject.SetActive(true);
            if (multiplierImage)
            {
                multiplierImage.gameObject.SetActive(true);
                multiplierImage.sprite = newType.multiplier;
                uiShiny = multiplierImage.gameObject.GetComponent<UIShiny>();
            }
        }
        else
        {
            if (atomicImage) atomicImage.gameObject.SetActive(false);
            if (meltdownImage) meltdownImage.gameObject.SetActive(false);
            if (multiplierImage) multiplierImage.gameObject.SetActive(false);
            uiShiny = icon.gameObject.GetComponent<UIShiny>();
        }
    }

    //public void GetRandom()
    //{
    //    if (index % 2 == 0) // empty slot
    //    {
    //        if (AtomicMeltdownSlotMachine.CachedEmptySymbol.HasValue)
    //            SetType(AtomicMeltdownSlotMachine.CachedEmptySymbol.Value);
    //    }
    //    else // real symbol
    //    {
    //        var random = AtomicMeltdownSlotMachine.CachedRealSymbols[
    //            Random.Range(0, AtomicMeltdownSlotMachine.CachedRealSymbols.Count)];
    //        SetType(random);
    //    }
    //}
    public void GetRandom()
    {
        if (index % 2 == 0)
        {
            if (AtomicMeltdownSlotMachine.CachedEmptySymbol.HasValue)
                SetType(AtomicMeltdownSlotMachine.CachedEmptySymbol.Value);
            return;
        }

        var all = AtomicMeltdownSlotMachine.CachedRealSymbols;
        var allowed = all.FindAll(r =>
        {
            var t = r.type;
            int reel = _parent.reelIndex;

            if (reel == 0) return t != AtomicMeltdownSlotType.Wild3x && t != AtomicMeltdownSlotType.Wild2x;
            if (reel == 1) return t != AtomicMeltdownSlotType.Wild10x && t != AtomicMeltdownSlotType.Wild5x && t != AtomicMeltdownSlotType.Wild2x;
            if (reel == 2) return t != AtomicMeltdownSlotType.Wild10x && t != AtomicMeltdownSlotType.Wild5x && t != AtomicMeltdownSlotType.Wild3x;

            return t != AtomicMeltdownSlotType.Wild10x && t != AtomicMeltdownSlotType.Wild5x &&
                   t != AtomicMeltdownSlotType.Wild3x && t != AtomicMeltdownSlotType.Wild2x;
        });

        SetType(allowed[Random.Range(0, allowed.Count)]);
    }
    #endregion

    #region Slot Animation

    [ContextMenu("Start Animation")]
    public void StartAnimation()
    {
        StopAnimation(); // reset any existing tweens

        uiShiny.enabled = true;

        bool isWild = IsWild(type);
        if (isWild)
        {
            atomicTween = CreateLoopTween(atomicImage, 0f);
            meltdownTween = CreateLoopTween(meltdownImage, delayBetween);
            multiplierTween = CreateLoopTween(multiplierImage, delayBetween * 2f);
        }
        else
        {
            animationTween = CreateTweenWithPause(icon, scaleAmount, animationDuration, animationPause);
        }

    }

    //[ContextMenu("Stop Animation")]
    //public void StopAnimation()
    //{
    //    uiShiny.enabled = false;

    //    atomicTween?.Kill(); atomicTween = null;
    //    meltdownTween?.Kill(); meltdownTween = null;
    //    multiplierTween?.Kill(); multiplierTween = null;
    //    animationTween?.Kill(); animationTween = null;

    //    if (icon) icon.rectTransform.localScale = Vector3.one;
    //    if (atomicImage) atomicImage.rectTransform.localScale = Vector3.one;
    //    if (meltdownImage) meltdownImage.rectTransform.localScale = Vector3.one;
    //    if (multiplierImage) multiplierImage.rectTransform.localScale = Vector3.one;
    //}
    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        if (uiShiny != null)
        {
            uiShiny.Stop();
            uiShiny.enabled = false;
        }
        atomicTween?.Kill(false); atomicTween = null;
        meltdownTween?.Kill(false); meltdownTween = null;
        multiplierTween?.Kill(false); multiplierTween = null;
        animationTween?.Kill(false); animationTween = null;

        const float backDuration = 0.2f;
        if (icon) icon.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
        if (atomicImage) atomicImage.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
        if (meltdownImage) meltdownImage.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
        if (multiplierImage) multiplierImage.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
    }

    private void OnDisable()
    {
        StopAnimation();
    }
    private Tween CreateTweenWithPause(Image target, float scale, float duration, float pause)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        var rect = target.rectTransform;

        // pause shiny while we animate up the first time
        if (uiShiny != null)
        {
            uiShiny.enabled = false;
            uiShiny.Stop();
        }

        var seq = DOTween.Sequence();

        // up
        seq.Append(rect.DOScale(scale, duration).SetEase(Ease.OutSine));

        // turn on shiny during the “hold”
        seq.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.enabled = true;
                uiShiny.Play();
            }
        });

        // hold at big size
        seq.AppendInterval(pause);

        // turn off shiny before going down
        seq.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.Stop();
                uiShiny.enabled = false;
            }
        });

        // a small buffer before the return
        seq.AppendInterval(pause * 0.5f);

        // down
        seq.Append(rect.DOScale(1f, duration).SetEase(Ease.OutSine));

        // mirror the whole thing back and forth (smooth, no restart snap)
        seq.SetLoops(-1, LoopType.Yoyo);

        return seq;
    }

    //private Tween CreateTweenWithPause(Image target, float scale, float duration, float pause)
    //{
    //    if (target == null || !target.gameObject.activeSelf) return null;

    //    var rect = target.rectTransform;

    //    if (uiShiny != null)
    //    {
    //        uiShiny.enabled = false;
    //        uiShiny.Stop();
    //    }

    //    Sequence sequence = DOTween.Sequence();

    //    sequence.Append(rect.DOScale(scale, duration).SetEase(Ease.OutSine));

    //    sequence.AppendCallback(() =>
    //    {
    //        if (uiShiny != null)
    //        {
    //            uiShiny.enabled = true;
    //            uiShiny.Play();
    //        }
    //    });

    //    sequence.AppendInterval(pause);

    //    sequence.AppendCallback(() =>
    //    {
    //        if (uiShiny != null)
    //        {
    //            uiShiny.Stop();
    //            uiShiny.enabled = false;
    //            Debug.Log("Shiny - Disabled");
    //        }
    //    });

    //    sequence.Append(rect.DOScale(1f, duration).SetEase(Ease.InSine));

    //    sequence.AppendInterval(pause * 0.5f);

    //    sequence.SetLoops(-1, LoopType.Restart);

    //    return sequence;
    //}
    private Tween CreateLoopTween(Image target, float startDelay)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        return target.rectTransform
            .DOScale(scaleAmount, animationDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo) // infinite loop
            .SetDelay(startDelay);
    }

    #endregion

    #region Helper Functions

    private static bool IsWild(AtomicMeltdownSlotType t)
    {
        return t == AtomicMeltdownSlotType.Wild10x
            || t == AtomicMeltdownSlotType.Wild5x
            || t == AtomicMeltdownSlotType.Wild3x
            || t == AtomicMeltdownSlotType.Wild2x;
    }

    #endregion
}