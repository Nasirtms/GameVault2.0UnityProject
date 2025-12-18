using Coffee.UIEffects;
using DG.Tweening;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[RequireComponent(typeof(Image))]
public class VegasSevenSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Children
    [SerializeField] private GameObject[] slots;


    [Header("Animation Settings")]
    public float scaleAmount = 1.1f;
    public float animationDuration = 0.5f;
    public float delayBetween = 0.1f;
    private UIShiny uiShiny;
    public float animationPause = 0.75f;

 
    private Tween animationTween;
    // Slot Details
    private int index;
    [HideInInspector] public VegasSevenSlotType type;
    public VegasSevenResource currentResource;

    private Animator slotAnimator;
    [HideInInspector] public bool isResultSet = false;
    private String slotAnimationBool;
    // Components
    private Image icon;
    private VegasSevenReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (VegasSevenSlotMachine.Instance != null)
            VegasSevenSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (VegasSevenSlotMachine.Instance != null)
            VegasSevenSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(VegasSevenReelScript parentReel, int index)
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

    public void SetType(VegasSevenResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);
        currentResource = newType;
        slots[newType.slotTypeIndex].gameObject.SetActive(true);
        type = newType.type;
        this.slotAnimationBool = newType.slotAnimationBool;


        //uiShiny = icon.gameObject.GetComponent<UIShiny>();

        //if (newType.type == VegasSevenSlotType.VegasSeven)
        //{
        //    if (doubleImage) doubleImage.gameObject.SetActive(true);
        //    if (bullseyeImage)
        //    {
        //        bullseyeImage.gameObject.SetActive(true);
        //        uiShiny = bullseyeImage.gameObject.GetComponent<UIShiny>();
        //    }
        //    if (jackpotImage) jackpotImage.gameObject.SetActive(true);
        //}
        //else
        //{
        //    if (doubleImage) doubleImage.gameObject.SetActive(false);
        //    if (bullseyeImage) bullseyeImage.gameObject.SetActive(false);
        //    if (jackpotImage) jackpotImage.gameObject.SetActive(false);
        //    uiShiny = icon.gameObject.GetComponent<UIShiny>();
        //}
    }

    public void GetRandom()
    {
        if (index % 2 == 0) // empty slot
        {
            if (VegasSevenSlotMachine.CachedEmptySymbol.HasValue)
                SetType(VegasSevenSlotMachine.CachedEmptySymbol.Value);
        }
        else // real symbol
        {
            var random = VegasSevenSlotMachine.CachedRealSymbols[
                UnityEngine.Random.Range(0, VegasSevenSlotMachine.CachedRealSymbols.Count)];
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


        //animationTween = CreateTweenWithPause(icon, scaleAmount, animationDuration, animationPause);
        slotAnimator = GetComponent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        if (uiShiny != null)
        {
            uiShiny.Stop();
            uiShiny.enabled = false;
        }

        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }

        animationTween?.Kill(false); animationTween = null;

        //const float backDuration = 0.2f;
        //if (icon) icon.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);

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
