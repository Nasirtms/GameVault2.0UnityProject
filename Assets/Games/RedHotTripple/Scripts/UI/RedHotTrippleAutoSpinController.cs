using UnityEngine;
using System.Collections;

public class RedHotTrippleAutoSpinController : MonoBehaviour
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

    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || RedHotTrippleSlotMachine.Instance.InSpin) return;

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
            RedHotTrippleUIManager.Instance.winAnimationCompleted = true;

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

            if (RedHotTrippleUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(RedHotTrippleUIManager.Instance.textAnimationCoroutine);
            if (RedHotTrippleUIManager.Instance.winCoroutine != null)
                StopCoroutine(RedHotTrippleUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            //RedHotTrippleUIManager.Instance.PlaySpinMusic("Spin");
            SlotSpinService.Instance.Spin(betAmount);

            if (RedHotTrippleUIManager.Instance.CurrentButtonSet() != "Auto")
                RedHotTrippleUIManager.Instance.UpdateButtons("Auto");

            yield return new WaitUntil(() => RedHotTrippleSlotMachine.Instance.isSpinAgain);

            if (cancelRequested || RedHotTrippleSlotMachine.Instance.isFreeGameReady)
                break;

            if (RedHotTrippleSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => RedHotTrippleSlotMachine.Instance.isPaylineCompleted);
            }

            yield return new WaitUntil(() => RedHotTrippleUIManager.Instance.winAnimationCompleted);

            if (cancelRequested || RedHotTrippleSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (RedHotTrippleSlotMachine.Instance.isFreeGameReady)
        {
            RedHotTrippleUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            RedHotTrippleUIManager.Instance.UpdateButtons("Auto Stop");
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