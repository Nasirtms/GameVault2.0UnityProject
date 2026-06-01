using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkRueRoyaleAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || UltimateFireLinkRueRoyaleSlotMachine.Instance.InSpin) return;

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
            UltimateFireLinkRueRoyaleUIManager.Instance.winAnimationCompleted = true;
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (UltimateFireLinkRueRoyaleUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(UltimateFireLinkRueRoyaleUIManager.Instance.textAnimationCoroutine);
            if (UltimateFireLinkRueRoyaleUIManager.Instance.winCoroutine != null)
                StopCoroutine(UltimateFireLinkRueRoyaleUIManager.Instance.winCoroutine);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            //SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => UltimateFireLinkRueRoyaleSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (UltimateFireLinkRueRoyaleSlotMachine.Instance.isFreeGameReady || UltimateFireLinkRueRoyaleSlotMachine.Instance.isBonusGame)
                break;

            UltimateFireLinkRueRoyaleUIManager.Instance.SetStopInteractable(true);

            if (UltimateFireLinkRueRoyaleSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => UltimateFireLinkRueRoyaleSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => UltimateFireLinkRueRoyaleUIManager.Instance.winAnimationCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        UltimateFireLinkRueRoyaleUIManager.Instance.autoStopButton.ShowButton(false);
        UltimateFireLinkRueRoyaleUIManager.Instance.autoButton.ShowButton(true);
        UltimateFireLinkRueRoyaleUIManager.Instance.SetAutoInteractable(true);
        if (!UltimateFireLinkRueRoyaleSlotMachine.Instance.isFreeGameReady)
        {
            UltimateFireLinkRueRoyaleUIManager.Instance.UpdateButtons("Idle");
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
