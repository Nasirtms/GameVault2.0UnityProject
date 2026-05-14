using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AtomicMeltdownBetController))]
[RequireComponent(typeof(AtomicMeltdownRulesPopupController))]
[RequireComponent(typeof(AtomicMeltdownAutoSpinController))]
public class AtomicMeltdownUIManager : GameBetServices
{
    #region Variables

    public static AtomicMeltdownUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    public TMP_Text coins;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private AtomicMeltdownUIButtonController spinButton;
    [SerializeField] private AtomicMeltdownUIButtonController stopButton;
    [SerializeField] private Animator LeverAnimator;

    [Header("Quick Spin Buttons")]
    [SerializeField] private Button quickSpinButtonOff;
    [SerializeField] private Button quickSpinButtonOn;

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
    [SerializeField] private TMP_Text winAmount;

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
    private float currentSpinWin;

    // Scripts
    private AtomicMeltdownBetController betController;
    private AtomicMeltdownRulesPopupController rulesPopupController;
    private AtomicMeltdownAutoSpinController autoSpinController;

    private string currentButtonSet;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        //if (Instance != null) return;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        winAmount.text = "0.00";
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
        betController = GetComponent<AtomicMeltdownBetController>();
        rulesPopupController = GetComponent<AtomicMeltdownRulesPopupController>();
        autoSpinController = GetComponent<AtomicMeltdownAutoSpinController>();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);

        QuickSpinToggle(false);
        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;

        PlayMusic("BG");
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

        quickSpinButtonOff.onClick.AddListener(() => QuickSpinToggle(true));
        quickSpinButtonOn.onClick.AddListener(() => QuickSpinToggle(false));

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
        if (soundName == null || AtomicMeltdownSoundManager.Instance == null) return;
        if (!AtomicMeltdownSoundManager.Instance.IsSoundMute())
            AtomicMeltdownSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (soundName == null || AtomicMeltdownSoundManager.Instance == null) return;
        if (!AtomicMeltdownSoundManager.Instance.IsMusicMute())
            AtomicMeltdownSoundManager.Instance.PlayMusic(soundName);
    }
    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AtomicMeltdownSoundManager.Instance.IsMusicMute())
            AtomicMeltdownSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AtomicMeltdownSoundManager.Instance.IsSoundMute())
            AtomicMeltdownSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AtomicMeltdownSoundManager.Instance.IsSoundMute())
            AtomicMeltdownSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AtomicMeltdownSoundManager.Instance.IsSoundMute())
            AtomicMeltdownSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AtomicMeltdownSoundManager.Instance.IsSoundMute())
            AtomicMeltdownSoundManager.Instance.StopWinMusic(soundName);
    }
    public void SoundActive(bool soundActive)
    {
        AtomicMeltdownSoundManager.Instance.MuteSFX(!soundActive);

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
        AtomicMeltdownSoundManager.Instance.MuteMusic(!musicActive);

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

    #endregion

    private void QuickSpinToggle(bool state)
    {
        quickSpinButtonOff.gameObject.SetActive(!state);
        quickSpinButtonOn.gameObject.SetActive(state);

        if (state)
        {
            AtomicMeltdownSlotMachine.Instance.settings.spinSettings.endSpin = AtomicMeltdownSpinType.All;
            AtomicMeltdownSlotMachine.Instance.settings.spinSettings.acceleration = new Vector2(1.25f, 1.5f);
        }
        else
        {
            AtomicMeltdownSlotMachine.Instance.settings.spinSettings.endSpin = AtomicMeltdownSpinType.Single;
            AtomicMeltdownSlotMachine.Instance.settings.spinSettings.acceleration = new Vector2(0.7f, 0.9f);
        }
    }

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
        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        UpdateButtons("Spin");
        PlaySound("SpinStart");
        PlaySpinMusic("Spin");
        LeverAnimator.SetTrigger("Play");

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        if (AtomicMeltdownSlotMachine.Instance.isFreeGame)
        {
            UpdateButtons("FreeSpin");
        }
        else
        {
            UpdateButtons("Default");
        }
        PlaySound("SpinStop");
        StopSpinMusic("Spin");

        AtomicMeltdownSlotMachine.Instance.isStopBtnPressed = true;

        if (AtomicMeltdownAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }

        AtomicMeltdownSlotMachine.Instance.StopWithResult();
    }

    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;

        UpdateButtons("Spin");
        PlaySound("SpinStart");
        PlaySpinMusic("Spin");

        float betAmount = betController.GetCurrentBet();

        LeverAnimator.SetTrigger("Play");

        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);
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
        //Debug.Log("Nasir 2 UpdateCoins()");
        if (UserManager.Instance != null)
        {
            coins.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }
    }
    
    public void UpdateButtons(string type)
    {
        //Debug.Log("Nasir 1 UpdateButtons()");
        bool interactable = false;

        switch (type)
        {
            case "Default":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;

            case "Transition":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "FreeSpin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;

            case "WinAnimation":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "AutoWinAnimation":
                interactable = false;
                SetStopInteractable(interactable);
                break;

            default:
                return;
        }

        spinButton.GetButtonComponent().interactable = interactable;

        exitGameButton.interactable = interactable;
        increaseBetButton.interactable = interactable;
        decreaseBetButton.interactable = interactable;

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

        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 0.5f, this.winAmount));
    }

    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimate)
    {
        float startValue = 0f;

        if (!string.IsNullOrEmpty(textToAnimate.text) && float.TryParse(textToAnimate.text, out float current))
        {
            startValue = current;
        }
        //Debug.Log($"start value, {startValue:0.00}");
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(startValue, target, t);

            textToAnimate.text = FormatFloorValue(displayed);
            //textToAnimate.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }
        //Debug.Log($"animate to target value, {target:0.00}");
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
        if (AtomicMeltdownAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("AutoWinAnimation");
        }
        else
        {
            UpdateButtons("WinAnimation");
        }

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

        if (AtomicMeltdownSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (AtomicMeltdownAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Spin");
        }
        else
        {
            UpdateButtons("Default");
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
