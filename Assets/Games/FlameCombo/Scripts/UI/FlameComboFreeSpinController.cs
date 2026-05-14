using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class FlameComboFreeSpinController : MonoBehaviour
{
    #region Variables

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
                yield return new WaitForSeconds(1f);
            }

            if (cancelRequested) yield break;

            float betAmount = FlameComboUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount();

            freeSpinDone++;

            yield return new WaitUntil(() => FlameComboSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;

            if (FlameComboSlotMachine.Instance.currentSpinResult != null)
            {
                if (FlameComboSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => FlameComboSlotMachine.Instance.isPaylineCompleted);
                }
            }
        }

        yield return new WaitForSeconds(0.5f);
        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;
        FlameComboSlotMachine.Instance.isFreeGame = false;

        FlameComboFreeGameTransitionController.Instance.EndFreeSpinTransition();
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
        FlameComboUIManager.Instance.UpdateButtons("Stop");
    }
    #endregion
}
