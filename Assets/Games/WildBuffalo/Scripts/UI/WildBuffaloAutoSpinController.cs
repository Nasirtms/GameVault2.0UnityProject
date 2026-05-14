using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildBuffaloAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    public float delayBetweenSpins = 1.5f;

    public bool isAutoRunning = false;
    public bool cancelRequested = false;

    private Coroutine autoSpinRoutine;
    private bool firstAuto;

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
        if (isAutoRunning || WildBuffaloSlotMachine.Instance.InSpin)
        {
            return;
        }

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
            WildBuffaloUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (WildBuffaloSlotMachine.Instance.isFreeGameReady)
                break;

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount))
                break;

            SlotSpinService.Instance.Spin(betAmount);

            if (WildBuffaloUIManager.Instance.CurrentButtonSet() != "Auto")
                WildBuffaloUIManager.Instance.UpdateButtons("Auto");

            if (WildBuffaloUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(WildBuffaloUIManager.Instance.textAnimationCoroutine);

            if (WildBuffaloUIManager.Instance.winCoroutine != null)
                StopCoroutine(WildBuffaloUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => WildBuffaloSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (WildBuffaloSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => WildBuffaloSlotMachine.Instance.isSlotAnimationCompleted);
            }

            yield return new WaitUntil(() => WildBuffaloUIManager.Instance.winAnimationCompleted);

            if (WildBuffaloSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (WildBuffaloSlotMachine.Instance.isFreeGameReady)
            WildBuffaloUIManager.Instance.UpdateButtons("Transition Start");
        else
            WildBuffaloUIManager.Instance.UpdateButtons("Auto Stop");

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