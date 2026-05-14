using Coffee.UIEffects;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WheelOfFortuneSlotScript : MonoBehaviour
{
    #region Variables

    [SerializeField] private GameObject[] slots;

    // Slot Details
    private int index;
    [HideInInspector] public WheelOfFortuneSlotType type;
    [HideInInspector] public WheelOfFortuneSlotResource currentResource;

    //[HideInInspector] public bool isResultSet = false;
    private Animator slotAnimator;
    private String slotAnimationBool;

    // Components
    private Image icon;
    private WheelOfFortuneReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods
    private void Start()
    {
        if (WheelOfFortuneSlotMachine.Instance != null)
            WheelOfFortuneSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (WheelOfFortuneSlotMachine.Instance != null)
            WheelOfFortuneSlotMachine.Instance.StopReelProcess -= HandleStart;
    }
    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(WheelOfFortuneReelScript parentReel, int index)
    {
        this.index = index;
        this._parent = parentReel;
        _rectTransform = GetComponent<RectTransform>();
        icon = transform.GetChild(0).GetComponent<Image>();
        GetRandom(false);
    }

    private void HandleStart()
    {
        StopAllCoroutines();
    }

    #endregion

    public void SetType(WheelOfFortuneSlotResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);
        currentResource = newType;
        slots[newType.slotTypeIndex].gameObject.SetActive(true);
        type = newType.type;
        this.slotAnimationBool = newType.slotAnimationBool;
    }
    public void GetRandom(bool finalResult)
    {

        if (finalResult)
        {
            var random = WheelOfFortuneSlotMachine.CachedRealSymbols[
             UnityEngine.Random.Range(0, WheelOfFortuneSlotMachine.CachedRealSymbols.Count)];
            SetType(random);
        }
        else
        {
            if (index % 2 != 0) // empty slot
            {
                if (WheelOfFortuneSlotMachine.CachedEmptySymbol.HasValue)
                    SetType(WheelOfFortuneSlotMachine.CachedEmptySymbol.Value);
            }
            else // real symbol
            {
                var random = WheelOfFortuneSlotMachine.CachedRealSymbols[
                    UnityEngine.Random.Range(0, WheelOfFortuneSlotMachine.CachedRealSymbols.Count)];
                SetType(random);
            }
        }
    }

    #region Slot Animation

    public void StartAnimation()
    {
        StopAnimation();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.enabled = true;
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }

    private void OnDisable()
    {
        StopAnimation();
    }
    #endregion
}