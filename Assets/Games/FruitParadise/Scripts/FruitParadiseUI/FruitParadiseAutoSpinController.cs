using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitParadiseAutoSpinController : MonoBehaviour
{
    [Header("Settings")]
    public float delayBetweenSpins = 1.5f;

    private FruitParadisePaylineController paylineController;

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
        if (isAutoRunning || FruitParadiseSlotMachine.Instance.InSpin)
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
        FruitParadiseSlotMachine.Instance.Stop();
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
            FruitParadiseUIManager.Instance.winAnimationCompleted = true;

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (FruitParadiseUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(FruitParadiseUIManager.Instance.textAnimationCoroutine);
            if (FruitParadiseUIManager.Instance.winCoroutine != null)
                StopCoroutine(FruitParadiseUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => !FruitParadiseSlotMachine.Instance.InSpin);

            if (cancelRequested) break;

            if (FruitParadiseSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => FruitParadiseSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => FruitParadiseUIManager.Instance.winAnimationCompleted);

            yield return new WaitForSeconds(delayBetweenSpins);

            if (FruitParadiseSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!FruitParadiseSlotMachine.Instance.isFreeGameReady)
        {
            FruitParadiseUIManager.Instance.UpdateButtons("Stop");
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
}
