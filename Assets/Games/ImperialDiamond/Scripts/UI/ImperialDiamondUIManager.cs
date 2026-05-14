using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(ImperialDiamondBetController))]
[RequireComponent(typeof(ImperialDiamondRulesPopupController))]
[RequireComponent(typeof(ImperialDiamondAutoSpinController))]
public class ImperialDiamondUIManager : GameBetServices
{
    #region Variables

    public static ImperialDiamondUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] public TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public ImperialDiamondUIButtonController spinButton;
    [SerializeField] private ImperialDiamondUIButtonController stopButton;

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

    [SerializeField] private Button paylineImageButton;
    [SerializeField] private Image paylineImage;
    public bool isPaylineVisible = false;

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
    private ImperialDiamondBetController betController;
    private ImperialDiamondRulesPopupController rulesPopupController;
    private ImperialDiamondAutoSpinController autoSpinController;

    private float currentSpinWin;

    public string currentButtonSet;

    public string currentUIStateType;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        betController = GetComponent<ImperialDiamondBetController>();
        rulesPopupController = GetComponent<ImperialDiamondRulesPopupController>();
        autoSpinController = GetComponent<ImperialDiamondAutoSpinController>();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);

        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;

        PlayMusic("BG");
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
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
        RemoveListeners(paylineImageButton);
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
        spinButton.GetButtonComponent().onClick.AddListener(OnClickSpin);
        stopButton.GetButtonComponent().onClick.AddListener(OnClickStop);

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);

        exitGameButton.onClick.AddListener(ExitGame);
        openRulesButton.onClick.AddListener(OpenRulesPopup);
        soundButton.onClick.AddListener(() => SoundActive(soundOn));
        musicButton.onClick.AddListener(() => MusicActive(musicOn));
        paylineImageButton.onClick.AddListener(TogglePayline);
    }

    #endregion

    #region Input Buttons

    #region Sound

    public void PlaySound(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsSoundMute())
            ImperialDiamondSoundManager.Instance.PlaySFX(soundName);
    }
    public void StopCurrentSFX()
    {
        if (!ImperialDiamondSoundManager.Instance.IsSoundMute())
            ImperialDiamondSoundManager.Instance.StopSFX();
    }
    public void PlayMusic(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsMusicMute())
            ImperialDiamondSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsMusicMute())
            ImperialDiamondSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsSoundMute())
            ImperialDiamondSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsSoundMute())
            ImperialDiamondSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsSoundMute())
            ImperialDiamondSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (soundName == null || ImperialDiamondSoundManager.Instance == null) return;
        if (!ImperialDiamondSoundManager.Instance.IsSoundMute())
            ImperialDiamondSoundManager.Instance.StopWinMusic(soundName);
    }
    public void SoundActive(bool soundActive)
    {
        ImperialDiamondSoundManager.Instance.MuteSFX(!soundActive);

        Image soundButtonImage = soundButton.transform.GetComponent<Image>();

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
        ImperialDiamondSoundManager.Instance.MuteMusic(!musicActive);

        Image musicButtonImage = musicButton.transform.GetComponent<Image>();

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
        PlaySound("SpinButton");

        StopCurrentSFX();
        if (ImperialDiamondSlotMachine.Instance.isFreeGame) return;
        float betAmount = betController.GetCurrentBet();

        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            //StopWinMusic("Win");
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
        PlaySound("StopButton");
        UpdateButtons("Stop");
        StopSpinMusic("Spin");
        ImperialDiamondSlotMachine.Instance.isStopBtnPressed = true;

        if (ImperialDiamondAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }

        ImperialDiamondSlotMachine.Instance.StopWithResult();
    }

    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;

        StopCurrentSFX();
        UpdateButtons("Spin");
        PlaySound("SpinButton");

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
    }

    #endregion

    #region UI Update
    private void TogglePayline()
    {
        var c = paylineImage.color;

        if (isPaylineVisible)
            c.a = 0f;  
        else
            c.a = 1f;

        paylineImage.color = c;
        isPaylineVisible = !isPaylineVisible;
    }
    public void UpdateCoins()
    {
        if (UserManager.Instance != null)
        {
            coins.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }
    }

    public void UpdateButtons(string type)
    {
        bool interactable = false;
        currentUIStateType = type;
        switch (type)
        {
            case "Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;

            case "Stop":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            case "Transition":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            case "enterfreeSpin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;
            case "exitfreeSpin":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            default:
                return;
        }

        spinButton.GetButtonComponent().interactable = interactable;

        paylineImageButton.interactable = interactable;
        exitGameButton.interactable = interactable;
        increaseBetButton.interactable = interactable;
        decreaseBetButton.interactable = interactable;
    }

    #endregion

    #region Text Animation
    private string FormatFloorValue(float value)
    {
        float floored = Mathf.Floor(value * 100f) / 100f;
        return floored.ToString("0.00");
    }
    public void UpdateWinAmount(float winAmount, bool compound)
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

        //PlayWinMusic("Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount));
    }

    public void PlayWinAnimationText(float target, float duration, TMP_Text textToAnimate)
    {
        StartCoroutine(AnimateToValue(target, duration, textToAnimate));
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
        //StopWinMusic("Win");
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
        if (ImperialDiamondSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (ImperialDiamondAutoSpinController.isAutoSpinning)
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

    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }
    #endregion
}
