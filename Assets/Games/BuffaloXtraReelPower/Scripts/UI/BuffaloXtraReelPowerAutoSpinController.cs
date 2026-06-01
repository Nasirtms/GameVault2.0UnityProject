using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffaloXtraReelPowerAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || BuffaloXtraReelPowerSlotMachine.Instance.InSpin)
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
            BuffaloXtraReelPowerUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (BuffaloXtraReelPowerSlotMachine.Instance.isFreeGameReady)
                break;

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount))
                break;

            SlotSpinService.Instance.Spin(betAmount);

            if (BuffaloXtraReelPowerUIManager.Instance.CurrentButtonSet() != "Auto")
                BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Auto");

            if (BuffaloXtraReelPowerUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(BuffaloXtraReelPowerUIManager.Instance.textAnimationCoroutine);

            if (BuffaloXtraReelPowerUIManager.Instance.winCoroutine != null)
                StopCoroutine(BuffaloXtraReelPowerUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => BuffaloXtraReelPowerSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (BuffaloXtraReelPowerSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => BuffaloXtraReelPowerSlotMachine.Instance.isSlotAnimationCompleted);
            }

            yield return new WaitUntil(() => BuffaloXtraReelPowerUIManager.Instance.winAnimationCompleted);

            if (BuffaloXtraReelPowerSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (BuffaloXtraReelPowerSlotMachine.Instance.isFreeGameReady)
            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Transition Start");
        else
            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Auto Stop");

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