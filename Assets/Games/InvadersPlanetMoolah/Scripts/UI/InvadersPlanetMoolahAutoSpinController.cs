using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvadersPlanetMoolahAutoSpinController : MonoBehaviour
{
    #region Variables

    [Header("Settings")]
    public float delayBetweenSpins = 1.5f;

    public bool isAutoRunning = false;
    public bool cancelRequested = false;

    private Coroutine autoSpinRoutine;
    private bool firstAuto;

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
        if (isAutoRunning || InvadersPlanetMoolahSlotMachine.Instance.InSpin)
        {
            return;
        }

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
            InvadersPlanetMoolahUIManager.Instance.winAnimationCompleted = true;

            if (!firstAuto)
                yield return new WaitForSeconds(delayBetweenSpins);
            else
                firstAuto = false;

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (InvadersPlanetMoolahSlotMachine.Instance.isFreeGameReady)
                break;

            float balance = UserManager.Instance.Coins;

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount))
                break;

            SlotSpinService.Instance.Spin(betAmount);

            if (InvadersPlanetMoolahUIManager.Instance.CurrentButtonSet() != "Auto")
                InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Auto");

            if (InvadersPlanetMoolahUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(InvadersPlanetMoolahUIManager.Instance.textAnimationCoroutine);

            if (InvadersPlanetMoolahUIManager.Instance.winCoroutine != null)
                StopCoroutine(InvadersPlanetMoolahUIManager.Instance.winCoroutine);

            yield return new WaitUntil(() => InvadersPlanetMoolahSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (InvadersPlanetMoolahSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => InvadersPlanetMoolahSlotMachine.Instance.isSlotAnimationCompleted);
            }

            yield return new WaitUntil(() => InvadersPlanetMoolahUIManager.Instance.winAnimationCompleted);

            if (InvadersPlanetMoolahSlotMachine.Instance.isFreeGameReady)
                break;
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (InvadersPlanetMoolahSlotMachine.Instance.isFreeGameReady)
            InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Transition Start");
        else
            InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Auto Stop");

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