using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkOlveraStreetAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || UltimateFireLinkOlveraStreetSlotMachine.Instance.InSpin) return;

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
            UltimateFireLinkOlveraStreetUIManager.Instance.winAnimationCompleted = true;
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (UltimateFireLinkOlveraStreetUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(UltimateFireLinkOlveraStreetUIManager.Instance.textAnimationCoroutine);
            if (UltimateFireLinkOlveraStreetUIManager.Instance.winCoroutine != null)
                StopCoroutine(UltimateFireLinkOlveraStreetUIManager.Instance.winCoroutine);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            //SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => UltimateFireLinkOlveraStreetSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGameReady || UltimateFireLinkOlveraStreetSlotMachine.Instance.isBonusGame)
                break;

            UltimateFireLinkOlveraStreetUIManager.Instance.SetStopInteractable(true);

            if (UltimateFireLinkOlveraStreetSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => UltimateFireLinkOlveraStreetSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => UltimateFireLinkOlveraStreetUIManager.Instance.winAnimationCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        UltimateFireLinkOlveraStreetUIManager.Instance.autoStopButton.ShowButton(false);
        UltimateFireLinkOlveraStreetUIManager.Instance.autoButton.ShowButton(true);
        UltimateFireLinkOlveraStreetUIManager.Instance.SetAutoInteractable(true);
        if (!UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGameReady)
        {
            UltimateFireLinkOlveraStreetUIManager.Instance.UpdateButtons("Idle");
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
