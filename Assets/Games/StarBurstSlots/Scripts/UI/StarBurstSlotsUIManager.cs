using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(StarBurstSlotsBetController))]
[RequireComponent(typeof(StarBurstSlotsRulesPopupController))]
[RequireComponent(typeof(StarBurstSlotsAutoSpinController))]
public class StarBurstSlotsUIManager : GameBetServices
{
    #region Variables

    public static StarBurstSlotsUIManager Instance;

    [Space(10)]
    [Header("User Details")]
    [SerializeField] private TMP_Text coins;

    [Header("Win")]
    [SerializeField] private TMP_Text winAmount;

    [Header("Bet Buttons")]
    [SerializeField] public Button decreaseBetButton;
    [SerializeField] public Button increaseBetButton;

    [Header("Spin Buttons")]
    [SerializeField] public StarBurstSlotsUIButtonController spinButton;
    [SerializeField] private StarBurstSlotsUIButtonController stopButton;

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

    [Header("Combo and Super Combo")]
    [SerializeField] public GameObject comboEffect;
    [SerializeField] public GameObject superComboEffect;

    private Coroutine freeSpinWinTextCoroutine;
    public Coroutine textAnimationCoroutine;
    private string currentButtonSet;

    private StarBurstSlotsBetController betController;
    private StarBurstSlotsRulesPopupController rulesPopupController;
    private StarBurstSlotsAutoSpinController autoSpinController;

    public bool hasShowenComboVfx;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        betController = GetComponent<StarBurstSlotsBetController>();
        rulesPopupController = GetComponent<StarBurstSlotsRulesPopupController>();
        autoSpinController = GetComponent<StarBurstSlotsAutoSpinController>();

        UpdateCoins();
        SetupInputButtons();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
        PlayMusic("Bg");
        GameBetServices.Instance.SetActiveUI(this, coins, UpdateCoins);
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
        if (!StarBurstSlotsSoundManager.Instance.IsSoundMute())
            StarBurstSlotsSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StarBurstSlotsSoundManager.Instance.IsMusicMute())
            StarBurstSlotsSoundManager.Instance.PlayMusic(soundName);
    }

    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StarBurstSlotsSoundManager.Instance.IsMusicMute())
            StarBurstSlotsSoundManager.Instance.StopMusic(soundName);
    }

    public void PlaySpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StarBurstSlotsSoundManager.Instance.IsSoundMute())
            StarBurstSlotsSoundManager.Instance.PlaySpinMusic(soundName);
    }

    public void StopSpinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StarBurstSlotsSoundManager.Instance.IsSoundMute())
            StarBurstSlotsSoundManager.Instance.StopSpinMusic(soundName);
    }
    private void PlayWinText(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StarBurstSlotsSoundManager.Instance.IsSoundMute())
            StarBurstSlotsSoundManager.Instance.PlayWinText(soundName);
    }

    private void StopWinText(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!StarBurstSlotsSoundManager.Instance.IsSoundMute())
            StarBurstSlotsSoundManager.Instance.StopWinText(soundName);
    }

    private void SoundActive(bool soundActive)
    {
        StarBurstSlotsSoundManager.Instance.MuteSFX(!soundActive);

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
        StarBurstSlotsSoundManager.Instance.MuteMusic(!musicActive);

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
        if (StarBurstSlotsSlotMachine.Instance.makeFreeGameReady)
        {
            return;
        }
        PlaySound("Button");

        float betAmount = betController.GetCurrentBet();
        if (!GameBetServices.Instance.TrySpinWithCurrentBet(betAmount)) return;


        PlaySpinMusic("ReelSpin");

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
        StopSpinMusic("ReelSpin");
        //if (TheGreenMachineDeluxeSoundManager.Instance != null && !TheGreenMachineDeluxeAutoSpinController.isAutoSpinning)
        //    TheGreenMachineDeluxeSoundManager.Instance.StopMusic("Spin1");

        StarBurstSlotsSlotMachine.Instance.isStopBtnPressed = true;
        if (StarBurstSlotsAutoSpinController.isAutoSpinning)
        {
            OnClickAutoStop();
        }
        StarBurstSlotsSlotMachine.Instance.StopWithResult();

        if (StarBurstSlotsAutoSpinController.isAutoSpinning)
        {
            SetStopInteractable(false);
        }
    }

    public void OnClickAuto()
    {
        if (StarBurstSlotsSlotMachine.Instance.makeFreeGameReady)
        {
            return;
        }

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

        if (StarBurstSlotsSlotMachine.Instance.InSpin)
        {
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
                break;

            case "Single Stop":
                inSpin = false;
                spinButton.ShowButton(true);
                //stopButton.ShowButton(false);
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
                SetAutoInteractable(true);
                break;
            case "WinAnimation":
                inSpin = false;
                spinButton.ShowButton(true);
                stopButton.ShowButton(false);
                spinButton.GetButtonComponent().interactable = false;
                break;
            case "Auto Win Animation":
                inSpin = false;
                spinButton.ShowButton(false);
                spinButton.GetButtonComponent().interactable = false;
                stopButton.ShowButton(true);
                break;
            case "Free Game Transition":
                inSpin = true;
                spinButton.ShowButton(true);
                spinButton.GetButtonComponent().interactable = false;
                stopButton.GetButtonComponent().interactable = false;
                break;

            case "Free Spin":
                inSpin = true;
                spinButton.ShowButton(false);
                break;

            case "Base Game Transition":
                inSpin = false;
                spinButton.ShowButton(true);
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
            //textToAnimate.text = displayed.ToString("0.00");
            textToAnimate.text = FormatFloorValue(displayed);
            timer += Time.deltaTime;
            yield return null;
        }
        textToAnimate.text = FormatFloorValue(target);
        //textToAnimate.text = target.ToString("0.00");

        if (!freeSpin)
        {
            StopWinText("WinText");
            PlaySound("WinTextEnd");
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

        // Ensure final value is exact
        textToAnimateOne.text = FormatFloorValue(target);
        textToAnimateTwo.text = FormatFloorValue(target);
        //textToAnimateOne.text = target.ToString("0.00");
        //textToAnimateTwo.text = target.ToString("0.00");

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

    public void SetAutoInteractable(bool state)
    {
        //autoButton.GetButtonComponent().interactable = state;
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
        if (StarBurstSlotsSlotMachine.Instance.isFreeGameReady)
        {
            UpdateButtons("Free Game Transition");
        }
        else if (StarBurstSlotsAutoSpinController.isAutoSpinning)
        {
            UpdateButtons("Auto Win Animation");
        }
        else
        {
            UpdateButtons("Win Animation");
        }
        StopCoroutine(winCoroutine);
    }

    public void ShowComboVfx(int slotCount)
    {
        if (!hasShowenComboVfx)
        {
            GameObject effectObj;

            if (slotCount > 2)
                effectObj = superComboEffect;
            else if (slotCount > 1)
                effectObj = comboEffect;
            else
                return;

            effectObj.SetActive(true);

            Transform effectTransform = effectObj.transform;
            effectTransform.localScale = Vector3.one;
            PlaySound("combo");
            Sequence seq = DOTween.Sequence();

            seq.Append(effectTransform.DOShakeScale(0.3f, 0.5f, 25)) // smaller strength looks better
               .AppendInterval(1f)
               .OnComplete(() =>
               {
                   effectObj.SetActive(false);
               });
           }
    }
    #endregion
}
