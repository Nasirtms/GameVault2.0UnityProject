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
            if (_background == null) _background = GetComponent<Image>();
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
        
        glowImage.enabled = false;
        glowImage.color = new Color(1f, 1f, 1f, 0.5f);
        
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
        PlayFlickerAnimationOnce(1f);
    }

    public void PlayFlickerAnimationOnce(float duration = 0.3f)
    {
        if (glowImage == null || glowImage.sprite == null) return;

        StopFlickerAnimation();

        flickerCoroutine = StartCoroutine(FlickerOnce(duration));
    }

    private IEnumerator FlickerOnce(float duration)
    {
        glowImage.enabled = true;
        yield return new WaitForSeconds(duration);
        glowImage.enabled = false;
    }

    public void StopFlickerAnimation()
    {
        if (flickerCoroutine != null)
            StopCoroutine(flickerCoroutine);

        if (glowImage != null)
            glowImage.enabled = false;
    }

    #endregion
}
