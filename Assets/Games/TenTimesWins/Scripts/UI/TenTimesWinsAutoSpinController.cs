using UnityEngine;
using System.Collections;

public class TenTimesWinsAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private int remainingSpins = -1;
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
        isAutoSpinning = false;
    }
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
        if (isAutoRunning || TenTimesWinsSlotMachine.Instance.InSpin) return;

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
            }
            TenTimesWinsUIManager.Instance.winAnimationCompleted = true;
            TenTimesWinsUIManager.Instance.SetStopInteractable(true);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            if (TenTimesWinsUIManager.Instance.textAnimationCoroutine != null)
            {
                StopCoroutine(TenTimesWinsUIManager.Instance.textAnimationCoroutine);
            }
            if (TenTimesWinsUIManager.Instance.winCoroutine != null)
                StopCoroutine(TenTimesWinsUIManager.Instance.winCoroutine);

            SlotSpinService.Instance.Spin(betAmount);

            if (TenTimesWinsUIManager.Instance.CurrentButtonSet() != "Auto Start")
            {
                TenTimesWinsUIManager.Instance.UpdateButtons("Auto Start");
            }

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (remainingSpins > 0)
            {
                remainingSpins--;
            }
            TenTimesWinsUIManager.Instance.PlaySound("Spin1");

            if (remainingSpins == 0)
            {
                isAutoSpinning = false;
            }

            TenTimesWinsUIManager.Instance.UpdateRemainingSpins(remainingSpins);

            yield return new WaitUntil(() => TenTimesWinsSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }


            if (TenTimesWinsSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => TenTimesWinsSlotMachine.Instance.isPaylineCompleted);
            }
            yield return new WaitUntil(() => TenTimesWinsUIManager.Instance.winAnimationCompleted);
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        TenTimesWinsUIManager.Instance.HideSpinCount();
        TenTimesWinsUIManager.Instance.UpdateButtons("Auto Stop");


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
