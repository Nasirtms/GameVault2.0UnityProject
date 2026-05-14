using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RichLittlePiggiesSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector] public RichLittlePiggiesSlotType slotType;
    [HideInInspector] public RichLittlePiggiesSlotResource currentResource;

    // Slot Children
    [SerializeField] private GameObject[] slots;
    [SerializeField] public SpriteRenderer revealSlotSprite;

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
        var random = RichLittlePiggiesSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, RichLittlePiggiesSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random);
    }

    public void SetType(RichLittlePiggiesSlotResource newType)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);
    }

    public void SetRevealSymbolTrue()
    {
        revealSlotSprite.gameObject.SetActive(true);
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
        //if (!isValidSlotForAnimation(slotType)) return;

        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
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

    public void RevealSlots()
    {
        var anim = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        anim.SetBool("reveal", true);
    }


    public void EndRevealSlotAniamtion()
    {
        StartCoroutine(EndRevealSlotAniamtionRoutine());
    }   

    private IEnumerator EndRevealSlotAniamtionRoutine()
    {
        var anim = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        anim.SetBool("reveal", false);
        revealSlotSprite.enabled = false;
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        revealSlotSprite.gameObject.SetActive(false);
        revealSlotSprite.enabled = true;
        revealSlotSprite.transform.localScale = Vector3.one;
        Color c = revealSlotSprite.color;
        c.a = 1f;
        revealSlotSprite.color = c;
    }


    #endregion
}
