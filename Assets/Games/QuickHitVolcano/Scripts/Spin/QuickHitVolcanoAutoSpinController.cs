using UnityEngine;
using System.Collections;

public class QuickHitVolcanoAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;


    private bool firstAuto;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    public bool IsAutoRunning => isAutoRunning;

    #endregion

    #region Unity Methods

    private void Start()
    {
    }

    #endregion

    #region Public References

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || QuickHitVolcanoSlotMachine.Instance.InSpin) return;

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
            QuickHitVolcanoUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }
            else
            {
                firstAuto = false;
            }

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (QuickHitVolcanoUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(QuickHitVolcanoUIManager.Instance.textAnimationCoroutine);

            if (QuickHitVolcanoUIManager.Instance.winCoroutine != null)
                StopCoroutine(QuickHitVolcanoUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => QuickHitVolcanoSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            QuickHitVolcanoUIManager.Instance.SetStopInteractable(true);
            
            if (QuickHitVolcanoSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted);
            }

            yield return new WaitUntil(() => QuickHitVolcanoUIManager.Instance.winAnimationCompleted);

            if (QuickHitVolcanoSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!QuickHitVolcanoSlotMachine.Instance.isFreeGameReady)
        {
            QuickHitVolcanoUIManager.Instance.UpdateButtons("Default");
        }

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
