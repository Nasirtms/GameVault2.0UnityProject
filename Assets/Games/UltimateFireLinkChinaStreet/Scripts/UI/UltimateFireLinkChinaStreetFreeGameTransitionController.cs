using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UltimateFireLinkChinaStreetFreeGameTransitionController : MonoBehaviour
{
    public static UltimateFireLinkChinaStreetFreeGameTransitionController Instance { get; private set; }

    public float hopHeight = 0.5f;
    public float duration = 0.6f;
    public Ease ease = Ease.OutQuad;


    [SerializeField] private TMP_Text freeSpinWin;


    private bool canStartFreeSpin = false;
    private bool canEndFreeSpin = false;

    private Animator FreeSpinAnimator;

    private UltimateFireLinkChinaStreetFreeSpinController freeSpinController;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        freeSpinController = GetComponent<UltimateFireLinkChinaStreetFreeSpinController>();
    }

    public void StartFreeSpins()
    {
        StartCoroutine(StratFreeSpinTransition());
    }

    public IEnumerator StratFreeSpinTransition()
    {
        yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted);

        freeSpinController.StartFreeSpins();
        UltimateFireLinkChinaStreetUIManager.Instance.PlayMusic("FreeSpinBG");
    }

    public void OnClickFreeSpinStart()
    {
    }

    public void EndFreeSpin()
    {
        StartCoroutine(EndFreeSpinTransition());
    }

    private IEnumerator EndFreeSpinTransition()
    {
        yield return new WaitUntil(() => UltimateFireLinkChinaStreetSlotMachine.Instance.isSlotAnimationCompleted);
        freeSpinWin.text = UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinWinAmount.ToString("F2");

        var pc = UltimateFireLinkChinaStreetPaylineController.Instance;

        pc.freeSpinEnd.transform.localScale = Vector3.zero;
        pc.freeSpinParent.SetActive(true);
        pc.freeSpinEnd.SetActive(true);
        pc.freeSpinEnd.transform.DOScale(0.8f, 0.5f).SetEase(Ease.OutBack);
        pc.ads.SetActive(true);
        pc.jackpots.SetActive(true);
        pc.freeSpinCountObj.SetActive(false);
        if (pc.freeSpin20.activeSelf) pc.freeSpin20.SetActive(false);
        if (pc.freeSpin15.activeSelf) pc.freeSpin15.SetActive(false);
        if (pc.freeSpin10.activeSelf) pc.freeSpin10.SetActive(false);
        if (pc.freeSpin7.activeSelf) pc.freeSpin7.SetActive(false);
        if (pc.freeSpin5.activeSelf) pc.freeSpin5.SetActive(false);
        if (pc.freeSpinMystery.activeSelf) pc.freeSpinMystery.SetActive(false);
        pc.freeSpinTypeParent.SetActive(false);
        pc.wildMultiplier.SetActive(false);
        yield return new WaitForSeconds(3f);

        pc.freeSpinEnd.transform.DOScale(Vector3.zero, 0.5f).OnComplete(() =>
        {
            pc.freeSpinEnd.SetActive(false);
            pc.freeSpinParent.SetActive(false);
        });

        yield return new WaitForSeconds(0.6f);

        float finalAmount = UltimateFireLinkChinaStreetSlotMachine.Instance.freeSpinWinAmount;
        if (finalAmount > 0)
        {
            WinAnimation(finalAmount);
        }
        yield return new WaitUntil(() => UltimateFireLinkChinaStreetUIManager.Instance.winAnimationCompleted);
    }

    public void OnClickFreeSpinEnd()
    {
    }

    private void WinAnimation(float freegamewin)
    {
        if (freegamewin > 0)
        {
            float betAmount = UltimateFireLinkChinaStreetUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freegamewin, UltimateFireLinkChinaStreetSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(PandaFortuneSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
}
