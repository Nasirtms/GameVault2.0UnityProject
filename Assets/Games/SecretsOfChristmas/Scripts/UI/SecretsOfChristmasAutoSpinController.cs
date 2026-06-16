using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretsOfChristmasAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || SecretsOfChristmasSlotMachine.Instance.InSpin)
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
            SecretsOfChristmasUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (SecretsOfChristmasSlotMachine.Instance.isFreeGameReady)
                break;

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount))
                break;

            SlotSpinService.Instance.Spin(betAmount);

            if (SecretsOfChristmasUIManager.Instance.CurrentButtonSet() != "Auto")
                SecretsOfChristmasUIManager.Instance.UpdateButtons("Auto");

            if (SecretsOfChristmasUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(SecretsOfChristmasUIManager.Instance.textAnimationCoroutine);

            if (SecretsOfChristmasUIManager.Instance.winCoroutine != null)
                StopCoroutine(SecretsOfChristmasUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => SecretsOfChristmasSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (SecretsOfChristmasSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => SecretsOfChristmasSlotMachine.Instance.isSlotAnimationCompleted);
            }

            yield return new WaitUntil(() => SecretsOfChristmasUIManager.Instance.winAnimationCompleted);

            if (SecretsOfChristmasSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (SecretsOfChristmasSlotMachine.Instance.isFreeGameReady)
            SecretsOfChristmasUIManager.Instance.UpdateButtons("Transition Start");
        else
            SecretsOfChristmasUIManager.Instance.UpdateButtons("Auto Stop");

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