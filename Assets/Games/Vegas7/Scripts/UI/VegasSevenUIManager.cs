using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(VegasSevenBetController))]
[RequireComponent(typeof(VegasSevenRulesPopupController))]
[RequireComponent(typeof(VegasSevenAutoSpinController))]
public class VegasSevenUIManager : GameBetServices
{
    #region Variables

    public static VegasSevenUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public VegasSevenUIButtonController spinButton;
    [SerializeField] private VegasSevenUIButtonController stopButton;
    [SerializeField] public VegasSevenUIButtonController autoBtton;
    [SerializeField] public Sprite autoOnSprite;
    [SerializeField] public Sprite autoOffSprite;

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
    [SerializeField] public TMP_Text winAmount;

    [Header("LightONOFF")]
    [SerializeField] private GameObject lightOffObject1;
    [SerializeField] private GameObject lightOffObject2;
    [SerializeField] private GameObject lightOffObject3;
    [SerializeField] private GameObject lightOffObject1V2;
    [SerializeField] private GameObject lightOffObject2V2;
    [SerializeField] private GameObject lightOffObject3V2;
    private Sequence flickerSequence;

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
    [HideInInspector] public bool winAnimationCompleted = false;
    public Coroutine textAnimationCoroutine;

    // Scripts
    private VegasSevenBetController betController;
    private VegasSevenRulesPopupController rulesPopupController;
    private VegasSevenAutoSpinController autoSpinController;

    private float currentSpinWin;

    public string currentButtonSet;

    public string currentUIStateType;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        StartFlicker(0.5f);
        betController = GetComponent<VegasSevenBetController>();
        rulesPopupController = GetComponent<VegasSevenRulesPopupController>();
        autoSpinController = GetComponent<VegasSevenAutoSpinController>();

        soundOn = true;
        musicOn = true;
        SoundActive(soundOn);
        MusicActive(musicOn);

        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("Background");
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
    }

    private void OnDestroy()
    {
        StopFlicker();
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

    bool autoActive;
    void OnClickAuto() {
        if (!autoActive)
            OnHoldSpin();
        else
            autoSpinController.CancelAutoSpin();

        autoActive = !autoActive;
        PlaySound("Button");

    }

    private void SetupInputButtons()
    {

        spinButton.GetButtonComponent().onClick.AddListener(OnClickSpin);
        stopButton.GetButtonComponent().onClick.AddListener(OnClickStop);
        autoBtton.GetButtonComponent().onClick.AddListener(OnClickAuto);

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
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsSoundMute())
            VegasSevenSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsMusicMute())
            VegasSevenSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsMusicMute())
            VegasSevenSoundManager.Instance.StopMusic(soundName);
    }
    public void PlaySpinMusic(string soundName)
    {
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsSoundMute())
            VegasSevenSoundManager.Instance.PlaySpinMusic(soundName);
    }
    public void StopSpinMusic(string soundName)
    {
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsSoundMute())
            VegasSevenSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinMusic(string soundName)
    {
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsSoundMute())
            VegasSevenSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (soundName == null || VegasSevenSoundManager.Instance == null) return;
        if (!VegasSevenSoundManager.Instance.IsSoundMute())
            VegasSevenSoundManager.Instance.StopWinMusic(soundName);
    }
    public void SoundActive(bool soundActive)
    {
        VegasSevenSoundManager.Instance.MuteSFX(!soundActive);

        Image soundButtonImage = soundButton.transform.GetChild(0).GetComponent<Image>();

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
        VegasSevenSoundManager.Instance.MuteMusic(!musicActive);

        Image musicButtonImage = musicButton.transform.GetChild(0).GetComponent<Image>();

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
        if (VegasSevenSlotMachine.Instance.InSpin) return;

        if(VegasSevenSlotMachine.Instance.isFreeGame) return;
        float betAmount = betController.GetCurrentBet();

        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        //PlaySpinMusic("Spin");

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
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
        PlaySound("Stop");
        StopSpinMusic("Spin");
        if (VegasSevenSlotMachine.Instance.isFreeGame)
        {
            UpdateButtons("enterfreeSpin");
        }
        else
        {
            if (!VegasSevenAutoSpinController.isAutoSpinning)
                UpdateButtons("Stop");
        }
        VegasSevenSlotMachine.Instance.isStopBtnPressed = true;
        VegasSevenSlotMachine.Instance.StopWithResult();
        
        if (VegasSevenAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    public void OnHoldSpin()
    {
        if (VegasSevenSlotMachine.Instance.InSpin) return;
        autoBtton.gameObject.transform.GetComponent<Image>().sprite = autoOffSprite;
        if (autoSpinController == null) return;

        float betAmount = betController.GetCurrentBet();

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Win");
        }
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
            case "Default":
                interactable = true;
                autoBtton.GetButtonComponent().interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoBtton.ShowButton(true);
                break;

            case "Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoBtton.GetButtonComponent().interactable = false;
                break;

            case "Auto Spin":
                interactable = false;
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoBtton.GetButtonComponent().interactable = true;
                break;

            case "Stop":
                interactable = true;
                autoBtton.GetButtonComponent().interactable = true;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                break;
            case "Transition":
                interactable = false;
                spinButton.ShowButton(true);
                autoBtton.gameObject.transform.GetComponent<Image>().sprite = autoOnSprite;
                autoBtton.GetButtonComponent().interactable = false;
                stopButton.ShowButton(false);
                break;
            case "enterfreeSpin":
                interactable = false;
                spinButton.ShowButton(false);
                autoBtton.GetButtonComponent().interactable = false;
                stopButton.ShowButton(true);
                break;
            case "exitfreeSpin":
                interactable = true;
                spinButton.ShowButton(true);
                autoBtton.GetButtonComponent().interactable = true;
                stopButton.ShowButton(false);
                break;

            default:
                return;
        }
        spinButton.GetButtonComponent().interactable = interactable;

        exitGameButton.interactable = interactable;
        increaseBetButton.interactable = interactable;
        decreaseBetButton.interactable = interactable;
    }

    #endregion

    #region Text Animation

    public void UpdateWinAmount(float winAmount, bool compound)
    {
        //Debug.Log("Manhoos win Amount " + winAmount);
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

        PlayWinMusic("Win");
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
            textToAnimate.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimate.text = target.ToString("0.00");
        StopWinMusic("Win");
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

        // Make sure it ends exactly at endValue
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
        if (VegasSevenSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Transition");
        }
        else if (VegasSevenAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Auto Spin");
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
        stopButton.GetButtonComponent().interactable = state;
    }

    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    public void StartFlicker(float interval)
    {
        StopFlicker();

        flickerSequence = DOTween.Sequence()
            .SetLoops(-1, LoopType.Restart);

        flickerSequence.AppendCallback(() => SetLights(true));
        flickerSequence.AppendInterval(interval);

        flickerSequence.AppendCallback(() => SetLights(false));
        flickerSequence.AppendInterval(interval);
    }

    public void StopFlicker()
    {
        if (flickerSequence != null && flickerSequence.IsActive())
        {
            flickerSequence.Kill();
        }
    }

    private void SetLights(bool state)
    {
        lightOffObject1.SetActive(state);
        lightOffObject2.SetActive(state);
        lightOffObject3.SetActive(state);
        lightOffObject1V2.SetActive(!state);
        lightOffObject2V2.SetActive(!state);
        lightOffObject3V2.SetActive(!state);
    }

    #endregion
}
