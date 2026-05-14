using UnityEngine;
using System.Collections;
public class AtomicMeltdownAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private bool firstAuto;
    private bool isAutoRunning = false;
    private bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

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

    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || AtomicMeltdownSlotMachine.Instance.InSpin) return;

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
            AtomicMeltdownUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            float balance = UserManager.Instance.Coins;

            if (AtomicMeltdownUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(AtomicMeltdownUIManager.Instance.textAnimationCoroutine);

            if (AtomicMeltdownUIManager.Instance.winCoroutine != null)
                StopCoroutine(AtomicMeltdownUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount))
            {
                AtomicMeltdownUIManager.Instance.StopSpinMusic("Spin");
                AtomicMeltdownUIManager.Instance.UpdateButtons("Default");
                break;
            }
            if (AtomicMeltdownSlotMachine.Instance.isFreeGameReady)
            {
                break;
            }
            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => AtomicMeltdownSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (AtomicMeltdownSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => AtomicMeltdownSlotMachine.Instance.isPaylineCompleted);
            }

            yield return new WaitUntil(() => AtomicMeltdownUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (AtomicMeltdownSlotMachine.Instance.isFreeGameReady)
            {
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!AtomicMeltdownSlotMachine.Instance.isFreeGameReady)
        {
            AtomicMeltdownUIManager.Instance.UpdateButtons("Default");
        }

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
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
}
