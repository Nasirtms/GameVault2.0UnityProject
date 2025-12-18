using UnityEngine;
using System.Collections;

public class DoubleJackpotBullseyeAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 2.5f;


    private bool firstAuto;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    #endregion

    #region Unity Methods

    private void Start()
    {
    }

    #endregion

    #region Public References

    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || DoubleJackpotBullseyeSlotMachine.Instance.InSpin) return;

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
            if(DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame)
            {
                yield return new WaitForSeconds(1f);
            }

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            float balance = UserManager.Instance.Coins;

            DoubleJackpotBullseyeUIManager.Instance.winAnimationCompleted = true;
            DoubleJackpotBullseyeUIManager.Instance.PlaySpinMusic("Spin");

            if (DoubleJackpotBullseyeUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(DoubleJackpotBullseyeUIManager.Instance.textAnimationCoroutine);
            if (DoubleJackpotBullseyeUIManager.Instance.winCoroutine != null)
                StopCoroutine(DoubleJackpotBullseyeUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);
            if (DoubleJackpotBullseyeUIManager.Instance.CurrentButtonSet() != "Spin")
                DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Spin");

            yield return new WaitUntil(() => DoubleJackpotBullseyeSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (DoubleJackpotBullseyeSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => DoubleJackpotBullseyeSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => DoubleJackpotBullseyeUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (DoubleJackpotBullseyeSlotMachine.Instance.isFreeGameReady || DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!DoubleJackpotBullseyeSlotMachine.Instance.isFreeGameReady)
        {
            DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Stop");
        }
        

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
