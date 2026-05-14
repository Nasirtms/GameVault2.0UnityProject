using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(FruitParadiseBetController))]
[RequireComponent(typeof(FruitParadiseRulesPopupController))]
[RequireComponent(typeof(FruitParadiseAutoSpinController))]
public class FruitParadiseUIManager : GameBetServices
{
    #region Variables
    public static FruitParadiseUIManager Instance;

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
    [SerializeField] public FruitParadiseUIButtonController spinButton;
    [SerializeField] public FruitParadiseUIButtonController stopButton;

    [Header("Menu Buttons")]
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button musicControlButton;
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Button soundControlButton;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Button openRulesButton;
    private bool soundOn;
    private bool musicOn;
    private FruitParadiseBetController betController;
    private FruitParadiseRulesPopupController rulesPopupController;
    private FruitParadiseAutoSpinController autoSpinController;

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
    [HideInInspector] public bool winAnimationCompleted = false;
    public Coroutine winCoroutine;
    public Coroutine textAnimationCoroutine;

    [HideInInspector] public bool singleSpin;
    [HideInInspector] public bool autoSpin;
    private float currentSpinWin;
    private Coroutine freeSpinWinTextCoroutine;
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
        winAmount.text = "0.00";
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
        betController = GetComponent<FruitParadiseBetController>();
        rulesPopupController = GetComponent<FruitParadiseRulesPopupController>();
        autoSpinController = GetComponent<FruitParadiseAutoSpinController>();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);
        UpdateCoins();
        SetupInputButtons();

        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("BG");
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
        if (!FruitParadiseSoundManager.Instance.IsSoundMute())
            FruitParadiseSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitParadiseSoundManager.Instance.IsMusicMute())
            FruitParadiseSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitParadiseSoundManager.Instance.IsMusicMute())
            FruitParadiseSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitParadiseSoundManager.Instance.IsSoundMute())
            FruitParadiseSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitParadiseSoundManager.Instance.IsSoundMute())
            FruitParadiseSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitParadiseSoundManager.Instance.IsSoundMute())
            FruitParadiseSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FruitParadiseSoundManager.Instance.IsSoundMute())
            FruitParadiseSoundManager.Instance.StopWinMusic(soundName);
    }
    private void SoundActive(bool soundActive)
    {
        FruitParadiseSoundManager.Instance.MuteSFX(!soundActive);

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
        FruitParadiseSoundManager.Instance.MuteMusic(!musicActive);

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
        PlaySound("FruitParadise_Increase_Button");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("FruitParadise_Decrease_Button");
        betController.DecreaseChipValue();
    }
    private void SetMaxBet()
    {
        if (betController == null) return;
        PlaySound("FruitParadise_Button");
        betController.SetMaxBet();
    }
    #endregion

    private void ExitGame()
    {
        PlaySound("FruitParadise_Button");
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
    }
    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        PlaySound("FruitParadise_Button");
        rulesPopupController.OpenPopup();
    }
    public void OnClickSpin()
    {
        if (FruitParadiseSlotMachine.Instance.InSpin) return;

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        UpdateButtons("Spin");
        singleSpin = true;

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
        PlaySound("FruitParadise_Button");
        StopSpinMusic("FruitParadise_Spin");
        if (FruitParadiseSlotMachine.Instance.isFreeGame)
        {
            UpdateButtons("Free Spin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        FruitParadiseSlotMachine.Instance.isStopBtnPressed = true;

        if (FruitParadiseAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }
        FruitParadiseSlotMachine.Instance.StopWithResult();
        FruitParadiseSlotMachine.Instance.InvokeStop();
    }

    public void ToggleSpinButton()
    {
        OnClickStop();
    }
    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;
        if (FruitParadiseSlotMachine.Instance.InSpin || FruitParadiseAutoSpinController.isAutoSpinning)
        {
            return;
        }
        UpdateButtons("Spin");
        float betAmount = betController.GetCurrentBet();
        if (textAnimationCoroutine != null)
        {
            StopWinMusic("Win");
            StopCoroutine(textAnimationCoroutine);
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
    #endregion

    #region Text Animation
    private string FormatFloorValue(float value)
    {
        float floored = Mathf.Floor(value * 100f) / 100f;
        return floored.ToString("0.00");
    }
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
    [HideInInspector] public float freeGameWinAmount;
    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        if (freeSpinWinTextCoroutine != null)
            StopCoroutine(freeSpinWinTextCoroutine);

        freeGameWinAmount += FruitParadiseSlotMachine.Instance.winAmount;
        freeSpinWinTextCoroutine = StartCoroutine(AnimateToValue(target, duration, textToAnimate));
        //UpdateWinAmount(freeGameWinAmount);
    }

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        PlayWinMusic("Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 0.5f, this.winAmount));
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
            //textToAnimate.text = displayed.ToString("0.00");
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        //textToAnimate.text = target.ToString("0.00");
        textToAnimate.text = FormatFloorValue(target);
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
            //textToAnimateOne.text = displayed.ToString("0.00");
            //textToAnimateTwo.text = displayed.ToString("0.00");
            textToAnimateOne.text = FormatFloorValue(displayed);
            textToAnimateTwo.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);
        // Ensure final value is exact
        //textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");

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

        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;
        if (FruitParadiseSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (FruitParadiseAutoSpinController.isAutoSpinning)
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
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }
    #endregion
}