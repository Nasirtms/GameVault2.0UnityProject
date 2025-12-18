using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class VegasSevenAutoSpinController : MonoBehaviour
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
        if (isAutoRunning || VegasSevenSlotMachine.Instance.InSpin) return;

        firstAuto = true;
        isAutoRunning = true;
        isAutoSpinning = true;
        cancelRequested = false;

        autoSpinRoutine = StartCoroutine(AutoSpinLoop(betAmount));
    }

    public void CancelAutoSpin()
    {
        VegasSevenUIManager.Instance.autoBtton.gameObject.transform.GetComponent<Image>().sprite = VegasSevenUIManager.Instance.autoOnSprite;
        cancelRequested = true;
        isAutoRunning = false;
    }

    #endregion

    #region Auto Spin

    private IEnumerator AutoSpinLoop(float betAmount)
    {
        while (!cancelRequested)
        {
            if(VegasSevenSlotMachine.Instance.isFreeGame)
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

            VegasSevenUIManager.Instance.winAnimationCompleted = true;
            VegasSevenUIManager.Instance.PlaySpinMusic("Spin");

            if (VegasSevenUIManager.Instance.textAnimationCoroutine != null)
                StopCoroutine(VegasSevenUIManager.Instance.textAnimationCoroutine);
            if (VegasSevenUIManager.Instance.winCoroutine != null)
                StopCoroutine(VegasSevenUIManager.Instance.winCoroutine);

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;

            SlotSpinService.Instance.Spin(betAmount);
            if (VegasSevenUIManager.Instance.CurrentButtonSet() != "Auto Spin")
                VegasSevenUIManager.Instance.UpdateButtons("Auto Spin");

            yield return new WaitUntil(() => VegasSevenSlotMachine.Instance.isSpinAgain);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (VegasSevenSlotMachine.Instance.GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => VegasSevenSlotMachine.Instance.isPaylineCompleted);

            }

            yield return new WaitUntil(() => VegasSevenUIManager.Instance.winAnimationCompleted);

            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }

            if (VegasSevenSlotMachine.Instance.isFreeGameReady || VegasSevenSlotMachine.Instance.isFreeGame)
            {
                StopAutoSpin();
                break;
            }
        }

        StopAutoSpin();
    }

    private void StopAutoSpin()
    {
        if (!VegasSevenSlotMachine.Instance.isFreeGameReady)
        {
            VegasSevenUIManager.Instance.UpdateButtons("Stop");
        }
        

        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }

    #endregion
}
