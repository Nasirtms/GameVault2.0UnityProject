using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PandaFortuneSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector] public PandaFortuneSlotType slotType;
    [HideInInspector] public PandaFortuneSlotResource currentResource;

    // Slot Children
    [SerializeField] private GameObject[] slots;

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
        var allResources = PandaFortuneSlotMachine.Instance.settings.slotResources;

        if (allResources != null && allResources.Count > 0)
        {
            List<PandaFortuneSlotResource> nonWild = new List<PandaFortuneSlotResource>();

            foreach (var r in allResources)
            {
                if (r.slotType != PandaFortuneSlotType.Wild)
                    nonWild.Add(r);
            }

            if (nonWild.Count > 0)
            {
                var random = nonWild[UnityEngine.Random.Range(0, nonWild.Count)];
                SetType(random);
                return;
            }
        }
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
        var random = PandaFortuneSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, PandaFortuneSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random);
    }

    public void SetType(PandaFortuneSlotResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);

        if (gameObject.name.Equals("PandaFrotuneSlots (4)") || gameObject.name.Equals("PandaFrotuneSlots"))
        {
            if(newType.slotTypeIndex == 5)
            {
                GetRandom();
            }
        }
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
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

    public void PlayWildShiftAnimation()
    {
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.SetBool("Wild-Shift", true);
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

    public void SetWildPosition()
    {
        var wild = slots[currentResource.slotTypeIndex].transform;
        wild.position = new Vector3(wild.position.x, 2.5f, wild.position.z);

        wild.DOMoveY(0, 0.4f).SetEase(Ease.OutCubic);
    }

    public void SetSlotActive(bool isActive)
    {
        if (slots == null || slots.Length == 0)
            return;

        int index = currentResource.slotTypeIndex;

        if (index < 0 || index >= slots.Length)
            return;

        GameObject currentSymbolObject = slots[index];
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
