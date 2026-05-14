using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(DoubleJackpotBullseyeBetController))]
[RequireComponent(typeof(DoubleJackpotBullseyeRulesPopupController))]
[RequireComponent(typeof(DoubleJackpotBullseyeAutoSpinController))]
public class DoubleJackpotBullseyeUIManager : GameBetServices
{
    #region Variables

    public static DoubleJackpotBullseyeUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public DoubleJackpotBullseyeUIButtonController spinButton;
    [SerializeField] private DoubleJackpotBullseyeUIButtonController stopButton;

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

    [Header("Win")]
    [SerializeField] public TMP_Text winAmount;

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
    [HideInInspector] public bool winAnimationCompleted = false;
    public Coroutine textAnimationCoroutine;

    // Scripts
    private DoubleJackpotBullseyeBetController betController;
    private DoubleJackpotBullseyeRulesPopupController rulesPopupController;
    private DoubleJackpotBullseyeAutoSpinController autoSpinController;

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
        betController = GetComponent<DoubleJackpotBullseyeBetController>();
        rulesPopupController = GetComponent<DoubleJackpotBullseyeRulesPopupController>();
        autoSpinController = GetComponent<DoubleJackpotBullseyeAutoSpinController>();

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
        RemoveListeners(spinButton.GetButtonComponent());
        RemoveListeners(stopButton.GetButtonComponent());

        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);

        RemoveListeners(exitGameButton);
        RemoveListeners(openRulesButton);
        RemoveListeners(soundButton);
        RemoveListeners(musicButton);
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
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsSoundMute())
            DoubleJackpotBullseyeSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsMusicMute())
            DoubleJackpotBullseyeSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsMusicMute())
            DoubleJackpotBullseyeSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsSoundMute())
            DoubleJackpotBullseyeSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsSoundMute())
            DoubleJackpotBullseyeSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsSoundMute())
            DoubleJackpotBullseyeSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (soundName == null || DoubleJackpotBullseyeSoundManager.Instance == null) return;
        if (!DoubleJackpotBullseyeSoundManager.Instance.IsSoundMute())
            DoubleJackpotBullseyeSoundManager.Instance.StopWinMusic(soundName);
    }
    public void SoundActive(bool soundActive)
    {
        DoubleJackpotBullseyeSoundManager.Instance.MuteSFX(!soundActive);

        Image soundButtonImage = soundButton.transform.GetChild(0).GetComponent<Image>();

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
        DoubleJackpotBullseyeSoundManager.Instance.MuteMusic(!musicActive);

        Image musicButtonImage = musicButton.transform.GetChild(0).GetComponent<Image>();

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
        //PlaySpinMusic("Spin");
        if(DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame) return;
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
        SlotSpinService.Instance.Spin(betAmount);
        UpdateButtons("Spin");
    }

    private void OnClickStop()
    {
        PlaySound("Stop");

        if (DoubleJackpotBullseyeSlotMachine.Instance.isFreeGame)
        {
            UpdateButtons("enterfreeSpin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        DoubleJackpotBullseyeSlotMachine.Instance.isStopBtnPressed = true;
        DoubleJackpotBullseyeSlotMachine.Instance.StopWithResult();
        
        if (DoubleJackpotBullseyeAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }
    }

    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;

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
            case "Transition":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            case "enterfreeSpin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;
            case "exitfreeSpin":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            default:
                return;
        }
        spinButton.GetButtonComponent().interactable = !inSpin;

        exitGameButton.interactable = !inSpin;
        increaseBetButton.interactable = !inSpin;
        decreaseBetButton.interactable = !inSpin;

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
            //textToAnimate.text = displayed.ToString("0.00");
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        //textToAnimate.text = target.ToString("0.00");
        textToAnimate.text = FormatFloorValue(target);
        StopWinMusic("Win");
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

    public IEnumerator AnimateValue(float startValue, float endValue, float duration, TMP_Text textToAnimate)
    {
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(startValue, endValue, t);
            textToAnimate.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        // Make sure it ends exactly at endValue
        textToAnimate.text = endValue.ToString("0.00");
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
        if (DoubleJackpotBullseyeSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (DoubleJackpotBullseyeAutoSpinController.isAutoSpinning)
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

    public bool IsAnyWinAnimationPlaying()
    {
        return (winAnimations != null && winAnimations.activeSelf)
               || winCoroutine != null
               || textAnimationCoroutine != null;
    }

    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    #endregion
}
