using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuickHitVolcanoFreeSpinController : MonoBehaviour
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
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }

    public void InitialFreeSpinText()
    {
        freeSpinsText.text = $"Free Game 0 of {totalFreeSpins}";
    }

    #endregion

    #region Free Spin

    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"Free Spin {freeSpinDone + 1} of {totalFreeSpins}";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

        QuickHitVolcanoUIManager.Instance.UpdateButtons("FreeSpin");

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
            if (QuickHitVolcanoUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(QuickHitVolcanoUIManager.Instance.textAnimationCoroutine);

            if (QuickHitVolcanoUIManager.Instance.winCoroutine != null)
                StopCoroutine(QuickHitVolcanoUIManager.Instance.winCoroutine);

            float betAmount = QuickHitVolcanoUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount();

            freeSpinDone++;

            yield return new WaitUntil(() => QuickHitVolcanoSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;

            if (QuickHitVolcanoSlotMachine.Instance.currentSpinResult != null)
            {
                if (QuickHitVolcanoSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted);
                }
            }
            if (QuickHitVolcanoSlotMachine.Instance.extraFreeGame)
            {
                yield return new WaitUntil(() => !QuickHitVolcanoSlotMachine.Instance.extraFreeGame);
                yield return new WaitForSeconds(1f);
            }
        }

        QuickHitVolcanoUIManager.Instance.UpdateButtons("Transition");

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;

        QuickHitVolcanoSlotMachine.Instance.isFreeGame = false;

        QuickHitVolcanoGameTransitionController.Instance.EndFreeSlotGame();
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
        QuickHitVolcanoUIManager.Instance.UpdateButtons("Default");
    }
    #endregion
}
