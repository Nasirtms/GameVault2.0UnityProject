using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class BiggerBassBonanzaSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector]public BiggerBassBonanzaSlotType slotType;
    [ReadOnly] public BiggerBassBonanzaSlotResource currentResource;

    [Header("Slots")]
    [SerializeField] private GameObject[] slots;

    [Header("Wild Effects")]
    public GameObject textBox;
    public TextMeshPro textBoxText;
    public GameObject wildParticle;
    public int activeMultiplierIndex;
    public List<GameObject> wildMultipliers;
    public List<GameObject> wildMultipliersAnimated;

    // Fish Slot
    private TextMeshPro fishText;
    private float fishAmount;
    public float wildCollectAmount;

    // Slot Animation
    private Animator slotAnimator;
    private String slotAnimationBool;
    private SortingGroup textSortingGroup;

    // Active Slot Sprites
    private SpriteRenderer[] slotRenderers;

    #endregion

    #region Slot Initialization

    public void Initialize()
    {
        textBoxText = textBox.GetComponentInChildren<TextMeshPro>();
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
        var random = BiggerBassBonanzaSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, BiggerBassBonanzaSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(BiggerBassBonanzaSlotResource newType, bool finalResult)
    {
        wildCollectAmount = 0f;

        slots[currentResource.slotTypeIndex].SetActive(false);
        HideMultiplier();

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;

        slots[newType.slotTypeIndex].SetActive(true);

        if (finalResult && BiggerBassBonanzaSlotMachine.Instance.isFishSlot(slotType))
        {
            fishText = slots[newType.slotTypeIndex].GetComponentInChildren<TextMeshPro>();
            fishText.text = $"${fishAmount.ToString("F2")}";
            textBoxText.text = $"${fishAmount.ToString("F2")}";
        }
        else if (BiggerBassBonanzaSlotMachine.Instance.isFishSlot(slotType))
        {
            fishText = slots[newType.slotTypeIndex].GetComponentInChildren<TextMeshPro>(); ;

            switch (slotType)
            {
                case BiggerBassBonanzaSlotType.VerySmallFish:
                    fishAmount = BiggerBassBonanzaSlotMachine.Instance.CurrentBet() * (Random.value < 0.5f ? 2f : 5f);
                    break;

                case BiggerBassBonanzaSlotType.SmallFish:
                    fishAmount = BiggerBassBonanzaSlotMachine.Instance.CurrentBet() * (Random.value < 0.5f ? 10f : 15f);
                    break;

                case BiggerBassBonanzaSlotType.MediumFish:
                    fishAmount = BiggerBassBonanzaSlotMachine.Instance.CurrentBet() * (Random.value < 0.5f ? 20f : 25f);
                    break;

                case BiggerBassBonanzaSlotType.BigFish:
                    fishAmount = BiggerBassBonanzaSlotMachine.Instance.CurrentBet() * (Random.value < 0.5f ? 50f : 100f);
                    break;

                case BiggerBassBonanzaSlotType.GoldenFish:
                    fishAmount = BiggerBassBonanzaSlotMachine.Instance.CurrentBet() * 4000f;
                    break;
            }

            fishText.text = $"${fishAmount.ToString("F2")}";
        }

        if (slotType == BiggerBassBonanzaSlotType.Wild && BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount] != 1)
        {
            HideMultiplier();

            if (BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount] == 2)
            {
                activeMultiplierIndex = 0;
                wildMultipliers[0].SetActive(true);
            }
            else if (BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount] == 3)
            {
                activeMultiplierIndex = 1;
                wildMultipliers[1].SetActive(true);
            }
            else if (BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount] == 10)
            {
                activeMultiplierIndex = 2;
                wildMultipliers[2].SetActive(true);
            }
        }
        else
        {
            HideMultiplier();
        }
    }

    public void HideMultiplier()
    {
        foreach (GameObject wildMultiplier in wildMultipliers)
        {
            wildMultiplier.SetActive(false);
        }
    }

    public void UpdateFishAmount(float fishAmount)
    {
        this.fishAmount = fishAmount;
    }

    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
        SetSpriteToPayline();

        slotAnimator = slots[currentResource.slotTypeIndex].transform.GetChild(0).GetComponent<Animator>();
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        SetSpriteToDefault();

        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
    }

    #endregion

    #region Slot Layering

    private void SetSpriteToPayline()
    {
        slotRenderers = slots[currentResource.slotTypeIndex].gameObject.GetComponentsInChildren<SpriteRenderer>();

        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Payline Slot";
        }

        if (currentResource.slotType == BiggerBassBonanzaSlotType.Scatter ||
            currentResource.slotType == BiggerBassBonanzaSlotType.BigFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.MediumFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.SmallFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.VerySmallFish)
        {
            SpriteMask mask = slots[currentResource.slotTypeIndex].GetComponentInChildren<SpriteMask>();
            mask.frontSortingLayerID = SortingLayer.NameToID("Payline Slot");
            mask.backSortingLayerID = SortingLayer.NameToID("Payline Slot");
        }

        if (currentResource.slotType == BiggerBassBonanzaSlotType.GoldenFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.BigFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.MediumFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.SmallFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.VerySmallFish)
        {
            textSortingGroup = slots[currentResource.slotTypeIndex].GetComponentInChildren<SortingGroup>();
            textSortingGroup.sortingLayerName = "Payline Slot";
        }
    }

    private void SetSpriteToDefault()
    {
        slotRenderers = slots[currentResource.slotTypeIndex].gameObject.GetComponentsInChildren<SpriteRenderer>();

        foreach (var slot in slotRenderers)
        {
            slot.sortingLayerName = "Default";
        }

        if (currentResource.slotType == BiggerBassBonanzaSlotType.Scatter ||
            currentResource.slotType == BiggerBassBonanzaSlotType.BigFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.MediumFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.SmallFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.VerySmallFish)
        {
            SpriteMask mask = slots[currentResource.slotTypeIndex].GetComponentInChildren<SpriteMask>();
            mask.frontSortingLayerID = SortingLayer.NameToID("Default");
            mask.backSortingLayerID = SortingLayer.NameToID("Default");
        }

        if (currentResource.slotType == BiggerBassBonanzaSlotType.GoldenFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.BigFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.MediumFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.SmallFish ||
            currentResource.slotType == BiggerBassBonanzaSlotType.VerySmallFish)
        {
            textSortingGroup = slots[currentResource.slotTypeIndex].GetComponentInChildren<SortingGroup>();
            textSortingGroup.sortingLayerName = "Default";
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

    public void MoveParticles(Vector3 targetPosition)
    {
        Vector3 movePos = transform.InverseTransformPoint(targetPosition);
        StartCoroutine(MoveAndResetParticles(movePos));
    }

    public void UpdateBox(float fishAmount, GameObject other)
    {
        float originalScale = textBox.transform.localScale.x;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(other.transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.Linear));
        sequence.Join(textBox.transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.Linear));

        wildCollectAmount += fishAmount;
        textBoxText.text = $"${wildCollectAmount.ToString("F2")}";

        sequence.AppendInterval(0.2f);

        sequence.Append(other.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
        sequence.Join(textBox.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
    }

    private IEnumerator MoveAndResetBox(Vector3 targetPosition)
    {
        Vector3 originalPosition = textBox.transform.localPosition;

        textBox.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        textBox.transform.DOLocalMove(targetPosition, 0.5f)
            .SetEase(Ease.Linear);

        yield return new WaitForSeconds(0.75f);

        textBox.SetActive(false);

        textBox.transform.localPosition = originalPosition;
    }

    private IEnumerator MoveAndResetParticles(Vector3 targetPosition)
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

    #endregion

    #region Multiplier

    public void MoveWild(GameObject wildMultiplier)
    {
        Vector3 originalPos = wildMultiplier.transform.localPosition;
        Vector3 movePos = wildMultiplier.transform.InverseTransformPoint(textBox.transform.position);
        Vector3 wildScale = wildMultiplier.transform.localScale;

        movePos.x = originalPos.x + (movePos.x * wildScale.x);
        movePos.y = originalPos.y + (movePos.y * wildScale.y);

        StartCoroutine(MoveAndResetWild(movePos, wildMultiplier));
    }

    private IEnumerator MoveAndResetWild(Vector3 targetPosition, GameObject wildMultiplier)
    {
        Vector3 originalPosition = wildMultiplier.transform.localPosition;

        wildMultiplier.SetActive(true);

        yield return new WaitForSeconds(0.1f);

        wildMultiplier.transform.DOLocalMove(targetPosition, 0.5f)
            .SetEase(Ease.Linear);

        yield return new WaitForSeconds(0.75f);

        wildMultiplier.SetActive(false);

        wildMultiplier.transform.localPosition = originalPosition;
    }

    #endregion

    #endregion

    #region Helper Functions

    public void ShowBox()
    {
        textBox.GetComponent<SpriteRenderer>().sortingOrder = 40;
        textBox.GetComponentInChildren<SortingGroup>().sortingOrder = 41;

        textBox.SetActive(true);
    }

    public void HideBox()
    {
        textBox.GetComponent<SpriteRenderer>().sortingOrder = 42;
        textBox.GetComponentInChildren<SortingGroup>().sortingOrder = 43;

        textBox.SetActive(false);
    }

    public float GetFishAmount()
    {
        return fishAmount;
    }

    #endregion
}
