using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AtomicMeltdownFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;

    #endregion

    #region Public References

    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        isFreeGame = true;
        firstSpin = true;
        freeSpinDone = 0;
        AtomicMeltdownUIManager.Instance.StopMusic("Background");
        StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        totalFreeSpins += freeSpins;
    }

    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
    }

    #endregion

    #region Free Spin

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

        AtomicMeltdownUIManager.Instance.UpdateButtons("FreeSpin");

        while (freeSpinDone < totalFreeSpins)
        {
            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins); // optional delay between spins
            }

            float betAmount = AtomicMeltdownUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            Debug.Log("Network Free Spins Done: " + freeSpinDone);

            freeSpinDone++;

            yield return new WaitUntil(() => AtomicMeltdownSlotMachine.Instance.isSpinAgain);

            if (AtomicMeltdownSlotMachine.Instance.currentSpinResult != null)
            {
                if (AtomicMeltdownSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => AtomicMeltdownSlotMachine.Instance.isPaylineCompleted);
                }
            }
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;
        AtomicMeltdownSlotMachine.Instance.isFreeGame = false;
        AtomicMeltdownFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }

    #endregion
}
