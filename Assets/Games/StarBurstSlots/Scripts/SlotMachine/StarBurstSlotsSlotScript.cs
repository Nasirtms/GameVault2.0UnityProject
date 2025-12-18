using Coffee.UIEffects;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Animator))]
public class StarBurstSlotsSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Details
    private int index;
     public StarBurstSlotsSlotType type;
    [HideInInspector] public StarBurstSlotsSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private StarBurstSlotsReelScript _parent;
    private Image _background;
    private RectTransform _rectTransform;
    private Animator _animator;
    private Canvas _canvas;

    [Header("UI References")]
    // Standalone border
    [SerializeField] public GameObject[] slots;
    [SerializeField] public GameObject[] slotsPaylineGlowOnce;
    [SerializeField] public GameObject[] slotWithGlowMaterial;
    [SerializeField] public GameObject[] slotWithoutGlowMaterial;
    [SerializeField] public ParticleSystem[] slotGlowParticle;
    [SerializeField] public GameObject canvas;
    [SerializeField] private TMP_Text paylineWin;
    [SerializeField] private TMP_Text paylineWinOnce;
    private Tween scaleTween;
    #endregion

    #region Unity Methods

    private void Start()
    {
        if (StarBurstSlotsSlotMachine.Instance != null)
            StarBurstSlotsSlotMachine.Instance.StopReelProcess += HandleStart;

    }

    private void OnDestroy()
    {
        if (StarBurstSlotsSlotMachine.Instance != null)
            StarBurstSlotsSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    private void Awake()
    {
        slotGlowTweens = new Tween[slotWithGlowMaterial.Length];
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

    public void Initialize(StarBurstSlotsReelScript parentReel, int index)
    {
        this._parent = parentReel;
        this.index = index;
        _animator = GetComponent<Animator>();
        _canvas = GetComponent<Canvas>();

        _rectTransform = GetComponent<RectTransform>();
        GetRandom();
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

    private StarBurstSlotsSlotResource GetRandomSlot()
    {
        var random = StarBurstSlotsSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, StarBurstSlotsSlotMachine.Instance.settings.resourcesList.Count)];

        return random;
    }

    public void ShowUniqueRandomVisual(HashSet<StarBurstSlotsSlotType> excludedTypes)
    {
        StarBurstSlotsSlotResource random;
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

    public void SetType(StarBurstSlotsSlotResource newType)
    {
        slots[currentResource.index].SetActive(false);

        this.currentResource = newType;
        //this.Background.sprite = newType.background;
        //this.icon.sprite = newType.frameImage;
        this.type = newType.type;
        slots[currentResource.index].SetActive(true);
    }

    public void GetRandom()
    {
        var random = StarBurstSlotsSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, StarBurstSlotsSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
    }

    #endregion

    #region Slot Animation

    #endregion

    #region Slot Borders and Text

    public void SetSortingLayer(int layer, bool enable)
    {
        //_canvas.overrideSorting = enable;
        //_canvas.sortingOrder = layer;
    }
    #endregion
    private Tween[] slotGlowTweens;
    private Material[] _baseMats;
    private Material[] _instancedMats;
    public void PlayAnimation(int index, bool playWildAnim, bool playWildOnSlotZero)
    {
        if(playWildOnSlotZero)
        {
            return;
        }
        slotWithGlowMaterial[index].gameObject.SetActive(true);
        slotWithoutGlowMaterial[index].gameObject.SetActive(false);
        if (index >= 0 && index <= 4)
        {
            slotGlowParticle[index].gameObject.SetActive(true);
        }
        if (index == 7 && playWildAnim)
        {
            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.SetBool(currentResource.winAnimationBoolName, true);
            }
        }
        else if (index != 7)
        {
            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.SetBool(currentResource.winAnimationBoolName, true);
            }
        }

        _canvas.sortingOrder = 3;

        // lazy init arrays
        _baseMats ??= new Material[slotWithGlowMaterial.Length];
        _instancedMats ??= new Material[slotWithGlowMaterial.Length];

        var img = slotWithGlowMaterial[index].GetComponent<Image>();

        // capture the original (shared/base) material once
        if (_baseMats[index] == null)
            _baseMats[index] = img.material; // whatever was assigned in the Inspector

        // make or reuse a private instance for this slot
        if (_instancedMats[index] == null)
            _instancedMats[index] = new Material(_baseMats[index]);

        // assign the unique instance so changes don’t leak to other Images
        img.material = _instancedMats[index];
        var mat = _instancedMats[index];

        // init params + tween
        mat.SetFloat("_CenterSoftness", 1f);
        mat.SetFloat("_CenterRadius", 1f);
        mat.SetFloat("_CenterStrength", 0f);

        slotGlowTweens[index]?.Kill();
        slotGlowTweens[index] = DOTween.To(
            () => mat.GetFloat("_CenterStrength"),
            x => mat.SetFloat("_CenterStrength", x),
            0.7f,
            0.65f
        )
        .SetLoops(-1, LoopType.Yoyo)
        .SetEase(Ease.InOutSine);
    }

    public void StopAnimation(int index)
    {
        slotWithGlowMaterial[index].gameObject.SetActive(false);
        slotWithoutGlowMaterial[index].gameObject.SetActive(true);
        if (index<5)
        {
            slotGlowParticle[index].gameObject.SetActive(false);
        }
        if (_animator != null)
            _animator.SetBool(currentResource.winAnimationBoolName, false);

        _canvas.sortingOrder = 1;

        if (slotGlowTweens != null && index < slotGlowTweens.Length)
        {
            slotGlowTweens[index]?.Kill();
            slotGlowTweens[index] = null;
        }

        var img = slotWithGlowMaterial[index].GetComponent<Image>();

        // reset the instanced mat if it exists
        if (_instancedMats != null && _instancedMats[index] != null)
        {
            var mat = _instancedMats[index];
            mat.SetFloat("_CenterSoftness", 0f);
            mat.SetFloat("_CenterRadius", 0f);
            mat.SetFloat("_CenterStrength", 0f);
        }

        // return to the original material to restore batching
        if (_baseMats != null && _baseMats[index] != null)
            img.material = _baseMats[index];
    }


    public void ShowPaylineWin(float winAmount)
    {
        scaleTween?.Kill();

        paylineWin.text = winAmount.ToString("0.00");
        paylineWin.transform.localScale = Vector3.zero;
        paylineWin.gameObject.SetActive(true);

        paylineWin.transform
            .DOScale(1f, 0.65f)
            .SetEase(Ease.InOutSine);
    }

    public void HidePaylineWin()
    {
        scaleTween?.Kill();
        scaleTween = null;
        paylineWin.gameObject.SetActive(false);
    }

    public void showPaylineWinOnce(float winAmount, int slotIndex)
    {
        paylineWinOnce.text = winAmount.ToString("0.00");
        paylineWinOnce.gameObject.SetActive(true);
        slotsPaylineGlowOnce[slotIndex].gameObject.SetActive(true);

        paylineWinOnce.transform
            .DOScale(1f,0.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                slotsPaylineGlowOnce[slotIndex].gameObject.SetActive(false);
                paylineWinOnce.gameObject.SetActive(false);
            });
    }
}
