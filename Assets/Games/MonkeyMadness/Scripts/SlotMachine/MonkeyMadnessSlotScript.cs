using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MonkeyMadnessSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Details
    private int index;
    [HideInInspector] public MonkeyMadnessSlotType type;
    [HideInInspector] public MonkeyMadnessSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;
    //public Mask slotMask;
    //public Mask slotGlowMask;
    // Components
    private MonkeyMadnessReelScript _parent;
    private Image _background;
    private RectTransform _rectTransform;

    // UI References
    [SerializeField] private Image glowImage;

    // Coroutines
    private Coroutine flickerCoroutine;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (MonkeyMadnessSlotMachine.Instance != null)
            MonkeyMadnessSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (MonkeyMadnessSlotMachine.Instance != null)
            MonkeyMadnessSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    private Image Background
    {
        get
        {
            if (_background == null) _background = transform.GetChild(1).GetComponent<Image>();
            return _background;
        }
    }

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector3(scale, scale, 1);
    }

    public void Initialize(MonkeyMadnessReelScript parentReel, int index)
    {
        this._parent = parentReel;
        this.index = index;

        _rectTransform = GetComponent<RectTransform>();

        glowImage.gameObject.SetActive(false);
        
        GetRandom();
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    #region Slot Switching

    public void SetType(MonkeyMadnessSlotResource newType, bool useBlurred = false)
    {
        this.currentResource = newType;
        this.Background.sprite = useBlurred && newType.blurredImage != null
            ? newType.blurredImage
            : newType.normalImage;
        this.type = newType.type;
        this.glowImage.sprite = newType.glowImage;
    }
    
    public void GetRandom()
    {
        if (index % 2 != 0) // empty slot
        {
            if (MonkeyMadnessSlotMachine.CachedEmptySymbol.HasValue)
                SetType(MonkeyMadnessSlotMachine.CachedEmptySymbol.Value);
        }
        else // real symbol
        {
            var random = MonkeyMadnessSlotMachine.CachedRealSymbols[
                Random.Range(0, MonkeyMadnessSlotMachine.CachedRealSymbols.Count)];
            SetType(random);
        }
    }

    #endregion

    #region Slot Animation

    [ContextMenu("Play Glow")]
    public void PlayGlow()
    {
        PlayFlickerAnimation(1f);
    }

    private Tween flickerTween;

    public void PlayFlickerAnimation(float duration = 0.3f)
    {
        if (glowImage == null) return;

        StopFlickerAnimation();

        glowImage.gameObject.SetActive(true);

        flickerTween = glowImage
            .DOFade(1f, duration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.Linear);
    }

    public void StopFlickerAnimation()
    {
        if (flickerTween != null && flickerTween.IsActive())
        {
            flickerTween.Kill();
        }

        if (glowImage != null)
        {
            glowImage.gameObject.SetActive(false);
        }
    }

    #endregion
}
