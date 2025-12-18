using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TheGreenMachineDeluxeFreeSpinController : MonoBehaviour
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
        freeSpinsText.text = $"Free Spin <color=orange>0</color> of <color=orange>{totalFreeSpins}</color>";
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
            freeSpinsText.text = $"Free Spin <color=orange>{freeSpinDone + 1}</color> of <color=orange>{totalFreeSpins}</color>";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

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

            float betAmount = TheGreenMachineDeluxeUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount();

            freeSpinDone++;

            yield return new WaitUntil(() => TheGreenMachineDeluxeSlotMachine.Instance.isSpinAgain);

            if (TheGreenMachineDeluxeSlotMachine.Instance.currentSpinResult != null)
            {
                if (TheGreenMachineDeluxeSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => TheGreenMachineDeluxeSlotMachine.Instance.isSlotAnimationCompleted);
                }
            }
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;

        //TheGreenMachineDeluxeSlotMachine.Instance.StopAllSlotAnimations();
        TheGreenMachineDeluxeSlotMachine.Instance.isFreeGame = false;

        TheGreenMachineDeluxeUIManager.Instance.freeGameSpinCount = 0;

        TheGreenMachineDeluxeFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }

    #endregion
}
