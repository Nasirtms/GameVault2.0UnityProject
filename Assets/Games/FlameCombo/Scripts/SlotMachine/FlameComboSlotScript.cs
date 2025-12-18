using Coffee.UIEffects;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[RequireComponent(typeof(Image))]
public class FlameComboSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Children
    [SerializeField] private GameObject[] slots;
    private Animator slotAnimator;
    private String slotAnimationBool;

    private Tween animationTween;
    // Slot Details
    private int index;
    [HideInInspector] public FlameComboSlotType type;
    public FlameComboSlotResource currentResource;

    [HideInInspector] public bool isResultSet = false;

    // Components
    public Image icon;
    private FlameComboReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (FlameComboSlotMachine.Instance != null)
            FlameComboSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (FlameComboSlotMachine.Instance != null)
            FlameComboSlotMachine.Instance.StopReelProcess -= HandleStart;
    }

    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(FlameComboReelScript parentReel, int index)
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

    public void SetType(FlameComboSlotResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotAnimationBool = newType.slotAnimationBool;
        this.type = newType.type;

        slots[newType.slotTypeIndex].SetActive(true);
    }
    public void GetRandom()
    {
        var random = FlameComboSlotMachine.Instance.settings.resourcesList[UnityEngine.Random.Range(0, FlameComboSlotMachine.Instance.settings.resourcesList.Count)];
        SetType(random);
    }

    #endregion

    #region Slot Animation
    [ContextMenu("Start Animation")]
    public void PlayAnimation()
    {
        StopAnimation();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }
    #endregion
}