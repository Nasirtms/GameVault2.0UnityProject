using Coffee.UIEffects;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RedHotTrippleSlotScript : MonoBehaviour
{
    #region Variables

    [SerializeField] private GameObject[] slots;

    // Slot Details
    private int index;
    [HideInInspector] public RedHotTrippleSlotType type;
    [HideInInspector] public RedHotTrippleSlotResource currentResource;

    private Animator slotAnimator;
    private String slotAnimationBool;

    // Components
    private Image icon;
    private RedHotTrippleReelScript _parent;
    private RectTransform _rectTransform;

    #endregion

    #region Unity Methods
    private void Start()
    {
        if (RedHotTrippleSlotMachine.Instance != null)
            RedHotTrippleSlotMachine.Instance.StopReelProcess += HandleStart;
    }

    private void OnDestroy()
    {
        if (RedHotTrippleSlotMachine.Instance != null)
            RedHotTrippleSlotMachine.Instance.StopReelProcess -= HandleStart;
    }
    #endregion

    #region Slot Settings

    public void UpdateScale(float scale)
    {
        _rectTransform.localScale = new Vector2(scale, scale);
    }

    public void Initialize(RedHotTrippleReelScript parentReel, int index)
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

    public void SetType(RedHotTrippleSlotResource newType)
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
            var random = RedHotTrippleSlotMachine.CachedRealSymbols[
             UnityEngine.Random.Range(0, RedHotTrippleSlotMachine.CachedRealSymbols.Count)];
            SetType(random);
        }
        else
        {
            if (index % 2 != 0) // empty slot
            {
                if (RedHotTrippleSlotMachine.CachedEmptySymbol.HasValue)
                    SetType(RedHotTrippleSlotMachine.CachedEmptySymbol.Value);
            }
            else // real symbol
            {
                var random = RedHotTrippleSlotMachine.CachedRealSymbols[
                    UnityEngine.Random.Range(0, RedHotTrippleSlotMachine.CachedRealSymbols.Count)];
                SetType(random);
            }
        }
    }

    #region Slot Animation

    public void StartAnimation()
    {
        StopAnimation();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.Rebind();
        slotAnimator.Update(0f);
        slotAnimator.enabled = true;
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
            slotAnimator.Update(0f);
        }
    }

    private void OnDisable()
    {
        StopAnimation();
    }
    #endregion
}