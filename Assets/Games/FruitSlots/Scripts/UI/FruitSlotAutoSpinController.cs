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

    private void Start()
    {
    }

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
            FruitSlotUIManager.Instance.PlaySound("FruitSlot_Spin");

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (FruitSlotUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(FruitSlotUIManager.Instance.textAnimationCoroutine);
            if (FruitSlotUIManager.Instance.winCoroutine != null)
                StopCoroutine(FruitSlotUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);
            yield return new WaitUntil(() => !FruitSlotMachine.Instance.InSpin);
            if (FruitSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => FruitSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => FruitSlotUIManager.Instance.winAnimationCompleted);

            if (cancelRequested) break;

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
}
