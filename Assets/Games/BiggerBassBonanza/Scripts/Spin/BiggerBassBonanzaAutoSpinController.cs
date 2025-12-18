using UnityEngine;
using System.Collections;

public class BiggerBassBonanzaAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 2.5f;



    private bool firstAuto;
    public bool isAutoRunning = false;
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
        if (isAutoRunning || BiggerBassBonanzaSlotMachine.Instance.inSpin) return;

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

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (BiggerBassBonanzaUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(BiggerBassBonanzaUIManager.Instance.textAnimationCoroutine);

            if (BiggerBassBonanzaUIManager.Instance.winCoroutine != null)
                StopCoroutine(BiggerBassBonanzaUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => !BiggerBassBonanzaSlotMachine.Instance.inSpin);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (BiggerBassBonanzaSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => BiggerBassBonanzaSlotMachine.Instance.isFishCollectionCompleted);
                yield return new WaitUntil(() => BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => BiggerBassBonanzaUIManager.Instance.winAnimationCompleted);
            }

            if (BiggerBassBonanzaSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (BiggerBassBonanzaSlotMachine.Instance.isFreeGameReady)
        {
            BiggerBassBonanzaUIManager.Instance.UpdateButtons("Game Transition");
        }
        else
        {
            BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
        }

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
