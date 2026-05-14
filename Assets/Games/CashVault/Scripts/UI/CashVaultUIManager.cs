using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CashVaultBetController))]
[RequireComponent(typeof(CashVaultRulesPopupController))]
[RequireComponent(typeof(CashVaultAutoSpinController))]
public class CashVaultUIManager : GameBetServices
{
    #region Variables

    public static CashVaultUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private CashVaultUIButtonController spinButton;
    [SerializeField] private CashVaultUIButtonController stopButton;
    [SerializeField] private CashVaultUIButtonController autoButton;
    [SerializeField] private CashVaultUIButtonController autoStopButton;

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

    private CashVaultBetController betController;
    private CashVaultRulesPopupController rulesPopupController;
    private CashVaultAutoSpinController autoSpinController;

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

    [SerializeField] private GameObject winPopupRoot;
    [SerializeField] private TMP_Text winText;
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
        betController = GetComponent<CashVaultBetController>();
        rulesPopupController = GetComponent<CashVaultRulesPopupController>();
        autoSpinController = GetComponent<CashVaultAutoSpinController>();

        //soundOn = true;
        //musicOn = true;
        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("BG");
    }

    private void OnDestroy()
    {
        RemoveListeners(spinButton?.GetButtonComponent());
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
        if (!CashVaultSoundManager.Instance.IsSoundMute())
            CashVaultSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CashVaultSoundManager.Instance.IsMusicMute())
            CashVaultSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CashVaultSoundManager.Instance.IsMusicMute())
            CashVaultSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CashVaultSoundManager.Instance.IsSoundMute())
            CashVaultSoundManager.Instance.SpinPlayMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CashVaultSoundManager.Instance.IsSoundMute())
            CashVaultSoundManager.Instance.SpinStopMusic(soundName);
    }
    public void PlayWinText(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CashVaultSoundManager.Instance.IsSoundMute())
            CashVaultSoundManager.Instance.PlayWinText(soundName);
    }

    public void StopWinText(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CashVaultSoundManager.Instance.IsSoundMute())
            CashVaultSoundManager.Instance.StopWinText(soundName);
    }

    private void SoundActive(bool soundActive)
    {
        CashVaultSoundManager.Instance.MuteSFX(!soundActive);

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
        CashVaultSoundManager.Instance.MuteMusic(!musicActive);

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
        SceneManagement.GoBackToMainMenu();
        //SceneManager.LoadScene("Main");
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
        PlaySound("Button");
        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinText("WinText");
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
        StopSpinMusic("Spin");
        if (CashVaultSlotMachine.Instance.isFreeGame || CashVaultSlotMachine.Instance.isMiniGame)
            UpdateButtons("Free Spin");
        else
        {
            if (CashVaultAutoSpinController.isAutoSpinning)
            {
                SetStopInteractable(false);
            }
        }

        if (CashVaultSlotMachine.Instance.isMiniGame)
        {
            CashVaultMiniGameSlotMachine.Instance.isStopBtnPressed = true;
            CashVaultMiniGameSlotMachine.Instance.StopWithResult();
        }
        else
        {
            CashVaultSlotMachine.Instance.isStopBtnPressed = true;
            CashVaultSlotMachine.Instance.StopWithResult();
        }
        //if (CashVaultAutoSpinController.isAutoSpinning)
        //{
        //    autoSpinController.CancelAutoSpin();
        //}
    }
    public void OnClickAuto()
    {
        PlaySound("Button");

        if (autoSpinController == null) return;
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
        //UpdateButtons("Spin");
    }
    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        PlaySound("Button");
        autoSpinController.CancelAutoSpin();
    }
    //public void OnHoldSpin()
    //{
    //    if (autoSpinController == null) return;
    //    float betAmount = betController.GetCurrentBet();
    //    if (textAnimationCoroutine != null)
    //    {
    //        StopCoroutine(textAnimationCoroutine);
    //        StopWinText("WinText");
    //    }
    //    if (winCoroutine != null)
    //    {
    //        StopCoroutine(winCoroutine);
    //    }

    //    autoSpinController.StartAutoSpin(betAmount);
    //    UpdateButtons("Spin");
    //}

    #endregion

    #region UI Update
    public void UpdateCoins()
    {
        if (UserManager.Instance != null)
        {
            coins.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }
    }

    public void UpdateCoinFromResponse(float coin)
    {
        if (UserManager.Instance != null)
        {
            UserManager.Instance.Coins = coin;
            coins.text = UserManager.Instance.FormatCoins(coin);
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
                SetAutoInteractable(false);
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
                SetStopInteractable(false);
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
                SetAutoInteractable(false);
                autoStopButton.ShowButton(true);
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
        float floored = Mathf.Floor(value * 1000f) / 1000f;
        return floored.ToString("0.000");
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
            this.winText.text = "0.00";
        }
    }
    
    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        //PlayWinText("Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount));
    }

    public void TextAnimation(float target, float duration, TMP_Text textToAnimate)
    {
        freeSpinWinTextCoroutine = StartCoroutine(AnimateToValue(target, duration, textToAnimate));
    }

    public IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimate)
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
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }
        textToAnimate.text = FormatFloorValue(target);
        StopWinText("Win");
        PlaySound("WinEnd");
    }
    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimateOne, TMP_Text textToAnimateTwo)
    {
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(0f, target, t);
            textToAnimateOne.text = FormatFloorValue(displayed);
            textToAnimateTwo.text = FormatFloorValue(displayed);
            timer += Time.deltaTime;
            yield return null;
        }
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);

        StopCoroutine(textAnimationCoroutine);
    }
    public void UpdateWinText(float winAmount)
    {
        StopAllCoroutines();
        StartCoroutine(WinPopupRoutine(winAmount));
    }
    public bool winAnimation = true;
    private IEnumerator WinPopupRoutine(float winAmount)
    {
        winAnimation = false;
        RectTransform rect = winPopupRoot.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = winPopupRoot.GetComponent<CanvasGroup>();

        if (winPopupRoot.transform.parent != null)
            winPopupRoot.transform.parent.gameObject.SetActive(true);

        // Reset state
        rect.localScale = Vector3.zero;
        canvasGroup.alpha = 1f;

        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 120f);
        PlayWinText("Win");
        Coroutine numberRoutine = StartCoroutine(
            AnimateToValue(winAmount, 1.2f, winText)
        );

        float popTime = 1f;
        float t = 0f;
        while (t < popTime)
        {
            float p = t / popTime;
            rect.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutCubic(p));

            t += Time.deltaTime;
            yield return null;
        }
        rect.localScale = Vector3.one;

        yield return numberRoutine;
        yield return new WaitForSeconds(0.5f);

        float fadeTime = 0.6f;
        t = 0f;
        while (t < fadeTime)
        {
            float p = t / fadeTime;
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, p);

            t += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = startPos;
        winPopupRoot.transform.parent.gameObject.SetActive(false);
        winAnimation = true;
    }
    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
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
    public void SetAutoInteractable(bool state)
    {
        autoButton.GetButtonComponent().interactable = state;
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
        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        yield return new WaitForSeconds(3.5f);

        animator.enabled = false;

        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;
        if (CashVaultSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition Start");
        }
        else if (CashVaultAutoSpinController.isAutoSpinning)
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