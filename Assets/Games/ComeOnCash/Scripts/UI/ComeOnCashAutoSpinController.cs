using UnityEngine;
using System.Collections;

public class ComeOnCashAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 2.5f;

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
        if (isAutoRunning || ComeOnCashSlotMachine.Instance.InSpin) return;

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
            if(ComeOnCashSlotMachine.Instance.isFreeGame)
            {
                yield return new WaitForSeconds(1f);
            }

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

            ComeOnCashUIManager.Instance.winAnimationCompleted = true;
            ComeOnCashUIManager.Instance.PlaySpinMusic("Spin");

            if (ComeOnCashUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(ComeOnCashUIManager.Instance.textAnimationCoroutine);
            if (ComeOnCashUIManager.Instance.winCoroutine != null)
                StopCoroutine(ComeOnCashUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);
            if (ComeOnCashUIManager.Instance.CurrentButtonSet() != "Spin")
                ComeOnCashUIManager.Instance.UpdateButtons("Spin");

            yield return new WaitUntil(() => ComeOnCashSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ComeOnCashSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => ComeOnCashSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => ComeOnCashUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ComeOnCashSlotMachine.Instance.isFreeGameReady || ComeOnCashSlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!ComeOnCashSlotMachine.Instance.isFreeGameReady)
        {
            ComeOnCashUIManager.Instance.UpdateButtons("Stop");
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
