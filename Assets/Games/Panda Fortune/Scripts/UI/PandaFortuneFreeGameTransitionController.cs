using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PandaFortuneFreeGameTransitionController : MonoBehaviour
{
    public static PandaFortuneFreeGameTransitionController Instance { get; private set; }

    public float hopHeight = 0.5f;
    public float duration = 0.6f;
    public Ease ease = Ease.OutQuad;

    [SerializeField] private GameObject FreeSpinStart;
    [SerializeField] private GameObject FreeSpinEnd;
    [SerializeField] private GameObject FreeSpinParent;
    [SerializeField] private TMP_Text freeSpinWin;

    private Animator FreeSpinAnimator;

    private PandaFortuneFreeSpinController freeSpinController;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        freeSpinController = GetComponent<PandaFortuneFreeSpinController>();
        FreeSpinAnimator = FreeSpinParent.GetComponent<Animator>();
    }

    public void StartFreeSpins()
    {
        StartCoroutine(StratFreeSpinTransition());
    }

    public IEnumerator StratFreeSpinTransition()
    {
        yield return new WaitUntil(() => PandaFortuneUIManager.Instance.winAnimationCompleted);
        PandaFortuneUIManager.Instance.UpdateButtons("FreeSpin");

        FreeSpinParent.SetActive(true);
        FreeSpinStart.SetActive(true);
        FreeSpinAnimator.SetBool("FreeSpinStart", true);

        yield return new WaitForSeconds(2.5f);
        
        FreeSpinAnimator.SetBool("FreeSpinStart", false);
        FreeSpinStart.SetActive(false);
        FreeSpinParent.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        PandaFortuneSlotMachine.Instance.SetWildOnFrozenReel();
        yield return new WaitForSeconds(1.5f);

        freeSpinController.StartFreeSpins();
    }

    public void EndFreeSpin()
    {
        StartCoroutine(EndFreeSpinTransition());
    }

    private IEnumerator EndFreeSpinTransition()
    {
        FreeSpinParent.SetActive(true);
        FreeSpinEnd.SetActive(true);
        FreeSpinAnimator.SetBool("FreeSpinEnd", true);

        float finalAmount = PandaFortuneSlotMachine.Instance.freeSpinWinAmount;
        freeSpinWin.text = "0.00";

        DOTween.To(
            () => 0.00,
            value => freeSpinWin.text = value.ToString("F2"),
            finalAmount,
            1.5f 
        ).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(2.5f);

        FreeSpinAnimator.SetBool("FreeSpinEnd", false);
        FreeSpinEnd.SetActive(false);
        FreeSpinParent.SetActive(false);

        if(finalAmount > 0)
        {
            WinAnimation(finalAmount);
        }
    }

    private void WinAnimation(float freegamewin)
    {
        if (freegamewin > 0)
        {
            float betAmount = PandaFortuneUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freegamewin, PandaFortuneSlotMachine.Instance.currentSpinResult.newBalance);
            Invoke(nameof(PandaFortuneSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }

    public void ReelSwap(Transform ReelA, Transform ReelB)
    {
        Debug.Log("tina is startting the reel swap routine");
        StartCoroutine(ReelSwapRoutine(ReelA, ReelB));
    }

    private IEnumerator ReelSwapRoutine(Transform ReelA, Transform ReelB)
    {
        Vector3 aStart = ReelA.position;
        Vector3 bStart = ReelB.position;

        Tween tweenA = ReelA.DOMove(bStart, duration).SetEase(ease);
        tweenA.OnUpdate(() =>
        {
            float t = tweenA.ElapsedPercentage();

            float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;

            ReelA.position = new Vector3(
                Mathf.Lerp(aStart.x, bStart.x, t),
                Mathf.Lerp(aStart.y, bStart.y, t) + arc,
                aStart.z
            );
        });

        Tween tweenB = ReelB.DOMove(aStart, duration).SetEase(ease);
        tweenB.OnUpdate(() =>
        {
            float t = tweenB.ElapsedPercentage();

            float arc = -Mathf.Sin(t * Mathf.PI) * hopHeight;

            ReelB.position = new Vector3(
                Mathf.Lerp(bStart.x, aStart.x, t),
                Mathf.Lerp(bStart.y, aStart.y, t) + arc,
                bStart.z
            );
        });

        yield return tweenA.WaitForCompletion();
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
}
