using UnityEngine;
using System.Collections;

public class PiratesOfTheCaribbeanAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;


    private bool firstAuto;
    private int remainingSpins = -1;
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

    public void SetSpinCount(int count)
    {
        remainingSpins = count;
    }

    public void StartAutoSpin(float betAmount)
    {
        if (isAutoRunning || PiratesOfTheCaribbeanSlotMachine.Instance.InSpin) return;

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
        while (!cancelRequested && (remainingSpins == -1 || remainingSpins > 0))
        {
            if (!firstAuto)
            {
                yield return new WaitForSeconds(delayBetweenSpins);
            }
            else
            {
                firstAuto = false;
                PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Cannon");
                PiratesOfTheCaribbeanUIManager.Instance.cannonAnimator.SetTrigger("Fire");
            }

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            PiratesOfTheCaribbeanUIManager.Instance.winAnimationCompleted = true;
            PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Spin");
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (PiratesOfTheCaribbeanUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(PiratesOfTheCaribbeanUIManager.Instance.textAnimationCoroutine);
            if (PiratesOfTheCaribbeanUIManager.Instance.winCoroutine != null)
                StopCoroutine(PiratesOfTheCaribbeanUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (PiratesOfTheCaribbeanUIManager.Instance.CurrentButtonSet() != "Auto Start")
                PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Auto Start");

            if (remainingSpins > 0)
                remainingSpins--;

            PiratesOfTheCaribbeanUIManager.Instance.UpdateRemainingSpins(remainingSpins);

            yield return new WaitUntil(() => PiratesOfTheCaribbeanSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (PiratesOfTheCaribbeanSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => PiratesOfTheCaribbeanSlotMachine.Instance.isSlotAnimationCompleted);
                yield return new WaitUntil(() => PiratesOfTheCaribbeanUIManager.Instance.winAnimationCompleted);
            }

            if (PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGameReady)
        {
            PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Transition Start");
        }
        else
        {
            PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Auto Stop");
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
