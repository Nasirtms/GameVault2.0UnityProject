using Coffee.UIEffects;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GoldRushGusSlotScript : MonoBehaviour
{
    #region Variables

    public int reelIndex;
    public int slotIndex;

    // Slot Resource
    [HideInInspector] public GoldRushGusSlotType slotType;
    [HideInInspector] public GoldRushGusSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;

    private SortingGroup textSortingGroup;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private String slotAnimationBool;

    public GameObject slotBorder;
    private Animator slotBorderAnimator;

    #endregion

    #region Unity Methods
    public void Initialize()
    {
        GetRandom();
    }

    #endregion

    #region Slot Settings
    public void UpdateScale(float scaleX, float scaleY)
    {
        transform.localScale = new Vector3(scaleX, scaleY, 1);
    }
    public void GetRandom(bool blur = false)
    {
        var random = GoldRushGusSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, GoldRushGusSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(GoldRushGusSlotResource newType, bool finalResult)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
        StopAnimation();
        SetSpriteToPayline();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.enabled = true;
        slotAnimator.SetBool(slotAnimationBool, true);

    }
    public void StopAnimation()
    {
        SetSpriteToDefault();
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }
    public void PlayBorderAnimation()
    {
        StopBorderAnimation();
        slotBorder.SetActive(true);
        slotBorderAnimator = transform.GetChild(14).GetComponent<Animator>();
        slotBorderAnimator.enabled = true;
        slotBorderAnimator.SetBool("payline", true);
    }
    public void StopBorderAnimation()
    {
        if (slotBorderAnimator != null)
        {
            slotBorderAnimator.SetBool("payline", false);
            slotBorder.SetActive(false);
            slotBorderAnimator.enabled = false;
        }
    }
    #endregion

    #region Slot Layering

    public void SetSpriteToPayline()
    {
        slotRenderers = slots[currentResource.slotTypeIndex].gameObject.GetComponentsInChildren<SpriteRenderer>();

        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Payline Slot";
        }
    }

    public void SetSpriteToDefault()
    {
        slotRenderers = slots[currentResource.slotTypeIndex].gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Default";
        }
    }

    #endregion
}