using Coffee.UIEffects;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class IrishPotLuckSlotScript : MonoBehaviour
{
    #region Variables

    public int reelIndex;
    public int slotIndex;

    // Slot Resource
    [HideInInspector] public IrishPotLuckSlotType slotType;
    [HideInInspector] public IrishPotLuckSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;

    private SortingGroup textSortingGroup;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private String slotAnimationBool;

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
        var random = IrishPotLuckSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, IrishPotLuckSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(IrishPotLuckSlotResource newType, bool finalResult)
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
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);
    }
    public void StopAnimation()
    {
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }
    #endregion

}