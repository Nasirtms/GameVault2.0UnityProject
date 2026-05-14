using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CleopatraGameTransitionController))]
public class CleopatraFreeSpinController : MonoBehaviour
{
    #region Variables

    [SerializeField] private TMP_Text freeSpinsText;

    [SerializeField] private float delayBetweenSpins = 1.5f;
    private int totalFreeSpins = 0;
    private int freeSpinDone = 0;
    private bool isFreeGame = false;

    private CleopatraGameTransitionController gameTransitionController;
    private Coroutine freeSpinRoutine;
    private bool cancelRequested;
    #endregion

    #region Unity Methods

    private void Start()
    {
        gameTransitionController = GetComponent<CleopatraGameTransitionController>();
    }
    public void ErrorFreeSpinReturn()
    {
        freeSpinDone--;
        UpdateSpinCount();
    }
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
        freeSpinDone = 0;
        CleopatraUIManager.Instance.StopMusic("Background");
        freeSpinRoutine = StartCoroutine(FreeSpinLoop());
    }

    public void ResetFreeSpins()
    {
        totalFreeSpins = 0;
    }

    public void UpdateFreeSpins(int freeSpins)
    {
        totalFreeSpins = freeSpins;
    }

    public void InitialFreeSpinText()
    {
        freeSpinsText.text = $"1/{totalFreeSpins}";
    }

    #endregion

    #region Free Spin

    private void UpdateSpinCount()
    {
        if (freeSpinsText != null)
            freeSpinsText.text = $"{freeSpinDone + 1}/{totalFreeSpins}";
    }

    private IEnumerator FreeSpinLoop()
    {
        yield return new WaitForSeconds(2.5f); // optional delay after transition in

        while (freeSpinDone < totalFreeSpins)
        {
            if (cancelRequested) yield break;
            float betAmount = CleopatraUIManager.Instance.GetComponent<CleopatraBetController>().GetCurrentBet();
            SlotSpinService.Instance.Spin(betAmount);

            UpdateSpinCount() ;

            yield return new WaitUntil(() => CleopatraSlotMachine.Instance.isSpinAgain);

            freeSpinDone++;
            if (cancelRequested) yield break;
            if (CleopatraSlotMachine.Instance.currentSpinResult != null)
            {
                if (CleopatraSlotMachine.Instance.GetWinAmount() > 0)
                {
                    yield return new WaitUntil(() => CleopatraSlotMachine.Instance.isPaylineCompleted);
                }
            }

            yield return new WaitForSeconds(delayBetweenSpins); // optional delay between spins
        }

        EndFreeSpins();
    }

    private void EndFreeSpins()
    {
        isFreeGame = false;

        CleopatraPaylineController.Instance.StopPaylines();
        CleopatraSlotMachine.Instance.isFreeGame = false;

        CleopatraUIManager.Instance.freeGameSpinCount = 0;

        gameTransitionController.PlayTransition();
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
        CleopatraUIManager.Instance.UpdateButtons("Single Stop");
    }
    #endregion
}
