using UnityEngine;
using System.Collections;

public class GoldRushGusAutoSpinController : MonoBehaviour
{
    #region Variables
    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private bool firstAuto;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;
    private int remainingSpins = -1;
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
    public void SetSpinCount(int count)
    {
        remainingSpins = count;
    }
    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || GoldRushGusSlotMachine.Instance.InSpin) return;

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
        while (!cancelRequested && (remainingSpins == -1 || remainingSpins > 0))
        {
            if (!firstAuto)
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }
            else
            {
                firstAuto = false;
            }

            GoldRushGusUIManager.Instance.winAnimationCompleted = true;
            GoldRushGusUIManager.Instance.isTreasureAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (GoldRushGusUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(GoldRushGusUIManager.Instance.textAnimationCoroutine);

            if (GoldRushGusUIManager.Instance.winCoroutine != null)
                StopCoroutine(GoldRushGusUIManager.Instance.winCoroutine);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            SlotSpinService.Instance.Spin(betAmount);

            if (GoldRushGusUIManager.Instance.CurrentButtonSet() != "Auto")
                GoldRushGusUIManager.Instance.UpdateButtons("Auto");
                
            if (remainingSpins > 0)
                remainingSpins--;

            //if (remainingSpins == 0)
            //    isAutoSpinning = false;

            GoldRushGusUIManager.Instance.UpdateRemainingSpins(remainingSpins);

            yield return new WaitUntil(() => GoldRushGusSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (GoldRushGusSlotMachine.Instance.isFreeGameReady || GoldRushGusSlotMachine.Instance.isMiniGameReady)
                break;

            if (GoldRushGusSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => GoldRushGusSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => GoldRushGusUIManager.Instance.winAnimationCompleted);
            }
            yield return new WaitUntil(() => GoldRushGusUIManager.Instance.isTreasureAnimationCompleted);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        GoldRushGusUIManager.Instance.HideSpinCount();
        if (GoldRushGusSlotMachine.Instance.isFreeGameReady || GoldRushGusSlotMachine.Instance.isMiniGameReady)
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Auto Stop");
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