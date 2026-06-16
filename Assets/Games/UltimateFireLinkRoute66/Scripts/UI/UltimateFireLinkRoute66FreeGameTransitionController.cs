using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UltimateFireLinkRoute66FreeGameTransitionController : MonoBehaviour
{
    public static UltimateFireLinkRoute66FreeGameTransitionController Instance { get; private set; }

    public float hopHeight = 0.5f;
    public float duration = 0.6f;
    public Ease ease = Ease.OutQuad;

    [SerializeField] private GameObject FreeSpinParent;
    [SerializeField] private GameObject FreeSpinStart;
    [SerializeField] private GameObject FreeSpinEnd;
    [SerializeField] private TMP_Text freeSpinWin;
    [SerializeField] private GameObject jackpots;
    [SerializeField] private GameObject logo;
    [SerializeField] private GameObject freeSpinCount;
    [SerializeField] private Button startFreeSpins;
    [SerializeField] private SpriteRenderer bg;
    [SerializeField] private SpriteRenderer freeSpinBg;
    private Tween freeSpinsTween;

    private bool canStartFreeSpin = false;

    private UltimateFireLinkRoute66FreeSpinController freeSpinController;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        freeSpinController = GetComponent<UltimateFireLinkRoute66FreeSpinController>();
        startFreeSpins.onClick.AddListener(OnClickFreeSpinStart);
    }

    public void StartFreeSpins()
    {
        StartCoroutine(StratFreeSpinTransition());
    }

    public IEnumerator StratFreeSpinTransition()
    {
        canStartFreeSpin = false;
        yield return new WaitUntil(() => UltimateFireLinkRoute66SlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => UltimateFireLinkRoute66UIManager.Instance.winAnimationCompleted);
        UltimateFireLinkRoute66UIManager.Instance.UpdateButtons("FreeSpin");

        FreeSpinStart.transform.localScale = Vector3.zero;
        FreeSpinParent.gameObject.SetActive(true);
        FreeSpinStart.SetActive(true);
        FreeSpinStart.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        jackpots.SetActive(false);
        logo.SetActive(false);
        freeSpinCount.SetActive(true);
        yield return new WaitForSeconds(1f);

        freeSpinsTween?.Kill();

        freeSpinsTween = startFreeSpins.transform
            .DOScale(1.2f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        canStartFreeSpin = true;
    }

    public void OnClickFreeSpinStart()
    {
        if (!canStartFreeSpin) return;
        FreeSpinStart.SetActive(false);
        FreeSpinParent.SetActive(false);
        bg.transform.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f);
        freeSpinBg.transform.GetComponent<SpriteRenderer>().DOFade(1f, 0.5f);
        freeSpinController.StartFreeSpins();

        UltimateFireLinkRoute66UIManager.Instance.PlayMusic("FreeSpinBG");
    }

    public void EndFreeSpin()
    {
        StartCoroutine(EndFreeSpinTransition());
    }

    private IEnumerator EndFreeSpinTransition()
    {
        yield return new WaitUntil(() => UltimateFireLinkRoute66SlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitForSeconds(1f);

        freeSpinWin.text = UltimateFireLinkRoute66SlotMachine.Instance.freeSpinWinAmount.ToString();

        FreeSpinStart.transform.localScale = Vector3.zero;
        FreeSpinParent.gameObject.SetActive(true);
        FreeSpinEnd.SetActive(true);
        FreeSpinEnd.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        jackpots.SetActive(true);
        logo.SetActive(true);
        freeSpinCount.SetActive(false);
        yield return new WaitForSeconds(2.5f);

        FreeSpinEnd.transform.DOScale(0f, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            FreeSpinEnd.SetActive(false);
            FreeSpinParent.SetActive(false);
            bg.transform.GetComponent<SpriteRenderer>().DOFade(1f, 0.5f);
            freeSpinBg.transform.GetComponent<SpriteRenderer>().DOFade(0f, 0.5f);
        });
    }

    private void WinAnimation(float freegamewin)
    {
        if (freegamewin > 0)
        {
            float betAmount = UltimateFireLinkRoute66UIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freegamewin, UltimateFireLinkRoute66SlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(PandaFortuneSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }


    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
}
