using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelOfFortuneFreeSpinController : MonoBehaviour
{
    #region Variables
    [Header("Wheels")]
    [SerializeField] private RectTransform wheelTransform;
    [SerializeField] private RectTransform wheelTransform1;

    public Vector3 wheelStartRotation;
    public Vector3 wheel1StartRotation;

    [SerializeField] private GameObject winImage;

    public List<float> prizeValues = new List<float>
    {
        150f, 750f, 150f, 600f, 100f, 150f, 1000f, 2000f, 150f, 1500f, 500f, 300f, 250f, 500f, 150f, 2000f, 100f, 1000f, 250f, 500f, 200f, 5000f,
    };

    [Header("Spin Settings")]
    [SerializeField] private float preSpinSpeed = 1020f;
    [SerializeField] private float preSpinTime = 0.7f;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int fullRotations = 2;
    [SerializeField] private float offset = 0f;

    private bool isFreeGame = false;
    private bool isSpinning = false;
    private bool keepSpinning = false;

    private Coroutine spinLoop;
    [SerializeField] private Animator lightsAnimator;
    [SerializeField] public float baseAngle = 18;
    public float baseAngle1 = 0;

    #endregion

    #region Public References
    private void Awake()
    {
        wheelStartRotation = wheelTransform.localEulerAngles;
        wheel1StartRotation = wheelTransform1.localEulerAngles;
    }
    public void StartFreeSpins()
    {
        if (isFreeGame || isSpinning) return;

        isFreeGame = true;
        winImage.SetActive(false);
        StartCoroutine(DoSpin());
    }
    public void ResetWheelsToStart()
    {
        if (wheelTransform != null)
        {
            wheelTransform.DOKill(true);
            wheelTransform.localEulerAngles = wheelStartRotation;
        }

        if (wheelTransform1 != null)
        {
            wheelTransform1.DOKill(true);
            wheelTransform1.localEulerAngles = wheel1StartRotation;
        }
    }
    #endregion

    #region Free Spin
    private IEnumerator DoSpin()
    {
        yield return new WaitForSeconds(2.8f);
        isSpinning = true;

        int segmentCount = prizeValues.Count;
        if (segmentCount == 0)
        {
            isSpinning = false;
            yield break;
        }

        int prizeIndex = WheelOfFortuneSlotMachine.Instance.freeSpinWinIndex;
        if (prizeIndex < 0 || prizeIndex >= segmentCount)
            prizeIndex = Random.Range(0, segmentCount);

        float segmentAngle = -(360f / segmentCount);

        // Reset to known state (IMPORTANT)
        wheelTransform.localEulerAngles = Vector3.zero;
        wheelTransform1.localEulerAngles = Vector3.zero;

        float extraSpins = fullRotations; // keep your existing value
        WheelOfFortuneUIManager.Instance.PlaySpinMusic("SpinWheel");
        // SpinWheel-style target calculation
        float targetRotation =
            (extraSpins * 360f) +
            (prizeIndex * segmentAngle) +
            baseAngle +offset;

        float targetRotation1 =
            (extraSpins * 360f) +
            (prizeIndex * segmentAngle) +
            baseAngle1;

        Tween spinTween = wheelTransform
            .DORotate(new Vector3(0f, 0f, -targetRotation), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);

        Tween spinTween1 = wheelTransform1
            .DORotate(new Vector3(0f, 0f, -targetRotation1), spinDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                WheelOfFortuneUIManager.Instance.StopSpinMusic("SpinWheel");
                WheelOfFortuneUIManager.Instance.PlaySound("Increase");

            }); 

        yield return spinTween.WaitForCompletion();
        yield return spinTween1.WaitForCompletion();

        // HARD SNAP (casino wheels always do this)
        wheelTransform.localEulerAngles = new Vector3(0f, 0f, -((prizeIndex * segmentAngle) + baseAngle)-offset);
        wheelTransform1.localEulerAngles = new Vector3(0f, 0f, -((prizeIndex * segmentAngle) + baseAngle1));

        winImage.SetActive(true);
        float prize = (prizeValues[prizeIndex] * WheelOfFortuneUIManager.Instance.CurrentBet()) / 5f;
        WheelOfFortuneSlotMachine.Instance.freeSpinWinAmount = prize;

        isSpinning = false;
        EndFreeSpin();
    }
    private void EndFreeSpin()
    {
        isFreeGame = false;
        WheelOfFortuneSlotMachine.Instance.isFreeGame = false;
        WheelOfFortuneFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    #endregion
}