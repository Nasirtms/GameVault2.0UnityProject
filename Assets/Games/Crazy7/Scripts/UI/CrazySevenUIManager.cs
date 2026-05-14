using Coffee.UIEffects;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CrazySevenBetController))]
[RequireComponent(typeof(CrazySevenMenuPanelController))]
[RequireComponent(typeof(CrazySevenRulesPopupController))]
[RequireComponent(typeof(CrazySevenAutoSpinController))]
[RequireComponent(typeof(CrazySevenGameTransitionController))]
public class CrazySevenUIManager : GameBetServices
{
    #region variables

    public static CrazySevenUIManager Instance;
    public CoinManager coinManager;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text username;
    [SerializeField] private TMP_Text coins;
    [SerializeField] public TMP_Text Coin_text;
    [SerializeField] private TMP_Text winAmount;
    [SerializeField] private float duration = 1.5f;
    
    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public CrazySevenUIButtonController spinButton;
    [SerializeField] public CrazySevenUIButtonController stopButton;
    [SerializeField] public CrazySevenUIButtonController autoButton;
    [SerializeField] public CrazySevenUIButtonController cancelButton;

    [Header("Menu Buttons")]
    [SerializeField] private Button openMenuButtton;
    [SerializeField] private Button closeMenuButtton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button musicControlButton;
    [SerializeField] private Button soundControlButton;
    [SerializeField] private Button openRulesButton;
    private bool soundActive;
    private bool musicActive;

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

    [Header("Free Game")]
    [SerializeField] TMP_Text freeGameWinAmountText;
    [HideInInspector] public int freeGameSpinCount;
    [HideInInspector] public float freeGameWinAmount;


    private CrazySevenBetController betController;
    private CrazySevenMenuPanelController menuPanelController;
    private CrazySevenRulesPopupController rulesPopupController;
    public CrazySevenAutoSpinController autoSpinController;
    private CrazySevenGameTransitionController gameTransitionController;

    private float currentSpinWin;
    public Coroutine textAnimationCoroutine;
    public Coroutine CointextAnimationCoroutine;
    public bool StopCoinCounter;
    [HideInInspector] public bool singleSpin;
    public bool autoSpin;
    private string currentButtonSet;
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
        betController = GetComponent<CrazySevenBetController>();
        menuPanelController = GetComponent<CrazySevenMenuPanelController>();
        rulesPopupController = GetComponent<CrazySevenRulesPopupController>();
        autoSpinController = GetComponent<CrazySevenAutoSpinController>();
        gameTransitionController = GetComponent<CrazySevenGameTransitionController>();

