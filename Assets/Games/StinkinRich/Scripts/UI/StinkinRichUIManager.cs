
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(StinkinRichBetController))]
[RequireComponent(typeof(StinkinRichRulesPopupController))]
[RequireComponent(typeof(StinkinRichAutoSpinController))]
public class StinkinRichUIManager : GameBetServices
{
    #region Variables

    public static StinkinRichUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;


    [Header("Spin Buttons")]
    [SerializeField] public StinkinRichUIButtonController spinButton;
    [SerializeField] public StinkinRichUIButtonController stopButton;
    [SerializeField] public StinkinRichUIButtonController autoButton;
    [SerializeField] public StinkinRichUIButtonController autoStopButton;

    [Header("AutoSpin")]
    [SerializeField] public GameObject autoSpinPopup;
    [SerializeField] private Button autoSpin10Button;
    [SerializeField] private Button autoSpin50Button;
    [SerializeField] private Button autoSpin100Button;
    [SerializeField] private Button autoSpin200Button;
    [SerializeField] private Button autoSpinInfinityButton;
    [SerializeField] public TMP_Text remainingSpins;

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

    private StinkinRichBetController betController;
    private StinkinRichRulesPopupController rulesPopupController;
    private StinkinRichAutoSpinController autoSpinController;

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

    [Header("Skunk")]
    [SerializeField] private GameObject skunk;
    [SerializeField] private string skunkCoinAnimationBool;
    [SerializeField] private string skunkCoinTossAnimationBool;
    [SerializeField] private string skunkIdleAnimationBool;
    [SerializeField] private string skunkHappyAnimationBool;
    [SerializeField] private string skunkThumbsUpAnimationBool;

    [Header("Trash Slots Animator Bools")]
    [SerializeField] public string normalTrashBool;
    [SerializeField] public string normalTrashOpenBool;
    [SerializeField] public string freeSpinTrashBool;
    [SerializeField] public string freeSpinTrashOpenBool;

    [Header("TrashForCashAnimations")]
    [SerializeField] private GameObject trashForCashParent;
    [SerializeField] private GameObject trashForCash;
    [SerializeField] private GameObject trashForCashStart;
    [SerializeField] private GameObject trashForCashStartBg;
    [SerializeField] private GameObject trashForCashEnd;
    [SerializeField] private TMP_Text bonusPays;
    [SerializeField] private TMP_Text multiplier;
    [SerializeField] private TMP_Text totalAmount;
    [SerializeField] private Button stopTrashStartAniamtionButton;
    [SerializeField] private Button stopTrashEndAnimationButton;
    private Tween stopTrashStartAniamtionButtonTween;
    private Tween stopTrashEndAnimationButtonTween;

    public bool waitForTrashForCashEnd = false;
    private bool autoStopPressed;

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
        betController = GetComponent<StinkinRichBetController>();
        rulesPopupController = GetComponent<StinkinRichRulesPopupController>();
        autoSpinController = GetComponent<StinkinRichAutoSpinController>();
        autoSpinPopup.SetActive(false);
        autoStopButton.gameObject.SetActive(false);
        remainingSpins.gameObject.SetActive(false);
        stopTrashStartAniamtionButton.onClick.AddListener(OnClickStopTrashForCashStartAnimation);
        stopTrashEndAnimationButton.onClick.AddListener(OnClickStopTrashForCashEndAnimation);

