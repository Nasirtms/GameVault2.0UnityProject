using UnityEngine;
using System.Collections;

public class ComeOnCash2AutoSpinController : MonoBehaviour
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
        if (isAutoRunning || ComeOnCash2SlotMachine.Instance.InSpin) return;

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
            if(ComeOnCash2SlotMachine.Instance.isFreeGame)
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

            ComeOnCash2UIManager.Instance.winAnimationCompleted = true;
            ComeOnCash2UIManager.Instance.PlaySpinMusic("Spin");

            if (ComeOnCash2UIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(ComeOnCash2UIManager.Instance.textAnimationCoroutine);
            if (ComeOnCash2UIManager.Instance.winCoroutine != null)
                StopCoroutine(ComeOnCash2UIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);
            if (ComeOnCash2UIManager.Instance.CurrentButtonSet() != "Spin")
                ComeOnCash2UIManager.Instance.UpdateButtons("Spin");

            yield return new WaitUntil(() => ComeOnCash2SlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ComeOnCash2SlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => ComeOnCash2SlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => ComeOnCash2UIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ComeOnCash2SlotMachine.Instance.isFreeGameReady || ComeOnCash2SlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!ComeOnCash2SlotMachine.Instance.isFreeGameReady)
        {
            ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
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
