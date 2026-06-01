
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(UltimateFireLinkOlveraStreetBetController))]
[RequireComponent(typeof(UltimateFireLinkOlveraStreetRulesPopupController))]
[RequireComponent(typeof(UltimateFireLinkOlveraStreetAutoSpinController))]
public class UltimateFireLinkOlveraStreetUIManager : GameBetServices
{
    #region Variables

    public static UltimateFireLinkOlveraStreetUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;


    [Header("Spin Buttons")]
    [SerializeField] public UltimateFireLinkOlveraStreetUIButtonController spinButton;
    [SerializeField] public UltimateFireLinkOlveraStreetUIButtonController stopButton;
    [SerializeField] public UltimateFireLinkOlveraStreetUIButtonController autoButton;
    [SerializeField] public UltimateFireLinkOlveraStreetUIButtonController autoStopButton;


    [Header("Menu Buttons")]
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button openRulesButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;

    [Header("Sound and Music")]
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    private bool soundOn;
    private bool musicOn;

    private UltimateFireLinkOlveraStreetBetController betController;
    private UltimateFireLinkOlveraStreetRulesPopupController rulesPopupController;
    private UltimateFireLinkOlveraStreetAutoSpinController autoSpinController;

    [HideInInspector] public bool singleSpin;
    [HideInInspector] public bool autoSpin;
    public Coroutine textAnimationCoroutine;

    private Coroutine freeSpinWinTextCoroutine;
    private string currentButtonSet;

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

