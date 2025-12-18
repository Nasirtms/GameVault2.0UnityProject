using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DoubleJackpotBullseyeSlotScript : MonoBehaviour
{
    #region Variables

    [Header("Symbol References")]
    public Image doubleImage;
    public Image bullseyeImage;
    public Image jackpotImage;

    [Header("Animation Settings")]
    public float scaleAmount = 1.1f;
    public float animationDuration = 0.5f;
    public float delayBetween = 0.1f;
    private UIShiny uiShiny;
    public float animationPause = 0.75f;

    private Tween doubleTween;
    private Tween bullseyeTween;
    private Tween jackpotTween;
    private Tween animationTween;
    // Slot Details
    private int index;
    [HideInInspector] public DoubleJackpotBullseyeSlotType type;
    public DoubleJackpotBullseyeSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private Image icon;
    private DoubleJackpotBullseyeReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (DoubleJackpotBullseyeSlotMachine.Instance != null)
            DoubleJackpotBullseyeSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (DoubleJackpotBullseyeSlotMachine.Instance != null)
            DoubleJackpotBullseyeSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(DoubleJackpotBullseyeReelScript parentReel, int index)
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

    public void SetType(DoubleJackpotBullseyeSlotResource newType)
    {
        currentResource = newType;
        icon.sprite = newType.icon;
        type = newType.type;

        if (newType.type == DoubleJackpotBullseyeSlotType.DoubleJackpotBullseye)
        {
            if (doubleImage) doubleImage.gameObject.SetActive(true);
            if (bullseyeImage)
            {
                bullseyeImage.gameObject.SetActive(true);
                uiShiny = bullseyeImage.gameObject.GetComponent<UIShiny>();
            }
            if (jackpotImage) jackpotImage.gameObject.SetActive(true);
        }
        else
        {
            if (doubleImage) doubleImage.gameObject.SetActive(false);
            if (bullseyeImage) bullseyeImage.gameObject.SetActive(false);
            if (jackpotImage) jackpotImage.gameObject.SetActive(false);
            uiShiny = icon.gameObject.GetComponent<UIShiny>();
        }
    }

    public void GetRandom()
    {
        if (index % 2 == 0) // empty slot
        {
            if (DoubleJackpotBullseyeSlotMachine.CachedEmptySymbol.HasValue)
                SetType(DoubleJackpotBullseyeSlotMachine.CachedEmptySymbol.Value);
        }
        else // real symbol
        {
            var random = DoubleJackpotBullseyeSlotMachine.CachedRealSymbols[
                Random.Range(0, DoubleJackpotBullseyeSlotMachine.CachedRealSymbols.Count)];
            SetType(random);
        }

    }

    #endregion

    #region Slot Animation

    [ContextMenu("Start Animation")]
    public void StartAnimation()
    {
        StopAnimation(); // reset any existing tweens

        if (uiShiny != null)
            uiShiny.enabled = true;

        doubleTween = CreateLoopTween(doubleImage, 0f);
        bullseyeTween = CreateLoopTween(bullseyeImage, delayBetween);
        jackpotTween = CreateLoopTween(jackpotImage, delayBetween * 2f);
        animationTween = CreateTweenWithPause(icon, scaleAmount, animationDuration, animationPause);
    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        if (uiShiny != null)
        {
            uiShiny.Stop();
            uiShiny.enabled = false;
        }

        doubleTween?.Kill(); doubleTween = null;
        bullseyeTween?.Kill(); bullseyeTween = null;
        jackpotTween?.Kill(); jackpotTween = null;
        animationTween?.Kill(false); animationTween = null;

        const float backDuration = 0.2f;
        if (icon) icon.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
        if (doubleImage) doubleImage.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
        if (bullseyeImage) bullseyeImage.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
        if (jackpotImage) jackpotImage.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
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
    #endregion
}
