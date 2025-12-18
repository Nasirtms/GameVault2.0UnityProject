using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(MonkeyMadnessBetController))]
[RequireComponent(typeof(MonkeyMadnessRulesPopupController))]
[RequireComponent(typeof(MonkeyMadnessAutoSpinController))]
public class MonkeyMadnessUIManager : GameBetServices
{
    #region Variables

    public static MonkeyMadnessUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private MonkeyMadnessUIButtonController spinButton;
    [SerializeField] private MonkeyMadnessUIButtonController stopButton;

    [Header("Fast Buttons")]
    [SerializeField] private Button fastButtonOff;
    [SerializeField] private Button fastButtonOn;

    [Header("Menu Buttons")]
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button openRulesButton;

    [Header("Title Image")]
    [SerializeField] private Image titleImage;
    [SerializeField] private List<Sprite> titleImages;
    [SerializeField] float delayInSwap = 2f;
    private int currentIndex = 0;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;
    public Coroutine textAnimationCoroutine;
    [Header("Win Animations")]
    [SerializeField] private List<Animator> winAnimators;
    [SerializeField] private GameObject winAnimations;
    [SerializeField] private GameObject niceWin;
    [SerializeField] private GameObject bigWin;
    [SerializeField] private GameObject megaWin;
    [SerializeField] private GameObject superWin;
    [SerializeField] private GameObject jackpotWin;
    [SerializeField] private TMP_Text niceWinText;
    [SerializeField] private TMP_Text bigWinText;
    [SerializeField] private TMP_Text megaWinText;
    [SerializeField] private TMP_Text superWinText;
    [SerializeField] private TMP_Text jackpotWinText;
    [SerializeField] private string niceWinTrigger;
    [SerializeField] private string bigWinTrigger;
    [SerializeField] private string megaWinTrigger;
    [SerializeField] private string superWinTrigger;
    [SerializeField] private string jackpotWinTrigger;
    public bool winAnimationCompleted = false;
    public Coroutine winCoroutine;

    // Scripts
    private MonkeyMadnessBetController betController;
    private MonkeyMadnessRulesPopupController rulesPopupController;
    private MonkeyMadnessAutoSpinController autoSpinController;

    private string currentButtonSet;

    //Sounds
    private bool soundActive = true;
    private bool musicActive = true;
    private int reelsStopped = 0;
    private int totalReels = 3;
    private bool hasStoppedSpinSound = false;
    private bool hasStoppedReelStopSFX = false;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
        betController = GetComponent<MonkeyMadnessBetController>();
        rulesPopupController = GetComponent<MonkeyMadnessRulesPopupController>();
        autoSpinController = GetComponent<MonkeyMadnessAutoSpinController>();

        //Sound
        MonkeyMadnessReelScript.OnSpinStart += HandleReelStart;
        MonkeyMadnessReelScript.OnSpinComplete += HandleReelStop;
        hasStoppedSpinSound = false;
        hasStoppedReelStopSFX = false;
        reelsStopped = 0;
        soundActive = true;
        musicActive = true;

        UpdateCoins();
        SetupInputButtons();
        ToggleFastButton(false);

