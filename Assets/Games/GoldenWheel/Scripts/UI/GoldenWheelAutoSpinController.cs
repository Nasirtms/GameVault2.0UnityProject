using UnityEngine;
using System.Collections;

public class GoldenWheelAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || GoldenWheelSlotMachine.Instance.InSpin) return;

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
            if(GoldenWheelSlotMachine.Instance.isFreeGame)
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

            GoldenWheelUIManager.Instance.winAnimationCompleted = true;
            GoldenWheelUIManager.Instance.PlaySpinMusic("Spin");

            if (GoldenWheelUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(GoldenWheelUIManager.Instance.textAnimationCoroutine);
            if (GoldenWheelUIManager.Instance.winCoroutine != null)
                StopCoroutine(GoldenWheelUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);
            if (GoldenWheelUIManager.Instance.CurrentButtonSet() != "Spin")
                GoldenWheelUIManager.Instance.UpdateButtons("Spin");

            yield return new WaitUntil(() => GoldenWheelSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (GoldenWheelSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => GoldenWheelSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => GoldenWheelUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (GoldenWheelSlotMachine.Instance.isFreeGameReady || GoldenWheelSlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!GoldenWheelSlotMachine.Instance.isFreeGameReady)
        {
            GoldenWheelUIManager.Instance.UpdateButtons("Stop");
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
