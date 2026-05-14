using DG.Tweening;
using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class UltimateFireLinkChinaStreetMiniGameSlotScript : MonoBehaviour
{
    #region Variables

    // Slot Resource
    [HideInInspector] public UltimateFireLinkChinaStreetMiniGameSlotType slotType;
    [HideInInspector] public UltimateFireLinkChinaStreetMiniGameSlotResource currentResource;
    [HideInInspector] public bool isResultSet = false;

    [SerializeField] private GameObject[] slots;
    private SortingGroup textSortingGroup;

    // Sphere Coin Text
    public TextMeshPro Sphere_Text;
    public float Sphere_Amount;

    //SphereAmount_text
    public GameObject wildParticle;
    public GameObject winObject;
    public TextMeshPro winText;
    public float WinCollectedAmount;

    public float duration = 2.5f;
    public int jumps = 5;

    public bool completed = false;
    #endregion
    public void UpdateScale(float scaleX, float scaleY)
    {
        transform.localScale = new Vector3(scaleX, scaleY, 1);
    }
    public void GetRandom(bool blur = false)
    {
        var random = UltimateFireLinkChinaStreetMiniGameSlotMachine.Instance.settings.slotResources[UnityEngine.Random.Range(0, UltimateFireLinkChinaStreetMiniGameSlotMachine.Instance.settings.slotResources.Count)];
        SetType(random, false);
    }
    public void SetType(UltimateFireLinkChinaStreetMiniGameSlotResource newType, bool finalResult)
    {
        WinCollectedAmount = 0f;
        //slots[currentResource.slotTypeIndex].SetActive(false);
        ClearAllSlots();

        this.currentResource = newType;
        this.slotType = newType.slotType;
        slots[newType.slotTypeIndex].SetActive(true);

        if (slotType == UltimateFireLinkChinaStreetMiniGameSlotType.FireLink100x)
        {
            Sphere_Text = slots[newType.slotTypeIndex].GetComponentInChildren<TextMeshPro>();

            if (finalResult)
            {
                //Sphere_Text.text = Sphere_Amount.ToString("F2");
                Sphere_Text.text = ToSpriteDigits(Sphere_Amount);
                //winText.text = ToSpriteDigits(Sphere_Amount);
            }
            else
            {
                float rand = Random.Range(0f, 1f);
                float bet = UltimateFireLinkChinaStreetSlotMachine.Instance.CurrentBet();
                float multiplier = GetRandomMultiplier(rand);

                //Sphere_Text.text = (bet * multiplier).ToString("F2");
                double value = bet * multiplier;
                Sphere_Text.text = ToSpriteDigits(value);
                //winText.text = ToSpriteDigits(value);
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
    public void ClearAllSlots()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetActive(false);
    }
    public void UpdateSphereAmount(float SphereAmount)
    {
        Sphere_Amount = SphereAmount;
    }
    public float GetSphereAmount()
    {
        return Sphere_Amount;
    }

    #region Box
    public void MoveSphereAmount(Vector3 targetPosition, float amount)
    {
        winText.text = ToSpriteDigits(amount);
        Debug.Log("LovKumar targetPosition : " + targetPosition);
        //Vector3 movePos = transform.InverseTransformPoint(targetPosition);
        Vector3 movePos = targetPosition;
        movePos.z = winText.transform.position.z;
        Debug.Log("LovKumar movePos : " + movePos);
        StartCoroutine(MoveAndResetSphereAmount(movePos));
    }
    private IEnumerator MoveAndResetSphereAmount(Vector3 targetPosition)
    {
        //Vector3 originalParticlePos = wildParticle.transform.localPosition;
        //Vector3 originalWinTextPos = winText.transform.localPosition;

        //wildParticle.SetActive(true);
        winObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        //wildParticle.transform.DOLocalJump(targetPosition, 1 ,1, 2.5f).SetEase(Ease.InOutSine);
        //winText.transform.DOLocalJump(targetPosition, 1, 1, 2.5f).SetEase(Ease.InOutSine);

        //GameObject winTextTemp = Instantiate(winText.gameObject, winText.transform.position, winText.transform.rotation, null);
        //winTextTemp.transform.DOLocalJump(targetPosition, jumps, 1, duration)
        //                .SetEase(Ease.InOutQuad).OnComplete(() => completed = true);


        Transform winObjectParentTemp = winObject.transform.parent;
        Vector3 winObjectLocalPositionTemp = winObject.transform.localPosition;
        winObject.transform.SetParent(null);
        winObject.transform.DOKill();
        winObject.transform.DOJump(targetPosition, jumps, 1, duration)
                        .SetEase(Ease.InOutQuad).OnComplete(() => completed = true);

        // wait until tween REALLY finishes
        yield return new WaitUntil(() => completed);

        //Destroy(winTextTemp);
        //wildParticle.SetActive(false);
        //yield return new WaitForSeconds(1f);
        winObject.SetActive(false);

        //wildParticle.transform.localPosition = originalParticlePos
        winObject.transform.SetParent(winObjectParentTemp);
        winObject.transform.localPosition = winObjectLocalPositionTemp;
        //winText.transform.localPosition = originalWinTextPos;
    }
    public void UpdateBox(float Sphere_Amount, GameObject other)
    {
        var targetbox = UltimateFireLinkChinaStreetMiniGameManager.Instance.Target_Text;
        float originalScale = winText.transform.localScale.x;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(other.transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.Linear));
        sequence.Join(targetbox.transform.DOScale(originalScale * 1.1f, 0.2f).SetEase(Ease.Linear));

        WinCollectedAmount += Sphere_Amount;
        UltimateFireLinkChinaStreetMiniGameManager.Instance.Target_Text.text = $"${WinCollectedAmount.ToString("F2")}";

        sequence.AppendInterval(0.2f);

        sequence.Append(other.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
        sequence.Join(targetbox.transform.DOScale(originalScale, 0.2f).SetEase(Ease.Linear));
    }
    #endregion

    #region Reset States
    public void ResetSlotState()
    {
        Sphere_Amount = 0f;
        WinCollectedAmount = 0f;
        completed = false;

        if (winObject != null)
            winObject.SetActive(false);

        ClearAllSlots();

        transform.DOKill();
    }
    #endregion
}