        username.text = UserManager.Instance.Username;
        soundActive = true;
        musicActive = true;
        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }

    private void OnDestroy()
    {
        RemoveListeners(spinButton?.GetButtonComponent());
        RemoveListeners(stopButton?.GetButtonComponent());
        RemoveListeners(autoButton?.GetButtonComponent());
        RemoveListeners(cancelButton?.GetButtonComponent());

        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);
        RemoveListeners(openMenuButtton);
        RemoveListeners(closeMenuButtton);
        RemoveListeners(exitGameButton);
        RemoveListeners(musicControlButton);
        RemoveListeners(soundControlButton);
        RemoveListeners(openRulesButton);
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;
    }

    private void RemoveListeners(Button button)
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    #endregion

    #region Input Buttons
    private void SetupInputButtons()
    {
        spinButton.GetButtonComponent().onClick.AddListener(OnClickSpin);
        stopButton.GetButtonComponent().onClick.AddListener(OnClickStop);
        autoButton.GetButtonComponent().onClick.AddListener(OnClickAuto);
        cancelButton.GetButtonComponent().onClick.AddListener(OnClickCancel);

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);

        openMenuButtton.onClick.AddListener(OpenMenuPanel);
        closeMenuButtton.onClick.AddListener(CloseMenuPanel);
        exitGameButton.onClick.AddListener(ExitGame);
        musicControlButton.onClick.AddListener(ToggleMusic);
        soundControlButton.onClick.AddListener(ToggleSound);
        openRulesButton.onClick.AddListener(OpenRulesPopup);
    }
    #region Sound
    public void PlaySound(string soundName)
    {
        if (soundName == null) return;
        if (!CrazySevenSoundManager.Instance.IsSoundMute())
            CrazySevenSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (soundName == null) return;
        if (!CrazySevenSoundManager.Instance.IsMusicMute())
            CrazySevenSoundManager.Instance.PlayMusic(soundName);
    }
    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CrazySevenSoundManager.Instance.IsMusicMute())
            CrazySevenSoundManager.Instance.StopMusic(soundName);
    }

    public void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CrazySevenSoundManager.Instance.IsMusicMute())
            CrazySevenSoundManager.Instance.PlayWinMusic(soundName);
    }

    private void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!CrazySevenSoundManager.Instance.IsMusicMute())
            CrazySevenSoundManager.Instance.StopWinMusic(soundName);
    }
    private void ToggleMusic()
    {
        if (menuPanelController == null) return;
        PlaySound("Crazy_7_Button");
        menuPanelController.MusicActive(musicActive);
        musicActive = !musicActive;
    }

    private void ToggleSound()
    {
        if (menuPanelController == null) return;
        PlaySound("Crazy_7_Button");
        menuPanelController.SoundActive(soundActive);
        soundActive = !soundActive;
    }
    #endregion

    #region Bet & Buttons
    private void IncreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Crazy_7_Increase_Button");
        betController.IncreaseChipValue();
    }

    private void DecreaseBetAmount()
    {
        if (betController == null) return;
        PlaySound("Crazy_7_Decrease_Button");
        betController.DecreaseChipValue();
    }

    private void OpenMenuPanel()
    {
        if (menuPanelController == null) return;
        PlaySound("Crazy_7_Button");
        menuPanelController.ShowMenuPanel();
    }

    private void CloseMenuPanel()
    {
        if (menuPanelController == null) return;
        PlaySound("Crazy_7_Button");
        menuPanelController.ShowMenuPanel();
    }

    private void ExitGame()
    {
        PlaySound("Crazy_7_Button");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
    }
    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        PlaySound("Crazy_7_Button");
        rulesPopupController.OpenPopup();
    }

    private void OnClickSpin()
    {
        CrazySevenSlotMachine.Instance.HideWinBanner();

        if (CrazySevenSlotMachine.Instance.InSpin) return;

        CrazySevenPaylineController.Instance.StopPaylines();
        CrazySevenPaylineController.Instance.ClearPaylineData();

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Crazy_7_Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        StopAllCoroutines();
        StopCoinCounter = true;
        winAmount.text = "0.00";

        UserManager.Instance.currentBetAmount = betAmount;
        PlaySound("Crazy_7_Spin");
        //PlayMusic("Crazy_7_Slot_Machine");

        UpdateButtons("Spin Start");

        UIShiny uIShiny = spinButton.gameObject.GetComponent<UIShiny>();
        if(uIShiny == null)
        {
            spinButton.gameObject.AddComponent<UIShiny>();
        }
        if (uIShiny != null)
        {
            uIShiny.enabled = false;
            
        }
        SlotSpinService.Instance.Spin(betAmount); 
    }

    private void OnClickStop()
    {
        CrazySevenSlotMachine.Instance.ToggleAnimator(false);
        if (CrazySevenSlotMachine.Instance.uIShiny != null)
        {
            CrazySevenSlotMachine.Instance.uIShiny.enabled = true;
        }
        PlaySound("Crazy_7_Button");
        StopMusic("Crazy_7_Slot_Machine");

        CrazySevenSlotMachine.Instance.isStopBtnPressed = true;
        CrazySevenSlotMachine.Instance.StopWithResult();
        CrazySevenSlotMachine.Instance.InvokeStop();

        UpdateButtons("Spin Stop");
    }

    public void ToggleSpinButton()
    {
        OnClickStop();
    }
    private void OnClickAuto()
    {
        if (CrazySevenSlotMachine.Instance.uIShiny != null)
        {
            CrazySevenSlotMachine.Instance.uIShiny.enabled = false;
        }
        CrazySevenSlotMachine.Instance.HideWinBanner();
        if (autoSpinController == null) return;
        CrazySevenPaylineController.Instance?.StopPaylines();
        CrazySevenPaylineController.Instance?.ClearPaylineData();
        PlaySound("Crazy_7_Spin");
        //PlayMusic("Crazy_7_Slot_Machine");
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            StopWinMusic("Crazy_7_Win");
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        float betAmount = betController.GetCurrentBet();
        winAmount.text = "0.00";
        if (autoSpinController.isAutoRunning)
        {
            autoSpinController.CancelAutoSpin(); 
        }
        //UpdateButtons("Auto Spin");
        CrazySevenSlotMachine.Instance.isStopBtnPressed = false;
        CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
        autoSpinController.StartAutoSpin(betAmount);
    }

    private void OnClickCancel()
    {
        CrazySevenSlotMachine.Instance.ToggleAnimator(false);
        if (CrazySevenSlotMachine.Instance.uIShiny != null)
        {
            CrazySevenSlotMachine.Instance.uIShiny.enabled = true;
        }

        if (autoSpinController == null) return;

        CrazySevenSlotMachine.Instance.isStopBtnPressed = true;
        PlaySound("Crazy_7_Button");
        StopMusic("Crazy_7_Slot_Machine");

        UpdateButtons("Cancel Auto");
        //ToggleSpinButton();

        if (coinManager != null)
            coinManager.StopBurstCoins();

        StopCoinCounter = true;
        CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
        autoSpinController.CancelAutoSpin();
        CrazySevenSlotMachine.Instance.InvokeStop();
    }
    #endregion

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
                spinButton.ShowButton(false);
                stopButton.ShowButton(true);
                autoButton.ShowButton(true);
                autoButton.GetButtonComponent().interactable = false;
                break;

            case "Spin Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                autoButton.ShowButton(true);
                autoButton.GetButtonComponent().interactable = true;
                break;

            case "Auto Spin":
                inSpin = true;
                spinButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = false;
                autoButton.ShowButton(false);
                break;

            case "Cancel Auto":
                inSpin = false;
                spinButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = true;
                autoButton.ShowButton(true);
                break;
            case "Free Spin":
                inSpin = true;
                spinButton.GetButtonComponent().interactable = false;
                autoButton.SetButtonInteractable(false);
                break;
            case "FreeSpin Stop":
                inSpin = false;
                spinButton.GetButtonComponent().interactable = true;
                autoButton.SetButtonInteractable(true);
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
    private string FormatFloorValue(float value)
    {
        float floored = Mathf.Floor(value * 100f) / 100f;
        return floored.ToString("0.00");
    }
    public void UpdateWinAmount(float winAmount)
    {
        if (winAmount > 0)
        {
            currentSpinWin = winAmount;
            PlayTextAnimation(currentSpinWin);
        }
        else
        {
            currentSpinWin = 0;
            this.winAmount.text = "0.00";
        }
    }
    public void PlayFreeGameWinAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        freeGameWinAmount += winAmount;
        textAnimationCoroutine = StartCoroutine(AnimateToValue(freeGameWinAmount, 1f, freeGameWinAmountText));

        UpdateWinAmount(freeGameWinAmount);
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
    public void PlayCoinTextAnimation(float winAmount)
    {
        if (CointextAnimationCoroutine != null)
            StopCoroutine(CointextAnimationCoroutine);

        CointextAnimationCoroutine = StartCoroutine(AnimateToValueTo(winAmount, 1f, this.Coin_text, false));
    }
    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        PlayWinMusic("Crazy_7_Win");
        textAnimationCoroutine = StartCoroutine(AnimateToValueTo(winAmount, 1f, this.winAmount, true));
    }
    private IEnumerator AnimateToValueTo(float target, float duration, TMP_Text textToAnimate, bool bottomText)
    {
        float startValue = 0f;

        if (!string.IsNullOrEmpty(textToAnimate.text) && float.TryParse(textToAnimate.text, out float current))
        {
            startValue = current;
        }
        
        float timer = 0f;

        while (timer < duration)
        {
            if (!bottomText && StopCoinCounter)
            {
                break;
            }
            float t = timer / duration;
            float displayed = Mathf.Lerp(startValue, target, t);
            //textToAnimate.text = displayed.ToString("0.00");
            textToAnimate.text = FormatFloorValue(displayed);

            timer += Time.deltaTime;
            yield return null;
        }

        //textToAnimate.text = target.ToString("0.00");
        textToAnimate.text = FormatFloorValue(target);

        if (bottomText)
        {
            StopWinMusic("Crazy_7_Win");
            PlaySound("Crazy_7_WinEnd");
        }    
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
    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }
    public void SetAutoStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }

    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }
    #endregion

    #region freeSpinhelper
    public void ExtraFreeSpin()
    {
        gameTransitionController.UpdateFreeSpins(freeGameSpinCount);
    }
    #endregion
}