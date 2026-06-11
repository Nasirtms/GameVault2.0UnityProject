using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(GoldRushGusBetController))]
[RequireComponent(typeof(GoldRushGusRulesPopupController))]
[RequireComponent(typeof(GoldRushGusAutoSpinController))]
public class GoldRushGusUIManager : GameBetServices
{
    #region Variables

    public static GoldRushGusUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private GoldRushGusUIButtonController spinButton;
    [SerializeField] private GoldRushGusUIButtonController stopButton;
    [SerializeField] private GoldRushGusUIButtonController autoButton;
    [SerializeField] private GoldRushGusUIButtonController autoStopButton;

    [Header("Fast Button")]
    [SerializeField] private Button fastButtonOff;
    [SerializeField] private Button fastButtonOn;

    [Header("Auto Spin Popup")]
    public GameObject autoSpinPopupPanel;
    [SerializeField] private GameObject autoSpinCountPanel;
    [SerializeField] private Button autoSpin10Button;
    [SerializeField] private Button autoSpin50Button;
    [SerializeField] private Button autoSpin100Button;
    [SerializeField] private Button autoSpin200Button;
    [SerializeField] private Button autoSpinInfinityButton;
    [SerializeField] private TMP_Text remainingSpins;

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

    private GoldRushGusBetController betController;
    private GoldRushGusRulesPopupController rulesPopupController;
    private GoldRushGusAutoSpinController autoSpinController;

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

    [Header("Treasure Win Animations")]
    public GameObject treasureWinAnimations;
    [SerializeField] private TMP_Text treasureWinText;
    [SerializeField] private string instantbool;
    [SerializeField] private string progressivebool;
    [SerializeField] private string coingamblebool;
    public bool isTreasureAnimationCompleted = false;
    public Coroutine jackpotCoroutine;

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
        betController = GetComponent<GoldRushGusBetController>();
        rulesPopupController = GetComponent<GoldRushGusRulesPopupController>();
        autoSpinController = GetComponent<GoldRushGusAutoSpinController>();

