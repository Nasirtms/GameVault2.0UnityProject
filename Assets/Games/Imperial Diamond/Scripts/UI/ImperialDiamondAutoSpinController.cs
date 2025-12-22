using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class ImperialDiamondAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || ImperialDiamondSlotMachine.Instance.InSpin) return;

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
            if (ImperialDiamondSlotMachine.Instance.isFreeGame)
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

            ImperialDiamondUIManager.Instance.winAnimationCompleted = true;
            //ImperialDiamondUIManager.Instance.PlaySpinMusic("Spin");

            if (ImperialDiamondUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(ImperialDiamondUIManager.Instance.textAnimationCoroutine);
            if (ImperialDiamondUIManager.Instance.winCoroutine != null)
                StopCoroutine(ImperialDiamondUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => ImperialDiamondSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ImperialDiamondSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => ImperialDiamondSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => ImperialDiamondUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (ImperialDiamondSlotMachine.Instance.isFreeGameReady || ImperialDiamondSlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!ImperialDiamondSlotMachine.Instance.isFreeGameReady)
        {
            ImperialDiamondUIManager.Instance.UpdateButtons("Stop");
        }

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
