using Coffee.UIEffects;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Canvas))]
public class TenTimesWinsSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Details
    private int index;
    [HideInInspector] public TenTimesWinsSlotType type;
    [HideInInspector] public TenTimesWinsSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private TenTimesWinsReelScript _parent;
    private Image _background;
    private RectTransform _rectTransform;
    //private Animator _animator;
    private Canvas _canvas;

    [Header("UI References")]
                 // Standalone border
    [SerializeField] private Image icon;
    #endregion

    #region Unity Methods

    private void Start()
    {
        if (TenTimesWinsSlotMachine.Instance != null)
            TenTimesWinsSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (TenTimesWinsSlotMachine.Instance != null)
            TenTimesWinsSlotMachine.Instance.StopReelProcess -= HandleStart;
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

    public void Initialize(TenTimesWinsReelScript parentReel, int index)
    {
        this._parent = parentReel;
        this.index = index;

        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponent<Canvas>();

        //_animator = GetComponent<Animator>();
        //if (_animator != null)
        //    _animator.enabled = false;

        _canvas.overrideSorting = false;

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

    private TenTimesWinsSlotResource GetRandomSlot()
    {
        var random = TenTimesWinsSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, TenTimesWinsSlotMachine.Instance.settings.resourcesList.Count)];

        return random;
    }

    public void ShowUniqueRandomVisual(HashSet<TenTimesWinsSlotType> excludedTypes)
    {
        TenTimesWinsSlotResource random;
        string parentName = gameObject.transform.parent.name;

        int safety = 100;

        do
        {
            random = GetRandomSlot();
            safety--;
        }
        while (
            safety > 0 &&
            (
                excludedTypes.Contains(random.type)
            )
        );

        excludedTypes.Add(random.type);
        SetType(random);
    }

    public void SetType(TenTimesWinsSlotResource newType)
    {
        this.currentResource = newType;
        //this.Background.sprite = newType.background;
        this.icon.sprite = newType.background;
        this.type = newType.type;
    }

    public void GetRandom()
    {
        var random = TenTimesWinsSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, TenTimesWinsSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
    }

    #endregion

    #region Slot Animation

    //public void PlayAnimation(string trigger)
    //{
    //    if (_animator == null) return;

    //    _animator.enabled = true;
    //    _animator.Rebind();
    //    _animator.Update(0f);
    //    _animator.SetTrigger(trigger);
    //}

    //public float GetClipLengthByName(string clipName)
    //{
    //    if (_animator.runtimeAnimatorController == null) return 0.5f;

    //    foreach (var clip in _animator.runtimeAnimatorController.animationClips)
    //    {
    //        if (clip.name == clipName)
    //            return clip.length;
    //    }

    //    return 0.5f; // fallback
    //}

    //public void ResetAnimator()
    //{
    //    if (_animator != null)
    //        _animator.enabled = false;

    //    this.Background.sprite = Background.sprite;
    //}

    #endregion

    #region Slot Borders and Text

    public void SetSortingLayer(int layer, bool enable)
    {
        _canvas.overrideSorting = enable;
        _canvas.sortingOrder = layer;
    }


    #endregion

    public void PlayAnimation()
    {
        if (icon == null) return;
        icon.rectTransform.localScale = Vector3.one;
        Sequence seq = DOTween.Sequence();
        seq.Append(icon.rectTransform.DOScale(1.2f, 0.2f).OnComplete(() =>
        {
            icon.GetComponent<UIShiny>().width = 0.2f;
            icon.GetComponent<UIShiny>().Play();
        }));
        seq.Append(icon.rectTransform.DOScale(1f, 0.2f));
        seq.SetLoops(2, LoopType.Restart);
    }
    public void StopAnimation()
    {
        if (icon == null) return;
        icon.rectTransform.DOKill();
        var shiny = icon.GetComponent<UIShiny>();
        if (shiny != null)
        {
            shiny.Stop();
            shiny.width = 0f;
        }
        icon.rectTransform.localScale = Vector3.one;
    }
}