    private bool autoStopPressed;

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
        betController = GetComponent<UltimateFireLinkOlveraStreetBetController>();
        rulesPopupController = GetComponent<UltimateFireLinkOlveraStreetRulesPopupController>();
        autoSpinController = GetComponent<UltimateFireLinkOlveraStreetAutoSpinController>();
        autoStopButton.gameObject.SetActive(false);


        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("BG");
    }

    private void OnDestroy()
    {
        //RemoveListeners(spinButton?.GetButtonComponent());
        RemoveListeners(stopButton?.GetButtonComponent());
        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);

        RemoveListeners(exitGameButton);
        RemoveListeners(openRulesButton);
        RemoveListeners(soundButton);
        RemoveListeners(musicButton);
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;
    }


    #endregion

    #region Buttons Setup
    private void RemoveListeners(Button button)
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }
    private void SetupInputButtons()
    {
        stopButton.GetButtonComponent().onClick.AddListener(OnClickStop);
        autoButton.GetButtonComponent().onClick.AddListener(OnClickAuto);
        autoStopButton.GetButtonComponent().onClick.AddListener(OnClickAutoStop);

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);

        exitGameButton.onClick.AddListener(ExitGame);
        openRulesButton.onClick.AddListener(OpenRulesPopup);
        soundButton.onClick.AddListener(() => SoundActive(soundOn));
        musicButton.onClick.AddListener(() => MusicActive(musicOn));
    }
    #endregion

    #region Input Buttons

    #region Sound

    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!UltimateFireLinkOlveraStreetSoundManager.Instance.IsSoundMute())
            UltimateFireLinkOlveraStreetSoundManager.Instance.PlaySFX(soundName);
    }
    public void StopCurrentSFX()
    {
        if (!UltimateFireLinkOlveraStreetSoundManager.Instance.IsSoundMute())
            UltimateFireLinkOlveraStreetSoundManager.Instance.StopSFX();
    }
    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!UltimateFireLinkOlveraStreetSoundManager.Instance.IsMusicMute())
            UltimateFireLinkOlveraStreetSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!UltimateFireLinkOlveraStreetSoundManager.Instance.IsMusicMute())
            UltimateFireLinkOlveraStreetSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!UltimateFireLinkOlveraStreetSoundManager.Instance.IsSoundMute())
            UltimateFireLinkOlveraStreetSoundManager.Instance.SpinPlayMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!UltimateFireLinkOlveraStreetSoundManager.Instance.IsSoundMute())
            UltimateFireLinkOlveraStreetSoundManager.Instance.SpinStopMusic(soundName);
    }
    //private void PlayWinMusic(string soundName)
    //{
    //    if (string.IsNullOrEmpty(soundName)) return;
    //    if (!SaharaRichesSoundManager.Instance.IsSoundMute())
    //        SaharaRichesSoundManager.Instance.PlayWinText(soundName);
    //}

    //private void StopWinMusic(string soundName)
    //{
    //    if (string.IsNullOrEmpty(soundName)) return;
    //    if (!SaharaRichesSoundManager.Instance.IsSoundMute())
    //        SaharaRichesSoundManager.Instance.StopWinText(soundName);
    //}


    private void SoundActive(bool soundActive)
    {
        UltimateFireLinkOlveraStreetSoundManager.Instance.MuteSFX(!soundActive);

        Image soundButtonImage = soundButton.GetComponent<Image>();

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
        UltimateFireLinkOlveraStreetSoundManager.Instance.MuteMusic(!musicActive);
            
        Image musicButtonImage = musicButton.GetComponent<Image>();

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

    #region Bet Control
    private void IncreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Bet");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Bet");
        betController.DecreaseChipValue();
    }
    private void SetMaxBet()
    {
        if (betController == null) return;

        PlaySound("Bet");
        betController.SetMaxBet();
    }
    #endregion

    private void ExitGame()
    {
        PlaySound("Button");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
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
        UpdateButtons("Spin");

        StopCurrentSFX();
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
        PlaySound("Button");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        PlaySound("Button");
        StopSpinMusic("Spin");
        UltimateFireLinkOlveraStreetSlotMachine.Instance.isStopBtnPressed = true;
        UltimateFireLinkOlveraStreetSlotMachine.Instance.StopWithResult();
    }

    private void OnClickAuto()
    {
        if (autoSpinController == null) return;
        autoButton.setInteractable(!UltimateFireLinkOlveraStreetSlotMachine.Instance.InSpin);

        PlaySound("Auto_Button");

        autoStopPressed = false;

        float betAmount = betController.GetCurrentBet();

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            //StopWinMusic("Win");
        }
        if (winCoroutine != null)
            StopCoroutine(winCoroutine);

        PlaySpinMusic("Spin");
        UpdateButtons("AutoSpin");
        autoSpinController.StartAutoSpin(betAmount);

    }


    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        autoStopPressed = true;
        PlaySound("AutoButton_Stop");
        StopSpinMusic("Spin");
        autoSpinController.CancelAutoSpin();

        autoStopButton.GetButtonComponent().interactable = false;
        if (!UltimateFireLinkOlveraStreetSlotMachine.Instance.InSpin)
        {
            UpdateButtons("Idle");
        }
    }

    public void SetAutoInteractable(bool state)
    {
        autoButton.setInteractable(state);
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
        currentButtonSet = type;

        // Defaults
        bool showSpin = false;
        bool spinInteractable = false;

        bool showStop = false;
        bool stopInteractable = false;

        bool showAuto = true;
        bool autoInteractable = false;

        bool showAutoStop = false;
        bool autoStopInteractable = false;

        bool betInteractable = false;
        bool exitInteractable = false;
        bool showRemainingSpins = false;

        switch (type)
        {
            case "Idle":
                showSpin = true;
                spinInteractable = true;

                showStop = false;

                showAuto = true;
                autoInteractable = true;

                showAutoStop = false;

                betInteractable = true;
                exitInteractable = true;
                showRemainingSpins = false;

                autoStopPressed = false;
                break;

            case "Spin":
                showSpin = false;
                spinInteractable = false;

                showStop = true;
                stopInteractable = false; // becomes true once result arrives

                showAuto = true;
                autoInteractable = false;

                showAutoStop = false;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = false;
                break;

            case "AutoSpin":
                showSpin = false;
                spinInteractable = false;

                showStop = true;
                stopInteractable = true;

                showAuto = false;
                autoInteractable = false;

                showAutoStop = true;
                autoStopInteractable = true;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = true;

                autoStopPressed = false;
                break;

            case "FreeSpin":
                showSpin = false;
                spinInteractable = false;

                showStop = true;
                stopInteractable = true;

                showAuto = true;
                autoInteractable = false;

                showAutoStop = false;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = false;
                break;

            case "Transition":
            case "WinAnimation":
            case "BonusLock":
                showSpin = true;
                spinInteractable = false;

                showStop = false;
                stopInteractable = false;

                showAuto = true;
                autoInteractable = false;

                showAutoStop = false;
                autoStopInteractable = false;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = false;
                break;

            default:
                Debug.LogWarning("Unknown button state: " + type);
                return;
        }

        spinButton.ShowButton(showSpin);
        spinButton.setInteractable(spinInteractable);

        stopButton.ShowButton(showStop);
        stopButton.setInteractable(stopInteractable);

        autoButton.ShowButton(showAuto);
        autoButton.setInteractable(autoInteractable);

        autoStopButton.ShowButton(showAutoStop);
        autoStopButton.setInteractable(autoStopInteractable);

        increaseBetButton.interactable = betInteractable;
        decreaseBetButton.interactable = betInteractable;
        exitGameButton.interactable = exitInteractable;
    }
    #endregion

    #region Text Animation
    private string FormatFloorValue(float value)
    {
        float floored = Mathf.Floor(value * 100f) / 100f;
        return floored.ToString("0.00");
    }
    private float currentSpinWin;

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

        //PlaySound("Win");
        textAnimationCoroutine = StartCoroutine(AnimateAmountFromZeroToTarget(winAmount, 1f, this.winAmount));
    }

    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        freeSpinWinTextCoroutine = StartCoroutine(AnimateAmountFromZeroToTarget(target, duration, textToAnimate));
    }

    public IEnumerator AnimateAmountFromZeroToTarget(float target, float duration, TMP_Text textToAnimate)
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
        textToAnimate.text = FormatFloorValue(target);
        //textToAnimate.text = target.ToString("0.00");
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

        // Ensure final value is exact
        //textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);
        StopCoroutine(textAnimationCoroutine);
    }

    private IEnumerator AnimateToValueTo(float target, float duration, TMP_Text textToAnimateOne)
    {
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(0f, target, t);
            textToAnimateOne.text = displayed.ToString("0.00");
            //textToAnimateTwo.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final value is exact
        textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");

        StopCoroutine(textAnimationCoroutine);
    }
    #endregion

    #region Helper Functions

    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }

    public string CurrentButtonSet()
    {
        return currentButtonSet;
    }

    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }

    public void SetSpinInteractable(bool state)
    {
        spinButton.GetButtonComponent().interactable = state;
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
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
        //textAnimationCoroutine = StartCoroutine(AnimateToValueTo(targetAmount, 2f, this.winAmount));
        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;
        if (UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (UltimateFireLinkOlveraStreetAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("AutoSpin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        StopCoroutine(winCoroutine);
    }

    #endregion
}
