using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class DayOfDeadAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private bool firstAuto;
    private int remainingSpins = -1;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    public bool IsAutoRunning => isAutoRunning;

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

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || DayOfDeadSlotMachine.Instance.InSpin) return;

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
        while (!cancelRequested && (remainingSpins == -1 || remainingSpins > 0))
        {
            if (!firstAuto)
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }
            else
            {
                firstAuto = false;
            }

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            DayOfDeadUIManager.Instance.winAnimationCompleted = true;
            //DayOfDeadUIManager.Instance.PlaySound("Spin");
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (DayOfDeadUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(DayOfDeadUIManager.Instance.textAnimationCoroutine);
            if (DayOfDeadUIManager.Instance.winCoroutine != null)
                StopCoroutine(DayOfDeadUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (DayOfDeadUIManager.Instance.CurrentButtonSet() != "Auto")
                DayOfDeadUIManager.Instance.UpdateButtons("Auto");

            yield return new WaitUntil(() => DayOfDeadSlotMachine.Instance.isSpinAgain);

            if (DayOfDeadSlotMachine.Instance.isFreeGameReady)
                break;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (DayOfDeadSlotMachine.Instance.isRespinActive)
            {
                yield return new WaitUntil(() => DayOfDeadSlotMachine.Instance.isWildCompleted);
            }
            if (DayOfDeadSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => DayOfDeadSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => DayOfDeadUIManager.Instance.winAnimationCompleted);

        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (DayOfDeadSlotMachine.Instance.isFreeGameReady)
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Auto Stop");
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
