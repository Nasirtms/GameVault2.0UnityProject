using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class WildXTrioSlotScript : MonoBehaviour
{
    #region Variables

    public int slotIndex;
    // Slot Resource
    [HideInInspector] public WildXTrioSlotType slotType;
    [HideInInspector] public WildXTrioSlotResource currentResource;
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
            if (WildXTrioSlotMachine.CachedEmptySymbol.HasValue)
            {
                SetType(WildXTrioSlotMachine.CachedEmptySymbol.Value);
            }
        }
        else
        {
            if (WildXTrioSlotMachine.CachedRealSymbols != null &&
                WildXTrioSlotMachine.CachedRealSymbols.Count > 0)
            {
                var random = WildXTrioSlotMachine.CachedRealSymbols[
                    UnityEngine.Random.Range(0, WildXTrioSlotMachine.CachedRealSymbols.Count)];

                SetType(random);
            }
        }
    }

    public void SetType(WildXTrioSlotResource newType)
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