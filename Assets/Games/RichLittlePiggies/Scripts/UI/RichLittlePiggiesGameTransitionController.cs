using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RichLittlePiggiesGameTransitionController : MonoBehaviour
{
    public static RichLittlePiggiesGameTransitionController Instance { get; private set; }

    [SerializeField] private GameObject freeSpinParent;
    [SerializeField] private GameObject freeSpinStart;
    [SerializeField] private GameObject freeSpinEnd;
    [SerializeField] private GameObject freeSpinCount;
    [SerializeField] private TMP_Text freeSpinWin;

    //[SerializeField] private Button startFreeSpins;
    //[SerializeField] private TMP_Text startFreeSpinsText;
    //private Tween startFreeSpinsTween;
    //[SerializeField] private Button endFreeSpins;
    //[SerializeField] private TMP_Text endFreeSpinsText;
    //private Tween endFreeSpinsTween;

    [SerializeField] private TMP_Text totalFreeSpin;

    [SerializeField] private GameObject anyThreeRibbons;
    [SerializeField] private GameObject anyTwoRibbons;
    [SerializeField] private GameObject ribbon3;
    [SerializeField] private GameObject ribbon2;
    [SerializeField] private GameObject anyOneRibbon;
    [SerializeField] private GameObject ribbon1;
    [SerializeField] private Sprite redRibbon;
    [SerializeField] private Sprite blueRibbon;
    [SerializeField] private Sprite yellowRibbon;

    private bool canStartFreeSpin = false;
    private bool canEndFreeSpin = false;


    private Animator FreeSpinAnimator;

    private RichLittlePiggiesFreeSpinController freeSpinController;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        freeSpinController = GetComponent<RichLittlePiggiesFreeSpinController>();
        FreeSpinAnimator = freeSpinParent.GetComponent<Animator>();
        //startFreeSpins.onClick.AddListener(OnClickFreeSpinStart);
        //endFreeSpins.onClick.AddListener(OnClickFreeSpinEnd);
    }

    public void StartFreeSpins()
    {
        StartCoroutine(StratFreeSpinTransition());
    }

    public IEnumerator StratFreeSpinTransition()
    {
        //canStartFreeSpin = false;
        DetermineGameType(RichLittlePiggiesSlotMachine.Instance.freeGameType, true);
        yield return new WaitUntil(() => RichLittlePiggiesSlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitForSeconds(1f);
        int x = RichLittlePiggiesSlotMachine.Instance.freeSpinCount;
        totalFreeSpin.text = $"You've got <color=#06FF00>{x}</color> free spins";
        yield return new WaitUntil(() => RichLittlePiggiesUIManager.Instance.winAnimationCompleted);
        RichLittlePiggiesUIManager.Instance.UpdateButtons("FreeSpin");

        freeSpinParent.SetActive(true);
        freeSpinStart.SetActive(true);

        freeSpinStart.transform.DOScale(0.8f, 0.5f).OnComplete(() =>
        {
            freeSpinStart.transform.DOScale(0.5f, 0.3f).SetEase(Ease.InOutSine);
        });

        yield return new WaitForSeconds(0.8f);

        yield return new WaitForSeconds(1f);

        freeSpinStart.transform.DOScale(0f, 0.5f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            freeSpinStart.SetActive(false);
            freeSpinParent.SetActive(false);
            freeSpinCount.SetActive(true);
            DetermineGameType(RichLittlePiggiesSlotMachine.Instance.freeGameType, false);
        });

        yield return new WaitForSeconds(1f);

        RichLittlePiggiesUIManager.Instance.PlayMusic("FreeSpinBG");
        freeSpinController.StartFreeSpins();
        //canStartFreeSpin = true;
        //startFreeSpinsTween?.Kill();

        //startFreeSpinsTween = startFreeSpinsText.transform
        //    .DOScale(1.2f, 0.5f)
        //    .SetEase(Ease.InOutSine)
        //    .SetLoops(-1, LoopType.Yoyo);
    }

    //public void OnClickFreeSpinStart()
    //{
    //    if (!canStartFreeSpin) return;
    //    //freeSpinPopupParent.SetActive(false);
    //    //freeSpinStartPopup.SetActive(false);
    //}

    public void EndFreeSpin()
    {
        StartCoroutine(EndFreeSpinTransition());
    }

    private IEnumerator EndFreeSpinTransition()
    {
        canEndFreeSpin = false;
        freeSpinParent.SetActive(true);
        freeSpinEnd.SetActive(true);
        freeSpinWin.text = RichLittlePiggiesSlotMachine.Instance.freeSpinWinAmount.ToString("F2");

        freeSpinEnd.transform.DOScale(0.8f, 0.5f).OnComplete(() =>
        {
            freeSpinEnd.transform.DOScale(0.5f, 0.3f).SetEase(Ease.InOutSine);
        });

        yield return new WaitForSeconds(0.8f);

        yield return new WaitForSeconds(1f);

        freeSpinEnd.transform.DOScale(0f, 0.5f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            freeSpinEnd.SetActive(false);
            freeSpinParent.SetActive(false);
            freeSpinCount.SetActive(false);
            DetermineGameType(RichLittlePiggiesSlotMachine.Instance.freeGameType, false);
        });

        yield return new WaitForSeconds(1f);

        float finalAmount = RichLittlePiggiesSlotMachine.Instance.freeSpinWinAmount;
        if (finalAmount > 0)
        {
            WinAnimation(finalAmount);
        }
        //yield return new WaitUntil(() => RichLittlePiggiesUIManager.Instance.winAnimationCompleted);

        //yield return new WaitForSeconds(1f);

        //endFreeSpinsTween?.Kill();

        //endFreeSpinsTween = endFreeSpinsText.transform
        //    .DOScale(1.2f, 0.5f)
        //    .SetEase(Ease.InOutSine)
        //    .SetLoops(-1, LoopType.Yoyo);
        //freeSpinParent.SetActive(false);
        canEndFreeSpin = true;
    }

    //public void OnClickFreeSpinEnd()
    //{
    //    if (!canEndFreeSpin) return;
    //}

    private void WinAnimation(float freegamewin)
    {
        if (freegamewin > 0)
        {
            float betAmount = RichLittlePiggiesUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, freegamewin, RichLittlePiggiesSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(PandaFortuneSlotMachine.Instance.UpdateGameCoin), 1f);
        }
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }

    private void DetermineGameType(string gameType, bool someFlag)
    {
        switch (gameType)
        {
            case "red&yellow&blue":
                anyThreeRibbons.SetActive(someFlag);
                break;
            case "red&yellow":
                anyTwoRibbons.SetActive(someFlag);
                ribbon3.GetComponent<Image>().sprite = redRibbon;
                ribbon2.GetComponent<Image>().sprite = yellowRibbon;
                break;
            case "red&blue":
                anyTwoRibbons.SetActive(someFlag);
                ribbon3.GetComponent<Image>().sprite = redRibbon;
                ribbon2.GetComponent<Image>().sprite = blueRibbon;
                break;
            case "yellow&blue":
                anyTwoRibbons.SetActive(someFlag);
                ribbon3.GetComponent<Image>().sprite = yellowRibbon;
                ribbon2.GetComponent<Image>().sprite = blueRibbon;
                break;
            case "red":
                anyOneRibbon.SetActive(someFlag);
                ribbon1.GetComponent<Image>().sprite = redRibbon;
                break;
            case "yellow":
                anyOneRibbon.SetActive(someFlag);
                ribbon1.GetComponent<Image>().sprite = yellowRibbon;
                break;
            case "blue":
                anyOneRibbon.SetActive(someFlag);
                ribbon1.GetComponent<Image>().sprite = blueRibbon;
                break;
        }
    }
}
