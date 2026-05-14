using UnityEngine;
using System.Collections;

public class WildXTrioAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || WildXTrioSlotMachine.Instance.InSpin) return;

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

            WildXTrioUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (WildXTrioUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(WildXTrioUIManager.Instance.textAnimationCoroutine);

            if (WildXTrioUIManager.Instance.winCoroutine != null)
                StopCoroutine(WildXTrioUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (WildXTrioUIManager.Instance.CurrentButtonSet() != "Auto")
                WildXTrioUIManager.Instance.UpdateButtons("Auto");

            yield return new WaitUntil(() => WildXTrioSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            
            if (WildXTrioSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => WildXTrioSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => WildXTrioUIManager.Instance.winAnimationCompleted);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        WildXTrioUIManager.Instance.UpdateButtons("Auto Stop");
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