using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class IrishPotLuckFreeSpinController : MonoBehaviour
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

    public void InitialFreeSpinText()
    {
        freeSpinsText.text = $"0 / {totalFreeSpins}";
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
    #endregion

    #region Free Spin

    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{freeSpinDone}/{totalFreeSpins}";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (freeSpinDone < totalFreeSpins)
        {
            if (firstSpin)
                firstSpin = false;
            else
                yield return new WaitForSeconds(delayBetweenSpins);

            if (cancelRequested) yield break;

            float betAmount = IrishPotLuckUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            freeSpinDone++;

            UpdateSpinCount();

            yield return new WaitUntil(() => IrishPotLuckSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;

            if (IrishPotLuckSlotMachine.Instance.currentSpinResult != null)
            {
                if (IrishPotLuckSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => IrishPotLuckSlotMachine.Instance.isSlotAnimationCompleted);
                }
            }
        }

        yield return new WaitForSeconds(1f);
        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        freeSpinDone = 0;
        totalFreeSpins = 0;
        isFreeGame = false;

        IrishPotLuckSlotMachine.Instance.isFreeGame = false;

        IrishPotLuckFreeGameTransitionController.Instance.EndFreeSpinTransition();
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
        IrishPotLuckUIManager.Instance.UpdateButtons("Free Spin End");
    }
    #endregion
}
