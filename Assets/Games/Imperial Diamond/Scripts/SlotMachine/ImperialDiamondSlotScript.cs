using Coffee.UIEffects;
using DG.Tweening;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;
[Serializable]
[RequireComponent(typeof(Image))]
public class ImperialDiamondSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Children
    [SerializeField] private GameObject[] slots;

    [Header("Animation Settings")]
    public float scaleAmount = 1.1f;
    public float animationDuration = 0.8f;
    public float delayBetween = 0.1f;
    private UIShiny uiShiny;
    public float animationPause = 0.75f;

    private Tween animationTween;
    // Slot Details
    private int index;
    [HideInInspector] public ImperialDiamondSlotType type;
    public ImperialDiamondResource currentResource;

    private Animator slotAnimator;
    [HideInInspector] public bool isResultSet = false;
    private String slotAnimationBool;
    // Components
    public Image icon;
    private ImperialDiamondReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (ImperialDiamondSlotMachine.Instance != null)
            ImperialDiamondSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (ImperialDiamondSlotMachine.Instance != null)
            ImperialDiamondSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(ImperialDiamondReelScript parentReel, int index)
    {
        this.index = index;
        this._parent = parentReel;
        _rectTransform = GetComponent<RectTransform>();
        GetRandom(false);
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Slot Switching

    public void SetType(ImperialDiamondResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);
        currentResource = newType;
        slots[newType.slotTypeIndex].gameObject.SetActive(true);
        type = newType.type;
        icon = slots[newType.slotTypeIndex].transform.GetComponent<Image>();
        //this.slotAnimationBool = newType.slotAnimationBool;
    }

    public void GetRandom(bool finalResult)
    {

        if (finalResult)
        {
            var random = ImperialDiamondSlotMachine.CachedRealSymbols[
             UnityEngine.Random.Range(0, ImperialDiamondSlotMachine.CachedRealSymbols.Count)];
            SetType(random);
        }
        else
        {
            if (index % 2 != 0) // empty slot
            {
                if (ImperialDiamondSlotMachine.CachedEmptySymbol.HasValue)
                    SetType(ImperialDiamondSlotMachine.CachedEmptySymbol.Value);
            }
            else // real symbol
            {
                var random = ImperialDiamondSlotMachine.CachedRealSymbols[
                    UnityEngine.Random.Range(0, ImperialDiamondSlotMachine.CachedRealSymbols.Count)];
                SetType(random);
            }
        }
    }

    #endregion

    #region Slot Animation

    [ContextMenu("Start Animation")]
    public void StartAnimation()
    {
        StopAnimation(); 

        animationTween = CreateTweenWithPause(icon, scaleAmount, animationDuration, animationPause);
        //slotAnimator = GetComponent<Animator>();
        //slotAnimator.SetBool(slotAnimationBool, true);
    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        //if (slotAnimator != null)
        //{
        //    slotAnimator.SetBool(slotAnimationBool, false);
        //}

        animationTween?.Kill(false); animationTween = null;

        const float backDuration = 0.2f;
        if (icon) icon.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private Tween CreateTweenWithPause(Image target, float scale, float duration, float pause)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        var rect = target.rectTransform;
        var seq = DOTween.Sequence();

        seq.Append(rect.DOScale(scale, duration).SetEase(Ease.OutSine));
        seq.AppendInterval(pause);
        seq.Append(rect.DOScale(1f, duration).SetEase(Ease.OutSine));

        seq.SetLoops(-1, LoopType.Yoyo);
        return seq;
    }
    #endregion
}