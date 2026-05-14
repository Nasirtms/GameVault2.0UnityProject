using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkChinaStreetAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || UltimateFireLinkChinaStreetSlotMachine.Instance.InSpin) return;

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
            UltimateFireLinkChinaStreetUIManager.Instance.winAnimationCompleted = true;
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (UltimateFireLinkChinaStreetUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(UltimateFireLinkChinaStreetUIManager.Instance.textAnimationCoroutine);
            if (UltimateFireLinkChinaStreetUIManager.Instance.winCoroutine != null)
                StopCoroutine(UltimateFireLinkChinaStreetUIManager.Instance.winCoroutine);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            //SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGameReady || UltimateFireLinkChinaStreetSlotMachine.Instance.isBonusGame)
                break;

            UltimateFireLinkChinaStreetUIManager.Instance.SetStopInteractable(true);

            if (UltimateFireLinkChinaStreetSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => UltimateFireLinkChinaStreetUIManager.Instance.winAnimationCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        UltimateFireLinkChinaStreetUIManager.Instance.autoStopButton.ShowButton(false);
        UltimateFireLinkChinaStreetUIManager.Instance.autoButton.ShowButton(true);
        UltimateFireLinkChinaStreetUIManager.Instance.SetAutoInteractable(true);
        if (!UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGameReady)
        {
            UltimateFireLinkChinaStreetUIManager.Instance.UpdateButtons("Idle");
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
