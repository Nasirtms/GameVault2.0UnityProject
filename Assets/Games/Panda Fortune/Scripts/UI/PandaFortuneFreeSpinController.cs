using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PandaFortuneFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private TMP_Text freeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;
    //private int paylinesToPlay;

    #endregion

    #region Public References

    public void StartFreeSpins()
    {
        if (isFreeGame) return;
        freeSpinsText.gameObject.SetActive(true);
        PandaFortuneSlotMachine.Instance.isFreeGame = true;
        isFreeGame = true;
        firstSpin = true;
        freeSpinDone = 0;

        StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        freeSpinDone = 0;
        totalFreeSpins = 0;
        UpdateSpinCount();
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        totalFreeSpins += freeSpins;
        UpdateSpinCount();
    }

    private void Start()
    {
        //paylinesToPlay = StarBurstSlotsPaylineController.Instance.activePaylines.Count;
    }

    #endregion

    #region Free Spin


    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

        while (freeSpinDone < totalFreeSpins)
        {
            //Debug.Log(" Total Free Spins: " + (totalFreeSpins));
            //Debug.Log(" Free Spins Done: " + (freeSpinDone));
            //Debug.Log(" Free Spins Left: " + (totalFreeSpins - freeSpinDone));
            if (firstSpin)
            {
                firstSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(delayBetweenSpins); // optional delay between spins
            }

            float betAmount = PandaFortuneUIManager.Instance.CurrentBet();
            //Debug.Log("spining the free spi")
            SlotSpinService.Instance.Spin(betAmount);

            Debug.Log("Deepak has a spin again - 10" + PandaFortuneSlotMachine.Instance.isSpinAgain);

            yield return new WaitUntil(() => PandaFortuneSlotMachine.Instance.isSpinAgain);
            //yield return new WaitForSeconds(4f);
            if (freeSpinDone == 0)
            {
                PandaFortuneSlotMachine.Instance.firstFreeSpin = false;
            }

            if (PandaFortuneSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => PandaFortuneSlotMachine.Instance.isSlotAnimationCompleted);
            }

            //yield return new WaitForSeconds(1.8f * paylinesToPlay);

            freeSpinDone++;
            UpdateSpinCount();
            //Debug.Log(" Free Spin Done: " + freeSpinDone);
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        freeSpinsText.gameObject.SetActive(false);
        ResetFreeSpins();
        isFreeGame = false;
        PandaFortuneFreeGameTransitionController.Instance.EndFreeSpin();
        PandaFortuneSlotMachine.Instance.isFreeGame = false;
        PandaFortuneSlotMachine.Instance.isFreeGameTwo = false;
        for (int i = 0; i < PandaFortuneSlotMachine.Instance.frozenReelsFreeGame2.Length; i++)
        {
            if (PandaFortuneSlotMachine.Instance.frozenReelsFreeGame2[i])
            {
                PandaFortuneSlotMachine.Instance.frozenReelsFreeGame2[i] = false;
            }
        }
        PandaFortuneUIManager.Instance.UpdateButtons("FreeSpinEnd");

        //PandaFortuneUIManager.Instance.freeGameSpinCount = 0;
        //Debug.Log("End free spins called");
        //StarBurstSlotsFreeGameTransitionController.Instance.EndFreeSpinTransition();
    }
    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"Free Spin : {freeSpinDone}/{totalFreeSpins}";
    }

    #endregion
}
