using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UltimateFireLinkChinaStreetFreeSpinController : MonoBehaviour
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
        UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGame = true;
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
        yield return new WaitForSeconds(2.5f); 

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

            float betAmount = UltimateFireLinkChinaStreetUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSpinAgain);
            if (freeSpinDone == 0)
            {
                UltimateFireLinkChinaStreetSlotMachine.Instance.firstFreeSpin = false;
            }

            if (UltimateFireLinkChinaStreetSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted);
            }

            freeSpinDone++;
            UpdateSpinCount();
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        freeSpinsText.gameObject.SetActive(false);
        ResetFreeSpins();
        isFreeGame = false;
        UltimateFireLinkChinaStreetFreeGameTransitionController.Instance.EndFreeSpin();
        UltimateFireLinkChinaStreetSlotMachine.Instance.isFreeGame = false;
        UltimateFireLinkChinaStreetUIManager.Instance.UpdateButtons("FreeSpinEnd");
    }
    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{freeSpinDone} of {totalFreeSpins}";
    }

    #endregion
}
