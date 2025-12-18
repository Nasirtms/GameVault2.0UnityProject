using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(FruitMaryBetController))]
[RequireComponent(typeof(FruitMaryRulesPopupController))]
[RequireComponent(typeof(FruitMaryAutoSpinController))]
public class FruitMaryUIManager : GameBetServices
{
    #region Variables
    public static FruitMaryUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;
    [SerializeField] public TMP_Text winAmount;
    [SerializeField] private float duration = 1.5f;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button maxBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public FruitMaryUIButtonController spinButton;
    [SerializeField] private Image autoSpinIndiactorImage;
    [SerializeField] public FruitMaryUIButtonController stopButton;

    [Header("Menu Buttons")]
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button musicControlButton; 
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Button soundControlButton;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Button openRulesButton;

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

    private bool soundOn;
    private bool musicOn;


    [Header("Free Game")]
    [SerializeField] TMP_Text freeGameWinAmountText;
    [HideInInspector] public int freeGameSpinCount;
    [HideInInspector] public float freeGameWinAmount;
    private FruitMaryGameTransitionController gameTransitionController;
    private Coroutine freeSpinWinTextCoroutine;

    private FruitMaryBetController betController;
    private FruitMaryRulesPopupController rulesPopupController;
    private FruitMaryAutoSpinController autoSpinController;

    public Coroutine textAnimationCoroutine;
    [HideInInspector] public bool singleSpin;
    [HideInInspector] public bool autoSpin;
    private float currentSpinWin;
    private string currentButtonSet;
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
        betController = GetComponent<FruitMaryBetController>();
        rulesPopupController = GetComponent<FruitMaryRulesPopupController>();
        autoSpinController = GetComponent<FruitMaryAutoSpinController>();
        gameTransitionController = GetComponent<FruitMaryGameTransitionController>();

        winAmount.text = "0.00";
        UpdateCoins();
        SetupInputButtons();