        if (titleImages.Count > 0 && titleImage != null)
        {
            StartCoroutine(LoopTitleImages());
        }
        PlayMusic("Background");

        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }

    private void OnDestroy()
    {
        RemoveListeners(spinButton.GetButtonComponent());
        RemoveListeners(stopButton.GetButtonComponent());

        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);
            
        RemoveListeners(exitGameButton);
        RemoveListeners(openRulesButton);
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;
    }

    #endregion

    #region Button Setup

    private void RemoveListeners(Button button)
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    private void SetupInputButtons()
    {
        stopButton.GetButtonComponent().onClick.AddListener(OnClickStop);

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);

        fastButtonOff.onClick.AddListener(() => ToggleFastButton(true));
        fastButtonOn.onClick.AddListener(() => ToggleFastButton(false));

        exitGameButton.onClick.AddListener(ExitGame);
        openRulesButton.onClick.AddListener(OpenRulesPopup);
    }

    #endregion

    #region Input Buttons

    #region Sound

    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName) || MonkeyMadnessSoundManager.Instance == null) return;
        if (!MonkeyMadnessSoundManager.Instance.IsSoundMute())
            MonkeyMadnessSoundManager.Instance.PlaySFX(soundName);
    }

    private void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName) || MonkeyMadnessSoundManager.Instance == null) return;
        if (!MonkeyMadnessSoundManager.Instance.IsMusicMute())
            MonkeyMadnessSoundManager.Instance.PlayMusic(soundName);
    }
    private void HandleReelStart(int index)
    {
        if (!hasStoppedSpinSound && index == 0)
        {
            hasStoppedSpinSound = true;
            MonkeyMadnessSoundManager.Instance.StopReelStopSFX();
        }
    }
    private void HandleReelStop(int index)
    {
        MonkeyMadnessSoundManager.Instance.PlayReelStopSFX("ReelStop");

        reelsStopped++;
        if (!hasStoppedReelStopSFX && reelsStopped >= totalReels)
        {
            hasStoppedReelStopSFX = true;
            MonkeyMadnessSoundManager.Instance.StopReelStopSFX();
        }
    }
    #endregion

    #region Bet Control

    private void IncreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Button");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Button");
        betController.DecreaseChipValue();
    }

    #endregion

    private void ToggleFastButton(bool active)
    {
        fastButtonOn.gameObject.SetActive(active);
        fastButtonOff.gameObject.SetActive(!active);

        if (active)
        {
            MonkeyMadnessSlotMachine.Instance.settings.spinSettings.endSpin = MonkeyMadnessSpinType.All;
            MonkeyMadnessSlotMachine.Instance.settings.spinSettings.acceleration = new Vector2(0.4f, 0.5f);
        }
        else
        {
            MonkeyMadnessSlotMachine.Instance.settings.spinSettings.endSpin = MonkeyMadnessSpinType.Single;
            MonkeyMadnessSlotMachine.Instance.settings.spinSettings.acceleration = new Vector2(0.1f, 0.2f);
        }
    }

    private void ExitGame()
    {
        PlaySound("Button");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManager.LoadScene("Main");
    }

    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        PlaySound("Button");
        rulesPopupController.OpenPopup();
    }

    #endregion

    #region Spin Buttons

    public void OnClickSpin()
    {
        PlaySound("Spin");
        hasStoppedSpinSound = false;
        hasStoppedReelStopSFX = false;
        reelsStopped = 0;

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }

        SlotSpinService.Instance.Spin(betAmount);
        UpdateButtons("Spin");
    }

    private void OnClickStop()
    {
        PlaySound("Button");
        UpdateButtons("Stop");

        MonkeyMadnessSlotMachine.Instance.isStopBtnPressed = true;
        if (MonkeyMadnessAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }

        //ReelStopSound
        if (!hasStoppedSpinSound)
        {
            hasStoppedSpinSound = false;
            MonkeyMadnessSoundManager.Instance.StopReelStopSFX();
        }

        if (!hasStoppedReelStopSFX)
        {
            hasStoppedReelStopSFX = true;
            MonkeyMadnessSoundManager.Instance.StopReelStopSFX();
        }
        MonkeyMadnessSlotMachine.Instance.StopWithResult();
        MonkeyMadnessSlotMachine.Instance.InvokeStop();
    }

    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;

        PlaySound("Spin");
        float betAmount = betController.GetCurrentBet();
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        autoSpinController.StartAutoSpin(betAmount);
        UpdateButtons("Spin");
    }

    #endregion

    #region UI Update

    public void UpdateCoins()
    {
        if (UserManager.Instance != null)
        {
            coins.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }
    }

    public void UpdateButtons(string type)
    {
        bool inSpin = false;

        switch (type)
        {
            case "Spin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;

            case "Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);

                break;

            default:
                return;
        }

        spinButton.GetButtonComponent().interactable = !inSpin;
        if (inSpin)
        {
            exitGameButton.interactable = !inSpin;
            increaseBetButton.interactable = !inSpin;
            decreaseBetButton.interactable = !inSpin;
        }
        else
        {
            exitGameButton.interactable = !inSpin;
            increaseBetButton.interactable = !inSpin;
            decreaseBetButton.interactable = !inSpin;
        }

        currentButtonSet = type;
    }

    private IEnumerator LoopTitleImages()
    {
        while (true)
        {
            titleImage.sprite = titleImages[currentIndex];
            titleImage.SetNativeSize();

            currentIndex = (currentIndex + 1) % titleImages.Count;

            yield return new WaitForSeconds(delayInSwap);
        }
    }

    #endregion

    #region Text Animation

    public void UpdateWinAmount(float winAmount)
    {
        if (winAmount > 0)
        {
            PlayTextAnimation(winAmount);
        }
        else
        {
            this.winAmount.text = "0.00";
        }
    }

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount));
    }

    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimate)
    {
        float startValue = 0f;

        if (!string.IsNullOrEmpty(textToAnimate.text) && float.TryParse(textToAnimate.text, out float current))
        {
            startValue = current;
        }

        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(startValue, target, t);
            textToAnimate.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimate.text = target.ToString("0.00");
    }
    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimateOne, TMP_Text textToAnimateTwo)
    {
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(0f, target, t);
            textToAnimateOne.text = displayed.ToString("0.00");
            textToAnimateTwo.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final value is exact
        textToAnimateOne.text = target.ToString("0.00");
        textToAnimateTwo.text = target.ToString("0.00");

        StopCoroutine(textAnimationCoroutine);

    }

    #endregion

    #region Helper Functions

    public string CurrentButtonSet()
    {
        return currentButtonSet;
    }

    public void SetSpinInteractable(bool state)
    {
        spinButton.SetButtonInteractable(state);
        TMP_Text text = spinButton.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (state)
        {
            text.color = new Color(1f, 1f, 1f, 1f);
        }
        else
            text.color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }

    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
        TMP_Text text = stopButton.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (state)
        {
            text.color = new Color(1f, 1f, 1f, 1f);
        }
        else
            text.color = new Color(0.5f, 0.5f, 0.5f, 1f);
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    #endregion
    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }

    #region Win Animations

    public void PlayNiceWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(niceWin, niceWinText, winAmount, 0, niceWinTrigger));
    }

    public void PlayBigWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(bigWin, bigWinText, winAmount, 1, bigWinTrigger));
    }

    public void PlayMegaWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(megaWin, megaWinText, winAmount, 2, megaWinTrigger));
    }

    public void PlaySuperWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(superWin, superWinText, winAmount, 3, superWinTrigger));
    }

    public void PlayJackpotWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(jackpotWin, jackpotWinText, winAmount, 4, jackpotWinTrigger));
    }

    private IEnumerator WinAnimation(GameObject winType, TMP_Text winText, float targetAmount, int animatorIndex, string animationTrigger)
    {
        winAnimationCompleted = false;

        yield return new WaitForSeconds(1.5f);

        winAnimations.SetActive(true);
        winType.SetActive(true);

        Animator animator = winAnimators[animatorIndex];

        animator.enabled = true;
        animator.SetTrigger(animationTrigger);

        winText.text = "0.00";

        yield return new WaitForSeconds(1.5f);

        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;

        StopCoroutine(winCoroutine);
    }

    #endregion
}
