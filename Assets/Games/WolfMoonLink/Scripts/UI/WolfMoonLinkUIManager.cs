using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(WolfMoonLinkBetController))]
[RequireComponent(typeof(WolfMoonLinkRulesPopupController))]
[RequireComponent(typeof(WolfMoonLinkAutoSpinController))]
public class WolfMoonLinkUIManager : GameBetServices
{
    #region Variables

    public static WolfMoonLinkUIManager Instance;

    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] public TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private WorldSpaceUIButton decreaseBetButton;
    [SerializeField] private WorldSpaceUIButton increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public WorldSpaceUIButton spinButton;
    [SerializeField] private WorldSpaceUIButton stopButton;
    [SerializeField] private WorldSpaceUIButton autoButton;
    [SerializeField] private WorldSpaceUIButton autoStopButton;

    [Header("Menu Buttons")]
    [SerializeField] private WorldSpaceUIButton openRulesButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button musicButton;

    [Header("Sound and Music")]
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite musicOnSprite;

    private bool soundOn;
    private bool musicOn;

    private float currentSpinWin;

    public string currentButtonSet;
    public string currentUIStateType;

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
    public bool winAnimationCompleted = false;
    public Coroutine textAnimationCoroutine;

    private WolfMoonLinkBetController betController;
    private WolfMoonLinkRulesPopupController rulesPopupController;
    private WolfMoonLinkAutoSpinController autoSpinController;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        betController = GetComponent<WolfMoonLinkBetController>();
        rulesPopupController = GetComponent<WolfMoonLinkRulesPopupController>();
        autoSpinController = GetComponent<WolfMoonLinkAutoSpinController>();

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
        RemoveListeners(spinButton);
        RemoveListeners(stopButton);
        RemoveListeners(autoButton);
        RemoveListeners(autoStopButton);

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

    private void RemoveListeners(WorldSpaceUIButton button)
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    private void SetupInputButtons()
    {
        spinButton.onClick.AddListener(OnClickSpin);
        stopButton.onClick.AddListener(OnClickStop);
        autoButton.onClick.AddListener(OnClickAuto);
        autoStopButton.onClick.AddListener(OnClickAutoStop);

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
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsSoundMute())
            WolfMoonLinkSoundManager.Instance.PlaySFX(soundName);
    }

    public void StopCurrentSFX()
    {
        if (!WolfMoonLinkSoundManager.Instance.IsSoundMute())
            WolfMoonLinkSoundManager.Instance.StopSFX();
    }

    public void PlayMusic(string soundName)
    {
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsMusicMute())
            WolfMoonLinkSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsMusicMute())
            WolfMoonLinkSoundManager.Instance.StopMusic(soundName);
    }

    public void PlaySpinMusic(string soundName)
    {
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsSoundMute())
            WolfMoonLinkSoundManager.Instance.PlaySpinMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsSoundMute())
            WolfMoonLinkSoundManager.Instance.StopSpinMusic(soundName);
    }

    private void PlayWinMusic(string soundName)
    {
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsSoundMute())
            WolfMoonLinkSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (soundName == null || WolfMoonLinkSoundManager.Instance == null) return;

        if (!WolfMoonLinkSoundManager.Instance.IsSoundMute())
            WolfMoonLinkSoundManager.Instance.StopWinMusic(soundName);
    }

    public void SoundActive(bool soundActive)
    {
        WolfMoonLinkSoundManager.Instance.MuteSFX(!soundActive);

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
        WolfMoonLinkSoundManager.Instance.MuteMusic(!musicActive);

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

        SceneManagement.GoBackToMainMenu();
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

        SlotSpinService.Instance.Spin(betAmount);
        UpdateButtons("Spin");
    }

    private void OnClickStop()
    {
        StopSpinMusic("Spin");

        WolfMoonLinkSlotMachine.Instance.isStopBtnPressed = true;
        WolfMoonLinkSlotMachine.Instance.StopWithResult();

        if (WolfMoonLinkAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    public void OnClickAuto()
    {
        if (autoSpinController == null) return;

        StopCurrentSFX();

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

    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;

        PlaySound("Button");

        autoSpinController.CancelAutoSpin();

        autoStopButton.SetActive(false);
        autoButton.SetActive(true);
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
        bool interactable = false;
        currentUIStateType = type;

        switch (type)
        {
            case "Spin":
                interactable = false;
                spinButton.SetActive(false);
                stopButton.SetActive(true);
                autoButton.SetInteractable(false);
                break;

            case "Stop":
                interactable = true;
                spinButton.SetActive(true);
                stopButton.SetActive(false);
                autoButton.SetInteractable(true);
                break;

            case "Auto":
                interactable = false;
                spinButton.SetActive(false);
                stopButton.SetActive(true);
                autoButton.SetActive(false);
                autoStopButton.SetActive(true);
                stopButton.SetInteractable(false);
                break;

            case "Auto Stop":
                interactable = true;
                spinButton.SetActive(true);
                stopButton.SetActive(false);
                autoButton.SetActive(true);
                autoStopButton.SetActive(false);
                SetAutoInteractable(true);
                break;

            case "WinAnimation":
                interactable = false;
                spinButton.SetActive(true);
                stopButton.SetActive(false);
                autoButton.SetActive(true);
                SetAutoInteractable(false);
                break;

            case "Auto Win Animation":
                interactable = false;
                spinButton.SetActive(false);
                stopButton.SetActive(true);
                SetAutoInteractable(false);
                autoStopButton.SetActive(true);
                break;

            default:
                return;
        }

        spinButton.SetInteractable(interactable);

        exitGameButton.interactable = interactable;
        increaseBetButton.SetInteractable(interactable);
        decreaseBetButton.SetInteractable(interactable);
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
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimate.text = FormatFloorValue(target);
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

        if (WolfMoonLinkAutoSpinController.isAutoSpinning)
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
        stopButton.SetInteractable(state);
    }

    public void SetAutoInteractable(bool state)
    {
        autoButton.SetInteractable(state);
    }

    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    #endregion
}