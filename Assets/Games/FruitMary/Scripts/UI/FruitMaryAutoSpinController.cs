using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitMaryAutoSpinController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;


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
        if (isAutoRunning || FruitMarySlotMachine.Instance.InSpin)
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
        FruitMarySlotMachine.Instance.Stop();
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
            FruitMaryUIManager.Instance.PlaySound("Spin");

            float balance = UserManager.Instance.Coins;
            FruitMaryUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (FruitMaryUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(FruitMaryUIManager.Instance.textAnimationCoroutine);

            if (FruitMaryUIManager.Instance.winCoroutine != null)
                StopCoroutine(FruitMaryUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (cancelRequested) break;

            yield return new WaitUntil(() => !FruitMarySlotMachine.Instance.InSpin);

            if (FruitMarySlotMachine.Instance.isFreeGameReady)
                break;
            if (FruitMarySlotMachine.Instance.isFruitMaryGameReady)
                break;

            if (FruitMarySlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => FruitMarySlotMachine.Instance.isPaylineCompleted);
                yield return new WaitUntil(() => FruitMaryUIManager.Instance.winAnimationCompleted);
            }
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
        if (!FruitMarySlotMachine.Instance.isFreeGameReady)
        {
            FruitMaryUIManager.Instance.UpdateButtons("Stop");
        }
    }
}
