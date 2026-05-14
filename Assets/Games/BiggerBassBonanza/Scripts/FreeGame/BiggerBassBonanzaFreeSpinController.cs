using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BiggerBassBonanzaFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private TMP_Text freeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;

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
        UpdateSpinCount();
    }

    #endregion

    #region Free Spin

    private void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{totalFreeSpins - freeSpinDone}";
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

            if (cancelRequested) yield break;
            float betAmount = BiggerBassBonanzaUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            freeSpinDone++;

            UpdateSpinCount();

            yield return new WaitUntil(() => !BiggerBassBonanzaSlotMachine.Instance.InSpin);

            if (cancelRequested) yield break;
            if (BiggerBassBonanzaSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => BiggerBassBonanzaSlotMachine.Instance.isFishCollectionCompleted);
                yield return new WaitUntil(() => BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted);
            }
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        if (BiggerBassBonanzaSlotMachine.Instance.wildCount >= 12 && BiggerBassBonanzaSlotMachine.Instance.retriggerCount == 2)
        {
            BiggerBassBonanzaFreeGameTransitionController.Instance.RetriggerFreeSpinTransition();
        }
        else if (BiggerBassBonanzaSlotMachine.Instance.wildCount >= 8 && BiggerBassBonanzaSlotMachine.Instance.retriggerCount == 1)
        {
            BiggerBassBonanzaFreeGameTransitionController.Instance.RetriggerFreeSpinTransition();
        }
        else if (BiggerBassBonanzaSlotMachine.Instance.wildCount >= 4 && BiggerBassBonanzaSlotMachine.Instance.retriggerCount == 0)
        {
            BiggerBassBonanzaFreeGameTransitionController.Instance.RetriggerFreeSpinTransition();
        }
        else
        {
            BiggerBassBonanzaSlotMachine.Instance.isFreeGame = false;
            BiggerBassBonanzaFreeGameTransitionController.Instance.EndFreeSpinTransition();
        }
        
        isFreeGame = false;
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
        BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
    }
    #endregion
}
