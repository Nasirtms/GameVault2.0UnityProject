using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class LuckySevenSlotScript : MonoBehaviour
{
    #region Variables

    public int slotIndex;
    // Slot Resource
    [HideInInspector] public LuckySevenSlotType slotType;
    [HideInInspector] public LuckySevenSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private String slotAnimationBool;

    #endregion

    #region Unity Methods
    public void Initialize()
    {
        GetRandom();
    }
    public void UpdateScale(float scaleX, float scaleY)
    {
        transform.localScale = new Vector3(scaleX, scaleY, 1);
    }
    #endregion

    #region Slot Settings
    public void GetRandom(bool blur = false)
    {
        //var random = WildXTrioSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, WildXTrioSlotMachine.Instance.settings.slotResources.Count)];
        //SetType(random, false);
        if (slotIndex % 2 == 0)
        {
            if (LuckySevenSlotMachine.CachedEmptySymbol.HasValue)
            {
                SetType(LuckySevenSlotMachine.CachedEmptySymbol.Value);
            }
        }
        else
        {
            if (LuckySevenSlotMachine.CachedRealSymbols != null &&
                LuckySevenSlotMachine.CachedRealSymbols.Count > 0)
            {
                var random = LuckySevenSlotMachine.CachedRealSymbols[
                    UnityEngine.Random.Range(0, LuckySevenSlotMachine.CachedRealSymbols.Count)];
                SetType(random);
            }
        }
    }

    public void SetType(LuckySevenSlotResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);
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