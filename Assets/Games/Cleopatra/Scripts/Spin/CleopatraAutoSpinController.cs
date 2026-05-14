using UnityEngine;
using System.Collections;

public class CleopatraAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;


    private int remainingSpins = -1;
    private bool firstAuto;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    public bool IsAutoRunning => isAutoRunning;

    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        MainMenuUIManager.PopupShown += HandlePopupShown;
    }

    private void OnDisable()
    {
        MainMenuUIManager.PopupShown -= HandlePopupShown;
    }
    #endregion

    #region Public References

    public void SetSpinCount(int count)
    {
        remainingSpins = count;
    }

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || CleopatraSlotMachine.Instance.InSpin) return;

        firstAuto = true;
        isAutoRunning = true;
        isAutoSpinning = true;
        cancelRequested = false;

        autoSpinRoutine = StartCoroutine(AutoSpinLoop(betAmount));
    }

    public void CancelAutoSpin()
    {
        cancelRequested = true;
        isAutoRunning = false;
    }

    #endregion

    #region Auto Spin

    private IEnumerator AutoSpinLoop(float betAmount)
    {
        while (!cancelRequested && (remainingSpins == -1 || remainingSpins > 0))
        {
            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            CleopatraUIManager.Instance.winAnimationCompleted = true;
            CleopatraUIManager.Instance.SetStopInteractable(true);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            //CleopatraUIManager.Instance.PlaySpinMusic("Spin");
            if (CleopatraUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(CleopatraUIManager.Instance.textAnimationCoroutine);
            if (CleopatraUIManager.Instance.winCoroutine != null)
                StopCoroutine(CleopatraUIManager.Instance.winCoroutine);

            if (CleopatraUIManager.Instance.CurrentButtonSet() != "Auto Start")
                CleopatraUIManager.Instance.UpdateButtons("Auto Start");

            if (remainingSpins > 0)
            {
                remainingSpins--;
            }

            if (remainingSpins == 0)
            {
                isAutoSpinning = false;
            }

            CleopatraUIManager.Instance.UpdateRemainingSpins(remainingSpins);

            yield return new WaitUntil(() => CleopatraSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (CleopatraSlotMachine.Instance.isFreeGameReady)
                break;

            if (CleopatraSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => CleopatraSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => CleopatraUIManager.Instance.winAnimationCompleted);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        CleopatraUIManager.Instance.HideSpinCount();
        CleopatraUIManager.Instance.UpdateButtons("Auto Stop");

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    private void HandlePopupShown()
    {
        if (!isAutoRunning) return;

        cancelRequested = true;

        if (autoSpinRoutine != null)
        {
            StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = null;
        }

        StopAutoSpin();
    }
    #endregion
}
