using UnityEngine;
using System.Collections;

public class RichLittlePiggiesAutoSpinController : MonoBehaviour
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
    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || RichLittlePiggiesSlotMachine.Instance.InSpin) return;

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
        while (!cancelRequested)
        {
            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            RichLittlePiggiesUIManager.Instance.winAnimationCompleted = true;
            RichLittlePiggiesUIManager.Instance.SetStopInteractable(true);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            SlotSpinService.Instance.Spin(betAmount);

            RichLittlePiggiesUIManager.Instance.PlaySpinMusic("Spin");
            if (RichLittlePiggiesUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(RichLittlePiggiesUIManager.Instance.textAnimationCoroutine);
            if (RichLittlePiggiesUIManager.Instance.winCoroutine != null)
                StopCoroutine(RichLittlePiggiesUIManager.Instance.winCoroutine);

            if (RichLittlePiggiesUIManager.Instance.CurrentButtonSet() != "Auto Start")
                RichLittlePiggiesUIManager.Instance.UpdateButtons("Auto Start");


            yield return new WaitUntil(() => RichLittlePiggiesSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (RichLittlePiggiesSlotMachine.Instance.isFreeGameReady)
                break;

            if (RichLittlePiggiesSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => RichLittlePiggiesSlotMachine.Instance.isSlotAnimationCompleted);
            }
            yield return new WaitUntil(() => RichLittlePiggiesUIManager.Instance.winAnimationCompleted);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        RichLittlePiggiesUIManager.Instance.UpdateButtons("Auto Stop");

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
