using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameComboAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || FlameComboSlotMachine.Instance.InSpin)
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
            FlameComboUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (FlameComboSlotMachine.Instance.isFreeGameReady)
                break;
            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (FlameComboUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(FlameComboUIManager.Instance.textAnimationCoroutine);
            if (FlameComboUIManager.Instance.winCoroutine != null)
                StopCoroutine(FlameComboUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => FlameComboSlotMachine.Instance.isSpinAgain);
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (FlameComboSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => FlameComboSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => FlameComboUIManager.Instance.winAnimationCompleted);

            

            if (FlameComboSlotMachine.Instance.isFreeGameReady)
                break;
        }
        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (FlameComboSlotMachine.Instance.isFreeGameReady)
        {
            FlameComboUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            FlameComboUIManager.Instance.UpdateButtons("Stop");
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