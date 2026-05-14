using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PandaFortuneAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || PandaFortuneSlotMachine.Instance.InSpin) return;


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
            PandaFortuneUIManager.Instance.winAnimationCompleted = true;
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (PandaFortuneUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(PandaFortuneUIManager.Instance.textAnimationCoroutine);
            if (PandaFortuneUIManager.Instance.winCoroutine != null)
                StopCoroutine(PandaFortuneUIManager.Instance.winCoroutine);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            SlotSpinService.Instance.Spin(betAmount);

            if (PandaFortuneUIManager.Instance.CurrentButtonSet() != "Spin")
                PandaFortuneUIManager.Instance.UpdateButtons("Spin");

            PandaFortuneUIManager.Instance.stopButton.GetButtonComponent().interactable = false;

            yield return new WaitUntil(() => PandaFortuneSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (PandaFortuneSlotMachine.Instance.isFreeGameReady || PandaFortuneSlotMachine.Instance.currentSpinResult.isBonusGame)
                break;

            PandaFortuneUIManager.Instance.SetStopInteractable(true);

            if (PandaFortuneSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => PandaFortuneSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => PandaFortuneUIManager.Instance.winAnimationCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!PandaFortuneSlotMachine.Instance.isFreeGameReady)
        {
            PandaFortuneUIManager.Instance.UpdateButtons("Stop");
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
