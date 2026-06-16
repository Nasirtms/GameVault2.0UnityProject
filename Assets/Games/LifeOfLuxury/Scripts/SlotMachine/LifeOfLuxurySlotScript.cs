using Coffee.UIEffects;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LifeOfLuxurySlotScript : MonoBehaviour
{
    #region Variables

    public int reelIndex;
    public int slotIndex;

    // Slot Resource
    [HideInInspector] public LifeOfLuxurySlotType slotType;
    [HideInInspector] public LifeOfLuxurySlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;

    private SortingGroup textSortingGroup;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    //private String slotAnimationBool;
    private string currentAnimationBool;

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
        var random = LifeOfLuxurySlotMachine.Instance.settings.slotResources[
            UnityEngine.Random.Range(0, LifeOfLuxurySlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(LifeOfLuxurySlotResource newType, bool finalResult)
    {
        StopAnimation();
        DisableCurrentSlot();

        currentResource = newType;
        slotType = newType.slotType;

        EnableSlotByGameMode(newType.slotTypeIndex);
        //slots[currentResource.slotTypeIndex].SetActive(false);
        //this.currentResource = newType;
        //this.slotType = newType.slotType;
        //this.slotAnimationBool = newType.slotAnimationBool;
        //slots[newType.slotTypeIndex].SetActive(true);
    }
    private void DisableCurrentSlot()
    {
        if (slots == null || slots.Length == 0)
            return;

        if (currentResource.slotTypeIndex < 0 || currentResource.slotTypeIndex >= slots.Length)
            return;

        GameObject currentSlot = slots[currentResource.slotTypeIndex];

        if (currentSlot != null)
            currentSlot.SetActive(false);
    }
    private void EnableSlotByGameMode(int slotTypeIndex)
    {
        if (slotTypeIndex < 0 || slotTypeIndex >= slots.Length)
            return;

        GameObject slotParent = slots[slotTypeIndex];

        if (slotParent == null)
            return;

        slotParent.SetActive(true);

        bool isFreeGame = LifeOfLuxurySlotMachine.Instance.isFreeGame;

        // child 0 = Base Game visual
        // child 1 = Free Game visual
        if (slotParent.transform.childCount > 0)
            slotParent.transform.GetChild(0).gameObject.SetActive(!isFreeGame);

        if (slotParent.transform.childCount > 1)
            slotParent.transform.GetChild(1).gameObject.SetActive(isFreeGame);

        currentAnimationBool = isFreeGame ? currentResource.freeGameAnimationBool : currentResource.baseGameAnimationBool;
    }
    #endregion

    #region Slot Animation
    public void PlayAnimation()
    {
        StopAnimation();

        GameObject activeVisual = GetActiveSlotVisual();

        if (activeVisual == null)
            return;

        slotAnimator = activeVisual.GetComponentInParent<Animator>();

        if (slotAnimator != null && !string.IsNullOrEmpty(currentAnimationBool))
        {
            slotAnimator.SetBool(currentAnimationBool, true);
        }
    }
    public void StopAnimation()
    {
        if (slotAnimator != null && !string.IsNullOrEmpty(currentAnimationBool))
        {
            slotAnimator.SetBool(currentAnimationBool, false);
        }
    }

    private GameObject GetActiveSlotVisual()
    {
        if (currentResource.slotTypeIndex < 0 || currentResource.slotTypeIndex >= slots.Length)
            return null;

        GameObject slotParent = slots[currentResource.slotTypeIndex];

        if (slotParent == null)
            return null;

        for (int i = 0; i < slotParent.transform.childCount; i++)
        {
            GameObject child = slotParent.transform.GetChild(i).gameObject;

            if (child.activeSelf)
                return child;
        }

        return slotParent;
    }
    #endregion
}