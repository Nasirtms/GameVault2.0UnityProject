using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
public class SaharaRichesSlotScript : MonoBehaviour
{
    #region Variables

    public int reelIndex;
    public int slotIndex;
    // Slot Resource
    [HideInInspector] public SaharaRichesSlotType slotType;
    [HideInInspector] public SaharaRichesSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;
    [SerializeField] public GameObject textBox; 
    [SerializeField] public TextMeshPro winText;
    [SerializeField] public GameObject PriceBox;
    public GameObject wildParticle;
    private SortingGroup textSortingGroup;
    public float wildCollectAmount;

    //Coin Collect
    public TextMeshPro CC_Text;
    public float CC_Amount;

    //Diamond Collect
    public TextMeshPro diamond_Text;
    private float diamond_Amount;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private String slotAnimationBool;

    //FreeSpin Multipliers
    public int activeFreeSpinIndex;
    public List<GameObject> freeSpinMultipliers;
    public bool isLocked = false;
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
        var random = SaharaRichesSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, SaharaRichesSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(SaharaRichesSlotResource newType, bool finalResult)
    {
        wildCollectAmount = 0f;
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);

        if (SaharaRichesSlotMachine.Instance.isCCSlot(slotType))
        {
            CC_Text = slots[newType.slotTypeIndex].GetComponentInChildren<TextMeshPro>();

            if (finalResult)
            {
                CC_Text.text = $"${CC_Amount.ToString("F2")}";
            }
            else
            {
                float rand = Random.Range(0f, 1f);
                float bet = SaharaRichesSlotMachine.Instance.CurrentBet();
                float multiplier;

                if (rand >= 0f && rand <= 0.1f) { multiplier = 1; }
                else if (rand > 0.1f && rand <= 0.2f) { multiplier = 2; }
                else if (rand > 0.2f && rand <= 0.3f) { multiplier = 3; }
                else if (rand > 0.3f && rand <= 0.4f) { multiplier = 4; }
                else if (rand > 0.4f && rand <= 0.5f) { multiplier = 5; }
                else if (rand > 0.5f && rand <= 0.6f) { multiplier = 6; }
                else if (rand > 0.6f && rand <= 0.7f) { multiplier = 10; }
                else if (rand > 0.7f && rand <= 0.85f) { multiplier = 15; }
                else { multiplier = 20; }

                CC_Text.text = $"${(bet * multiplier).ToString("F2")}";
            }
        }
    }

    public void UpdateCCAmount(float CCAmount)
    {
        CC_Amount = CCAmount;
    }
    public void UpdateDiamondAmount(float diamondAmount)
    {
        diamond_Amount = diamondAmount;
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
        SetSpriteToDefault();
        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }
    #endregion

    #region Slot Borders and Text

    public void SetTextGroupVisible(bool status)
    {
        if (textBox != null)
            textBox.SetActive(status);
    }

    public void SetWinText(string text)
    {
        if (winText != null)
        {
            winText.text = $"{text:F2}";
        }
    }

    public void HideAllVisualOverlays()
    {
        SetTextGroupVisible(false);
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

    #region Wild Effects

    #region Box

    public void MoveBox(Vector3 targetPosition)
    {
        Vector3 movePos = transform.InverseTransformPoint(targetPosition);
        StartCoroutine(MoveAndResetBox(movePos));
    }

    public void MoveCCParticles(Vector3 targetPosition)
    {
        Vector3 movePos = transform.InverseTransformPoint(targetPosition);
        StartCoroutine(MoveAndResetCCParticles(movePos));
    }
    public void MoveDiamondParticles(Vector3 targetPosition)
    {
        Vector3 movePos = transform.InverseTransformPoint(targetPosition);
        StartCoroutine(MoveAndResetDiamondParticles(movePos));
    }
    public void UpdateBox(float CC_Amount, GameObject other)
    {
        var targetbox = SaharaRichesPaylineController.Instance.Target_Text;
        float originalScale = targetbox.transform.localScale.x;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(other.transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.Linear));
        sequence.Join(targetbox.transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.Linear));

        wildCollectAmount += CC_Amount;
        SaharaRichesPaylineController.Instance.Target_Text.text = $"${wildCollectAmount.ToString("F2")}";

        sequence.AppendInterval(0.2f);

        sequence.Append(other.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
        sequence.Join(targetbox.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
    }

    private IEnumerator MoveAndResetBox(Vector3 targetPosition)
    {
        Vector3 originalPosition = PriceBox.transform.localPosition;
        Vector3 originalScale = PriceBox.transform.localScale;

        PriceBox.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        PriceBox.transform.DOLocalMove(targetPosition, 0.5f)
            .SetEase(Ease.Linear);

        PriceBox.transform.DOScale(originalScale * 1.2f, 0.1f)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            PriceBox.transform.DOScale(originalScale, 0.1f)
                .SetEase(Ease.InQuad);
        });
        yield return new WaitForSeconds(0.75f);

        PriceBox.SetActive(false);

        PriceBox.transform.localPosition = originalPosition;
        PriceBox.transform.localScale = originalScale;
    }

    private IEnumerator MoveAndResetCCParticles(Vector3 targetPosition)
    {
        Vector3 originalPosition = wildParticle.transform.localPosition;

        wildParticle.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        wildParticle.transform.DOLocalMove(targetPosition, 0.5f)
            .SetEase(Ease.Linear);

        yield return new WaitForSeconds(0.75f);

        wildParticle.SetActive(false);

        wildParticle.transform.localPosition = originalPosition;
    }
    private IEnumerator MoveAndResetDiamondParticles(Vector3 targetPosition)
    {
        Vector3 originalPosition = wildParticle.transform.localPosition;

        wildParticle.SetActive(true);
        diamond_Text.enabled = true;
        yield return new WaitForSeconds(0.25f);

        wildParticle.transform.DOLocalMove(targetPosition, 0.5f)
            .SetEase(Ease.Linear);

        yield return new WaitForSeconds(0.75f);

        wildParticle.SetActive(false);
        diamond_Text.enabled = false;
        wildParticle.transform.localPosition = originalPosition;
    }
    #endregion

    #endregion

    #region Helper Functions
    public int GetFreeSpinValue()
    {
        switch (slotType)
        {
            case SaharaRichesSlotType.FreeSpin3: return 3;
            case SaharaRichesSlotType.FreeSpin4: return 4;
            case SaharaRichesSlotType.FreeSpin5: return 5;
            case SaharaRichesSlotType.FreeSpin10: return 10;
            default: return 0;
        }
    }
    // Return IEnumerator, not Coroutine
    public IEnumerator MoveFreeSpinIcon(int index, Vector3 targetWorldPos)
    {
        if (PriceBox == null || index < 0 || index >= PriceBox.transform.childCount)
            yield break;

        // ✅ Use PriceBox space (matches DOLocalMove on a child of PriceBox)
        Vector3 targetLocal = PriceBox.transform.InverseTransformPoint(targetWorldPos);

        // ensure parent is visible
        PriceBox.SetActive(true);

        yield return MoveAndResetFreeSpinIcon(index, targetLocal);
    }

    private IEnumerator MoveAndResetFreeSpinIcon(int childIndex, Vector3 targetLocalPos)
    {
        var icon = PriceBox.transform.GetChild(childIndex).gameObject;
        var tr = icon.transform;

        Vector3 originalPos = tr.localPosition;
        Vector3 originalScale = tr.localScale;
        bool wasActive = icon.activeSelf;

        icon.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        Sequence seq = DOTween.Sequence();
        seq.Append(tr.DOScale(originalScale * 1.2f, 0.12f));
        seq.Join(tr.DOLocalMove(targetLocalPos, 0.45f).SetEase(Ease.Linear));
        seq.Append(tr.DOScale(originalScale, 0.12f));
        yield return seq.WaitForCompletion();

        icon.SetActive(wasActive);
        tr.localPosition = originalPos;
        tr.localScale = originalScale;
    }

    public float GetCCAmount()
    {
        return CC_Amount;
    }
    public float GetDiamondAmount()
    {
        return diamond_Amount;
    }
    #endregion
}