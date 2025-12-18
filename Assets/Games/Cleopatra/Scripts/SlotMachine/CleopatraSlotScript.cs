using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Canvas))]
public class CleopatraSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Details
    private int index;
    [HideInInspector] public CleopatraSlotType type;
    [HideInInspector] public CleopatraSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Components
    private CleopatraReelScript _parent;
    private Image _background;
    private RectTransform _rectTransform;
    private Animator _animator;
    private Canvas _canvas;

    [Header("UI References")]
    [SerializeField] private GameObject backgroundOverlay;       // Black semi-transparent BG
    [SerializeField] private GameObject textGroup;               // Container for Line Win + Border
    [SerializeField] private TMP_Text winText;                   // Win amount or label
    [SerializeField] private Image innerBorder;                  // Border under text group
    [SerializeField] private Image outerBorder;                  // Standalone border

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (CleopatraSlotMachine.Instance != null)
            CleopatraSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (CleopatraSlotMachine.Instance != null)
            CleopatraSlotMachine.Instance.StopReelProcess -= HandleStart;
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

    public void Initialize(CleopatraReelScript parentReel, int index)
    {
        this._parent = parentReel;
        this.index = index;

        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponent<Canvas>();

        _animator = GetComponent<Animator>();
        if (_animator != null)
            _animator.enabled = false;

        _canvas.overrideSorting = false;

        HideAllVisualOverlays();
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

    private CleopatraSlotResource GetRandomSlot()
    {
        var random = CleopatraSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, CleopatraSlotMachine.Instance.settings.resourcesList.Count)];

        return random;
    }

    public void ShowUniqueRandomVisual(HashSet<CleopatraSlotType> excludedTypes)
    {
        CleopatraSlotResource random;
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

    public void SetType(CleopatraSlotResource newType)
    {
        this.currentResource = newType;
        this.Background.sprite = newType.background;
        this.type = newType.type;
    }

    public void GetRandom()
    {
        var random = CleopatraSlotMachine.Instance.settings.resourcesList[
                Random.Range(0, CleopatraSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation(string trigger)
    {
        if (_animator == null) return;

        _animator.enabled = true;
        _animator.Rebind();
        _animator.Update(0f);
        _animator.SetTrigger(trigger);
    }

    public float GetClipLengthByName(string clipName)
    {
        if (_animator.runtimeAnimatorController == null) return 0.5f;

        foreach (var clip in _animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        return 0.5f; // fallback
    }

    public void ResetAnimator()
    {
        if (_animator != null)
            _animator.enabled = false;

        this.Background.sprite = Background.sprite;
    }

    #endregion

    #region Slot Borders and Text

    public void SetOverlayVisible(bool status)
    {
        if (backgroundOverlay != null)
            backgroundOverlay.SetActive(status);
    }

    public void SetBorderVisible(bool status) 
    {
        if (outerBorder != null)
            outerBorder.gameObject.SetActive(status);
    }

    public void SetTextGroupVisible(bool status)
    {
        if (textGroup != null)
            textGroup.SetActive(status);
    }

    public void SetWinText(string text)
    {
        if (winText != null)
        {
            winText.text = text;
        }
    }

    public void SetBorderColor(Color color)
    {
        if (innerBorder != null)
            innerBorder.color = color;
        if (outerBorder != null)
            outerBorder.color = color;
    }

    public void SetSortingLayer(int layer, bool enable)
    {
        _canvas.overrideSorting = enable;
        _canvas.sortingOrder = layer;
    }

    public void HideAllVisualOverlays()
    {
        SetOverlayVisible(false);
        SetTextGroupVisible(false);
        SetBorderColor(Color.clear);
    }

    #endregion
}
