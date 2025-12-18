using UnityEngine;
using System.Collections;

public class ZombieParadiseAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || ZombieParadiseSlotMachine.Instance.InSpin) return;

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
            ZombieParadiseUIManager.Instance.PlaySound("Spin");
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            ZombieParadiseUIManager.Instance.winAnimationCompleted = true;
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (ZombieParadiseUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(ZombieParadiseUIManager.Instance.textAnimationCoroutine);
            if (ZombieParadiseUIManager.Instance.winCoroutine != null)
                StopCoroutine(ZombieParadiseUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (ZombieParadiseUIManager.Instance.CurrentButtonSet() != "Spin Start")
                ZombieParadiseUIManager.Instance.UpdateButtons("Spin Start");

            yield return new WaitUntil(() => ZombieParadiseSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ZombieParadiseSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => ZombieParadiseUIManager.Instance.winAnimationCompleted);
            }

            if (ZombieParadiseSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (ZombieParadiseSlotMachine.Instance.isFreeGameReady)
        {
            ZombieParadiseUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            ZombieParadiseUIManager.Instance.UpdateButtons("Spin Stop");
        }

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
