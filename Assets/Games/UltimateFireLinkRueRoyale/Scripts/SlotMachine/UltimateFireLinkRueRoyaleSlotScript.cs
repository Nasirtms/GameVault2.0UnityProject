using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class UltimateFireLinkRueRoyaleSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector] public UltimateFireLinkRueRoyaleSlotType slotType;
    [HideInInspector] public UltimateFireLinkRueRoyaleSlotResource currentResource;

    // Slot Children
    [SerializeField] private GameObject[] normalSlots;
    //[SerializeField] private GameObject[] freeGameSlots;
    //[SerializeField] private TMP_Text multiplier;

    // Slot Animation
    private Animator slotAnimator;
    private String slotAnimationBool;

    // Active Slot Sprites
    private SpriteRenderer[] slotRenderers;

    #endregion

    #region Slot Initialization

    public void Initialize()
    {
        // Pick a NON-WILD symbol on initialization
        //var allResources = StinkinRichSlotMachine.Instance.settings.slotResources;

        //if (allResources != null && allResources.Count > 0)
        //{
        //    List<StinkinRichSlotResource> nonWild = new List<StinkinRichSlotResource>();

        //    foreach (var r in allResources)
        //    {
        //        if (r.slotType != StinkinRichSlotType.TrashForCash || r.slotType != StinkinRichSlotType.Wild || r.slotType!= StinkinRichSlotType.KeysToRiches)
        //        {
        //            nonWild.Add(r);
        //        }
        //    }

        //    if (nonWild.Count > 0)
        //    {
        //        var random = nonWild[UnityEngine.Random.Range(0, nonWild.Count)];
        //        SetType(random);
        //        return;
        //    }
        //}
        GetRandom();
    }


    public void UpdateScale(float scaleX, float scaleY)
    {
        transform.localScale = new Vector3(scaleX, scaleY, 1);
    }

    #endregion

    #region Slot Switching

    public void GetRandom(bool blur = false)
    {
        var allResources = UltimateFireLinkRueRoyaleSlotMachine.Instance.settings.slotResources;

        if (allResources == null || allResources.Count == 0)
            return;

        List<UltimateFireLinkRueRoyaleSlotResource> filtered = new List<UltimateFireLinkRueRoyaleSlotResource>();

        foreach (var r in allResources)
        {
            if (r.slotType != UltimateFireLinkRueRoyaleSlotType.Freegames &&
                r.slotType != UltimateFireLinkRueRoyaleSlotType.Minor &&
                r.slotType != UltimateFireLinkRueRoyaleSlotType.Major &&
                r.slotType != UltimateFireLinkRueRoyaleSlotType.Mega &&
                r.slotType != UltimateFireLinkRueRoyaleSlotType.Mini)
            {
                filtered.Add(r);
            }
        }

        // fallback safety (important)
        if (filtered.Count == 0)
        {
            var randomFallback = allResources[Random.Range(0, allResources.Count)];
            SetType(randomFallback);
            return;
        }

        var random = filtered[Random.Range(0, filtered.Count)];
        SetType(random);
    }

    public void SetType(UltimateFireLinkRueRoyaleSlotResource newType)
    {
        normalSlots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        normalSlots[newType.slotTypeIndex].SetActive(true);
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
        StopAnimation();
        slotAnimator = normalSlots[currentResource.slotTypeIndex].transform.GetComponentInParent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }

    public void PlayWildShiftAnimation()
    {
        slotAnimator = normalSlots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.SetBool("Wild-Shift", true);
    }

    #endregion

    #region Slot Layering

    public void SetSpriteToPayline()
    {
        slotRenderers = normalSlots[currentResource.slotTypeIndex].gameObject.GetComponentsInChildren<SpriteRenderer>();

        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Payline Slot";
        }
    }

    public void SetSpriteToDefault()
    {
        slotRenderers = normalSlots[currentResource.slotTypeIndex].gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Default";
        }
    }

    public void SetSlotActive(bool isActive)
    {
        if (normalSlots == null || normalSlots.Length == 0)
            return;

        int index = currentResource.slotTypeIndex;

        if (index < 0 || index >= normalSlots.Length)
            return;

        GameObject currentSymbolObject = normalSlots[index];
        if (currentSymbolObject == null)
            return;

        var renderers = currentSymbolObject.GetComponentsInChildren<SpriteRenderer>(true);
        float targetAlpha = isActive ? 1f : 0f;

        foreach (var r in renderers)
        {
            Color c = r.color;
            c.a = targetAlpha;
            r.color = c;
        }
    }

    #endregion
}