        autoSpinPopupPanel.SetActive(false);
        autoSpinCountPanel.SetActive(false);

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);
        ToggleFast(false);
        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("BG");
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

        autoSpin10Button.onClick.AddListener(() => OnAutoSpinOptionSelected(10));
        autoSpin50Button.onClick.AddListener(() => OnAutoSpinOptionSelected(50));
        autoSpin100Button.onClick.AddListener(() => OnAutoSpinOptionSelected(100));
        autoSpin200Button.onClick.AddListener(() => OnAutoSpinOptionSelected(200));
        autoSpinInfinityButton.onClick.AddListener(() => OnAutoSpinOptionSelected(-1));

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);

        fastButtonOff.onClick.AddListener(() => {
            PlaySound("Button");
            ToggleFast(true);

        }); 
        fastButtonOn.onClick.AddListener(() => {
            PlaySound("Button");
            ToggleFast(false);

        });

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
        if (!GoldRushGusSoundManager.Instance.IsSoundMute())
            GoldRushGusSoundManager.Instance.PlaySFX(soundName);
    }
    public void StopCurrentSFX()
    {
        if (!GoldRushGusSoundManager.Instance.IsSoundMute())
            GoldRushGusSoundManager.Instance.StopSFX();
    }
    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldRushGusSoundManager.Instance.IsMusicMute())
            GoldRushGusSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldRushGusSoundManager.Instance.IsMusicMute())
            GoldRushGusSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldRushGusSoundManager.Instance.IsSoundMute())
            GoldRushGusSoundManager.Instance.SpinPlayMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!GoldRushGusSoundManager.Instance.IsSoundMute())
            GoldRushGusSoundManager.Instance.SpinStopMusic(soundName);
    }

    private void SoundActive(bool soundActive)
    {
        GoldRushGusSoundManager.Instance.MuteSFX(!soundActive);

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

    private void MusicActive(bool musicActive)
    {
        GoldRushGusSoundManager.Instance.MuteMusic(!musicActive);

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
        PlaySound("Bet");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Bet");
        betController.DecreaseChipValue();
    }

    #endregion

    private void ToggleFast(bool state)
    {
        fastButtonOn.gameObject.SetActive(state);
        fastButtonOff.gameObject.SetActive(!state);

        if (state)
        {
            GoldRushGusSlotMachine.Instance.settings.spinSettings.startSpin = GoldRushGusSpinMode.SpinAll;
            GoldRushGusSlotMachine.Instance.settings.spinSettings.endSpin = GoldRushGusSpinMode.SpinAll;
        }
        else
        {
            GoldRushGusSlotMachine.Instance.settings.spinSettings.startSpin = GoldRushGusSpinMode.SpinAll;
            GoldRushGusSlotMachine.Instance.settings.spinSettings.endSpin = GoldRushGusSpinMode.SpinOneByOne;
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
        PlaySound("SpinButton");
        autoSpinPopupPanel.SetActive(false);
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
        UpdateButtons("Spin");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        PlaySound("Button");
        StopCurrentSFX();
        StopSpinMusic("Spin");
        GoldRushGusSlotMachine.Instance.isStopBtnPressed = true;
        GoldRushGusSlotMachine.Instance.StopWithResult();
        if (GoldRushGusAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    public void OnClickAuto()
    {
        PlaySound("Button");

        if (autoSpinController == null) return;

        if (autoSpinPopupPanel.transform.localScale.y == 1)
        {
            autoSpinPopupPanel.transform.localScale = new Vector3(1, 0, 1);
            return;
        }

        autoSpinPopupPanel.transform.localScale = new Vector3(1, 0, 1);
        autoSpinPopupPanel.SetActive(true);

        autoSpinPopupPanel.transform
            .DOScaleY(1f, 0.3f)
            .SetEase(Ease.OutBack);
    }
    private void OnAutoSpinOptionSelected(int spinCount)
    {
        PlaySound("SpinButton");

        autoSpinPopupPanel.transform.localScale = new Vector3(1, 0, 1);
        autoSpinPopupPanel.SetActive(false);
        autoSpinCountPanel.SetActive(true);

        float betAmount = betController.GetCurrentBet();
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            //StopWinMusic("Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        autoSpinController.SetSpinCount(spinCount);
        autoSpinController.StartAutoSpin(betAmount);
        //UpdateButtons("Auto");
    }

    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        autoSpinCountPanel.SetActive(false);
        PlaySound("Button");
        //StopSpinMusic("Spin");
        autoSpinController.CancelAutoSpin();
        autoButton.ShowButton(true);
        autoStopButton.ShowButton(false);
        SetAutoInteractable(false);
    }
    #endregion

    #region UI Update
    public void UpdateRemainingSpins(int remainingSpins)
    {
        if (remainingSpins == -1)
        {
            this.remainingSpins.text = "\t<sprite name=Infinity>";
            return;
        }

        this.remainingSpins.text = remainingSpins.ToString();
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

        switch (type)
        {
            case "Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                autoButton.GetButtonComponent().interactable = false;
                break;

            case "Stop":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                SetAutoInteractable(true);
                break;

            case "Auto":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(false);
                autoStopButton.ShowButton(true);
                stopButton.GetButtonComponent().interactable = false;
                break;

            case "Auto Stop":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(true);
                break;

            case "Transition Start":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(false);
                break;

            case "Auto Jackpot Animation":
                interactable = false;
                stopButton.GetButtonComponent().interactable = false;
                autoStopButton.GetButtonComponent().interactable = false;
                SetAutoInteractable(false);
                break;

            case "Free Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(false);
                break;

            case "Free Spin End":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(true);
                break;

            case "Base Game Transition":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoStopButton.ShowButton(false);
                SetAutoInteractable(true);
                break;

            case "WinAnimation":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                SetAutoInteractable(false);
                break;

            case "Auto Win Animation":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoStopButton.ShowButton(true);
                SetAutoInteractable(false);
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
    public void HideSpinCount()
    {
        autoSpinCountPanel.SetActive(false);
    }
    #endregion

    #region Treasure Chest Animations
    public void PlayInstantWinAnimation(float winAmount)
    {
        jackpotCoroutine = StartCoroutine(TreasureChestAnimation(treasureWinAnimations, treasureWinText, winAmount, instantbool));
    }
    public void PlayProgressiveWinAnimation(float winAmount)
    {
        jackpotCoroutine = StartCoroutine(TreasureChestAnimation(treasureWinAnimations, treasureWinText, winAmount, progressivebool));
    }
    public void PlayCoinGambleAnimation(float winAmount)
    {
        
        jackpotCoroutine = StartCoroutine(TreasureChestAnimation(treasureWinAnimations, treasureWinText, winAmount, coingamblebool));
    }
    private IEnumerator TreasureChestAnimation(GameObject winType, TMP_Text winText, float targetAmount, string animationTrigger)
    {
        isTreasureAnimationCompleted = false;
        yield return new WaitForSeconds(2f);

        winType.SetActive(true);
        Animator animator = treasureWinAnimations.transform.GetComponent<Animator>();
        animator.enabled = true;
        animator.SetBool(animationTrigger, true);
        if (animationTrigger == coingamblebool)
        {
            winText.text = "";
        }
        else
        {
            winText.text = GoldRushGusSlotMachine.Instance.ToSpriteDigits(targetAmount);
        }
        PlaySound("JackpotWin");
        yield return null;
        // it will wait until the animation is finished
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.normalizedTime < 1f)
        {
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            yield return null;
        }

        //yield return new WaitForSeconds(3f);
        animator.SetBool(animationTrigger, false);

        animator.enabled = false;
        winType.SetActive(false);

        isTreasureAnimationCompleted = true;
        GoldRushGusSlotMachine.Instance.isTreasureChestAnimationRunning = false;
        //UpdateButtons("Stop");
        if (GoldRushGusAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Auto Win Animation");
        }
        else
        {
            UpdateButtons("Stop");
        }

        StopCoroutine(jackpotCoroutine);
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
        //if(GoldRushGusSlotMachine.Instance.progressiveWin || GoldRushGusSlotMachine.Instance.instantWin)
        yield return new WaitUntil(() => !GoldRushGusSlotMachine.Instance.isTreasureChestAnimationRunning);

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
        if (GoldRushGusSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition Start");
        }
        else if (GoldRushGusAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Auto Win Animation");
        }
        else
        {
            UpdateButtons("Stop");
        }
        StopCoroutine(winCoroutine);
    }
    #endregion
}