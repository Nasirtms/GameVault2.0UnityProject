using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldenDragonAutoSpinController : MonoBehaviour
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


    #region Public References
    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || GoldenDragonSlotMachine.Instance.InSpin)
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
            GoldenDragonUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested || GoldenDragonSlotMachine.Instance.isMiniGame)
            {
                StopAutoSpin();
                break;
            }
            //GoldenDragonUIManager.Instance.PlaySound("FruitParadise_Spin");

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (GoldenDragonUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(GoldenDragonUIManager.Instance.textAnimationCoroutine);
            if (GoldenDragonUIManager.Instance.winCoroutine != null)
                StopCoroutine(GoldenDragonUIManager.Instance.winCoroutine);


            yield return new WaitUntil(() => GoldenDragonSlotMachine.Instance.isSpinAgain);

            if (GoldenDragonSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => GoldenDragonSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => GoldenDragonUIManager.Instance.winAnimationCompleted);

            if (cancelRequested || GoldenDragonSlotMachine.Instance.isMiniGame)
            {
                StopAutoSpin();
                break;
            }

            if (GoldenDragonSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!GoldenDragonSlotMachine.Instance.isFreeGameReady)
        {
            GoldenDragonUIManager.Instance.UpdateButtons("Stop");
        }
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }
    #endregion
}
