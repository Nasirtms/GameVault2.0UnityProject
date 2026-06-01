using Coffee.UIEffects;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(GoldGobblersBetController))]
[RequireComponent(typeof(GoldGobblersRulesPopupController))]
[RequireComponent(typeof(GoldGobblersAutoSpinController))]

public class GoldGobblersUIManager : GameBetServices
{
    #region Variables

    public static GoldGobblersUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;
    //[SerializeField] private Button maxBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private GoldGobblersUIButtonController spinButton;
    [SerializeField] private GoldGobblersUIButtonController stopButton;
    [SerializeField] private GoldGobblersUIButtonController autoSpinButton;
    [SerializeField] private GoldGobblersUIButtonController autoStopButton;

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

    [Header("Gobblers")]
    [SerializeField] public GameObject redGobbler;
    [SerializeField] public GameObject greenGobbler;
    [SerializeField] public GameObject blueGobbler;

    [Header("5X5 / 3X5 Slot Machine")]
    [SerializeField] private SpriteRenderer slotMachineFrame;
    [SerializeField] private Sprite fiveByFiveMachineFrame;
    [SerializeField] private GameObject fiveByFiveMachine;
    [SerializeField] private Sprite threeByFiveMachineFrame;
    [SerializeField] private GameObject threeByFiveMachine;
    public float slotScaleYForFiveByFive = 0.12f;
    public float slotScaleYForThreeByFive = 0.2f;
    public float bottomYForFiveByFive = -0.685f;
    public float bottomYForThreeByFive = -0.368f;

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

    private GoldGobblersBetController betController;
    private GoldGobblersRulesPopupController rulesPopupController;
    private GoldGobblersAutoSpinController autoSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        betController = GetComponent<GoldGobblersBetController>();
        rulesPopupController = GetComponent<GoldGobblersRulesPopupController>();
        autoSpinController = GetComponent<GoldGobblersAutoSpinController>();

