using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkRoute66AutoSpinController : MonoBehaviour
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
        if (isAutoRunning || UltimateFireLinkRoute66SlotMachine.Instance.InSpin) return;

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
            UltimateFireLinkRoute66UIManager.Instance.winAnimationCompleted = true;
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (UltimateFireLinkRoute66UIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(UltimateFireLinkRoute66UIManager.Instance.textAnimationCoroutine);
            if (UltimateFireLinkRoute66UIManager.Instance.winCoroutine != null)
                StopCoroutine(UltimateFireLinkRoute66UIManager.Instance.winCoroutine);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            //SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => UltimateFireLinkRoute66SlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (UltimateFireLinkRoute66SlotMachine.Instance.isFreeGameReady || UltimateFireLinkRoute66SlotMachine.Instance.isBonusGame)
                break;

            UltimateFireLinkRoute66UIManager.Instance.SetStopInteractable(true);

            if (UltimateFireLinkRoute66SlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => UltimateFireLinkRoute66SlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => UltimateFireLinkRoute66UIManager.Instance.winAnimationCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        UltimateFireLinkRoute66UIManager.Instance.autoStopButton.ShowButton(false);
        UltimateFireLinkRoute66UIManager.Instance.autoButton.ShowButton(true);
        UltimateFireLinkRoute66UIManager.Instance.SetAutoInteractable(true);
        if (!UltimateFireLinkRoute66SlotMachine.Instance.isFreeGameReady)
        {
            UltimateFireLinkRoute66UIManager.Instance.UpdateButtons("Idle");
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
