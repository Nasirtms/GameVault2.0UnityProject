using Coffee.UIEffects;
using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class StickyPiggySlotScript : MonoBehaviour
{
    #region Variables

    public int reelIndex;
    public int slotIndex;

    // Slot Resource
    [HideInInspector] public StickyPiggySlotType slotType;
    [HideInInspector] public StickyPiggySlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;

    private SortingGroup textSortingGroup;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private String slotAnimationBool;

    public bool isLocked = false;
    public GameObject key;
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
        var random = StickyPiggySlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, StickyPiggySlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(StickyPiggySlotResource newType, bool finalResult)
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
        //SetSpriteToPayline();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);

    }

    [ContextMenu("Stop Animation")]
    public void StopAnimation()
    {
        //SetSpriteToDefault();
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }
    [ContextMenu("Start Animation")]
    public void PlayBonusAnimation()
    {
        StopBonusAnimation();
        //SetSpriteToPayline();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.SetBool("BonusKey", true);

    }

    [ContextMenu("Stop Animation")]
    public void StopBonusAnimation()
    {
        //SetSpriteToDefault();
        if (slotAnimator != null)
        {
            slotAnimator.SetBool("BonusKey", false);
        }
    }
    #endregion

    #region Move BonusKey
    public IEnumerator MoveKey(Vector3 targetPosition, System.Action onComplete = null)
    {
        //Debug.Log("Target Pos: " + targetPosition);  
        yield return StartCoroutine(MoveAndResetKey(targetPosition, onComplete));
    }

    private IEnumerator MoveAndResetKey(Vector3 targetLocalPosition, System.Action onComplete)
    {
        Vector3 originalPosition = key.transform.position;
        //Debug.Log("LovKumar Particles Original Pos : " + originalPosition);
        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(0.25f)
           .Append(key.transform
               .DOMove(targetLocalPosition, .9f)
               .SetEase(Ease.InBack))
           .Join(key.transform
               .DOScale(0.9f, .9f)
               .SetEase(Ease.InBack))
           .OnComplete(() =>
           {
               onComplete?.Invoke();
               key.SetActive(false);
               key.transform.position = originalPosition;
           });
        yield return seq.WaitForCompletion();

    }
    #endregion
}