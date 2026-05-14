using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildXReelAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || WildXReelSlotMachine.Instance.InSpin) return;

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
            if (WildXReelSlotMachine.Instance.isFreeGame)
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

            WildXReelUIManager.Instance.winAnimationCompleted = true;

            if (WildXReelUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(WildXReelUIManager.Instance.textAnimationCoroutine);
            if (WildXReelUIManager.Instance.winCoroutine != null)
                StopCoroutine(WildXReelUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);

            if (WildXReelUIManager.Instance.CurrentButtonSet() != "Auto")
                WildXReelUIManager.Instance.UpdateButtons("Auto");

            yield return new WaitUntil(() => WildXReelSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (WildXReelSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => WildXReelSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => WildXReelUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        WildXReelUIManager.Instance.UpdateButtons("Auto Stop");

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