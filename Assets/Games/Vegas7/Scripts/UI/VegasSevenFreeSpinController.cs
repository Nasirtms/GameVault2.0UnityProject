using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VegasSevenFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private TMP_Text freeSpinsText;
    [SerializeField] private TMP_Text secondFreeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;
    private bool firstSpin;
    [SerializeField] GameObject freeSpinTextHeader;

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
    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
    }
    public void StartFreeSpins()
    {
        if (isFreeGame) return;

        cancelRequested = false;
        isFreeGame = true;
        firstSpin = true;
        freeSpinDone = 0;
        freeSpinTextHeader.SetActive(true);
        InitialFreeSpinText();
        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
        //VegasSevenFreeGameTransitionController.Instance.SetChilliUI(2);
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
        freeSpinsText.text = $"{totalFreeSpins}";
        secondFreeSpinsText.text = $"{totalFreeSpins}";
    }

    #endregion

    #region Free Spin

    public void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{totalFreeSpins - freeSpinDone - 1}";
        if (secondFreeSpinsText != null)
            secondFreeSpinsText.text = $"{totalFreeSpins - freeSpinDone - 1}";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

        VegasSevenUIManager.Instance.UpdateButtons("enterfreeSpin");

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

            if(VegasSevenSlotMachine.Instance.showRetriggerMultiplier)
            {
                VegasSevenSlotMachine.Instance.IncreaseRetrigger();
                VegasSevenSlotMachine.Instance.showRetriggerMultiplier = false;
            }

            if (cancelRequested) yield break;

            if (VegasSevenUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(VegasSevenUIManager.Instance.textAnimationCoroutine);

            if (VegasSevenUIManager.Instance.winCoroutine != null)
                StopCoroutine(VegasSevenUIManager.Instance.winCoroutine);

            float betAmount = VegasSevenUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount();

            freeSpinDone++;

            yield return new WaitUntil(() => VegasSevenSlotMachine.Instance.isSpinAgain);
            if (cancelRequested) yield break;
            if (VegasSevenSlotMachine.Instance.currentSpinResult != null)
            {
                if (VegasSevenSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => VegasSevenSlotMachine.Instance.isPaylineCompleted);
                }
            }
         
        }

        VegasSevenUIManager.Instance.UpdateButtons("Transition");

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;
        freeSpinTextHeader.SetActive(false);
        VegasSevenSlotMachine.Instance.isFreeGame = false;
        freeSpinsText.text = "5";
        secondFreeSpinsText.text = "5";
        VegasSevenFreeGameTransitionController.Instance.EndFreeSpinTransition();
        VegasSevenUIManager.Instance.autoBtton.ShowButton(true);
        VegasSevenSlotMachine.Instance.retriggerCount = 1;
        VegasSevenSlotMachine.Instance.scatterMultiplierImageIndex = 0;
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
        VegasSevenUIManager.Instance.UpdateButtons("exitfreeSpin");
    }
    #endregion
}