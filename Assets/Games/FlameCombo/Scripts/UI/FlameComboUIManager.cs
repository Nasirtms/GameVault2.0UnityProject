using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(FlameComboBetController))]
[RequireComponent(typeof(FlameComboRulesPopupController))]
[RequireComponent(typeof(FlameComboAutoSpinController))]

public class FlameComboUIManager : GameBetServices
{
    #region Variables
    public static FlameComboUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;
    [SerializeField] public TMP_Text winAmount;
    [SerializeField] private float duration = 1.5f;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public FlameComboUIButtonController spinButton;
    [SerializeField] public FlameComboUIButtonController stopButton;

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

    private FlameComboBetController betController;
    private FlameComboRulesPopupController rulesPopupController;
    private FlameComboAutoSpinController autoSpinController;

    [Header("Title Image")]
    [SerializeField] public List<GameObject> titleImages;
    [SerializeField] float delay = 5f;

    private int index = 0;
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
        betController = GetComponent<FlameComboBetController>();
        rulesPopupController = GetComponent<FlameComboRulesPopupController>();
        autoSpinController = GetComponent<FlameComboAutoSpinController>();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);
        UpdateCoins();
        SetupInputButtons();

        StartTitleLoop();
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

        soundControlButton.onClick.AddListener(() => SoundActive(soundOn));
        musicControlButton.onClick.AddListener(() => MusicActive(musicOn));
        openRulesButton.onClick.AddListener(OpenRulesPopup);
    }

    #endregion

    #region Sound
    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsSoundMute())
            FlameComboSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsMusicMute())
            FlameComboSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsMusicMute())
            FlameComboSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsSoundMute())
            FlameComboSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsSoundMute())
            FlameComboSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsSoundMute())
            FlameComboSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!FlameComboSoundManager.Instance.IsSoundMute())
            FlameComboSoundManager.Instance.StopWinMusic(soundName);
    }
    private void SoundActive(bool soundActive)
    {
        FlameComboSoundManager.Instance.MuteSFX(!soundActive);

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
        FlameComboSoundManager.Instance.MuteMusic(!musicActive);

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

    #region Bet && Buttons
    private void IncreaseBetAmount()
    {
        if (betController == null) return;
        //PlaySound("FruitParadise_Increase_Button");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        //PlaySound("FruitParadise_Decrease_Button");
        betController.DecreaseChipValue();
    }

    private void ExitGame()
    {
        //PlaySound("FruitParadise_Button");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManager.LoadScene("Main");
    }
    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        //PlaySound("FruitParadise_Button");
        rulesPopupController.OpenPopup();
    }
    #endregion

    #region Spin Buttons
    public void OnClickSpin()
    {
        if (FlameComboSlotMachine.Instance.InSpin) return;

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        UpdateButtons("Spin");
        //PlaySound("FruitParadise_Spin");

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
    }

    private void OnClickStop()
    {
        //PlaySound("FruitParadise_Button");
        if (FlameComboSlotMachine.Instance.isFreeGame)
        {
            UpdateButtons("Free Spin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        FlameComboSlotMachine.Instance.isStopBtnPressed = true;

        if (FlameComboAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }
        FlameComboSlotMachine.Instance.StopWithResult();
        FlameComboSlotMachine.Instance.InvokeStop();
    }
    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;

        UpdateButtons("Spin");
        //PlaySpinMusic("Spin");

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

    #region Title Images
    public void StartTitleLoop()
    {
        if (titleImages == null || titleImages.Count == 0)
            return;

        CancelInvoke(nameof(SwapTitleImage));

        index = 0;
        ShowOnly(index);

        InvokeRepeating(nameof(SwapTitleImage), delay, delay);
    }
    public void StopTitleLoop()
    {
        CancelInvoke(nameof(SwapTitleImage));
        HideAll();
    }

    private void SwapTitleImage()
    {
        if (titleImages == null || titleImages.Count == 0)
            return;

        index = (index + 1) % titleImages.Count;
        ShowOnly(index);
    }

    private void ShowOnly(int i)
    {
        for (int a = 0; a < titleImages.Count; a++)
            titleImages[a].SetActive(a == i);
    }

    private void HideAll()
    {
        foreach (var img in titleImages)
            img.SetActive(false);
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

            case "WinAnimation":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "AutoWinAnimation":
                inSpin = false;
                SetStopInteractable(inSpin);
                break;
            default:
                return;
        }
        spinButton.GetButtonComponent().interactable = inSpin;

        exitGameButton.interactable = inSpin;
        increaseBetButton.interactable = inSpin;
        decreaseBetButton.interactable = inSpin;
        currentButtonSet = type;
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
    [HideInInspector] public float freeGameWinAmount;
    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        if (freeSpinWinTextCoroutine != null)
            StopCoroutine(freeSpinWinTextCoroutine);

        freeGameWinAmount += FlameComboSlotMachine.Instance.winAmount;
        freeSpinWinTextCoroutine = StartCoroutine(AnimateToValue(target, duration, textToAnimate));
        //UpdateWinAmount(freeGameWinAmount);
    }

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        //PlayWinMusic("Win");
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
            textToAnimate.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimate.text = target.ToString("0.00");
        //StopWinMusic("Win");
        //PlaySound("WinEnd");
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

        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;
        if (FlameComboSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition Start");
        }
        else if (FlameComboAutoSpinController.isAutoSpinning)
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