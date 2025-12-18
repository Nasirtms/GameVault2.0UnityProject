using System;
using UnityEngine;

public class ZombieParadiseSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector] public ZombieParadiseSlotType slotType;
    [HideInInspector] public ZombieParadiseSlotResource currentResource;
    
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
        var random = ZombieParadiseSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, ZombieParadiseSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random);
    }

    public void SetType(ZombieParadiseSlotResource newType)
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
        //if (!isValidSlotForAnimation(slotType)) return;

        slotAnimator = slots[currentResource.slotTypeIndex].GetComponent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        //if (!isValidSlotForAnimation(slotType)) return;

        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
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

    //#region Helper Functions

    //private bool isValidSlotForAnimation(ZombieParadiseSlotType slotType)
    //{
    //    switch (slotType)
    //    {
    //        case ZombieParadiseSlotType.Nine:
    //        case ZombieParadiseSlotType.Ten:
    //        case ZombieParadiseSlotType.Jack:
    //        case ZombieParadiseSlotType.Queen:
    //        case ZombieParadiseSlotType.King:
    //        case ZombieParadiseSlotType.Ace:
    //        case ZombieParadiseSlotType.Spider:
    //            return false;
    //        default:
    //            return true;
    //    }
    //}

    //#endregion
}
