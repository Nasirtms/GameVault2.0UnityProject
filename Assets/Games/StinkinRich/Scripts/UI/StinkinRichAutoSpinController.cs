using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StinkinRichAutoSpinController : MonoBehaviour
{
    #region Variables
    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private int remainingSpins = -1;
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
        if (isAutoRunning || StinkinRichSlotMachine.Instance.InSpin) return;


        firstAuto = true;
        isAutoRunning = true;
        isAutoSpinning = true;
        cancelRequested = false;

        autoSpinRoutine = StartCoroutine(AutoSpinLoop(betAmount));
    }

    public void SetSpinCount(int count)
    {
        remainingSpins = count;
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
            StinkinRichUIManager.Instance.winAnimationCompleted = true;
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            if (StinkinRichUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(StinkinRichUIManager.Instance.textAnimationCoroutine);
            if (StinkinRichUIManager.Instance.winCoroutine != null)
                StopCoroutine(StinkinRichUIManager.Instance.winCoroutine);

            if (remainingSpins > 0)
            {
                remainingSpins--;
            }

            if (remainingSpins == 0)
            {
                isAutoSpinning = false;
            }
            StinkinRichUIManager.Instance.UpdateRemainingSpins(remainingSpins);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            //SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => StinkinRichSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (StinkinRichSlotMachine.Instance.isFreeGameReady || StinkinRichSlotMachine.Instance.isBonusGame)
                break;

            StinkinRichUIManager.Instance.SetStopInteractable(true);

            if (StinkinRichSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => StinkinRichSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => StinkinRichUIManager.Instance.winAnimationCompleted);
            yield return new WaitForSeconds(delayBetweenSpins);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        StinkinRichUIManager.Instance.HideSpinCount();
        StinkinRichUIManager.Instance.autoStopButton.ShowButton(false);
        StinkinRichUIManager.Instance.autoButton.ShowButton(true);
        StinkinRichUIManager.Instance.SetAutoInteractable(true);
        StinkinRichUIManager.Instance.remainingSpins.gameObject.SetActive(false);
        if (!StinkinRichSlotMachine.Instance.isFreeGameReady)
        {
            StinkinRichUIManager.Instance.UpdateButtons("Idle");
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
