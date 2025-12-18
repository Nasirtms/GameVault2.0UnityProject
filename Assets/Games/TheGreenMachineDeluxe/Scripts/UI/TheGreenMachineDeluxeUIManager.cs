using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(TheGreenMachineDeluxeBetController))]
[RequireComponent(typeof(TheGreenMachineDeluxeRulesPopupController))]
[RequireComponent(typeof(TheGreenMachineDeluxeAutoSpinController))]
public class TheGreenMachineDeluxeUIManager : GameBetServices
{
    #region Variables

    public static TheGreenMachineDeluxeUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private TheGreenMachineDeluxeUIButtonController spinButton;
    [SerializeField] private TheGreenMachineDeluxeUIButtonController stopButton;
    [SerializeField] private TheGreenMachineDeluxeUIButtonController autoButton;
    [SerializeField] private TheGreenMachineDeluxeUIButtonController autoStopButton;

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
    //[SerializeField] private TMP_Text niceWinText;
    //[SerializeField] private TMP_Text bigWinText;
    //[SerializeField] private TMP_Text megaWinText;
    //[SerializeField] private TMP_Text superWinText;
    //[SerializeField] private TMP_Text jackpotWinText;
    [SerializeField] private string niceWinTrigger;
    [SerializeField] private string bigWinTrigger;
    [SerializeField] private string megaWinTrigger;
    [SerializeField] private string superWinTrigger;
    [SerializeField] private string jackpotWinTrigger;
    public bool winAnimationCompleted = false;
    public Coroutine winCoroutine;

    [Header("Free Game")]
    [SerializeField] TMP_Text freeGameWinAmountText;
    [HideInInspector] public int freeGameSpinCount;

    private Coroutine freeSpinWinTextCoroutine;
    public Coroutine textAnimationCoroutine;
    private string currentButtonSet;

    private TheGreenMachineDeluxeBetController betController;
    private TheGreenMachineDeluxeRulesPopupController rulesPopupController;
    private TheGreenMachineDeluxeAutoSpinController autoSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        betController = GetComponent<TheGreenMachineDeluxeBetController>();
        rulesPopupController = GetComponent<TheGreenMachineDeluxeRulesPopupController>();
        autoSpinController = GetComponent<TheGreenMachineDeluxeAutoSpinController>();

        UpdateCoins();
        SetupInputButtons();
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
        if (!TheGreenMachineDeluxeSoundManager.Instance.IsSoundMute())
            TheGreenMachineDeluxeSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!TheGreenMachineDeluxeSoundManager.Instance.IsMusicMute())
            TheGreenMachineDeluxeSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!TheGreenMachineDeluxeSoundManager.Instance.IsMusicMute())
            TheGreenMachineDeluxeSoundManager.Instance.StopMusic(soundName);
    }

    private void PlayWinText(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!TheGreenMachineDeluxeSoundManager.Instance.IsMusicMute())
            TheGreenMachineDeluxeSoundManager.Instance.PlayWinText(soundName);
    }

    private void StopWinText(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!TheGreenMachineDeluxeSoundManager.Instance.IsMusicMute())
            TheGreenMachineDeluxeSoundManager.Instance.StopWinText(soundName);
    }

    private void SoundActive(bool soundActive)
    {
        TheGreenMachineDeluxeSoundManager.Instance.MuteSFX(!soundActive);

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
        TheGreenMachineDeluxeSoundManager.Instance.MuteMusic(!musicActive);

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
        PlaySound("VolumeIncrease");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("VolumeDecrease");
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

    private void OnClickSpin()
    {
        PlaySound("Button");

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        PlayMusic("ReelSpin");

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinText("WinText");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        UpdateButtons("Single Start");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        PlaySound("Button");

        //if (TheGreenMachineDeluxeSoundManager.Instance != null && !TheGreenMachineDeluxeAutoSpinController.isAutoSpinning)
        //    TheGreenMachineDeluxeSoundManager.Instance.StopMusic("Spin1");

        TheGreenMachineDeluxeSlotMachine.Instance.isStopBtnPressed = true;
        TheGreenMachineDeluxeSlotMachine.Instance.StopWithResult();

        if (TheGreenMachineDeluxeAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    private void OnClickAuto()
    {
        if (autoSpinController == null) return;

        PlaySound("Button");
        
        float betAmount = betController.GetCurrentBet();
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinText("WinText");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        autoSpinController.StartAutoSpin(betAmount);
    }

    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        PlaySound("Button");

        //TheGreenMachineDeluxeSoundManager.Instance.StopMusic("Spin1");

        autoSpinController.CancelAutoSpin();

        if (TheGreenMachineDeluxeSlotMachine.Instance.InSpin)
        {
            autoStopButton.ShowButton(false);
            autoButton.ShowButton(true);
            SetAutoInteractable(false);
        }

        //if (!TheGreenMachineDeluxeSlotMachine.Instance.isPaylineCompleted)
        //{
        //    TheGreenMachineDeluxeAutoSpinController.isAutoSpinning = false;
        //    UpdateButtons("Auto Stop");
        //}
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
                //stopButton.ShowButton(true);
                autoButton.GetButtonComponent().interactable = false;
                break;

            case "Single Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                //stopButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = true;
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

            case "Free Game Transition":
                inSpin = true;
                spinButton.ShowButton(true);
                autoButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = false;
                SetAutoInteractable(false);
                break;

            case "Free Spin":
                spinButton.ShowButton(false);
                break;

            case "Base Game Transition":
                inSpin = false;
                spinButton.ShowButton(true);
                autoButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = true;
                SetAutoInteractable(true);
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

        PlayWinText("WinText");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount, false));
    }

    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        freeSpinWinTextCoroutine = StartCoroutine(AnimateToValue(target, duration, textToAnimate, true));
    }

    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimate, bool freeSpin)
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

        if (!freeSpin)
        {
            StopWinText("WinText");
            PlaySound("WinTextEnd");
        }
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

    public void SetAutoInteractable(bool state)
    {
        autoButton.GetButtonComponent().interactable = state;
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }
    #endregion

    #region Win Animations

    public void PlayNiceWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(niceWin, winAmount, 0, niceWinTrigger));
    }

    public void PlayBigWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(bigWin,  winAmount, 1, bigWinTrigger));
    }

    public void PlayMegaWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(megaWin,  winAmount, 2, megaWinTrigger));
    }

    public void PlaySuperWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(superWin,  winAmount, 3, superWinTrigger));
    }

    public void PlayJackpotWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(jackpotWin, winAmount, 4, jackpotWinTrigger));
    }

    private IEnumerator WinAnimation(GameObject winType,float targetAmount, int animatorIndex, string animationTrigger)
    {
        winAnimationCompleted = false;

        yield return new WaitForSeconds(1.5f);

        winAnimations.SetActive(true);
        winType.SetActive(true);

        Animator animator = winAnimators[animatorIndex];

        animator.enabled = true;
        animator.SetTrigger(animationTrigger);

        //winText.text = "0.00";

        yield return new WaitForSeconds(1.5f);

        textAnimationCoroutine = StartCoroutine(AnimateToValueTo(targetAmount, 2f, this.winAmount));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;

        StopCoroutine(winCoroutine);
    }

    #endregion
}
