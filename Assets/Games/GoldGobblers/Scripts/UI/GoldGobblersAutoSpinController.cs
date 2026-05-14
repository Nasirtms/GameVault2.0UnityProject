using UnityEngine;
using System.Collections;

public class GoldGobblersAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || GoldGobblersSlotMachine.Instance.InSpin) return;

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
            GoldGobblersUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (GoldGobblersUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(GoldGobblersUIManager.Instance.textAnimationCoroutine);
            if (GoldGobblersUIManager.Instance.winCoroutine != null)
                StopCoroutine(GoldGobblersUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (GoldGobblersUIManager.Instance.CurrentButtonSet() != "Spin Start")
                GoldGobblersUIManager.Instance.UpdateButtons("Spin Start");

            yield return new WaitUntil(() => GoldGobblersSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (GoldGobblersSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => GoldGobblersSlotMachine.Instance.isSlotAnimationCompleted);

            }
            yield return new WaitUntil(() => GoldGobblersUIManager.Instance.winAnimationCompleted);
            if (GoldGobblersSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (GoldGobblersSlotMachine.Instance.isFreeGameReady)
        {
            GoldGobblersUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            GoldGobblersUIManager.Instance.UpdateButtons("Spin Stop");
        }

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
