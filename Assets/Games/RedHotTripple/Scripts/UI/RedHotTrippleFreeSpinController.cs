using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RedHotTrippleFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private float delayBetweenSpins = 1.5f;
    [SerializeField] private TMP_Text freeSpinsText;
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
    public void InitialFreeSpinText()
    {
        freeSpinsText.text = $"0/{totalFreeSpins}";
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{freeSpinDone}/{totalFreeSpins}";
    }
    #endregion

    #region Free Spin

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

            if (cancelRequested) yield break;
            float betAmount = RedHotTrippleUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            freeSpinDone++;
            UpdateSpinCount();
            yield return new WaitUntil(() => RedHotTrippleSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;
            if (RedHotTrippleSlotMachine.Instance.currentSpinResult != null)
            {
                if (RedHotTrippleSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => RedHotTrippleSlotMachine.Instance.isPaylineCompleted);
                }
            }
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        freeSpinDone = 0;
        totalFreeSpins = 0;
        isFreeGame = false;
        RedHotTrippleSlotMachine.Instance.isFreeGame = false;
        RedHotTrippleSlotMachine.Instance.UpdateCachedSymbols();
        RedHotTrippleFreeGameTransitionController.Instance.EndFreeSpinTransition();
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
        RedHotTrippleUIManager.Instance.UpdateButtons("Stop");
    }
    #endregion
}