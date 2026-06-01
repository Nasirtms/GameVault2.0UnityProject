using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IrishPotLuckAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || IrishPotLuckSlotMachine.Instance.InSpin)
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
            IrishPotLuckUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (IrishPotLuckSlotMachine.Instance.isFreeGameReady || IrishPotLuckSlotMachine.Instance.isJackpotGameReady)
                break;

            float balance = UserManager.Instance.Coins;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (IrishPotLuckUIManager.Instance.CurrentButtonSet() != "Auto")
                IrishPotLuckUIManager.Instance.UpdateButtons("Auto");

            if (IrishPotLuckUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(IrishPotLuckUIManager.Instance.textAnimationCoroutine);
            if (IrishPotLuckUIManager.Instance.winCoroutine != null)
                StopCoroutine(IrishPotLuckUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => IrishPotLuckSlotMachine.Instance.isSpinAgain);
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (IrishPotLuckSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => IrishPotLuckUIManager.Instance.winAnimationCompleted);

            if (IrishPotLuckSlotMachine.Instance.isFreeGameReady || IrishPotLuckSlotMachine.Instance.isJackpotGameReady)
                break;
        }
        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (IrishPotLuckSlotMachine.Instance.isFreeGameReady)
            IrishPotLuckUIManager.Instance.UpdateButtons("Transition Start");
        else
            IrishPotLuckUIManager.Instance.UpdateButtons("Auto Stop");

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