using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class GoldGobblersFreeGameTransitionController : MonoBehaviour
{
    #region Variables

    public static GoldGobblersFreeGameTransitionController Instance;

    [SerializeField] private UnityEngine.UI.Image freeSpinBackground;
    //[SerializeField] private GameObject freeSpinWinFrame;
    //[SerializeField] private GameObject freeSpinStartFrame;
    [SerializeField] private GameObject jackpotTitle;
    [SerializeField] private GameObject freeSpinTitle;
    //[SerializeField] private Image spinStartGlow;
    //[SerializeField] private Image spinEndGlow;

    [SerializeField] private GameObject freeSpinStart;
    [SerializeField] private GameObject freeSpinEnd;
    [SerializeField] private GameObject goldRocksParticles; 
    [SerializeField] private TMP_Text freeSpinWinText;
    [SerializeField] private TMP_Text freeSpinCount;
    [SerializeField] private UnityEngine.UI.Button startFreeSpinButton;
    [SerializeField] private TMP_Text startFreeSpinButtonText;
    private Tween startFreeSpinButtonTextTween;
    [SerializeField] private UnityEngine.UI.Button endFreeSpinButton;
    [SerializeField] private TMP_Text endFreeSpinButtonText;
    private Tween endFreeSpinButtonTextTween;

    private Coroutine glowCoroutine;

    private GoldGobblersFreeSpinController freeSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        freeSpinController = GetComponent<GoldGobblersFreeSpinController>();
        startFreeSpinButton.onClick.AddListener(onClickStartFreeSpins);
        endFreeSpinButton.onClick.AddListener(onClickEndFreeSpins);
        //freeSpinWinText = freeSpinWinFrame.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>();
    }

    #endregion

    #region Public References
    public void StartFreeSpinTransition()
    {
        GoldGobblersSlotMachine.Instance.isFreeGame = true;
        freeSpinController.ResetFreeSpins();

        StartCoroutine(StartFreeSpin());
    }

    public void EndFreeSpinTransition()
    {
        StartCoroutine(EndFreeSpin());
    }

    public void UpdateFreeSpinsCount(int freeSpins)
    {
        freeSpinController.UpdateFreeSpins(freeSpins);
    }
    public void NetworkErrorFreeSpin()
    {
        freeSpinController.ErrorFreeSpinReturn();
    }
    #endregion

    #region Game Transition
    [ContextMenu("Start Free Spin Transition")]
    private void StartFreeSpin_ContextMenu()
    {
        StartCoroutine(StartFreeSpin());
    }

    private IEnumerator StartFreeSpin()
    {
        freeSpinCount.text = $"{GoldGobblersSlotMachine.Instance.freeSpinCount}";
        yield return new WaitUntil(() => GoldGobblersSlotMachine.Instance.isSlotAnimationCompleted);
        yield return new WaitForSeconds(1f);
        string gameType = GoldGobblersSlotMachine.Instance.freeGameType;
        //string gameType = "red&green&blue";
        string startBool;

        if (gameType.Equals("blue") || gameType.Equals("red&blue") || gameType.Equals("red&blue") || gameType.Equals("red&green&blue"))
        {
            startBool = "freespinstartwild";
        }
        else startBool = "freespinstart";

        freeSpinStart.gameObject.SetActive(true);
        freeSpinStart.gameObject.transform.GetComponent<Animator>().SetBool(startBool, true);
        yield return new WaitForSeconds(1.3f);

        startFreeSpinButtonTextTween?.Kill();

        startFreeSpinButtonTextTween = startFreeSpinButtonText.transform
            .DOScale(1.2f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void onClickStartFreeSpins()
    {
        StartCoroutine(onClickStartFreeSpinsRoutine());
    }

    private IEnumerator onClickStartFreeSpinsRoutine()
    {
        freeSpinStart.gameObject.SetActive(false);
        startFreeSpinButtonTextTween?.Kill();
        startFreeSpinButtonText.transform.localScale = Vector3.one;
        string gameType = GoldGobblersSlotMachine.Instance.freeGameType;
        goldRocksParticles.SetActive(true);
        goldRocksParticles.transform.GetComponent<ParticleSystem>().Play();
        ShowBackground();

        switch (gameType)
        {
            case "red&green&blue":
                SwitchGameViewToFiveByFive(true);
                break;
            case "red&blue":
                SwitchGameViewToFiveByFive(true);
                break;
            case "red&green":
                SwitchGameViewToFiveByFive(true);
                break;
            case "red":
                SwitchGameViewToFiveByFive(true);
                break;
            default:
                break;
        }
        GoldGobblersUIManager.Instance.PlayGobblerAnimations(gameType);

        yield return new WaitForSeconds(2f);
        goldRocksParticles.SetActive(false);

        jackpotTitle.SetActive(false);
        freeSpinTitle.SetActive(true);
        freeSpinController.InitialFreeSpinText();

        GoldGobblersUIManager.Instance.UpdateButtons("Free Spin");

        freeSpinController.StartFreeSpins();
    }

    private IEnumerator EndFreeSpin()
    {
        yield return new WaitForSeconds(1f);

        freeSpinWinText.text = GoldGobblersSlotMachine.Instance.freeSpinWinAmount.ToString("F2");
        freeSpinEnd.gameObject.SetActive(true);
        freeSpinEnd.gameObject.transform.GetComponent<Animator>().SetBool("freespinend", true);
        yield return new WaitForSeconds(1.3f);

        endFreeSpinButtonTextTween?.Kill();

        endFreeSpinButtonTextTween = endFreeSpinButtonText.transform
            .DOScale(1.2f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void onClickEndFreeSpins()
    {
        StartCoroutine(onClickEndFreeSpinsRoutine());
    }

    private IEnumerator onClickEndFreeSpinsRoutine()
    {
        freeSpinEnd.gameObject.SetActive(false);
        endFreeSpinButtonTextTween?.Kill();
        startFreeSpinButtonText.transform.localScale = Vector3.one;
        string gameType = GoldGobblersSlotMachine.Instance.freeGameType;
        goldRocksParticles.SetActive(true);
        goldRocksParticles.transform.GetComponent<ParticleSystem>().Play();
        HideBackground();

        if (GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted)
        {
            SwitchGameViewToFiveByFive(false);
        }

        GoldGobblersUIManager.Instance.PlayGobblerAnimations("idle");

        yield return new WaitForSeconds(2f);
        goldRocksParticles.SetActive(false);

        jackpotTitle.SetActive(true);
        freeSpinTitle.SetActive(false);
        GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted = false;
        GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted = false;
        GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted = false;

        GoldGobblersUIManager.Instance.UpdateButtons("Transition End");
        GoldGobblersPaylineController.Instance.StopPaylines();

        if (GoldGobblersSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            WinAnimation();
        }
    }

    private void WinAnimation()
    {
        if (GoldGobblersSlotMachine.Instance.freeSpinWinAmount > 0)
        {
            float freeGameWin = GoldGobblersSlotMachine.Instance.freeSpinWinAmount;
            float betAmount = GoldGobblersUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, freeGameWin, GoldGobblersSlotMachine.Instance.currentSpinResult.newBalance);
            //Invoke(nameof(ZombieParadiseSlotMachine.Instance.UpdateGameCoin), 1f);
            GoldGobblersUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    //private IEnumerator Glow(Image glow)
    //{
    //    while (true)
    //    {
    //        glow.DOFade(1f, 1f);

    //        yield return new WaitForSeconds(0.25f);

    //        glow.DOFade(0f, 1f);

    //        yield return new WaitForSeconds(0.25f);
    //    }
    //}

    #endregion

    #region Helper Functions

    private void ShowBackground()
    {
        freeSpinBackground.DOFade(1f, 2f);
    }

    private void HideBackground()
    {
        freeSpinBackground.DOFade(0f, 2f);
    }

    private void SwitchGameViewToFiveByFive(bool isStartFreeGame)
    {
        GoldGobblersSlotMachine.Instance.ChangeReels(isStartFreeGame);
        //GoldGobblersSlotMachine.Instance.ChangeSlotBottomPosition(isStartFreeGame);
        GoldGobblersPaylineController.Instance.ChangeSlotScale(isStartFreeGame);
        GoldGobblersUIManager.Instance.SwitchSlotMachine(isStartFreeGame);
        GoldGobblersSlotMachine.Instance.InitializeReels();
    }

    public void MiddleGroundFreeGameTransition()
    {
        bool red = GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted;
        bool green = GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted;
        bool blue = GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted;
        string gameType = GoldGobblersSlotMachine.Instance.freeGameType;

        if (red && !green && !blue)
        {
            if (gameType.Equals("green&blue"))
            {
                GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted = true;
                GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green&blue");
                ShowGreenAndBlueLable("green&blue");
            }
            else if (gameType.Equals("blue"))
            {
                GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&blue");
                ShowGreenAndBlueLable("blue");
            }
            else if (gameType.Equals("green"))
            {
                GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green");
                ShowGreenAndBlueLable("green");
            }
        }
        else if (green && !red && !blue)
        {
            if (gameType.Equals("red&blue"))
            {
                GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted = true;
                GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green&blue");
                ConvertGameForRedFreeSpinInMiddle("blue", true);
            }
            else if (gameType.Equals("blue"))
            {
                GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("green&blue");
                ShowGreenAndBlueLable("blue");
            }
            else if (gameType.Equals("red"))
            {
                GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green");
                ConvertGameForRedFreeSpinInMiddle("", false);
            }
        }
        else if (blue && !red && !green)
        {
            if (gameType.Equals("red&green"))
            {
                GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted = true;
                GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green&blue");
                ConvertGameForRedFreeSpinInMiddle("green", true);
            }
            else if (gameType.Equals("green"))
            {
                GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("green&blue");
                ShowGreenAndBlueLable("green");
            }
            else if (gameType.Equals("red"))
            {
                GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&blue");
                ConvertGameForRedFreeSpinInMiddle("", false);
            }
        }
        else if (red && green && !blue)
        {
            if (gameType.Equals("blue"))
            {
                GoldGobblersSlotMachine.Instance.hasBlueFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green&blue");
                ShowGreenAndBlueLable("blue");
            }
        }
        else if (red && blue && !green)
        {
            if (gameType.Equals("green"))
            {
                GoldGobblersSlotMachine.Instance.hasGreenFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green&blue");
                ShowGreenAndBlueLable("green");
            }
        }
        else if (green && blue && !red)
        {
            if (gameType.Equals("red"))
            {
                GoldGobblersSlotMachine.Instance.hasRedFreeGameStarted = true;
                GoldGobblersUIManager.Instance.PlayGobblerAnimations("red&green&blue");
                ConvertGameForRedFreeSpinInMiddle("", false);
            }
        }
    }

    private void ShowGreenAndBlueLable(string startBool)
    {
        StartCoroutine(ShowGreenAndBlueLableCoroutine(startBool));
    }

    private IEnumerator ShowGreenAndBlueLableCoroutine(string startBool)
    {
        freeSpinStart.SetActive(true);
        freeSpinStart.transform.GetComponent<Animator>().SetBool(startBool, true);
        yield return new WaitForSeconds(1.3f);
        freeSpinStart.SetActive(false);
        GoldGobblersSlotMachine.Instance.hasNewFreeGameTriggeredInBetween = false;
    }

    private void ConvertGameForRedFreeSpinInMiddle(string startBool, bool showAdditionalAnim)
    {
        StartCoroutine(RedFreeSpinTransition(startBool, showAdditionalAnim));
    }

    private IEnumerator RedFreeSpinTransition(string startBool, bool anim)
    {
        yield return new WaitForSeconds(1f);
        goldRocksParticles.SetActive(true);
        goldRocksParticles.transform.GetComponent<ParticleSystem>().Play();
        yield return new WaitForSeconds(1f);
        SwitchGameViewToFiveByFive(true);
        yield return new WaitForSeconds(1f);
        goldRocksParticles.SetActive(false);
        if (anim)
        {
            ShowGreenAndBlueLable(startBool);
        }
        else
        {
            GoldGobblersSlotMachine.Instance.hasNewFreeGameTriggeredInBetween = false;
            yield return null; 
        }
    }

    #endregion
}