        PlayMusic("Music");

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }

    private void OnDestroy()
    {
        RemoveListeners(spinButton?.GetButtonComponent());
        RemoveListeners(stopButton?.GetButtonComponent());

        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);
        RemoveListeners(exitGameButton);
        RemoveListeners(musicControlButton);
        RemoveListeners(soundControlButton);
        RemoveListeners(openRulesButton);
        RemoveListeners(maxBetButton);

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
        maxBetButton.onClick.AddListener(SetMaxBet);

        exitGameButton.onClick.AddListener(ExitGame);
        soundControlButton.onClick.AddListener(() => SoundActive(soundOn));
        musicControlButton.onClick.AddListener(() => MusicActive(musicOn));
        openRulesButton.onClick.AddListener(OpenRulesPopup);
    }

    #endregion

    #region Sound
    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitMarySoundManager.Instance.IsSoundMute())
            FruitMarySoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitMarySoundManager.Instance.IsMusicMute())
            FruitMarySoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitMarySoundManager.Instance.IsMusicMute())
            FruitMarySoundManager.Instance.StopMusic(soundName);
    }

    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitMarySoundManager.Instance.IsMusicMute())
            FruitMarySoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitMarySoundManager.Instance.IsMusicMute())
            FruitMarySoundManager.Instance.StopWinMusic(soundName);
    }
    private void SoundActive(bool soundActive)
    {
        FruitMarySoundManager.Instance.MuteSFX(!soundActive);

        Image soundButtonImage = soundControlButton.GetComponent<Image>();

        if (soundActive)
        {
            soundButtonImage.sprite = soundOnSprite;
        }
        else
        {
            soundButtonImage.sprite = soundOffSprite;
        }

        soundOn = !soundOn;
    }

    private void MusicActive(bool musicActive)
    {
        FruitMarySoundManager.Instance.MuteMusic(!musicActive);

        Image musicButtonImage = musicControlButton.GetComponent<Image>();

        if (musicActive)
        {
            musicButtonImage.sprite = musicOnSprite;
        }
        else
        {
            musicButtonImage.sprite = musicOffSprite;
        }

        musicOn = !musicOn;
    }
    #endregion


    #region Bet
    private void IncreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("FruitMary_Button");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("FruitMary_Button");
        betController.DecreaseChipValue();
    }
    private void SetMaxBet()
    { 
        if (betController == null) return;
        PlaySound("FruitMary_Button");
        betController.SetMaxBet();
    }
    #endregion

    private void ExitGame()
    {
        PlaySound("FruitMary_Button");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManager.LoadScene("Main");
    }
    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        PlaySound("FruitMary_Button");
        rulesPopupController.OpenPopup();
    }

    public void ShowStopButton()
    {
        stopButton.ShowButton(true);
        spinButton.ShowButton(false);
    }
    public void OnClickSpin()
    {
        if (FruitMarySlotMachine.Instance.InSpin) return;
        FruitMaryPaylineController.Instance?.StopPaylines();
        FruitMaryPaylineController.Instance?.ClearPaylineData();
        if (FruitMarySlotMachine.Instance.isFreeGameReady || FruitMarySlotMachine.Instance.isFruitMaryGameReady)
        {
            return;
        }


        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;
        singleSpin = true;
        PlaySound("Spin");
        UpdateButtons("Spin");

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }

        UserManager.Instance.currentBetAmount = betAmount;
        singleSpin = false;
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        PlaySound("FruitMary_Button");
        if (FruitMarySlotMachine.Instance.isFreeGame)
        {
            UpdateButtons("Free Spin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        FruitMarySlotMachine.Instance.isStopBtnPressed = true;

        if (FruitMaryAutoSpinController.isAutoSpinning)
            autoSpinController.CancelAutoSpin();

        FruitMarySlotMachine.Instance.StopWithResult();
        FruitMarySlotMachine.Instance.InvokeStop();
    }

    public void ToggleSpinButton()
    {
        OnClickStop();
    }
    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;
        FruitMaryPaylineController.Instance?.StopPaylines();
        FruitMaryPaylineController.Instance?.ClearPaylineData();
        if (FruitMarySlotMachine.Instance.InSpin || FruitMaryAutoSpinController.isAutoSpinning)
        {
            return;
        }
        if (FruitMarySlotMachine.Instance.isFreeGameReady || FruitMarySlotMachine.Instance.isFruitMaryGameReady)
        {
            return;
        }
        UpdateButtons("Spin");
        float betAmount = betController.GetCurrentBet();

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        autoSpinController.StartAutoSpin(betAmount);
    }
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
                inSpin = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;

            case "Stop":
                inSpin = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "Transition Start":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            case "Transition End":
                inSpin = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            case "Free Spin":
                inSpin = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;
            case "FreeSpin End":
                inSpin = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            default:
                return;
        }

        spinButton.GetButtonComponent().interactable = inSpin;

        exitGameButton.interactable = inSpin;
        increaseBetButton.interactable = inSpin;
        decreaseBetButton.interactable = inSpin;
        maxBetButton.interactable = inSpin;
        currentButtonSet = type;
    }

    public void HideStopButton()
    {
     
        stopButton.ShowButton(false);
        spinButton.ShowButton(true);
    }
    #endregion

    #region Text Animation
    public void UpdateWinAmount(float winAmount, bool compound = false)
    {
        if (winAmount > 0)
        {
            if (compound)
            {
                currentSpinWin += winAmount;
            }
            else
            {
                currentSpinWin = winAmount;
            }
            PlayTextAnimation(currentSpinWin);
        }
        else
        {
            currentSpinWin = 0;
            this.winAmount.text = "0.00";
        }
    }
    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        PlayWinMusic("Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 0.5f, this.winAmount));
    }
    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        freeSpinWinTextCoroutine = StartCoroutine(AnimateToValue(target, duration, textToAnimate));
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
        StopWinMusic("Win");
        PlaySound("WinEnd");
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
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;
        if (FruitMarySlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Free Spin");
        }
        else if (FruitMaryAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Spin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        StopCoroutine(winCoroutine);
    }

    #endregion

    #region Helper Functions
    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }

    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }
    #endregion
}