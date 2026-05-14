using UnityEngine;
using System.Collections;

public class WheelOfFortuneAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private bool firstAuto;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += HandlePopupShown;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= HandlePopupShown;
    }
    #endregion

    #region Public References

    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || WheelOfFortuneSlotMachine.Instance.InSpin) return;

        firstAuto = true;
        isAutoRunning = true;
        isAutoSpinning = true;
        cancelRequested = false;

        autoSpinRoutine = StartCoroutine(AutoSpinLoop(betAmount));
    }

    public void CancelAutoSpin()
    {
        cancelRequested = true;
        isAutoRunning = false;
    }

    #endregion

    #region Auto Spin

    private IEnumerator AutoSpinLoop(float betAmount)
    {
        while (!cancelRequested)
        {
            WheelOfFortuneUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }


            float balance = UserManager.Instance.Coins;

            if (WheelOfFortuneUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(WheelOfFortuneUIManager.Instance.textAnimationCoroutine);
            if (WheelOfFortuneUIManager.Instance.winCoroutine != null)
                StopCoroutine(WheelOfFortuneUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            WheelOfFortuneUIManager.Instance.PlaySpinMusic("Spin");
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => WheelOfFortuneSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (WheelOfFortuneSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => WheelOfFortuneSlotMachine.Instance.isPaylineCompleted);
            }

            yield return new WaitUntil(() => WheelOfFortuneUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (WheelOfFortuneSlotMachine.Instance.isFreeGameReady)
            {
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!WheelOfFortuneSlotMachine.Instance.isFreeGameReady)
        {
            WheelOfFortuneUIManager.Instance.UpdateButtons("Default");
        }

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }
    private void HandlePopupShown()
    {
        if (!isAutoRunning) return;

        cancelRequested = true;

        if (autoSpinRoutine != null)
        {
            StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = null;
        }

        StopAutoSpin();
    }
    #endregion
}
