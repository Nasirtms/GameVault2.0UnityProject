using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(RichLittlePiggiesBetController))]
[RequireComponent(typeof(RichLittlePiggiesRulesPopupController))]
[RequireComponent(typeof(RichLittlePiggiesAutoSpinController))]
public class RichLittlePiggiesUIManager : GameBetServices
{
    #region Variables

    public static RichLittlePiggiesUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private RichLittlePiggiesUIButtonController spinButton;
    [SerializeField] private RichLittlePiggiesUIButtonController stopButton;
    [SerializeField] private RichLittlePiggiesUIButtonController autoButton;
    [SerializeField] private RichLittlePiggiesUIButtonController autoStopButton;

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

    [Header("Free Game")]
    [SerializeField] TMP_Text freeGameWinAmountText;
    [HideInInspector] public int freeGameSpinCount;
    [HideInInspector] public float freeGameWinAmount;

    [Header("Free Win Indicator")]
    [SerializeField] private GameObject freeWinIndicatorFrame;
    [SerializeField] private GameObject freeWinIndicatorText;
    private Coroutine freeWinIndicator;
    private bool textIndicatorActive = false;

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
    [SerializeField] private GameObject bluePiggy;
    [SerializeField] private GameObject yellowPiggy;
    [SerializeField] private GameObject redPiggy;
    public Coroutine winCoroutine;
    [HideInInspector] public bool winAnimationCompleted = false;
    public Coroutine textAnimationCoroutine;

    private RichLittlePiggiesBetController betController;
    private RichLittlePiggiesRulesPopupController rulesPopupController;
    private RichLittlePiggiesAutoSpinController autoSpinController;
    private RichLittlePiggiesGameTransitionController gameTransitionController;

    [HideInInspector] public bool autoSpin;
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
        betController = GetComponent<RichLittlePiggiesBetController>();
        rulesPopupController = GetComponent<RichLittlePiggiesRulesPopupController>();
        autoSpinController = GetComponent<RichLittlePiggiesAutoSpinController>();
        gameTransitionController = GetComponent<RichLittlePiggiesGameTransitionController>();

