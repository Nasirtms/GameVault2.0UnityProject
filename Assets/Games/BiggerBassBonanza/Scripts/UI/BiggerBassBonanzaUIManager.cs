using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BiggerBassBonanzaBetController))]
[RequireComponent(typeof(BiggerBassBonanzaRulesPopupController))]

[RequireComponent(typeof(BiggerBassBonanzaAutoSpinController))]
public class BiggerBassBonanzaUIManager : GameBetServices
{
    #region Variables

    public static BiggerBassBonanzaUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;
    [SerializeField] private Button maxBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private BiggerBassBonanzaUIButtonController spinButton;
    [SerializeField] private BiggerBassBonanzaUIButtonController stopButton;
    [SerializeField] private BiggerBassBonanzaUIButtonController autoButton;
    [SerializeField] private BiggerBassBonanzaUIButtonController cancelButton;

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
    public Coroutine winCoroutine;

    private Coroutine freeSpinWinTextCoroutine;
    public Coroutine textAnimationCoroutine;
    private string currentButtonSet;
    private float currentSpinWin;

    private BiggerBassBonanzaBetController betController;
    private BiggerBassBonanzaRulesPopupController rulesPopupController;

    private BiggerBassBonanzaAutoSpinController autoSpinController;

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
        winAmount.text = BiggerBassBonanzaSlotMachine.Instance.winAmount.ToString("0.00");
        betController = GetComponent<BiggerBassBonanzaBetController>();
        rulesPopupController = GetComponent<BiggerBassBonanzaRulesPopupController>();
        autoSpinController = GetComponent<BiggerBassBonanzaAutoSpinController>();


        UpdateCoins();
        SetupInputButtons();
        
        PlayMusic("Background");

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }

    private void OnDestroy()
    {
        RemoveListeners(stopButton?.GetButtonComponent());
        RemoveListeners(autoButton?.GetButtonComponent());
        RemoveListeners(cancelButton?.GetButtonComponent());
        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);
        RemoveListeners(maxBetButton);
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
        spinButton.GetButtonComponent().onClick.AddListener(OnClickSpin);
        stopButton.GetButtonComponent().onClick.AddListener(OnClickStop);
        autoButton.GetButtonComponent().onClick.AddListener(OnClickAuto);
        cancelButton.GetButtonComponent().onClick.AddListener(OnClickCancel);
        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);
        maxBetButton.onClick.AddListener(SetMaxBet);

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
        if (!BiggerBassBonanzaSoundManager.Instance.IsSoundMute())
            BiggerBassBonanzaSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!BiggerBassBonanzaSoundManager.Instance.IsMusicMute())
            BiggerBassBonanzaSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!BiggerBassBonanzaSoundManager.Instance.IsMusicMute())
            BiggerBassBonanzaSoundManager.Instance.StopMusic(soundName);
    }

    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!BiggerBassBonanzaSoundManager.Instance.IsMusicMute())
            BiggerBassBonanzaSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!BiggerBassBonanzaSoundManager.Instance.IsMusicMute())
            BiggerBassBonanzaSoundManager.Instance.StopWinMusic(soundName);
    }
    private void SoundActive(bool soundActive)
    {
        BiggerBassBonanzaSoundManager.Instance.MuteSFX(!soundActive);

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
        BiggerBassBonanzaSoundManager.Instance.MuteMusic(!musicActive);

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
        PlaySound("Button");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Button");
        betController.DecreaseChipValue();
    }

    private void SetMaxBet()
    {
        if (betController == null) return;
        PlaySound("Button");
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
        PlaySound("SpinButton");

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }

        UpdateButtons("Single Spin");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        if (BiggerBassBonanzaSlotMachine.Instance.isStopBtnPressed) return;

        PlaySound("Button");

        BiggerBassBonanzaSlotMachine.Instance.isStopBtnPressed = true;
        BiggerBassBonanzaSlotMachine.Instance.StopWithResult();

        if (BiggerBassBonanzaAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    private void OnClickAuto()
    {
        if (autoSpinController == null) return;
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;

        float betAmount = betController.GetCurrentBet();
        autoSpinController.StartAutoSpin(betAmount);

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }

        //UpdateButtons("Auto Spin");
    }

    private void OnClickCancel()
    {
        if (autoSpinController == null) return;

        autoSpinController.CancelAutoSpin();

        UpdateButtons("Auto Cancel");
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
            case "Default":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                cancelButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = true;
                spinButton.GetButtonComponent().interactable = true;
                break;

            case "Single Spin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                cancelButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = false;
                stopButton.GetButtonComponent().interactable = false;
                break;

            case "Auto Spin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(false);
                cancelButton.ShowButton(true);
                stopButton.GetButtonComponent().interactable = false; 
                break;

            case "Auto Cancel":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                cancelButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = false;
                break;

            case "Game Transition":
                inSpin = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                cancelButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = false;
                spinButton.GetButtonComponent().interactable = false;
                break;

            case "Free Spin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                cancelButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = false;
                break;

            default:
                return;
        }

        exitGameButton.interactable = !inSpin;
        increaseBetButton.interactable = !inSpin;
        decreaseBetButton.interactable = !inSpin;
        maxBetButton.interactable = !inSpin;

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

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        PlayWinMusic("Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount));
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
            //textToAnimate.text = displayed.ToString("0.00");
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }
        textToAnimate.text = FormatFloorValue(target);
        //textToAnimate.text = target.ToString("0.00");
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

        // Ensure final value is exact
        //textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);

        StopCoroutine(textAnimationCoroutine);
    }

    #endregion

    #region Win Animations

    public bool winAnimationCompleted = false;

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
        yield return new WaitUntil(() => BiggerBassBonanzaSlotMachine.Instance.isFishCollectionCompleted);

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

        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText)); // Made this method

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;

        StopCoroutine(winCoroutine);
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

    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }

    public void SetSpinInteractable(bool state)
    {
        spinButton.GetButtonComponent().interactable = state;
    }

    public void SetAutoInteractable(bool state)
    {
        autoButton.GetButtonComponent().interactable = state;
    }

    #endregion
}
