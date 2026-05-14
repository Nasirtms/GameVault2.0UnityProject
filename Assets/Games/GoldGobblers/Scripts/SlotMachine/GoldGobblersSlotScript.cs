using System;
using TMPro;
using UnityEngine;

public class GoldGobblersSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector] public GoldGobblersSlotType slotType;
    [HideInInspector] public GoldGobblersSlotResource currentResource;
    
    // Slot Children
    [SerializeField] private GameObject[] slots;
    [SerializeField] private GameObject[] slotParticals;
    [SerializeField] public TMP_Text tunnelSlotText;

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
        var random = GoldGobblersSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, GoldGobblersSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random);
    }

    public void SetType(GoldGobblersSlotResource newType)
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

        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        int x = currentResource.slotTypeIndex;
        switch (x)
        {
            case 0:
                slotParticals[18].SetActive(true);
                break;
            case 1:
                slotParticals[19].SetActive(true);
                break;
            case 2:
                slotParticals[20].SetActive(true);
                break;
            case 5:
                slotParticals[21].SetActive(true);
                break;
            case 6:
                slotParticals[22].SetActive(true);
                break;
            case 7:
                slotParticals[23].SetActive(true);
                break;
            case 8:
                slotParticals[24].SetActive(true);
                break;
            case 10:
                slotParticals[25].SetActive(true);
                break;
            case 11:
                slotParticals[26].SetActive(true);
                break;
            default:
                break;
        }
        switch (x)
        {
            case 9:
                break;
            case 10:
                break;
            case 11:
                break;
            case 13:
                break;
            case 14:
                break;
            case 15:
                break;
            case 16:
                break;
            case 17:
                break;
            default:
                slotParticals[currentResource.slotTypeIndex].SetActive(true);
                break;
        }
        slotAnimator.SetBool(slotAnimationBool, true);
    }

    public void StopAnimation()
    {
        //if (!isValidSlotForAnimation(slotType)) return;

        if (slotAnimator != null)
        {
            slotAnimator.SetBool(slotAnimationBool, false);
        }
        int x = currentResource.slotTypeIndex;
        switch (x)
        {
            case 0:
                slotParticals[18].SetActive(false);
                break;
            case 1:
                slotParticals[19].SetActive(false);
                break;
            case 2:
                slotParticals[20].SetActive(false);
                break;
            case 5:
                slotParticals[21].SetActive(false);
                break;
            case 6:
                slotParticals[22].SetActive(false);
                break;
            case 7:
                slotParticals[23].SetActive(false);
                break;
            case 8:
                slotParticals[24].SetActive(false);
                break;
            case 10:
                slotParticals[25].SetActive(false);
                break;
            case 11:
                slotParticals[26].SetActive(false);
                break;
            default:
                break;
        }
        switch (x)
        {
            case 9:
                break;
            case 10:
                break;
            case 11:
                break;
            case 13:
                break;
            case 14:
                break;
            case 15:
                break;
            case 16:
                break;
            case 17:
                break;
            default:
                slotParticals[currentResource.slotTypeIndex].SetActive(false);
                break;
        }
    }

    public void ChangeScaleYOfSlots(bool isFreegame)
    {
        if (isFreegame)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].transform.localScale = new Vector3(0.7f, GoldGobblersUIManager.Instance.slotScaleYForFiveByFive, 1f);
            }
        }
        else
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].transform.localScale = new Vector3(0.7f, GoldGobblersUIManager.Instance.slotScaleYForThreeByFive, 1f);
            }
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
}
