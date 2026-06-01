using System;
using TMPro;
using UnityEngine;

public class WolfMoonLinkSlotScript : MonoBehaviour
{
    #region Variables

    public int slotIndex;

    [HideInInspector] public WolfMoonLinkSlotType slotType;
    [HideInInspector] public WolfMoonLinkSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

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
        if (slotIndex % 2 == 0)
        {
            if (WolfMoonLinkSlotMachine.CachedEmptySymbol.HasValue)
            {
                SetType(WolfMoonLinkSlotMachine.CachedEmptySymbol.Value);
            }
        }
        else
        {
            if (WolfMoonLinkSlotMachine.CachedRealSymbols != null &&
                WolfMoonLinkSlotMachine.CachedRealSymbols.Count > 0)
            {
                var random = WolfMoonLinkSlotMachine.CachedRealSymbols[
                    UnityEngine.Random.Range(0, WolfMoonLinkSlotMachine.CachedRealSymbols.Count)];

                SetType(random);
            }
        }
    }

    public void SetType(WolfMoonLinkSlotResource newType)
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