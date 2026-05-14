using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CashVaultAutoSpinController : MonoBehaviour
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
    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || CashVaultSlotMachine.Instance.InSpin) return;

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
            if (!firstAuto)
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }
            else
            {
                firstAuto = false;
            }
            CashVaultUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (CashVaultUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(CashVaultUIManager.Instance.textAnimationCoroutine);

            if (CashVaultUIManager.Instance.winCoroutine != null)
                StopCoroutine(CashVaultUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (CashVaultUIManager.Instance.CurrentButtonSet() != "Auto")
                CashVaultUIManager.Instance.UpdateButtons("Auto");

            yield return new WaitUntil(() => CashVaultSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (CashVaultSlotMachine.Instance.isFreeGameReady || CashVaultSlotMachine.Instance.isMiniGameReady || CashVaultSlotMachine.Instance.isBlindFeature)
                break;

            if (CashVaultSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => CashVaultSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => CashVaultUIManager.Instance.winAnimationCompleted);
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (CashVaultSlotMachine.Instance.isFreeGameReady)
        {
            CashVaultUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            CashVaultUIManager.Instance.UpdateButtons("Auto Stop");
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