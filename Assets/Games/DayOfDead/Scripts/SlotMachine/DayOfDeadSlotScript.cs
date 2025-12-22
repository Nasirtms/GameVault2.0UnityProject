using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayOfDeadSlotScript : MonoBehaviour
{
    #region Variables
    //public SpriteMaskInteraction interaction = SpriteMaskInteraction.VisibleInsideMask;

    public int reelIndex;
    public int slotIndex;

    // Slot Resource
    [HideInInspector] public DayOfDeadSlotType slotType;
    [HideInInspector] public DayOfDeadSlotResource currentResource;

    // Slot Children
    [SerializeField] private GameObject[] slots;

    // Slot Animation
    public Animator slotAnimator;
    private String slotAnimationBool;

    // Active Slot Sprites
    private SpriteRenderer[] slotRenderers;
    public bool isLocked = false;

    public GameObject wildParticle;
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

        var random = DayOfDeadSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, DayOfDeadSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random);
    }

    public void SetType(DayOfDeadSlotResource newType)
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
        //SetSpriteToDefault();
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }
    #endregion

    #region Wild particles

    public IEnumerator MoveParticles(Vector3 targetPosition)
    {
        Vector3 movePos = transform.InverseTransformPoint(targetPosition);
        yield return MoveAndResetParticles(movePos);
    }
    private IEnumerator MoveAndResetParticles(Vector3 targetPosition)
    {
        Vector3 originalPosition = wildParticle.transform.localPosition;

        wildParticle.SetActive(true);

        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(0.25f)
           .Append(wildParticle.transform
               .DOLocalMove(targetPosition, 1f)
               .SetEase(Ease.Linear))
           .AppendInterval(0.75f)
           .OnComplete(() =>
           {
               wildParticle.SetActive(false);
               wildParticle.transform.localPosition = originalPosition;
           });

        yield return seq.WaitForCompletion();
    }
    #endregion
}