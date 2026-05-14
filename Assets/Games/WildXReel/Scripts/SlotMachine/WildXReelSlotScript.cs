using Coffee.UIEffects;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;
[Serializable]
[RequireComponent(typeof(Image))]
public class WildXReelSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Children
    [SerializeField] private GameObject[] slots;

    [Header("Animation Settings")]
    public float scaleAmount = 1.1f;
    public float animationDuration = 0.8f;
    public float delayBetween = 0.1f;
    public UIShiny uiShiny;
    public float animationPause = 0.75f;

    private Tween animationTween;
    // Slot Details
    private int index;
    [HideInInspector] public WildXReelSlotType type;
    public WildXReelResource currentResource;

    //private Animator slotAnimator;
    [HideInInspector] public bool isResultSet = false;
    //private String slotAnimationBool;
    // Components
    public Image icon;
    private WildXReelReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (WildXReelSlotMachine.Instance != null)
            WildXReelSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (WildXReelSlotMachine.Instance != null)
            WildXReelSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(WildXReelReelScript parentReel, int index)
    {
        this.index = index;
        this._parent = parentReel;
        _rectTransform = GetComponent<RectTransform>();
        GetRandom();
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Slot Switching

    public void SetType(WildXReelResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);
        currentResource = newType;
        slots[newType.slotTypeIndex].gameObject.SetActive(true);
        type = newType.type;
        icon = slots[newType.slotTypeIndex].transform.GetComponent<Image>();
        uiShiny = icon.gameObject.transform.GetComponent<UIShiny>();
    }

    public void GetRandom()
    {
        if (index % 2 == 0) // empty slot
        {
            if (WildXReelSlotMachine.CachedEmptySymbol.HasValue)
                SetType(WildXReelSlotMachine.CachedEmptySymbol.Value);
        }
        else // real symbol
        {
            var random = WildXReelSlotMachine.CachedRealSymbols[
                UnityEngine.Random.Range(0, WildXReelSlotMachine.CachedRealSymbols.Count)];
            SetType(random);
        }
    }

    #endregion

    #region Slot Animation

    [ContextMenu("Start Animation")]
    public void StartAnimation()
    {
        StopAnimation();
        uiShiny.enabled = true;

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
        animationTween?.Kill(false); animationTween = null;

        const float backDuration = 0.2f;
        if (icon) icon.rectTransform.DOScale(0.8851f, backDuration).SetEase(Ease.OutSine);
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    private Tween CreateTweenWithPause(Image target, float scale, float duration, float pause)
    {
        if (target == null || !target.gameObject.activeSelf) return null;

        var rect = target.rectTransform;
        if (uiShiny != null)
        {
            uiShiny.enabled = false;
            uiShiny.Stop();
        }
        var seq = DOTween.Sequence();

        seq.Append(rect.DOScale(scale, duration).SetEase(Ease.OutSine));
        seq.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.enabled = true;
                uiShiny.Play();
            }
        });
        seq.AppendInterval(pause);
        seq.AppendCallback(() =>
        {
            if (uiShiny != null)
            {
                uiShiny.Stop();
                uiShiny.enabled = false;
            }
        });
        seq.AppendInterval(pause * 0.5f);
        seq.Append(rect.DOScale(0.8851f, duration).SetEase(Ease.OutSine));

        seq.SetLoops(-1, LoopType.Yoyo);
        return seq;
    }
    #endregion
}