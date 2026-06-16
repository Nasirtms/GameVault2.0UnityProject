using DG.Tweening;
using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
public class CashVaultSlotScript : MonoBehaviour
{
    #region Variables
    // Slot Resource
    [HideInInspector] public CashVaultSlotType slotType;
    [HideInInspector] public CashVaultSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    // Slot Children
    [SerializeField] private GameObject[] slots;
    private SortingGroup textSortingGroup;

    private SpriteRenderer[] slotRenderers;
    private Animator slotAnimator;
    private String slotAnimationBool;

    public GameObject slotBorder;
    private Animator slotBorderAnimator;

    // Sphere Coin Text
    public TextMeshPro Sphere_Text;
    public float Sphere_Amount;

    public GameObject wildParticle;
    public float wildCollectAmount;

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
        var random = CashVaultSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, CashVaultSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }

    public void SetType(CashVaultSlotResource newType, bool finalResult)
    {
        slots[currentResource.slotTypeIndex].SetActive(false);

        this.currentResource = newType;
        this.slotType = newType.slotType;
        this.slotAnimationBool = newType.slotAnimationBool;
        slots[newType.slotTypeIndex].SetActive(true);

        if (slotType == CashVaultSlotType.Sphere)
        {
            Sphere_Text = slots[newType.slotTypeIndex].GetComponentInChildren<TextMeshPro>();
            if (Sphere_Text == null)
            {
                return;
            }
            if (transform.GetSiblingIndex() == 4)
            {
                Sphere_Text.gameObject.SetActive(false);
                return;
            }
            Sphere_Text.gameObject.SetActive(true);

            if (finalResult)
            {
                //Sphere_Text.text = Sphere_Amount.ToString("F2");
                Sphere_Text.text = ToSpriteDigits(Sphere_Amount);
            }
            else
            {
                float rand = Random.Range(0f, 1f);
                float bet = CashVaultSlotMachine.Instance.CurrentBet();
                float multiplier = GetRandomMultiplier(rand);

                //Sphere_Text.text = (bet * multiplier).ToString("F2");
                double value = bet * multiplier;
                Sphere_Text.text = ToSpriteDigits(value);
            }
        }
    }
    private float GetRandomMultiplier(float rand)
    {
        if (rand <= 0.1f) return 1;
        if (rand <= 0.2f) return 2;
        if (rand <= 0.3f) return 3;
        if (rand <= 0.4f) return 4;
        if (rand <= 0.5f) return 5;
        if (rand <= 0.6f) return 6;
        if (rand <= 0.65f) return 7;
        if (rand <= 0.7f) return 8;
        if (rand <= 0.75f) return 10;
        if (rand <= 0.8f) return 12.5f;
        if (rand <= 0.85f) return 15;
        if (rand <= 0.9f) return 25;
        if (rand <= 0.94f) return 50;
        if (rand <= 0.98f) return 100;
        return 150;
    }
    public string ToSpriteDigits(double value)
    {
        double floored = System.Math.Floor(value * 100) / 100;
        string s = floored.ToString("0.00", CultureInfo.InvariantCulture);

        StringBuilder sb = new StringBuilder(s.Length * 10);

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (ch >= '0' && ch <= '9')
            {
                sb.Append($"<sprite index={ch - '0'}>");
            }
            else if (ch == '.')
            {
                sb.Append("<sprite index=10>"); // dot sprite
            }
        }

        return sb.ToString();
    }
    #endregion

    #region Slot Animation

    public void PlayAnimation()
    {
        StopAnimation();
        //SetSpriteToPayline();
        slotAnimator = slots[currentResource.slotTypeIndex].GetComponentInParent<Animator>();
        slotAnimator.enabled = true;
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
    public void PlayBorderAnimation()
    {
        StopBorderAnimation();
        slotBorder.SetActive(true);
        slotBorderAnimator = transform.GetChild(14).GetComponent<Animator>();  //Child Index is Based on how it is set in unity
        slotBorderAnimator.enabled = true;
        slotBorderAnimator.SetBool("play", true);
    }
    public void StopBorderAnimation()
    {
        if (slotBorderAnimator != null)
        {
            slotBorderAnimator.SetBool("play", false);
            slotBorder.SetActive(false);
            slotBorderAnimator.enabled = false;
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

    #region Box

    //public void MoveBox(Vector3 targetPosition)
    //{
    //    Vector3 movePos = transform.InverseTransformPoint(targetPosition);
    //    StartCoroutine(MoveAndResetBox(movePos));
    //}

    //public void MoveSphereParticles(Vector3 targetPosition)
    //{
    //    Vector3 movePos = transform.InverseTransformPoint(targetPosition);
    //    StartCoroutine(MoveAndResetSphereParticles(movePos));
    //}

    //public void UpdateBox(float CC_Amount, GameObject other)
    //{
    //    var targetbox = SaharaRichesPaylineController.Instance.Target_Text;
    //    float originalScale = targetbox.transform.localScale.x;

    //    Sequence sequence = DOTween.Sequence();

    //    sequence.Append(other.transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.Linear));
    //    sequence.Join(targetbox.transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.Linear));

    //    wildCollectAmount += CC_Amount;
    //    SaharaRichesPaylineController.Instance.Target_Text.text = $"${wildCollectAmount.ToString("F2")}";

    //    sequence.AppendInterval(0.2f);

    //    sequence.Append(other.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
    //    sequence.Join(targetbox.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
    //}

    //private IEnumerator MoveAndResetBox(Vector3 targetPosition)
    //{
    //    Vector3 originalPosition = PriceBox.transform.localPosition;
    //    Vector3 originalScale = PriceBox.transform.localScale;

    //    PriceBox.SetActive(true);

    //    yield return new WaitForSeconds(0.25f);

    //    PriceBox.transform.DOLocalMove(targetPosition, 0.5f)
    //        .SetEase(Ease.Linear);

    //    PriceBox.transform.DOScale(originalScale * 1.2f, 0.1f)
    //    .SetEase(Ease.OutQuad)
    //    .OnComplete(() =>
    //    {
    //        PriceBox.transform.DOScale(originalScale, 0.1f)
    //            .SetEase(Ease.InQuad);
    //    });
    //    yield return new WaitForSeconds(0.75f);

    //    PriceBox.SetActive(false);

    //    PriceBox.transform.localPosition = originalPosition;
    //    PriceBox.transform.localScale = originalScale;
    //}

    //private IEnumerator MoveAndResetSphereParticles(Vector3 targetPosition)
    //{
    //    Vector3 originalPosition = wildParticle.transform.localPosition;

    //    wildParticle.SetActive(true);

    //    yield return new WaitForSeconds(0.25f);

    //    wildParticle.transform.DOLocalMove(targetPosition, 0.5f)
    //        .SetEase(Ease.Linear);

    //    yield return new WaitForSeconds(0.75f);

    //    wildParticle.SetActive(false);

    //    wildParticle.transform.localPosition = originalPosition;
    //}

    #endregion

    #region Helper Functions
    public void UpdateSphereAmount(float SphereAmount)
    {
        Sphere_Amount = SphereAmount;
    }
    public float GetSphereAmount()
    {
        return Sphere_Amount;
    }

    #endregion
}