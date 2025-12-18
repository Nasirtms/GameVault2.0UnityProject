using Coffee.UIEffects;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[RequireComponent(typeof(Image))]
public class GoldenDragonSlotScript : MonoBehaviour
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
    [HideInInspector] public GoldenDragonSlotType type;
    public GoldenDragonSlotResource currentResource;

    private Animator slotAnimator;
    [HideInInspector] public bool isResultSet = false;

    // Components
    public Image icon;
    private GoldenDragonReelScript _parent;
    private RectTransform _rectTransform;

    private Tween colorTween;
    private Transform child;
    private Image sr;
    #endregion

    #region Unity Methods

    private void Start()
    {
        if (GoldenDragonSlotMachine.Instance != null)
            GoldenDragonSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (GoldenDragonSlotMachine.Instance != null)
            GoldenDragonSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(GoldenDragonReelScript parentReel, int index)
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

    public void SetType(GoldenDragonSlotResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);
        currentResource = newType;
        slots[newType.slotTypeIndex].gameObject.SetActive(true);
        type = newType.type;
        uiShiny = slots[newType.slotTypeIndex].transform.GetComponent<UIShiny>();
        icon = slots[newType.slotTypeIndex].transform.GetComponent<Image>();
    }
    public void GetRandom()
    {
        var random = GoldenDragonSlotMachine.Instance.settings.resourcesList[UnityEngine.Random.Range(0, GoldenDragonSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
    }

    #endregion

    #region Slot Animation

    [ContextMenu("Start Animation")]
    public void PlayAnimation()
    {
        StopAnimation(); // reset any existing tweens

        if (uiShiny != null)
            uiShiny.enabled = true;

        if (child == null)
        {
            child = transform.GetChild(12);
        }

        child.gameObject.SetActive(true);

        if (sr == null)
            sr = child.GetComponent<Image>();
        colorTween?.Kill();

        float hue = 0f;
        float d = 1f; // just for cleaner code

        Sequence seq = DOTween.Sequence();

        // 🔵 Blue
        seq.Append(DOTween.To(() => hue, x =>
        {
            hue = x;
            sr.color = Color.HSVToRGB(hue, 1f, 1f);
        }, 0.66f, d));

        // 🟡 Yellow
        seq.Append(DOTween.To(() => hue, x =>
        {
            hue = x;
            sr.color = Color.HSVToRGB(hue, 1f, 1f);
        }, 0.16f, d));

        // 🔴 Red
        seq.Append(DOTween.To(() => hue, x =>
        {
            hue = x;
            sr.color = Color.HSVToRGB(hue, 1f, 1f);
        }, 0.0f, d));

        seq.SetLoops(-1, LoopType.Restart);

        colorTween = seq;
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
        colorTween?.Kill();
        colorTween = null;

        if (sr != null)
        {
            sr.color = new Color32(0x00, 0x7C, 0xFF, 0xFF); // Original color
        }

        if (child != null)
        {
            child.gameObject.SetActive(false);
        }

        animationTween?.Kill(false); 
        animationTween = null;

        const float backDuration = 0.2f;
        if (icon) icon.rectTransform.DOScale(1f, backDuration).SetEase(Ease.OutSine);
    }

    private void OnDisable()
    {
        colorTween?.Kill();
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
    #endregion
}