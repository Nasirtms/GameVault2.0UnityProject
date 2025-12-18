using UnityEngine;
using System.Collections;

public class TheGreenMachineDeluxeAutoSpinController : MonoBehaviour
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

    private void Start()
    {
    }

    #endregion

    #region Public References

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || TheGreenMachineDeluxeSlotMachine.Instance.InSpin) return;

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
                TheGreenMachineDeluxeUIManager.Instance.PlayMusic("ReelSpin");
            }

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            TheGreenMachineDeluxeUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (TheGreenMachineDeluxeUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(TheGreenMachineDeluxeUIManager.Instance.textAnimationCoroutine);

            if (TheGreenMachineDeluxeUIManager.Instance.winCoroutine != null)
                StopCoroutine(TheGreenMachineDeluxeUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (TheGreenMachineDeluxeUIManager.Instance.CurrentButtonSet() != "Auto Start")
                TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Auto Start");

            yield return new WaitUntil(() => TheGreenMachineDeluxeSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (TheGreenMachineDeluxeSlotMachine.Instance.isFreeGameReady)
                break;
           
            if (TheGreenMachineDeluxeSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => TheGreenMachineDeluxeSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => TheGreenMachineDeluxeUIManager.Instance.winAnimationCompleted);
            }
            //else
            //{
            //    TheGreenMachineDeluxeUIManager.Instance.winAnimationCompleted = true;
            //}
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (TheGreenMachineDeluxeSlotMachine.Instance.isFreeGameReady)
        {
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Free Game Transition");
        }
        else
        {
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Auto Stop");
        }
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
