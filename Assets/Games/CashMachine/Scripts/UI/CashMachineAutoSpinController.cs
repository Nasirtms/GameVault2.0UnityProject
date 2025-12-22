using UnityEngine;
using System.Collections;

public class CashMachineAutoSpinController : MonoBehaviour
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

    private void Start()
    {
    }

    #endregion

    #region Public References

    public bool IsAutoRunning => isAutoRunning;

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || CashMachineSlotMachine.Instance.InSpin) return;

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
            if(CashMachineSlotMachine.Instance.isFreeGame)
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

            CashMachineUIManager.Instance.winAnimationCompleted = true;
            CashMachineUIManager.Instance.PlaySpinMusic("Spin");

            if (CashMachineUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(CashMachineUIManager.Instance.textAnimationCoroutine);
            if (CashMachineUIManager.Instance.winCoroutine != null)
                StopCoroutine(CashMachineUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);
            if (CashMachineUIManager.Instance.CurrentButtonSet() != "Spin")
                CashMachineUIManager.Instance.UpdateButtons("Spin");

            yield return new WaitUntil(() => CashMachineSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (CashMachineSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => CashMachineSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => CashMachineUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (CashMachineSlotMachine.Instance.isFreeGameReady || CashMachineSlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!CashMachineSlotMachine.Instance.isFreeGameReady)
        {
            CashMachineUIManager.Instance.UpdateButtons("Stop");
        }
        

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
