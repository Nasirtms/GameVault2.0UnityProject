using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelOfFortuneFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static WheelOfFortuneFreeGameTransitionController Instance;
    [SerializeField] private RectTransform machineFrame;
    [SerializeField] private RectTransform spinWheel;
    [SerializeField] private float transitionDuration = 1.5f;

    [SerializeField] public GameObject lookupImage;

    private Vector2 machineFrameStartPos;
    private Vector2 spinWheelStartPos;

    private WheelOfFortuneFreeSpinController freeSpinController;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<WheelOfFortuneFreeSpinController>();
        machineFrameStartPos = machineFrame.anchoredPosition;
        spinWheelStartPos = spinWheel.anchoredPosition;
    }

    #endregion

    #region Public References

    public void StartFreeSpinTransition()
    {
        WheelOfFortuneSlotMachine.Instance.isFreeGame = true;
        StartCoroutine(StartFreeSpin());
    }

    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }
    #endregion

    #region Game Transition

    private IEnumerator StartFreeSpin()
    {
        //yield return new WaitUntil(() => WheelOfFortuneUIManager.Instance.winAnimationCompleted);

        yield return new WaitForSeconds(0.5f);

        WheelOfFortuneUIManager.Instance.StopTitleLoop();
        lookupImage.SetActive(true);

        WheelOfFortunePaylineController.Instance.StopPaylineLoop();
        WheelOfFortunePaylineController.Instance.ClearPaylineResults();

        yield return new WaitForSeconds(0.5f);
        AnimateTransitionDown();

        yield return new WaitForSeconds(1f);

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {

        lookupImage.SetActive(false);
        WheelOfFortuneUIManager.Instance.StartTitleLoop();

        WheelOfFortunePaylineController.Instance.StopPaylineLoop();
        WheelOfFortunePaylineController.Instance.ClearPaylineResults();

        yield return new WaitForSeconds(0.5f);

        if (WheelOfFortuneSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
        else
        {
            WheelOfFortuneUIManager.Instance.UpdateButtons("Default");
        }
        yield return new WaitUntil(() => WheelOfFortuneUIManager.Instance.winAnimationCompleted);
        yield return new WaitForSeconds(2f);
        AnimateTransitionUp();
        WheelOfFortuneUIManager.Instance.UpdateButtons("Default");
    }

    private void WinAnimation()
    {
        if (WheelOfFortuneSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = WheelOfFortuneSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = WheelOfFortuneUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, WheelOfFortuneSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(WheelOfFortuneSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }
    private void AnimateTransitionDown()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(
            machineFrame.DOAnchorPosY(-1580f, transitionDuration)
                        .SetEase(Ease.InOutSine)
        );

        seq.Join(
            spinWheel.DOAnchorPosY(-1575f, transitionDuration)
                     .SetEase(Ease.InOutSine)
        );
    }
    private void AnimateTransitionUp()
    {
        Sequence seq = DOTween.Sequence();

        seq.Append(
            machineFrame.DOAnchorPos(machineFrameStartPos, transitionDuration)
                        .SetEase(Ease.InOutSine)
        );

        seq.Join(
            spinWheel.DOAnchorPos(spinWheelStartPos, transitionDuration)
                     .SetEase(Ease.InOutSine)
        );
    }
    #endregion
}
