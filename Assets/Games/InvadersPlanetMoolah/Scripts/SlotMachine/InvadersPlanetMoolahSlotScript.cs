using Coffee.UIEffects;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class InvadersPlanetMoolahSlotScript : MonoBehaviour
{
    #region Variables

    public int reelIndex;
    public int slotIndex;

    [HideInInspector] public InvadersPlanetMoolahSlotType slotType;
    [HideInInspector] public InvadersPlanetMoolahSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    [SerializeField] private GameObject[] slots;

    private SortingGroup textSortingGroup;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private string slotAnimationBool;

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
        var random = InvadersPlanetMoolahSlotMachine.Instance.settings.slotResources[
            UnityEngine.Random.Range(0, InvadersPlanetMoolahSlotMachine.Instance.settings.slotResources.Count)
        ];

        SetType(random, false);
    }

    public void SetType(InvadersPlanetMoolahSlotResource newType, bool finalResult)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        currentResource = newType;
        slotType = newType.slotType;
        slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
        SetSpriteToPayline();

        // StopAnimation();
        // slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        // slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        SetSpriteToDefault();

        // if (slotAnimator != null)
        // {
        //     slotAnimator.SetBool(slotAnimationBool, false);
        // }
    }

    #endregion

    #region Slot Layering

    public void SetSpriteToPayline()
    {
        slotRenderers = slots[currentResource.slotTypeIndex].GetComponentsInChildren<SpriteRenderer>();

        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Payline Slot";
        }
    }

    public void SetSpriteToDefault()
    {
        slotRenderers = slots[currentResource.slotTypeIndex].GetComponentsInChildren<SpriteRenderer>();

        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Default";
        }
    }

    #endregion
}