        UpdateCoins();
        SetupInputButtons();
        PlayMusic("Background");
 
        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
        PlayGobblerAnimations("idle");
    }

    private void OnDestroy()
    {
        RemoveListeners(stopButton?.GetButtonComponent());
        RemoveListeners(autoSpinButton?.GetButtonComponent());
        RemoveListeners(autoStopButton?.GetButtonComponent());

        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);
        //RemoveListeners(maxBetButton);

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
        autoSpinButton.GetButtonComponent().onClick.AddListener(OnHoldSpin);
        autoStopButton.GetButtonComponent().onClick.AddListener(OnAutoStop);

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);
        //maxBetButton.onClick.AddListener(SetMaxBet);

        exitGameButton.onClick.AddListener(ExitGame);
        openRulesButton.onClick.AddListener(OpenRulesPopup);
        soundButton.onClick.AddListener(() => SoundActive(soundOn));
        musicButton.onClick.AddListener(() => MusicActive(musicOn));
        //soundButton.onClick.AddListener(ToggleSound);
        //musicButton.onClick.AddListener(ToggleMusic);
    }

    #endregion

    #region Input Buttons

    #region Sound

    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldGobblersSoundManager.Instance.IsSoundMute())
            GoldGobblersSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldGobblersSoundManager.Instance.IsMusicMute())
            GoldGobblersSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldGobblersSoundManager.Instance.IsMusicMute())
            GoldGobblersSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldGobblersSoundManager.Instance.IsSoundMute())
            GoldGobblersSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldGobblersSoundManager.Instance.IsSoundMute())
            GoldGobblersSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void SoundActive(bool soundActive)
    {
        GoldGobblersSoundManager.Instance.MuteSFX(!soundActive);

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
        GoldGobblersSoundManager.Instance.MuteMusic(!musicActive);

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
        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        PlaySpinMusic("Spin");

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }

        UpdateButtons("Spin Start");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        PlaySound("Button");
        if (GoldGobblersSlotMachine.Instance.isFreeGame) 
        {
            UpdateButtons("Free Spin");
        }
        else
        {
            UpdateButtons("Spin Stop");
        }
        GoldGobblersSlotMachine.Instance.isStopBtnPressed = true;
        GoldGobblersSlotMachine.Instance.StopWithResult();

        if (GoldGobblersAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }
    }

    public void OnAutoStop()
    {
        if (GoldGobblersSlotMachine.Instance.InSpin) return;

        autoSpinButton.gameObject.SetActive(true);
        autoStopButton.gameObject.SetActive(false);

        if (GoldGobblersAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }
    }

    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;
        autoSpinButton.gameObject.SetActive(false);
        autoStopButton.gameObject.SetActive(true);

        float betAmount = betController.GetCurrentBet();
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
            case "Spin Start":
                inSpin = true;
                spinButton.gameObject.transform.parent.gameObject.SetActive(!inSpin);
                autoSpinButton.GetButtonComponent().interactable = !inSpin;
                //stopButton.gameObject.transform.parent.gameObject.SetActive(inSpin);
                break;

            case "Spin Stop":
                inSpin = false;
                spinButton.gameObject.transform.parent.gameObject.SetActive(!inSpin);
                autoSpinButton.GetButtonComponent().interactable = !inSpin;
                //stopButton.gameObject.transform.parent.gameObject.SetActive(inSpin);
                break;

            case "Transition Start":
                inSpin = true;
                spinButton.gameObject.transform.parent.gameObject.SetActive(inSpin);
                autoSpinButton.GetButtonComponent().interactable = inSpin;
                //stopButton.gameObject.transform.parent.gameObject.SetActive(!inSpin);
                spinButton.GetButtonComponent().interactable = false;
                break;

            case "Transition End":
                inSpin = false;
                spinButton.gameObject.transform.parent.gameObject.SetActive(!inSpin);
                autoSpinButton.GetButtonComponent().interactable = !inSpin;
                //stopButton.gameObject.transform.parent.gameObject.SetActive(inSpin);
                spinButton.GetButtonComponent().interactable = true;
                break;

            case "Free Spin":
                inSpin = true;
                spinButton.gameObject.transform.parent.gameObject.SetActive(!inSpin);
                autoSpinButton.GetButtonComponent().interactable = !inSpin;
                //stopButton.gameObject.transform.parent.gameObject.SetActive(inSpin);
                break;

            default:
                return;
        }

        if (inSpin)
        {
            exitGameButton.interactable = !inSpin;
            increaseBetButton.interactable = !inSpin;
            decreaseBetButton.interactable = !inSpin;
            //maxBetButton.interactable = !inSpin;
        }
        else
        {
            exitGameButton.interactable = !inSpin;
            increaseBetButton.interactable = !inSpin;
            decreaseBetButton.interactable = !inSpin;
            //maxBetButton.interactable = !inSpin;
        }

        currentButtonSet = type;
    }

    #endregion

    #region Text Animation
    private string FormatFloorValue(float value)
    {
        float floored = Mathf.Floor(value * 1000f) / 1000f;
        return floored.ToString("0.000");
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

        //textToAnimate.text = target.ToString("0.00");
        textToAnimate.text = FormatFloorValue(target);
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

        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

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

    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    public void PlayGobblerAnimations(string type)
    {
        Debug.Log("Playing Gobbler Animations 1: " + type);
        Animator redAnimator = redGobbler.GetComponent<Animator>();
        Animator greenAnimator = greenGobbler.GetComponent<Animator>();
        Animator blueAnimator = blueGobbler.GetComponent<Animator>();
        switch (type)
        {
            case "idle":
                redAnimator.SetBool("redgobbleridle", true);
                redAnimator.SetBool("redgobblerhappy", false);
                greenAnimator.SetBool("greengobbleridle", true);
                greenAnimator.SetBool("greengobblerhappy", false);
                blueAnimator.SetBool("bluegobbleridel", true);
                blueAnimator.SetBool("bluegobblerhappy", false);
                break;

            case "red":
                redAnimator.SetBool("redgobbleridle", false);
                redAnimator.SetBool("redgobblerhappy", true);
                greenAnimator.SetBool("greengobbleridle", true);
                greenAnimator.SetBool("greengobblerhappy", false);
                blueAnimator.SetBool("bluegobbleridel", true);
                blueAnimator.SetBool("bluegobblerhappy", false);
                break;

            case "green":
                redAnimator.SetBool("redgobbleridle", true);
                redAnimator.SetBool("redgobblerhappy", false);
                greenAnimator.SetBool("greengobbleridle", false);
                greenAnimator.SetBool("greengobblerhappy", true);
                blueAnimator.SetBool("bluegobbleridel", true);
                blueAnimator.SetBool("bluegobblerhappy", false);
                break;

            case "blue":
                redAnimator.SetBool("redgobbleridle", true);
                redAnimator.SetBool("redgobblerhappy", false);
                greenAnimator.SetBool("greengobbleridle", true);
                greenAnimator.SetBool("greengobblerhappy", false);
                blueAnimator.SetBool("bluegobbleridel", false);
                blueAnimator.SetBool("bluegobblerhappy", true);
                break;

            case "red&blue":
                redAnimator.SetBool("redgobbleridle", false);
                redAnimator.SetBool("redgobblerhappy", true);
                greenAnimator.SetBool("greengobbleridle", true);
                greenAnimator.SetBool("greengobblerhappy", false);
                blueAnimator.SetBool("bluegobbleridel", false);
                blueAnimator.SetBool("bluegobblerhappy", true);
                break;

            case "red&green":
                redAnimator.SetBool("redgobbleridle", false);
                redAnimator.SetBool("redgobblerhappy", true);
                greenAnimator.SetBool("greengobbleridle", false);
                greenAnimator.SetBool("greengobblerhappy", true);
                blueAnimator.SetBool("bluegobbleridel", true);
                blueAnimator.SetBool("bluegobblerhappy", false);
                break;

            case "green&blue":
                redAnimator.SetBool("redgobbleridle", true);
                redAnimator.SetBool("redgobblerhappy", false);
                greenAnimator.SetBool("greengobbleridle", false);
                greenAnimator.SetBool("greengobblerhappy", true);
                blueAnimator.SetBool("bluegobbleridel", false);
                blueAnimator.SetBool("bluegobblerhappy", true);
                break;

            case "red&green&blue":
                redAnimator.SetBool("redgobbleridle", false);
                redAnimator.SetBool("redgobblerhappy", true);
                greenAnimator.SetBool("greengobbleridle", false);
                greenAnimator.SetBool("greengobblerhappy", true);
                blueAnimator.SetBool("bluegobbleridel", false);
                blueAnimator.SetBool("bluegobblerhappy", true);
                break;
        }
    }
    public void SwitchSlotMachine(bool isfreeGame)
    {
        if (isfreeGame)
        {
            fiveByFiveMachine.SetActive(true);
            threeByFiveMachine.SetActive(false);
            slotMachineFrame.sprite = fiveByFiveMachineFrame;
        }
        else
        {
            fiveByFiveMachine.SetActive(false);
            threeByFiveMachine.SetActive(true);
            slotMachineFrame.sprite = threeByFiveMachineFrame;
        }
    }
    #endregion
}
