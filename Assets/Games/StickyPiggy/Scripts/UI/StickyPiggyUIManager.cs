using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(StickyPiggyBetController))]
[RequireComponent(typeof(StickyPiggyAutoSpinController))]
public class StickyPiggyUIManager : GameBetServices
{
    #region Variables

    public static StickyPiggyUIManager Instance;

    [SerializeField] private StickyPiggyRulesPopupController rulesPopupController;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] private StickyPiggyUIButtonController spinButton;
    [SerializeField] private StickyPiggyUIButtonController stopButton;

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

    private StickyPiggyBetController betController;
    private StickyPiggyAutoSpinController autoSpinController;

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
        betController = GetComponent<StickyPiggyBetController>();
        autoSpinController = GetComponent<StickyPiggyAutoSpinController>();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);

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
        if (!StickyPiggySoundManager.Instance.IsSoundMute())
            StickyPiggySoundManager.Instance.PlaySFX(soundName);
    }
    public void StopCurrentSFX()
    {
        if (!StickyPiggySoundManager.Instance.IsSoundMute())
            StickyPiggySoundManager.Instance.StopSFX();
    }
    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StickyPiggySoundManager.Instance.IsMusicMute())
            StickyPiggySoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StickyPiggySoundManager.Instance.IsMusicMute())
            StickyPiggySoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StickyPiggySoundManager.Instance.IsSoundMute())
            StickyPiggySoundManager.Instance.SpinPlayMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StickyPiggySoundManager.Instance.IsSoundMute())
            StickyPiggySoundManager.Instance.SpinStopMusic(soundName);
    }

    private void SoundActive(bool soundActive)
    {
        StickyPiggySoundManager.Instance.MuteSFX(!soundActive);
        Image soundButtonImage = soundButton.GetComponent<Image>();

        if (soundActive)
            soundButtonImage.sprite = soundOnSprite;
        else
            soundButtonImage.sprite = soundOffSprite;

        soundOn = !soundOn;
    }

    private void MusicActive(bool musicActive)
    {
        StickyPiggySoundManager.Instance.MuteMusic(!musicActive);
        Image musicButtonImage = musicButton.GetComponent<Image>();

        if (musicActive)
            musicButtonImage.sprite = musicOnSprite;
        else
            musicButtonImage.sprite = musicOffSprite;

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
        if (StickyPiggySlotMachine.Instance.InSpin) return;

        StopCurrentSFX();
        PlaySound("Button");
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
        if (StickyPiggySlotMachine.Instance.isFreeGame)
            UpdateButtons("Free Spin");
        else
            UpdateButtons("Stop");

        if (StickyPiggyAutoSpinController.isAutoSpinning)
        {
            autoSpinController.CancelAutoSpin();
        }
        StickyPiggySlotMachine.Instance.isStopBtnPressed = true;
        StickyPiggySlotMachine.Instance.StopWithResult();
    }
    public void OnHoldSpin()
    {
        if (autoSpinController == null) return;

        UpdateButtons("Spin");
        StopCurrentSFX();
        float betAmount = betController.GetCurrentBet();

        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        if (winCoroutine != null)
            StopCoroutine(winCoroutine);

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
        bool interactable = false;

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

            case "Transition Start":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "Transition End":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "Free Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                break;

            case "Free Spin End":
                interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "WinAnimation":
                interactable = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;

            case "Auto Win Animation":
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

            PlayTextAnimation(currentSpinWin , false);
        }
        else
        {
            currentSpinWin = 0;
            this.winAmount.text = "0.00";
        }
    }

    private void PlayTextAnimation(float winAmount, bool useSpriteDigits)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        //PlaySound("Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount, useSpriteDigits));
    }

    public void TextAnimation(float target, float duration, TMP_Text textToAnimate, bool useSpriteDigits)
    {
        freeSpinWinTextCoroutine = StartCoroutine(AnimateToValue(target, duration, textToAnimate, useSpriteDigits));
    }

    private IEnumerator AnimateToValue(float target, float duration, TMP_Text textToAnimate, bool useSpriteDigits)
    {
        float startValue = useSpriteDigits
            ? GetNumberFromSpriteText(textToAnimate.text)
            : GetNumberFromNormalText(textToAnimate.text);

        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            float displayed = Mathf.Lerp(startValue, target, t);

            textToAnimate.text = useSpriteDigits
                ? ToSpriteDigits(displayed)
                : FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimate.text = useSpriteDigits
            ? ToSpriteDigits(target)
            : FormatFloorValue(target);
    }

    private float GetNumberFromNormalText(string text)
    {
        if (float.TryParse(text, out float value))
            return value;

        return 0f;
    }

    private float GetNumberFromSpriteText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        string number = text
            .Replace("<sprite index=0>", "0")
            .Replace("<sprite index=1>", "1")
            .Replace("<sprite index=2>", "2")
            .Replace("<sprite index=3>", "3")
            .Replace("<sprite index=4>", "4")
            .Replace("<sprite index=5>", "5")
            .Replace("<sprite index=6>", "6")
            .Replace("<sprite index=7>", "7")
            .Replace("<sprite index=8>", "8")
            .Replace("<sprite index=9>", "9")
            .Replace("<sprite index=10>", ".");

        if (float.TryParse(number, out float value))
            return value;

        return 0f;
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
    private string ToSpriteDigits(float value)
    {
        string s = FormatFloorValue(value);
        StringBuilder sb = new StringBuilder(s.Length * 10);

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (ch >= '0' && ch <= '9')
                sb.Append($"<sprite index={ch - '0'}>");
            else if (ch == '.')
                sb.Append("<sprite index=10>");
        }

        return sb.ToString();
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
        if (StickyPiggySlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition Start");
        }
        else if (StickyPiggyAutoSpinController.isAutoSpinning)
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