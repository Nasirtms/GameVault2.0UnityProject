using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoldenDragonFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private TMP_Text freeSpinsText;

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

    public void InitialFreeSpinText()
    {
        freeSpinsText.text = $"Free Spins : 0/{totalFreeSpins}";
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
    #endregion

    #region Free Spin

    private void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"Free Spins : {freeSpinDone + 1}/{totalFreeSpins}";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(1f);

        while (freeSpinDone < totalFreeSpins)
        {
            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }

            float betAmount = GoldenDragonUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount();

            freeSpinDone++;

            yield return new WaitUntil(() => GoldenDragonSlotMachine.Instance.isSpinAgain);

            if (GoldenDragonSlotMachine.Instance.currentSpinResult != null)
            {
                if (GoldenDragonSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => GoldenDragonSlotMachine.Instance.isPaylineCompleted);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;
        GoldenDragonSlotMachine.Instance.isFreeGame = false;

        GoldenDragonFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }

    #endregion
}