using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StarBurstSlotsFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private TMP_Text freeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;
    private int paylinesToPlay;

    private Coroutine freeSpinRoutine;
    private bool cancelRequested;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += CancelFreeSpins;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= CancelFreeSpins;
    }
    #endregion

    #region Public References

    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        cancelRequested = false;
        isFreeGame = true;
        firstSpin = true;
        freeSpinDone = 0;

        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        totalFreeSpins += freeSpins;
        Debug.Log(" Deepak from free spin controller updating toatal free spins: " + totalFreeSpins);
    }

    private void Start()
    {
        paylinesToPlay = StarBurstSlotsPaylineController.Instance.activePaylines.Count;
    }

    #endregion

    #region Free Spin

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

        while (freeSpinDone < totalFreeSpins)
        {
            Debug.Log(" Total Free Spins: " + (totalFreeSpins));
            Debug.Log(" Free Spins Done: " + (freeSpinDone));
            Debug.Log(" Free Spins Left: " + (totalFreeSpins - freeSpinDone));
            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins); // optional delay between spins
            }

            if (cancelRequested) yield break;

            float betAmount = StarBurstSlotsUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);


            yield return new WaitUntil(() => StarBurstSlotsSlotMachine.Instance.isSpinAgain);

            //if (StarBurstSlotsSlotMachine.Instance.GetWinAmount() > 0)
            //{
            //    yield return new WaitUntil(() => StarBurstSlotsSlotMachine.Instance.isSlotAnimationCompleted);
            //    yield return new WaitUntil(() => StarBurstSlotsSlotMachine.Instance.isPaylineCompleted);
            //}
            if (cancelRequested) yield break;
            if (StarBurstSlotsPaylineController.Instance != null)
            {
                paylinesToPlay = StarBurstSlotsPaylineController.Instance.activePaylines.Count;
            }

            yield return new WaitForSeconds(1.8f * paylinesToPlay);

            freeSpinDone++;
            Debug.Log(" Free Spin Done: " + freeSpinDone);
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;

        StarBurstSlotsSlotMachine.Instance.isFreeGame = false;

        StarBurstSlotsUIManager.Instance.freeGameSpinCount = 0;
        Debug.Log("End free spins called");
        StarBurstSlotsFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    private void CancelFreeSpins()
    {
        if (!isFreeGame) return;

        cancelRequested = true;

        if (freeSpinRoutine != null)
        {
            StopCoroutine(freeSpinRoutine);
            freeSpinRoutine = null;
        }
        StarBurstSlotsUIManager.Instance.UpdateButtons("Base Game Transition");
    }
    #endregion
}
