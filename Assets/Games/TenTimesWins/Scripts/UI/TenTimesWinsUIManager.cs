using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(TenTimesWinsBetController))]
[RequireComponent(typeof(TenTimesWinsRulesPopupController))]
[RequireComponent(typeof(TenTimesWinsAutoSpinController))]
public class TenTimesWinsUIManager : GameBetServices
{
    #region Variables

    public static TenTimesWinsUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] private Button decreaseBetButton;
    [SerializeField] private Button increaseBetButton;

    [Header("Multiplier")]
    [SerializeField] private Button increaseMultiplierButton;
    [SerializeField] private Button decreaseMultiplierButton;

    [Header("Spin Buttons")]
    [SerializeField] public TenTimesWinsUIButtonController spinButton;
    [SerializeField] private TenTimesWinsUIButtonController stopButton;

    [Header("Auto Spin Popup")]
    [SerializeField] private GameObject autoSpinPopupPanel;
    [SerializeField] private Button autoSpin10Button;
    [SerializeField] private Button autoSpin50Button;
    [SerializeField] private Button autoSpin100Button;
    [SerializeField] private Button autoSpin200Button;
    [SerializeField] private Button autoSpinInfinityButton;
    [SerializeField] private TMP_Text remainingSpins;

    [Header("Menu Buttons")]
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button openRulesButton;

    [Header("Sound and Music")]
    [SerializeField] private Sprite soundOffSprite;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Sprite musicOnSprite;
    private bool soundOn;
    private bool musicOn;
    private int reelsStopped = 0;
    private int totalReels = 3; // adjust if needed
    private bool hasStoppedSpinSound = false;
    private bool hasStoppedReelStopSFX = false;

    [Header("Win Animations")]
    [SerializeField] private List<Animator> winAnimators;
    [SerializeField] private GameObject winAnimations;
    [SerializeField] private GameObject bigWin;
    [SerializeField] private GameObject niceWin;
    [SerializeField] private TMP_Text niceWinText;
    public Coroutine winCoroutine;
    [SerializeField] private GameObject megaWin;
    [SerializeField] private GameObject superWin;
    [SerializeField] private GameObject jackpotWin;
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

    [Header("Free Game")]


    [Header("Free Win Indicator")]

    public Coroutine textAnimationCoroutine;

    private TenTimesWinsBetController betController;
    private TenTimesWinsRulesPopupController rulesPopupController;
    private TenTimesWinsAutoSpinController autoSpinController;

    [HideInInspector] public bool autoSpin;
    private string currentButtonSet;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }
    private void Update()
    {
        if (autoSpinPopupPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            bool clickedInside = false;
            foreach (var r in results)
            {
                if (r.gameObject == autoSpinPopupPanel || r.gameObject.transform.IsChildOf(autoSpinPopupPanel.transform))
                {
                    clickedInside = true;
                    break;
                }
            }

            if (!clickedInside)
            {
                autoSpinPopupPanel.SetActive(false);
                spinButton.SetButtonInteractable(true);
            }
        }
    }
    private void Start()
    {
        betController = GetComponent<TenTimesWinsBetController>();
        rulesPopupController = GetComponent<TenTimesWinsRulesPopupController>();
        autoSpinController = GetComponent<TenTimesWinsAutoSpinController>();

        autoSpinPopupPanel.SetActive(false);
        TenTimesWinsReelScript.OnSpinStart += HandleReelStart;
        TenTimesWinsReelScript.OnSpinComplete += HandleReelStop;
        hasStoppedSpinSound = false;
        hasStoppedReelStopSFX = false;
        reelsStopped = 0;
        soundOn = true;
        musicOn = true;
        if (!TenTimesWinsSoundManager.Instance.IsMusicMute())
        {
            PlayMusic("Background");
        }
        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;

        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
    }

    private void OnDestroy()
    {
        //spinButton?.GetButtonComponent().onClick.RemoveAllListeners();
        RemoveListeners(stopButton?.GetButtonComponent());

        RemoveListeners(increaseBetButton);
        RemoveListeners(decreaseBetButton);

        RemoveListeners(exitGameButton);
        RemoveListeners(openRulesButton);
        TenTimesWinsReelScript.OnSpinStart -= HandleReelStart;
        TenTimesWinsReelScript.OnSpinComplete -= HandleReelStop;
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

        autoSpin10Button.onClick.AddListener(() => OnAutoSpinOptionSelected(10));
        autoSpin50Button.onClick.AddListener(() => OnAutoSpinOptionSelected(50));
        autoSpin100Button.onClick.AddListener(() => OnAutoSpinOptionSelected(100));
        autoSpin200Button.onClick.AddListener(() => OnAutoSpinOptionSelected(200));
        autoSpinInfinityButton.onClick.AddListener(() => OnAutoSpinOptionSelected(-1));

        increaseBetButton.onClick.AddListener(IncreaseBetAmount);
        decreaseBetButton.onClick.AddListener(DecreaseBetAmount);
        increaseMultiplierButton.onClick.AddListener(IncreaseMultiplier);
        decreaseMultiplierButton.onClick.AddListener(DecreaseMultiplier);

        exitGameButton.onClick.AddListener(ExitGame);
        openRulesButton.onClick.AddListener(OpenRulesPopup);
    }

    #endregion

    #region Input Buttons

    #region Sound
    private void HandleReelStart(int index)
    {
        if (!hasStoppedSpinSound && index == 0)
        {
            hasStoppedSpinSound = true;
            TenTimesWinsSoundManager.Instance.StopReelStopSFX();
        }
    }
    private void HandleReelStop(int index)
    {
        TenTimesWinsSoundManager.Instance.PlayReelStopSFX("ReelStop");

        reelsStopped++;
        if (!hasStoppedReelStopSFX && reelsStopped >= totalReels)
        {
            hasStoppedReelStopSFX = true;
            TenTimesWinsSoundManager.Instance.StopReelStopSFX();
        }
    }
    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!TenTimesWinsSoundManager.Instance.IsSoundMute())
            TenTimesWinsSoundManager.Instance.PlaySFX(soundName);
    }

    private void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!TenTimesWinsSoundManager.Instance.IsMusicMute())
            TenTimesWinsSoundManager.Instance.PlayMusic(soundName);
    }
    public void SoundActive(bool soundActive)
    {
        TenTimesWinsSoundManager.Instance.MuteSFX(!soundActive);
        soundOn = !soundOn;
    }

    public void MusicActive(bool musicActive)
    {
        TenTimesWinsSoundManager.Instance.MuteMusic(!musicActive);
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
        PlaySound("Rules_Close");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
    }

    private void OpenRulesPopup()
    {
        if (rulesPopupController == null) return;
        PlaySound("Rules_Popup");
        rulesPopupController.OpenPopup();
    }

    #endregion
    
    #region Spin Buttons

    public void OnClickSpin()
    {
        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;

        PlaySound("Spin_Button");
        PlaySound("Spin1");
        hasStoppedSpinSound = false;
        hasStoppedReelStopSFX = false;
        reelsStopped = 0;

        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }

        if (autoSpinPopupPanel.activeSelf)
            autoSpinPopupPanel.SetActive(false);

        remainingSpins.gameObject.SetActive(false);
        SlotSpinService.Instance.Spin(betAmount);
        UpdateButtons("Single Start");
    }

    private void OnClickStop()
    {
        PlaySound("Spin_Button");
        TenTimesWinsSlotMachine.Instance.isStopBtnPressed = true;
        if (TenTimesWinsAutoSpinController.isAutoSpinning)
        {
            OnClickAutoStop();
        }
        if (!hasStoppedSpinSound)
        {
            hasStoppedSpinSound = false;
            TenTimesWinsSoundManager.Instance.StopReelStopSFX();
        }

        if (!hasStoppedReelStopSFX)
        {
            hasStoppedReelStopSFX = true;
            TenTimesWinsSoundManager.Instance.StopReelStopSFX();
        }
        TenTimesWinsSlotMachine.Instance.StopWithResult();
    }


    public void OnClickAuto()
    {
        if (autoSpinController == null) return;

        PlaySound("Auto_Button");
        remainingSpins.gameObject.SetActive(true);

        //Feedback-01 LovKumar 17/10/2025     
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        if (autoSpinPopupPanel.activeSelf == true)
        {
            autoSpinPopupPanel.SetActive(false);
            return;
        }

        autoSpinPopupPanel.SetActive(true);
    }

    private void OnClickAutoStop()
    {
        if (autoSpinController == null) return;
        autoSpinController.CancelAutoSpin();
        remainingSpins.gameObject.SetActive(false);

        UpdateButtons("Single Stop");
        if (!TenTimesWinsSlotMachine.Instance.isPaylineCompleted)
        {
            TenTimesWinsAutoSpinController.isAutoSpinning = false;
        }
    }

    private void OnAutoSpinOptionSelected(int spinCount)
    { 
        autoSpinPopupPanel.SetActive(false);

        float betAmount = betController.GetCurrentBet();
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        if (winCoroutine != null)
        {
            StopCoroutine(winCoroutine);
        }
        autoSpinController.SetSpinCount(spinCount);
        autoSpinController.StartAutoSpin(betAmount);
    }

    #endregion

    #region Multiplier Control

    private void IncreaseMultiplier()
    {
        PlaySound("Increase");
        if (betController == null) return;
        betController.IncreaseMultiplier();
    }

    private void DecreaseMultiplier()
    {
        PlaySound("Decrease");
        if (betController == null) return;
        betController.DecreaseMultiplier();
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

    public void UpdateRemainingSpins(int remainingSpins)
    {
        if (remainingSpins == -1)
        {
            this.remainingSpins.text = "<sprite name=Infinity>";
            return;
        }

        this.remainingSpins.text = remainingSpins.ToString();
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
                spinButton.GetButtonComponent().interactable = !inSpin;
                break;

            case "Single Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                //stopButton.ShowButton(false);
                spinButton.GetButtonComponent().interactable = !inSpin;
                break;

            case "Auto Start":
                inSpin = true;
                spinButton.ShowButton(false);
                //stopButton.ShowButton(true);
                break;

            case "Auto Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                //stopButton.ShowButton(false);
                break;

            case "Free Spin":
                inSpin = true;
                break;

            default:
                return;
        }

        if (inSpin)
        {
            exitGameButton.interactable = !inSpin;
            increaseBetButton.interactable = !inSpin;
            decreaseBetButton.interactable = !inSpin;
            increaseMultiplierButton.interactable = !inSpin;
            decreaseMultiplierButton.interactable = !inSpin;
            openRulesButton.interactable = !inSpin;
        }
        else
        {
            exitGameButton.interactable = !inSpin;
            increaseBetButton.interactable = !inSpin;
            decreaseBetButton.interactable = !inSpin;
            increaseMultiplierButton.interactable = !inSpin;
            decreaseMultiplierButton.interactable = !inSpin;
            openRulesButton.interactable = !inSpin;
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
            PlayTextAnimation(winAmount);
        }
        else
        {
            this.winAmount.text = "0.00";
        }
    }

    public void PlayFreeGameWinAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);
    }

    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount));
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

        // Ensure final value is exact
        //textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);
        StopCoroutine(textAnimationCoroutine);
    }
    #endregion

    #region Helper Functions

    public string CurrentButtonSet()
    {
        return currentButtonSet;
    }

    public void HideSpinCount()
    {
        //autoSpinCountPanel.SetActive(false);
    }

    public bool GetStopInteractable()
    {
        Debug.Log("Stop Button Active: " + stopButton.gameObject.activeSelf);
        bool state = stopButton.GetButtonComponent().interactable;
        Debug.Log("Stop Button Interactable: " + state);
        return state;
    }

    public void SetStopInteractable(bool state)
    {
        stopButton.GetButtonComponent().interactable = state;
    }

    public void PlayMegaWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(megaWin, megaWinText, winAmount, 2, megaWinTrigger));
    }

    #region Win Animations

    public void PlayNiceWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(niceWin, niceWinText, winAmount, 0, niceWinTrigger));
    }
    public void CancelAutoSpin()
    {
        autoSpinController.CancelAutoSpin();
    }

    #endregion
    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }

    public void PlaySuperWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(superWin, superWinText, winAmount, 3, superWinTrigger));
    }

    public void PlayBigWinAnimation(float winAmount)
    {
        winCoroutine = StartCoroutine(WinAnimation(bigWin, bigWinText, winAmount, 1, bigWinTrigger));
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

        // Start win amount counting animation
        textAnimationCoroutine = StartCoroutine(AnimateToValue(targetAmount, 2f, this.winAmount, winText));

        // Start looping scale animation
        Tween scaleTween = winText.transform.DOScale(1.2f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(3.5f);

        // Stop the scale tween smoothly
        scaleTween.Kill();
        winText.transform.localScale = Vector3.one; // reset scale

        animator.enabled = false;
        winType.SetActive(false);
        winAnimations.SetActive(false);

        winAnimationCompleted = true;

        StopCoroutine(winCoroutine);
    }



    #endregion
}