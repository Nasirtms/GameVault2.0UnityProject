using UnityEngine;
using System.Collections;

public class MonkeyMadnessAutoSpinController : MonoBehaviour
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

    private void Start()
    {
    }

    #endregion

    #region Public References

    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || MonkeyMadnessSlotMachine.Instance.InSpin) return;

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
        MonkeyMadnessSlotMachine.Instance.Stop();
    }

    #endregion

    #region Auto Spin

    private IEnumerator AutoSpinLoop(float betAmount)
    {
        while (!cancelRequested)
        {
            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            MonkeyMadnessUIManager.Instance.winAnimationCompleted = true;
            MonkeyMadnessUIManager.Instance.PlaySound("Spin");
            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (MonkeyMadnessUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(MonkeyMadnessUIManager.Instance.textAnimationCoroutine);

            if (MonkeyMadnessUIManager.Instance.winCoroutine != null)
                StopCoroutine(MonkeyMadnessUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (MonkeyMadnessUIManager.Instance.CurrentButtonSet() != "Spin")
                MonkeyMadnessUIManager.Instance.UpdateButtons("Spin");

            yield return new WaitUntil(() => MonkeyMadnessSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (MonkeyMadnessSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => MonkeyMadnessSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => MonkeyMadnessUIManager.Instance.winAnimationCompleted);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        MonkeyMadnessUIManager.Instance.UpdateButtons("Stop");

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