        PlaySkunkIdleAnimations();

        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("BG");
    }

    private void OnDestroy()
    {
        //RemoveListeners(spinButton?.GetButtonComponent());
        RemoveListeners(stopButton?.GetButtonComponent());
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

        autoSpin10Button.onClick.AddListener(() => OnAutoSpinOptionSelected(10));
        autoSpin50Button.onClick.AddListener(() => OnAutoSpinOptionSelected(50));
        autoSpin100Button.onClick.AddListener(() => OnAutoSpinOptionSelected(100));
        autoSpin200Button.onClick.AddListener(() => OnAutoSpinOptionSelected(200));
        autoSpinInfinityButton.onClick.AddListener(() => OnAutoSpinOptionSelected(-1));

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
        if (!StinkinRichSoundManager.Instance.IsSoundMute())
            StinkinRichSoundManager.Instance.PlaySFX(soundName);
    }
    public void StopCurrentSFX()
    {
        if (!StinkinRichSoundManager.Instance.IsSoundMute())
            StinkinRichSoundManager.Instance.StopSFX();
    }
    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StinkinRichSoundManager.Instance.IsMusicMute())
            StinkinRichSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StinkinRichSoundManager.Instance.IsMusicMute())
            StinkinRichSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StinkinRichSoundManager.Instance.IsSoundMute())
            StinkinRichSoundManager.Instance.SpinPlayMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StinkinRichSoundManager.Instance.IsSoundMute())
            StinkinRichSoundManager.Instance.SpinStopMusic(soundName);
    }
    //private void PlayWinMusic(string soundName)
    //{
    //    if (string.IsNullOrEmpty(soundName)) return;
    //    if (!SaharaRichesSoundManager.Instance.IsSoundMute())
    //        SaharaRichesSoundManager.Instance.PlayWinText(soundName);
    //}

    //private void StopWinMusic(string soundName)
    //{
    //    if (string.IsNullOrEmpty(soundName)) return;
    //    if (!SaharaRichesSoundManager.Instance.IsSoundMute())
    //        SaharaRichesSoundManager.Instance.StopWinText(soundName);
    //}


    private void SoundActive(bool soundActive)
    {
        StinkinRichSoundManager.Instance.MuteSFX(!soundActive);

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
        StinkinRichSoundManager.Instance.MuteMusic(!musicActive);
            
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
        PlaySound("Bet");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Bet");
        betController.DecreaseChipValue();
    }
    private void SetMaxBet()
    {
        if (betController == null) return;

        PlaySound("Bet");
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
        UpdateButtons("Spin");

        StopCurrentSFX();
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
        autoSpinPopup.SetActive(false);
        PlaySound("Button");
        SlotSpinService.Instance.Spin(betAmount);
    }

    private void OnClickStop()
    {
        PlaySound("Button");
        StopSpinMusic("Spin");
        StinkinRichSlotMachine.Instance.isStopBtnPressed = true;
        StinkinRichSlotMachine.Instance.StopWithResult();
    }

    private void OnClickAuto()
    {
        if (autoSpinController == null) return;
        autoButton.setInteractable(!StinkinRichSlotMachine.Instance.InSpin);

        PlaySound("Auto_Button");

        if (autoSpinPopup.activeSelf == true)
        {
            autoSpinPopup.SetActive(false);
            return;
        }

        autoSpinPopup.SetActive(true);
    }

    public void HideSpinCount()
    {
        autoSpinPopup.SetActive(false);
    }

    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        autoStopPressed = true;
        PlaySound("AutoButton_Stop");
        StopSpinMusic("Spin");
        autoSpinPopup.SetActive(false);
        autoSpinController.CancelAutoSpin();

        autoStopButton.GetButtonComponent().interactable = false;
        if (!StinkinRichSlotMachine.Instance.InSpin)
        {
            UpdateButtons("Idle");
        }
    }

    public void SetAutoInteractable(bool state)
    {
        autoButton.setInteractable(state);
    }

    private void OnAutoSpinOptionSelected(int spinCount)
    {
        PlaySound("Auto_Button");

        autoStopPressed = false;
        autoSpinPopup.SetActive(false);

        float betAmount = betController.GetCurrentBet();

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            //StopWinMusic("Win");
        }
        if (winCoroutine != null)
            StopCoroutine(winCoroutine);

        PlaySpinMusic("Spin");
        UpdateButtons("AutoSpin");
        autoSpinController.SetSpinCount(spinCount);
        autoSpinController.StartAutoSpin(betAmount);
    }

    public void UpdateRemainingSpins(int remainingSpins)
    {
        if (remainingSpins == -1)
        {
            this.remainingSpins.text = "∞";
            return;
        }

        this.remainingSpins.text = remainingSpins.ToString();
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
        currentButtonSet = type;

        // Defaults
        bool showSpin = false;
        bool spinInteractable = false;

        bool showStop = false;
        bool stopInteractable = false;

        bool showAuto = true;
        bool autoInteractable = false;

        bool showAutoStop = false;
        bool autoStopInteractable = false;

        bool betInteractable = false;
        bool exitInteractable = false;
        bool showRemainingSpins = false;

        switch (type)
        {
            case "Idle":
                showSpin = true;
                spinInteractable = true;

                showStop = false;

                showAuto = true;
                autoInteractable = true;

                showAutoStop = false;

                betInteractable = true;
                exitInteractable = true;
                showRemainingSpins = false;

                autoStopPressed = false;
                break;

            case "Spin":
                showSpin = false;
                spinInteractable = false;

                showStop = true;
                stopInteractable = false; // becomes true once result arrives

                showAuto = true;
                autoInteractable = false;

                showAutoStop = false;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = false;
                break;

            case "AutoSpin":
                showSpin = false;
                spinInteractable = false;

                showStop = true;
                stopInteractable = true;

                showAuto = false;
                autoInteractable = false;

                showAutoStop = true;
                autoStopInteractable = true;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = true;

                autoStopPressed = false;
                break;

            case "FreeSpin":
                showSpin = false;
                spinInteractable = false;

                showStop = true;
                stopInteractable = true;

                showAuto = true;
                autoInteractable = false;

                showAutoStop = false;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = false;
                break;

            case "Transition":
            case "WinAnimation":
            case "BonusLock":
                showSpin = true;
                spinInteractable = false;

                showStop = false;
                stopInteractable = false;

                showAuto = true;
                autoInteractable = false;

                showAutoStop = false;
                autoStopInteractable = false;

                betInteractable = false;
                exitInteractable = false;
                showRemainingSpins = false;
                break;

            default:
                Debug.LogWarning("Unknown button state: " + type);
                return;
        }

        spinButton.ShowButton(showSpin);
        spinButton.setInteractable(spinInteractable);

        stopButton.ShowButton(showStop);
        stopButton.setInteractable(stopInteractable);

        autoButton.ShowButton(showAuto);
        autoButton.setInteractable(autoInteractable);

        autoStopButton.ShowButton(showAutoStop);
        autoStopButton.setInteractable(autoStopInteractable);

        remainingSpins.gameObject.SetActive(showRemainingSpins);

        increaseBetButton.interactable = betInteractable;
        decreaseBetButton.interactable = betInteractable;
        exitGameButton.interactable = exitInteractable;
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
        textAnimationCoroutine = StartCoroutine(AnimateAmountFromZeroToTarget(winAmount, 1f, this.winAmount));
    }

    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        freeSpinWinTextCoroutine = StartCoroutine(AnimateAmountFromZeroToTarget(target, duration, textToAnimate));
    }

    public IEnumerator AnimateAmountFromZeroToTarget(float target, float duration, TMP_Text textToAnimate)
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

        // Ensure final value is exact
        //textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);
        StopCoroutine(textAnimationCoroutine);
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

    public void SetSpinInteractable(bool state)
    {
        spinButton.GetButtonComponent().interactable = state;
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
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
        if (StinkinRichSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (StinkinRichAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("AutoSpin");
        }
        else
        {
            UpdateButtons("Stop");
        }
        StopCoroutine(winCoroutine);
    }

    public void PlaySkunkIdleAnimations()
    {
        string skunkAnimator = Random.Range(0, 2) == 0 ? skunkIdleAnimationBool : skunkCoinTossAnimationBool;
        var animator = skunk.GetComponent<Animator>();
        animator.SetBool(skunkHappyAnimationBool, false);
        animator.SetBool(skunkThumbsUpAnimationBool, false);
        animator.SetBool(skunkAnimator, true);
    }

    public void PlaySkunkWinAnimations()
    {
        string skunkAnimator = Random.Range(0, 2) == 0 ? skunkHappyAnimationBool : skunkThumbsUpAnimationBool;
        var animator = skunk.GetComponent<Animator>();
        animator.SetBool(skunkIdleAnimationBool, false);
        animator.SetBool(skunkCoinTossAnimationBool, false);
        animator.SetBool(skunkAnimator, true);
    }

    public void PlayTrashForCashStart()
    {
        StartCoroutine(TrashForCashStart());
    }

    private IEnumerator TrashForCashStart()
    {
        StinkinRichPaylineController.Instance.PlayTrashSlots();
        trashForCashParent.SetActive(true);
        trashForCash.SetActive(true);
        trashForCashStart.SetActive(true);

        var animator = trashForCash.GetComponent<Animator>();
        animator.SetBool("start", true);
        yield return new WaitForSeconds(2f);
        stopTrashStartAniamtionButtonTween?.Kill();

        stopTrashStartAniamtionButtonTween = stopTrashStartAniamtionButton.transform
            .DOScale(1.05f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnClickStopTrashForCashStartAnimation()
    {
        StartCoroutine(stopTrashForCashStartAnimation());
    }

    private IEnumerator stopTrashForCashStartAnimation()
    {
        var animator = trashForCash.GetComponent<Animator>();
        animator.SetBool("start", false);
        animator.SetBool("disappear", true);
        yield return new WaitForSeconds(1f);
        trashForCashStart.SetActive(false);
        trashForCashStartBg.transform.localScale = Vector3.one;
        trashForCash.SetActive(false);
        trashForCashParent.SetActive(false);
        StinkinRichSlotMachine.Instance.canClickTrashSlots = true;
        stopTrashStartAniamtionButtonTween?.Kill();
        stopTrashStartAniamtionButton.transform.localScale = Vector3.one;
    }

    public void PlayTrashForCashEnd()
    {
        StartCoroutine(TrashForCashEnd());
    }

    private IEnumerator TrashForCashEnd()
    {
        if (!StinkinRichSlotMachine.Instance.isFreeGame)
        {
            yield return new WaitUntil(() => winAnimationCompleted);
        }
        yield return new WaitForSeconds(0.8f);
        float w = CurrentBet();
        int x = StinkinRichSlotMachine.Instance.trashNotMultiplier1 + StinkinRichSlotMachine.Instance.trashNotMultiplier2;
        int y = StinkinRichSlotMachine.Instance.trashMultiplier;
        float xFloat = (float)x;
        float yFloat = (float)y;
        float z = (xFloat * yFloat) * w;
        totalAmount.text = z.ToString("F2");
        bonusPays.text = xFloat.ToString("F2");
        multiplier.text = y.ToString() + "  x";
        trashForCashParent.SetActive(true);
        trashForCash.SetActive(true);
        trashForCashEnd.SetActive(true);
        var animator = trashForCash.GetComponent<Animator>();
        animator.SetBool("end", true);
        yield return new WaitForSeconds(3f);
        stopTrashEndAnimationButtonTween?.Kill();

        stopTrashEndAnimationButtonTween = stopTrashEndAnimationButton.transform
            .DOScale(1.2f, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnClickStopTrashForCashEndAnimation()
    {
        var animator = trashForCash.GetComponent<Animator>();
        animator.SetBool("end", false);
        trashForCashEnd.SetActive(false);
        trashForCash.SetActive(false);
        trashForCashParent.SetActive(false);
        if (!StinkinRichSlotMachine.Instance.isFreeGame)
        {
            autoButton.GetButtonComponent().interactable = true;
            spinButton.GetButtonComponent().interactable = true;
        }
        else stopButton.GetButtonComponent().interactable = true;
        stopTrashEndAnimationButtonTween?.Kill();
        stopTrashEndAnimationButton.transform.localScale = Vector3.one;
        waitForTrashForCashEnd = false;
    }


    #endregion
}
