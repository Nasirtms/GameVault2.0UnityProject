using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IrishPotLuckJackpotWheelController : MonoBehaviour
{
    #region Variables

    [Header("Wheels")]
    [SerializeField] private RectTransform wheel1Transform;
    [SerializeField] private RectTransform wheel2Transform;
    [SerializeField] private RectTransform wheel3Transform;

    [Header("Pin Image")]
    [SerializeField] private RectTransform pinImage;
    [SerializeField] private float pinMoveDuration = 0.5f;
    [SerializeField] private float wheel1PinY = 600f;
    [SerializeField] private float wheel2PinY = 450f;
    [SerializeField] private float wheel3PinY = 270f;
    [SerializeField] private float megaJackpotPinY = 135f;

    [Header("Prize Values")]
    public List<float> wheel1PrizeValues = new List<float>
    {
        1f, 2.5f, 1f, 2.5f, 10f, 1f, 2.5f, 1f, 2.5f, 1f, 2.5f, 1f,
        10f, 2.5f, 1f, 2.5f, 1f, 2.5f, 1f, 10f, 2.5f, 1f, 2.5f, 20f
    };

    public List<float> wheel2PrizeValues = new List<float>
    {
        10f, 100f, 10f, 1f, 10f, 40f, 10f, 1f, 60f, 10f, 40f, 10f,
        1f, 10f, 60f, 10f, 1f, 40f
    };

    public List<float> wheel3PrizeValues = new List<float>
    {
        1f, 20f, 200f, 20f, 100f, 20f, 100f, 20f, 200f, 20f, 100f, 20f
    };

    [Header("Spin Settings")]
    [SerializeField] private float spinDuration = 3f;
    [SerializeField] private int fullRotations = 3;

    [Header("Base Angles")]
    public float wheel1BaseAngle = 0f;
    public float wheel2BaseAngle = 0f;
    public float wheel3BaseAngle = 0f;

    [Header("Fake Backend Result")]
    public int fakeWheel1Index = 0;
    public int fakeWheel2Index = 0;
    public int fakeWheel3Index = 0;
    public float fakeWinAmount = 0f;

    [Header("Start Rotations")]
    public Vector3 wheel1StartRotation;
    public Vector3 wheel2StartRotation;
    public Vector3 wheel3StartRotation;

    [Header("Jackpot Multipliers")]
    [SerializeField] private float arrowMultiplier = 1f;
    [SerializeField] private float megaMultiplier = 5000f;

    public int CurrentWheel1Index { get; private set; }
    public int CurrentWheel2Index { get; private set; }
    public int CurrentWheel3Index { get; private set; }

    public float CurrentJackpotMultiplier { get; private set; }
    public float CurrentJackpotWinAmount { get; private set; }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        wheel1StartRotation = wheel1Transform.localEulerAngles;
        wheel2StartRotation = wheel2Transform.localEulerAngles;
        wheel3StartRotation = wheel3Transform.localEulerAngles;
    }

    #endregion

    #region Public Methods

    public IEnumerator StartJackpotWheelSpin()
    {
        ResetWheelsToStart();

        CurrentJackpotMultiplier = 0f;
        CurrentJackpotWinAmount = 0f;

        GetFakeBackendResult();

        yield return new WaitForSeconds(1f);

        CurrentWheel1Index = ClampIndex(CurrentWheel1Index, wheel1PrizeValues.Count);
        float wheel1Result = wheel1PrizeValues[CurrentWheel1Index];

        yield return StartCoroutine(
            SpinWheel(wheel1Transform, wheel1PrizeValues.Count, CurrentWheel1Index, wheel1BaseAngle)
        );

        if (!IsArrow(wheel1Result))
        {
            SetJackpotWinAmountFromBackend(fakeWinAmount);
            yield break;
        }

        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(MovePinToY(wheel2PinY));
        CurrentWheel2Index = ClampIndex(CurrentWheel2Index, wheel2PrizeValues.Count);
        float wheel2Result = wheel2PrizeValues[CurrentWheel2Index];

        yield return StartCoroutine(
            SpinWheel(wheel2Transform, wheel2PrizeValues.Count, CurrentWheel2Index, wheel2BaseAngle)
        );

        if (!IsArrow(wheel2Result))
        {
            SetJackpotWinAmountFromBackend(fakeWinAmount);
            yield break;
        }

        yield return new WaitForSeconds(1.5f);
        yield return StartCoroutine(MovePinToY(wheel3PinY));
        CurrentWheel3Index = ClampIndex(CurrentWheel3Index, wheel3PrizeValues.Count);
        float wheel3Result = wheel3PrizeValues[CurrentWheel3Index];

        yield return StartCoroutine(
            SpinWheel(wheel3Transform, wheel3PrizeValues.Count, CurrentWheel3Index, wheel3BaseAngle)
        );

        if (IsArrow(wheel3Result))
        {
            // Move pin to Mega Jackpot position
            yield return new WaitForSeconds(1.5f);
            yield return StartCoroutine(MovePinToY(megaJackpotPinY));
            SetJackpotWinAmountFromBackend(fakeWinAmount);

            if (fakeWinAmount <= 0f)
            {
                CurrentJackpotMultiplier = megaMultiplier;
                CurrentJackpotWinAmount = IrishPotLuckUIManager.Instance.CurrentBet() * megaMultiplier;
            }
        }
        else
        {
            SetJackpotWinAmountFromBackend(fakeWinAmount);
        }
    }
    public void ResetWheelsToStart()
    {
        wheel1Transform.DOKill(true);
        wheel2Transform.DOKill(true);
        wheel3Transform.DOKill(true);

        wheel1Transform.localEulerAngles = wheel1StartRotation;
        wheel2Transform.localEulerAngles = wheel2StartRotation;
        wheel3Transform.localEulerAngles = wheel3StartRotation;

        if (pinImage != null)
        {
            pinImage.DOKill();
            Vector2 pos = pinImage.anchoredPosition;
            pos.y = wheel1PinY;
            pinImage.anchoredPosition = pos;
        }
    }
    private IEnumerator MovePinToY(float targetY)
    {
        if (pinImage == null)
            yield break;

        pinImage.DOKill();

        Vector3 originalScale = pinImage.localScale;
        Vector3 punchScale = originalScale * 1.15f;

        Sequence seq = DOTween.Sequence();

        // First scale up a little bit
        seq.Append(
            pinImage.DOScale(punchScale, 0.2f)
                .SetEase(Ease.OutBack)
        );

        // While scaling back down, move to target Y
        seq.Append(
            pinImage.DOScale(originalScale, pinMoveDuration)
                .SetEase(Ease.InOutSine)
        );

        seq.Join(
            pinImage.DOAnchorPosY(targetY, pinMoveDuration)
                .SetEase(Ease.InOutSine)
        );

        yield return seq.WaitForCompletion();
    }
    #endregion

    #region Fake Backend

    private void GetFakeBackendResult()
    {
        CurrentWheel1Index = fakeWheel1Index;
        CurrentWheel2Index = fakeWheel2Index;
        CurrentWheel3Index = fakeWheel3Index;
        CurrentJackpotWinAmount = fakeWinAmount;
    }

    #endregion

    #region Spin Logic

    private IEnumerator SpinWheel(RectTransform wheelTransform, int segmentCount, int prizeIndex, float baseAngle)
    {
        if (wheelTransform == null || segmentCount <= 0)
            yield break;

        wheelTransform.DOKill(true);
        wheelTransform.localEulerAngles = Vector3.zero;

        float segmentAngle = -(360f / segmentCount);

        float targetRotation = (fullRotations * 360f) + (prizeIndex * segmentAngle) + baseAngle;

        Tween spinTween = wheelTransform.DORotate(new Vector3(0f, 0f, -targetRotation), spinDuration,RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);

        yield return spinTween.WaitForCompletion();

        wheelTransform.localEulerAngles = new Vector3(0f,0f, -((prizeIndex * segmentAngle) + baseAngle));
    }

    private bool IsArrow(float value)
    {
        return Mathf.Approximately(value, arrowMultiplier);
    }

    private int ClampIndex(int index, int count)
    {
        if (count <= 0)
            return 0;

        return Mathf.Clamp(index, 0, count - 1);
    }

    private void SetJackpotWinAmountFromBackend(float winAmount)
    {
        CurrentJackpotWinAmount = winAmount;

        float betAmount = IrishPotLuckUIManager.Instance.CurrentBet();

        if (betAmount > 0f)
            CurrentJackpotMultiplier = winAmount / betAmount;
        else
            CurrentJackpotMultiplier = 0f;

        Debug.Log("Final Jackpot Win Amount: " + CurrentJackpotWinAmount);
        Debug.Log("Final Jackpot Multiplier: " + CurrentJackpotMultiplier);
    }

    #endregion
}