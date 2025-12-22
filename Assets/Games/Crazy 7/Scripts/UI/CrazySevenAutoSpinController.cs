using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using UnityEngine;


public class CrazySevenAutoSpinController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float delayBetweenSpins = 1.5f;

    private bool firstAuto;
    public bool isAutoRunning = false;
    public bool cancelRequested = false;
    private Coroutine autoSpinRoutine;

    public static bool isAutoSpinning = false;

    public bool IsAutoRunning => isAutoRunning;

    private void Start()
    {

    }

    public void StartAutoSpin(float betAmount)
    {
        if (CrazySevenSlotMachine.Instance.uIShiny != null)
        {
            CrazySevenSlotMachine.Instance.uIShiny.enabled = false;
        }
        if (isAutoRunning || CrazySevenSlotMachine.Instance.InSpin) return;

        firstAuto = true;
        isAutoRunning = true;
        isAutoSpinning = true;
        cancelRequested = false;
        autoSpinRoutine = StartCoroutine(AutoSpinLoop(betAmount));

    }

    public void CancelAutoSpin()
    {
        cancelRequested = true;
        if (autoSpinRoutine != null)
        {
            StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = null;
        }
        isAutoSpinning = false;
        isAutoRunning = false;
        CrazySevenSlotMachine.Instance.StopWithResult();
    }

    private IEnumerator AutoSpinLoop(float betAmount)
    {
        while (!cancelRequested)
        {
            CrazySevenUIManager.Instance.winAnimationCompleted = true;

            if (CrazySevenSlotMachine.Instance.uIShiny != null)
            {
                CrazySevenSlotMachine.Instance.uIShiny.enabled = false;
            }
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

            var sm = CrazySevenSlotMachine.Instance;
            var bannerTween = sm.CurrentWinBannerTween;
            if (bannerTween != null && bannerTween.IsActive())
            {
                try { sm.winBannerCG?.DOFade(0f, 0.2f); } catch { }
                bannerTween.Kill();
                bannerTween = null;
            }

            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) break;
            CrazySevenUIManager.Instance.PlayMusic("Crazy_7_Slot_Machine");

            if (CrazySevenUIManager.Instance.textAnimationCoroutine != null)
            {
                StopCoroutine(CrazySevenUIManager.Instance.textAnimationCoroutine);
            }
            if (CrazySevenUIManager.Instance.winCoroutine != null)
            {
                StopCoroutine(CrazySevenUIManager.Instance.winCoroutine);
            }

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => !CrazySevenSlotMachine.Instance.InSpin);

            //if (cancelRequested) break;

            yield return new WaitUntil(() => CrazySevenSlotMachine.Instance.isSpinAgain);
            if (cancelRequested)
            {
                StopAutoSpin();
                break;
            }
            if (CrazySevenSlotMachine.Instance.isFreeGameReady)
                break;
            if (CrazySevenSlotMachine.Instance.winAmount > 0)
            {
                yield return new WaitUntil(() => CrazySevenSlotMachine.Instance.isPaylineCompleted);
                yield return new WaitUntil(() => CrazySevenUIManager.Instance.winAnimationCompleted);
                yield return new WaitForSeconds(0.5f);
            }
        }
        StopAutoSpin();
    }
    private void StopAutoSpin()
    {
        isAutoSpinning = false;
        isAutoRunning = false;
        cancelRequested = false;
    }
}