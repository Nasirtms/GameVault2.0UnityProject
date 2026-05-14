using UnityEngine;
using System.Collections;

public class SaharaRichesAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || SaharaRichesSlotMachine.Instance.InSpin) return;


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
            SaharaRichesUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (SaharaRichesUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(SaharaRichesUIManager.Instance.textAnimationCoroutine);

            if (SaharaRichesUIManager.Instance.winCoroutine != null)
                StopCoroutine(SaharaRichesUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (SaharaRichesUIManager.Instance.CurrentButtonSet() != "Auto")
                SaharaRichesUIManager.Instance.UpdateButtons("Auto");

            yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isSpinAgain);
            //yield return new WaitUntil(() => !SaharaRichesSlotMachine.Instance.InSpin);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (SaharaRichesSlotMachine.Instance.isFreeGameReady || SaharaRichesSlotMachine.Instance.currentSpinResult.isBonusGame)
                break;

            if (SaharaRichesSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isDiamondCollectionCompleted);
                yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isCoinCollectionCompleted);
                yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isFreeSpinCollectionCompleted);
                yield return new WaitUntil(() => SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => SaharaRichesUIManager.Instance.winAnimationCompleted);
            }
            yield return new WaitUntil(() => SaharaRichesJackpotAnimator.Instance.isJackpotCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (SaharaRichesSlotMachine.Instance.isFreeGameReady)
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Auto Stop");
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