        freeGameSpinCount = 0;
        PlayPiggyAnimation();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);

        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("Background");
    }

    private void OnDestroy()
    {
        //spinButton?.GetButtonComponent().onClick.RemoveAllListeners();
        RemoveListeners(stopButton?.GetButtonComponent());
        RemoveListeners(autoButton?.GetButtonComponent());
        RemoveListeners(autoStopButton?.GetButtonComponent());

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
        if (!RichLittlePiggiesSoundManager.Instance.IsSoundMute())
            RichLittlePiggiesSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!RichLittlePiggiesSoundManager.Instance.IsMusicMute())
            RichLittlePiggiesSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!RichLittlePiggiesSoundManager.Instance.IsMusicMute())
            RichLittlePiggiesSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!RichLittlePiggiesSoundManager.Instance.IsSoundMute())
            RichLittlePiggiesSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!RichLittlePiggiesSoundManager.Instance.IsSoundMute())
            RichLittlePiggiesSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!RichLittlePiggiesSoundManager.Instance.IsSoundMute())
            RichLittlePiggiesSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!RichLittlePiggiesSoundManager.Instance.IsSoundMute())
            RichLittlePiggiesSoundManager.Instance.StopWinMusic(soundName);
    }
    public void SoundActive(bool soundActive)
    {
        RichLittlePiggiesSoundManager.Instance.MuteSFX(!soundActive);
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

    public void MusicActive(bool musicActive)
    {
        RichLittlePiggiesSoundManager.Instance.MuteMusic(!musicActive);

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
        PlaySound("Increase");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Decrease");
        betController.DecreaseChipValue();
    }

    #endregion

    private void ExitGame()
    {
        PlaySound("Rules_Close");
        if(UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
    }

    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        PlaySound("Rules_Popup");
        rulesPopupController.OpenPopup();
    }

    #endregion
    
    #region Spin Buttons

    public void OnClickSpin()
    {
        if (RichLittlePiggiesSlotMachine.Instance.isFreeGameReady)
        {
            return;
        }
        PlaySound("Spin_Button");
        PlaySpinMusic("Spin");


        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
        }
        if (winCoroutine != null)
            StopCoroutine(winCoroutine);

        SlotSpinService.Instance.Spin(betAmount);
        UpdateButtons("Single Start");
    }

    private void OnClickStop()
    {
        PlaySound("Spin_Button");
        StopSpinMusic("Spin");
        RichLittlePiggiesSlotMachine.Instance.isStopBtnPressed = true;
        RichLittlePiggiesSlotMachine.Instance.StopWithResult();

        if (RichLittlePiggiesAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    private void OnClickAuto()
    {
        if (autoSpinController == null) return;
        if(RichLittlePiggiesSlotMachine.Instance.InSpin) return;

        PlaySound("Auto_Button");
        float betAmount = CurrentBet();
        autoSpinController.StartAutoSpin(betAmount);
    }


    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        PlaySound("AutoButton_Stop");
        StopSpinMusic("Spin");

        autoSpinController.CancelAutoSpin();


        autoStopButton.ShowButton(false);
        autoButton.ShowButton(true);
        SetAutoInteractable(false);
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

    public void UpdateRemainingSpins(int remainingSpins)
    {
        if (remainingSpins == -1)
        {
            //this.remainingSpins.text = "<sprite name=Infinity>";
            return;
        }

        //this.remainingSpins.text = remainingSpins.ToString();
    }

    public void UpdateButtons(string type)
    {
        bool inSpin = false;

        switch (type)
        {
            case "Single Start":
                inSpin = true;
                spinButton.ShowButton(false);
                //stopButton.ShowButton(true);
                autoButton.SetButtonInteractable(false);
                break;

            case "Single Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                //stopButton.ShowButton(false);
                autoButton.SetButtonInteractable(true);
                break;

            case "Auto Start":
                inSpin = true;
                spinButton.ShowButton(false);
                //stopButton.ShowButton(true);
                autoButton.ShowButton(false);
                autoStopButton.ShowButton(true);
                break;

            case "Auto Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                //stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(true);
                break;

            case "FreeSpin":
                inSpin = true;
                autoButton.SetButtonInteractable(false);
                spinButton.gameObject.SetActive(false);
                stopButton.GetButtonComponent().interactable = false;
                break;

            case "FreeSpinEnd":
                inSpin = false;
                autoButton.SetButtonInteractable(true);
                spinButton.gameObject.SetActive(true);
                stopButton.GetButtonComponent().interactable = false;
                break;
            default:
                return;
        }

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

    #endregion

    #region Text Animation
    private string FormatFloorValue(float value)
    {
        float floored = Mathf.Floor(value * 100f) / 100f;
        return floored.ToString("0.00");
    }
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

    public void PlayFreeGameWinAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        freeGameWinAmount += winAmount;
        textAnimationCoroutine = StartCoroutine(AnimateToValue(freeGameWinAmount, 1f, freeGameWinAmountText));
    }

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        PlayWinMusic("Win");
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
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

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
            textToAnimateOne.text = FormatFloorValue(displayed);
            textToAnimateTwo.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        // Ensure final value is exact
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);

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
        if (RichLittlePiggiesSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Free Spin");
        }
        else if (RichLittlePiggiesAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Auto Start");
        }
        else
        {
            UpdateButtons("Single Stop");
        }
        StopCoroutine(winCoroutine);
    }

    #endregion

    #region Free Spin

    public void ShowFreeSpinIndicator()
    {
        if (freeWinIndicator != null)
            StopCoroutine(freeWinIndicator);

        if (freeWinIndicatorFrame != null)
            freeWinIndicatorFrame.SetActive(true);

        freeWinIndicator = StartCoroutine(BlinkTextIndicator());
    }

    public void HideFreeSpinIndicator()
    {
        StopCoroutine(freeWinIndicator);

        if (freeWinIndicatorFrame != null)
            freeWinIndicatorFrame.SetActive(false);
    }

    public void ExtraFreeSpin()
    {
        //gameTransitionController.UpdateFreeSpins(freeGameSpinCount);
    }

    private IEnumerator BlinkTextIndicator()
    {
        while (true)
        {
            freeWinIndicatorText.SetActive(textIndicatorActive);

            yield return new WaitForSeconds(0.5f);

            textIndicatorActive = !textIndicatorActive;
        }
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


    public bool GetStopInteractable()
    {
        Debug.Log("Stop Button Active: " + stopButton.gameObject.activeSelf);
        bool state = stopButton.GetButtonComponent().interactable;
        Debug.Log("Stop Button Interactable: " + state);
        return state;
    }

    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }

    public bool GetAutoInteractable()
    {
        return autoButton.GetButtonComponent().interactable;
    }

    public void SetAutoInteractable(bool state)
    {
        autoButton.SetButtonInteractable(state);
    }

    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    private void PlayPiggyAnimation()
    {
        redPiggy.GetComponent<Animator>().SetBool("idle", true);
        bluePiggy.GetComponent<Animator>().SetBool("idle", true);
        yellowPiggy.GetComponent<Animator>().SetBool("idle", true);
    }
    #endregion
}
