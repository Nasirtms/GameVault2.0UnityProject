using UnityEngine;
using System.Collections;

public class SuperBombAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || SuperBombSlotMachine.Instance.InSpin) return;

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

            if (cancelRequested || SuperBombSlotMachine.Instance.makeFreeGameReady || SuperBombSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                yield break;
            }

            SuperBombUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (SuperBombUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(SuperBombUIManager.Instance.textAnimationCoroutine);

            if (SuperBombUIManager.Instance.winCoroutine != null)
                StopCoroutine(SuperBombUIManager.Instance.winCoroutine);

            if (SuperBombSlotMachine.Instance.makeFreeGameReady || SuperBombSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                break;
            }

            SlotSpinService.Instance.Spin(betAmount);

            if (SuperBombUIManager.Instance.CurrentButtonSet() != "Auto Start")
                SuperBombUIManager.Instance.UpdateButtons("Auto Start");

            yield return new WaitUntil(() => SuperBombSlotMachine.Instance.isSpinAgain);

            if (cancelRequested || SuperBombSlotMachine.Instance.makeFreeGameReady || SuperBombSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                break;
            }

            //Debug.Log("Has free game: " + SuperBombSlotMachine.Instance.isFreeGameReady);
            if (SuperBombSlotMachine.Instance.isFreeGameReady)
                break;
           
            if (SuperBombSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => SuperBombSlotMachine.Instance.isPaylineCompleted);
                yield return new WaitUntil(() => SuperBombUIManager.Instance.winAnimationCompleted);
                yield return new WaitUntil(() => SuperBombSlotMachine.Instance.hasShowedTotalWin);
            }
            else
            {
                SuperBombUIManager.Instance.winAnimationCompleted = true;
            }

            if (SuperBombSlotMachine.Instance.makeFreeGameReady || SuperBombSlotMachine.Instance.isFreeGameReady)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    public void StopAutoSpin()
    {
        if (SuperBombSlotMachine.Instance.isFreeGameReady)
        {
            SuperBombUIManager.Instance.UpdateButtons("Free Game Transition");
        }
        else
        {
            SuperBombUIManager.Instance.UpdateButtons("Auto Stop");
        }
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
        if(SuperBombSlotMachine.Instance.isFreeSpinWhenNoPayline)
        {
            SuperBombSlotMachine.Instance.isFreeSpinWhenNoPayline = false;
            SuperBombSlotMachine.Instance.isFreeGameReady = false;
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
