using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelOfFortuneFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private RectTransform wheelTransform;
    [SerializeField] private GameObject winImage;

    //[SerializeField] private RectTransform wheelTransform2;

    public List<float> prizeValues = new List<float>
    {
        150f, 750f, 150f, 600f, 100f, 150f, 1000f, 2000f, 150f, 1500f, 500f, 300f, 250f, 500f, 150f, 2000f, 100f, 1000f, 250f, 500f, 200f, 5000f,
    };

    [SerializeField] private float preSpinSpeed = 720f;
    [SerializeField] private float preSpinTime = 0.7f;
    [SerializeField] private float spinDuration = 5f;
    [SerializeField] private int fullRotations = 2;
    [SerializeField] private int forcedPrizeIndex = -1;

    private float angleOffset = 7.5f;
    private bool isFreeGame = false;
    private bool isSpinning = false;
    private bool keepSpinning = false;

    [SerializeField] private Animator lightsAnimator;
    #endregion

    #region Public References
    public void StartFreeSpins()
    {
        if (isFreeGame || isSpinning) return;

        isFreeGame = true;
        winImage.SetActive(false);
        StartCoroutine(DoSpin());
    }

    #endregion

    #region Free Spin
    private IEnumerator DoSpin()
    {
        isSpinning = true;
        yield return new WaitForSeconds(3.5f);

        if (lightsAnimator != null)
            lightsAnimator.SetBool("On", true);

        int segmentCount = prizeValues.Count;
        if (segmentCount == 0)
        {
            Debug.LogError("No prize values configured for wheel.");
            isSpinning = false;
            yield break;
        }

        int prizeIndex = forcedPrizeIndex;
        if (prizeIndex < 0 || prizeIndex >= segmentCount)
        {
            prizeIndex = Random.Range(0, segmentCount);
        }

        //Coroutine spinLoop = StartCoroutine(SpinWhileWaiting());
        //yield return new WaitForSeconds(preSpinTime);
        //StopCoroutine(spinLoop);
        Coroutine spinLoop = StartCoroutine(SpinWhileWaiting());
        yield return new WaitForSeconds(preSpinTime);

        // SAFE STOP — no more NullReference
        keepSpinning = false;

        float segmentAngle = 360f / segmentCount;
        float targetSegmentAngle = (prizeIndex * segmentAngle + angleOffset) % 360f;
        float currentAngle = wheelTransform.localEulerAngles.z % 360f;

        // Smallest signed angle from current to target
        float deltaToTarget = Mathf.DeltaAngle(currentAngle, targetSegmentAngle);

        // Force spin to go "forward" (clockwise-ish) and add some full spins
        if (deltaToTarget > 0)
            deltaToTarget -= 360f;

        float totalSpinAngle = deltaToTarget - (fullRotations * 360f);
        float finalAngle = currentAngle + totalSpinAngle;

        Tween spinTween = wheelTransform
            .DORotate(new Vector3(0f, 0f, finalAngle), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);

        yield return spinTween.WaitForCompletion();
        winImage.SetActive(true);
        // Snap both wheels exactly to final segment

        // Get prize
        float prize = (prizeValues[prizeIndex] * WheelOfFortuneUIManager.Instance.CurrentBet()) / 5;
        Debug.Log($"[FreeSpin] Landed on index {prizeIndex}, prize = {prize}");

        // Store in your slot machine logic
        WheelOfFortuneSlotMachine.Instance.freeSpinWinAmount += prize;

        // Tiny delay so the result visually "settles"
        yield return new WaitForSeconds(1f);

        isSpinning = false;

        if (lightsAnimator != null)
            lightsAnimator.SetBool("On", false);

        EndFreeSpin();
    }
    private IEnumerator SpinWhileWaiting()
    {
        keepSpinning = true;

        while (keepSpinning)
        {
            float angle = wheelTransform.localEulerAngles.z - preSpinSpeed * Time.deltaTime;
            wheelTransform.localEulerAngles = new Vector3(0f, 0f, (angle + 360f) % 360f);
            yield return null;
        }
    }
    //private IEnumerator SpinWhileWaiting()
    //{
    //    while (true)
    //    {
    //        float angle = wheelTransform.localEulerAngles.z - preSpinSpeed * Time.deltaTime;
    //        wheelTransform.localEulerAngles = new Vector3(0f, 0f, (angle + 360f) % 360f);
    //        yield return null;
    //    }
    //}
    private void EndFreeSpin()
    {
        isFreeGame = false;
        WheelOfFortuneSlotMachine.Instance.isFreeGame = false;
        WheelOfFortuneFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    #endregion
}