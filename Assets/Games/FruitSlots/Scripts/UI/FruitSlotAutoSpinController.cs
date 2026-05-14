using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSlotAutoSpinController : MonoBehaviour
{
    [Header("Settings")]
    public float delayBetweenSpins = 1.5f;
    private FruitSlotPaylineController paylineController;

    public bool isAutoRunning = false;
    public bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    public bool IsAutoRunning => isAutoRunning;

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
    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || FruitSlotMachine.Instance.InSpin)
        {
            return;
        }

        isAutoRunning = true;
        isAutoSpinning = true;
        cancelRequested = false;

        autoSpinRoutine = StartCoroutine(AutoSpinLoop(betAmount));
    }
    public void CancelAutoSpin()
    {
        cancelRequested = true;
        FruitSlotMachine.Instance.Stop();
        if (autoSpinRoutine != null)
        {
            StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = null;
        }

        StopAutoSpin();
    }

    private IEnumerator AutoSpinLoop(float betAmount)
    {
        while (!cancelRequested)
        {
            FruitSlotUIManager.Instance.winAnimationCompleted = true;

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (FruitSlotUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(FruitSlotUIManager.Instance.textAnimationCoroutine);
            if (FruitSlotUIManager.Instance.winCoroutine != null)
                StopCoroutine(FruitSlotUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);
            yield return new WaitUntil(() => !FruitSlotMachine.Instance.InSpin);

            if (cancelRequested) break;

            if (FruitSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => FruitSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => FruitSlotUIManager.Instance.winAnimationCompleted);

            yield return new WaitForSeconds(delayBetweenSpins);

            if (FruitSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
        if (!FruitSlotMachine.Instance.isFreeGameReady)
        {
            FruitSlotUIManager.Instance.UpdateButtons("Stop");
        }
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
}
