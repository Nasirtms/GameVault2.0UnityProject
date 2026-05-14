using UnityEngine;
using System.Collections;

public class StarBurstSlotsAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || StarBurstSlotsSlotMachine.Instance.InSpin) return;

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

            if (cancelRequested || StarBurstSlotsSlotMachine.Instance.makeFreeGameReady || StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                yield break;
            }

            StarBurstSlotsUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (StarBurstSlotsUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(StarBurstSlotsUIManager.Instance.textAnimationCoroutine);

            if (StarBurstSlotsUIManager.Instance.winCoroutine != null)
                StopCoroutine(StarBurstSlotsUIManager.Instance.winCoroutine);

            if (StarBurstSlotsSlotMachine.Instance.makeFreeGameReady || StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                break;
            }

            SlotSpinService.Instance.Spin(betAmount);

            if (StarBurstSlotsUIManager.Instance.CurrentButtonSet() != "Auto Start")
                StarBurstSlotsUIManager.Instance.UpdateButtons("Auto Start");

            yield return new WaitUntil(() => StarBurstSlotsSlotMachine.Instance.isSpinAgain);

            if (cancelRequested || StarBurstSlotsSlotMachine.Instance.makeFreeGameReady || StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                break;
            }

            //Debug.Log("Has free game: " + StarBurstSlotsSlotMachine.Instance.isFreeGameReady);
            if (StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
                break;
           
            if (StarBurstSlotsSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => StarBurstSlotsSlotMachine.Instance.isPaylineCompleted);
                yield return new WaitUntil(() => StarBurstSlotsUIManager.Instance.winAnimationCompleted);
            }
            else
            {
                StarBurstSlotsUIManager.Instance.winAnimationCompleted = true;
            }

            if (StarBurstSlotsSlotMachine.Instance.makeFreeGameReady || StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    public void StopAutoSpin()
    {
        if (StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
        {
            StarBurstSlotsUIManager.Instance.UpdateButtons("Free Game Transition");
        }
        else
        {
            StarBurstSlotsUIManager.Instance.UpdateButtons("Auto Stop");
        }
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
        if(StarBurstSlotsSlotMachine.Instance.isFreeSpinWhenNoPayline)
        {
            StarBurstSlotsSlotMachine.Instance.isFreeSpinWhenNoPayline = false;
            StarBurstSlotsSlotMachine.Instance.isFreeGameReady = false;
        }
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
