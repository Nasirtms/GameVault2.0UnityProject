using System.Collections;
using TMPro;
using UnityEngine;

public class BuffaloXtraReelPowerFreeSpinController : MonoBehaviour
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
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
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
        freeSpinsText.text = $"0 OF {totalFreeSpins}";
    }

    #endregion

    #region Free Spin

    private void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"Free Spin {freeSpinDone + 1} OF {totalFreeSpins}";
    }

    private IEnumerator FreeSpinLoop()
    {
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
            float betAmount = BuffaloXtraReelPowerUIManager.Instance.CurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount();

            freeSpinDone++;

            yield return new WaitUntil(() => BuffaloXtraReelPowerSlotMachine.Instance.isSpinAgain);

            if (cancelRequested) yield break;

            if (BuffaloXtraReelPowerSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => BuffaloXtraReelPowerSlotMachine.Instance.isSlotAnimationCompleted);
            }
        }

        yield return new WaitForSeconds(1.5f);

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;

        BuffaloXtraReelPowerSlotMachine.Instance.isFreeGame = false;

        BuffaloXtraReelPowerFreeGameTransitionController.Instance.EndFreeSpinTransition();
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
        BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Stop");
    }
    #endregion
}
