using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(DayOfDeadBetController))]
[RequireComponent(typeof(DayOfDeadRulesPopupController))]
[RequireComponent(typeof(DayOfDeadAutoSpinController))]
public class DayOfDeadUIManager : GameBetServices
{
    #region Variables

    public static DayOfDeadUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private DayOfDeadUIButtonController spinButton;
    [SerializeField] private DayOfDeadUIButtonController stopButton;
    [SerializeField] private DayOfDeadUIButtonController autoButton;
    [SerializeField] private DayOfDeadUIButtonController autoStopButton;
    //public Animator cannonAnimator;

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

    private DayOfDeadBetController betController;
    private DayOfDeadRulesPopupController rulesPopupController;
    private DayOfDeadAutoSpinController autoSpinController;

    [HideInInspector] public bool singleSpin;
    [HideInInspector] public bool autoSpin;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        betController = GetComponent<DayOfDeadBetController>();
        rulesPopupController = GetComponent<DayOfDeadRulesPopupController>();
        autoSpinController = GetComponent<DayOfDeadAutoSpinController>();

        UpdateCoins();
        SetupInputButtons();
        PlayMusic("BG");
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
    }

    private void OnDestroy()
    {
        RemoveListeners(spinButton?.GetButtonComponent());
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
        spinButton.GetButtonComponent().onClick.AddListener(OnClickSpin);
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
        if (!DayOfDeadSoundManager.Instance.IsSoundMute())
            DayOfDeadSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!DayOfDeadSoundManager.Instance.IsMusicMute())
            DayOfDeadSoundManager.Instance.PlayMusic(soundName);
    }   

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!DayOfDeadSoundManager.Instance.IsMusicMute())
            DayOfDeadSoundManager.Instance.StopMusic(soundName);
    }

    private void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!DayOfDeadSoundManager.Instance.IsMusicMute())
            DayOfDeadSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!DayOfDeadSoundManager.Instance.IsMusicMute())
            DayOfDeadSoundManager.Instance.StopWinMusic(soundName);
    }

    private void SoundActive(bool soundActive)
    {
        DayOfDeadSoundManager.Instance.MuteSFX(!soundActive);

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
        DayOfDeadSoundManager.Instance.MuteMusic(!musicActive);

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
        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (DayOfDeadSlotMachine.Instance.isRespinActive)
            return;
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            //StopWinMusic("Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        //PlaySound("Spin");

        UpdateButtons("Single Start");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        //PlaySound("Button");
        
        DayOfDeadSlotMachine.Instance.isStopBtnPressed = true;
        DayOfDeadSlotMachine.Instance.StopWithResult();

        if (DayOfDeadAutoSpinController.isAutoSpinning || DayOfDeadSlotMachine.Instance.isRespinActive)
        {
            SetStopInteractable(false);
        }
    }

    private void OnClickAuto()
    {
        //PlaySound("Button");
        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;
        if (DayOfDeadSlotMachine.Instance.isRespinActive)
            return;
        autoSpinController.StartAutoSpin(betAmount);
        UpdateButtons("Auto Start");
    }
    private void OnClickAutoStop()
    {
        if (DayOfDeadSlotMachine.Instance.isRespinActive)
            return;
        if (autoSpinController == null) return;
        //PlaySound("Button");
        autoSpinController.CancelAutoSpin();

        autoButton.ShowButton(true);
        autoStopButton.ShowButton(false);
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

    public void UpdateButtons(string type)
    {
        bool inSpin = false;

        switch (type)
        {
            case "Single Start":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.GetButtonComponent().interactable = false;
                break;

            case "Single Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = true;
                break;

            case "Auto Start":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(false);
                autoStopButton.ShowButton(true);
                stopButton.GetButtonComponent().interactable = false;
                break;

            case "Auto Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(true);
                break;

            case "Win Animation":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                spinButton.GetButtonComponent().interactable = false;
                autoButton.ShowButton(true);
                SetAutoInteractable(false);
                break;
            case "Auto Win Animation":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = false;
                SetAutoInteractable(false);
                autoStopButton.ShowButton(true);
                break;
            case "Transition Start":
                inSpin = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(false);
                spinButton.GetButtonComponent().interactable = false;
                break;

            case "Auto Respin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(false);
                autoStopButton.ShowButton(true);
                SetAutoInteractable(false);
                break;

            case "Auto Respin End":
                inSpin = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = true;
                autoButton.ShowButton(false);
                autoStopButton.ShowButton(true);
                SetAutoInteractable(true);
                break;

            case "Free Spin":
                inSpin = true;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(false);
                spinButton.GetButtonComponent().interactable = false;
                break;

            case "Free Spin End":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(true);
                spinButton.GetButtonComponent().interactable = true;
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

        //PlayWinMusic("Win");
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

        //SetAutoInteractable(true);
        //SetSpinInteractable(true);
        if (DayOfDeadSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition Start");
        }
        else if (DayOfDeadAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Auto Win Animation");
        }
        else if (DayOfDeadAutoSpinController.isAutoSpinning && DayOfDeadSlotMachine.Instance.isRespinActive)
        {
            UpdateButtons("Auto ReSpin");
        }
        else
        {
            UpdateButtons("Single Stop");
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

    public void SetAutoInteractable(bool state)
    {
        autoButton.GetButtonComponent().interactable = state;
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
}