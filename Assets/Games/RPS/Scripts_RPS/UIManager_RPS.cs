// UIManager.cs
using DG.Tweening;
using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager_RPS : MonoBehaviour
{
    public static UIManager_RPS Instance;
    [SerializeField] private GameManager_RPS gameManager;
    public WheelManager_RPS wheelManager;
    [SerializeField] private BetManager_RPS betManager;
    [Header("Text Fields")]
    [SerializeField] private TMP_Text BalanceText;
    [SerializeField] public TMP_Text BetAmountText;
    [SerializeField] private TMP_Text WinningAmountText;
    [SerializeField] private TMP_Text WheelLevelText;
    [SerializeField] private TMP_Text RubyValueText;
    [SerializeField] private TMP_Text DiamondValueText;
    [SerializeField] private List<TextMeshProUGUI> segmentTexts;

    [Header("Buttons")]
    [SerializeField] public Button playButton;
    [SerializeField] public Sprite playButtonSprite1;
    [SerializeField] public Sprite playButtonSprite2;
    [SerializeField] public Button rockButton;
    [SerializeField] public Button paperButton;
    [SerializeField] public Button scissorsButton;

    [Header("Menu UI")]
    [SerializeField] private GameObject[] rulePanels; // 0 = panel1, 1 = panel2, 2 = panel3
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Image musicToggleImage;   // ← The image you want to swap sprite on
    [SerializeField] private Sprite musicOnSprite;
    [SerializeField] private Sprite musicOffSprite;
    [SerializeField] private Toggle forceWinToggle;
    [SerializeField] private Toggle forceLoseToggle;
    [SerializeField] private Toggle forceTieToggle;
    [SerializeField] private Toggle stopSpin;
    [SerializeField] private InputField chosenInputField;

    [Header("Refs")]
    [SerializeField] private RectTransform messageRoot; // parent container (the Image's RectTransform)
    [SerializeField] private TMP_Text MessageText;
    [SerializeField] private Image messageBg;           // optional: the parent Image (not required)

    [Header("Anim Settings")]
    [SerializeField] private float yRise = 1250f;        // how far it moves up (in UI pixels)
    [SerializeField] private float inDuration = 0.3f;   // slide+fade in time
    [SerializeField] private float holdTime = 1.2f;     // how long to stay visible
    [SerializeField] private float outDuration = 0.35f; // fade out time
    [SerializeField] private Ease inEase = Ease.OutBack;
    [SerializeField] private Ease outEase = Ease.OutQuad;
    private CanvasGroup cg;
    private Sequence seq;
    private Vector2 baseAnchoredPos;

    public GameSettings_RPS settings;

    private Coroutine winAnimationCoroutine;
    private int currentRuleIndex = 0;
    private void Start()
    {
        GameBetServices.Instance.SetActiveUI(this, BalanceText, UpdateCoins);

        OnToggleValueChanged(musicToggle.isOn);
        forceWinToggle.onValueChanged.AddListener(OnForceWinToggle);
        forceLoseToggle.onValueChanged.AddListener(OnForceLoseToggle);
        forceTieToggle.onValueChanged.AddListener(OnForceTieToggle);
        stopSpin.onValueChanged.AddListener(OnStopSpinToggle);
        UpdateCoins();
        // Subscribe to the toggle's event
        musicToggle.onValueChanged.AddListener(OnToggleValueChanged);
        chosenInputField.onEndEdit.AddListener(OnChosenIndexChanged);
        PlayMusic("Background");
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }
    private void Destroy()
    {
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;

    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        if (!messageRoot) messageRoot = GetComponent<RectTransform>();
        if (!MessageText) MessageText = GetComponentInChildren<TMP_Text>(true);
        cg = messageRoot.GetComponent<CanvasGroup>();
        if (!cg) cg = messageRoot.gameObject.AddComponent<CanvasGroup>();

        baseAnchoredPos = messageRoot.anchoredPosition;
        // start hidden
        cg.alpha = 0f;
        messageRoot.gameObject.SetActive(false);


        //ApiHandler.instance?.GameStarted(SceneManagement.currentGameID);
    }
    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsSoundMute())
        {
            AudioManager_RPS.Instance.PlaySFX(soundName);
        }
    }
    public void PlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsMusicMute())
        {
            AudioManager_RPS.Instance.PlayMusic(soundName);

        } 
    }
    public void StopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsMusicMute())
            AudioManager_RPS.Instance.StopMusic(soundName);
    }
    public void PlayWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsMusicMute())
        {
            AudioManager_RPS.Instance.WinPlayMusic(soundName);

        }
    }
    public void StopWinMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsSoundMute())
            AudioManager_RPS.Instance.WinStopMusic(soundName);
    }
    public void WheelSpinPlayMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsMusicMute())
        {
            AudioManager_RPS.Instance.WheelPlayMusic(soundName);

        }
    }
    public void WheelSpinStopMusic(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;
        if (!AudioManager_RPS.Instance.IsMusicMute())
            AudioManager_RPS.Instance.WheelStopMusic(soundName);
    }
    public void UpdateCoins()
    {
        if (UserManager.Instance != null)
        {
            BalanceText.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }

    }
    public void UpdateBet()
    {
        if (BetAmountText != null)
            BetAmountText.text = betManager.CurrentBet.ToString("F2");
    }
    public void UpdateWinning(float amount)
    {
        if (WinningAmountText != null)
            WinningAmountText.text = amount.ToString("F2");
    }
    public void UpdateWheelLevel(int level)
    {
        if (WheelLevelText != null)
            WheelLevelText.text = "Wheel Level " + level;
    }

    public void SetMessage(string text)
    {
        MessageText.text = text;

        if (seq != null && seq.IsActive()) seq.Kill();
        DOTween.Kill(messageRoot);

        messageRoot.gameObject.SetActive(true);
        messageRoot.anchoredPosition = baseAnchoredPos - new Vector2(0f, yRise * 0.45f);
        cg.alpha = 0f;

        seq = DOTween.Sequence();

        // slide + fade in
        seq.Join(messageRoot.DOAnchorPos(baseAnchoredPos, inDuration).SetEase(inEase));
        seq.Join(cg.DOFade(1f, inDuration));

        seq.AppendInterval(holdTime); //hold

        // fade out + small rise for a nicer exit
        seq.Append(messageRoot.DOAnchorPos(baseAnchoredPos + new Vector2(0f, yRise * 0.25f), outDuration).SetEase(outEase));
        seq.Join(cg.DOFade(0f, outDuration).SetEase(Ease.InOutSine));

        seq.OnComplete(() =>
        {
            messageRoot.anchoredPosition = baseAnchoredPos;
            messageRoot.gameObject.SetActive(false);
        });
    }
    public void HideMessageImmediate()
    {
        if (seq != null && seq.IsActive()) seq.Kill();
        cg.alpha = 0f;
        messageRoot.anchoredPosition = baseAnchoredPos;
        messageRoot.gameObject.SetActive(false);
    }
    public void SetPlayInteractable(bool enabled)
    {
        if (playButton != null)
            playButton.interactable = enabled;
        if (playButton.interactable == false)
        {
            playButton.image.sprite = playButtonSprite2;
        }
    }

    public void SetChoiceInteractable(bool enabled)
    {
        if (rockButton != null)
        {
            rockButton.interactable = enabled;
        }
        if (paperButton != null)
        {
            paperButton.interactable = enabled;

        }
        if (scissorsButton != null)
        {
            scissorsButton.interactable = enabled;
        }
    }
    public void UpdateCurrencyDisplays(float rubyValue, float diamondValue)
    {
        if (RubyValueText != null)
            RubyValueText.text = "" +rubyValue.ToString("F2");
        if (DiamondValueText != null)
            DiamondValueText.text = "" + diamondValue.ToString("F2");
    }
    public void UpdateSegmentTexts(float currentBet, float[] multipliers)
    {
        for (int i = 0; i < segmentTexts.Count && i < multipliers.Length; i++)
        {
            segmentTexts[i].text = (multipliers[i] * currentBet).ToString("F2");
        }
    }
    public void OnHomeButtonClicked()
    {
        PlaySound("Click");
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
    }
    public void OnRulesButtonClicked()
    {
        currentRuleIndex = 0;
        ShowRulePanel(currentRuleIndex);
    }
    public void OnRuleForwardClicked()
    {
        PlaySound("Click");
        rulePanels[currentRuleIndex].SetActive(false);
        currentRuleIndex = (currentRuleIndex + 1) % rulePanels.Length;
        ShowRulePanel(currentRuleIndex);
    }

    public void OnRuleBackwardClicked()
    {
        PlaySound("Click");
        rulePanels[currentRuleIndex].SetActive(false);
        currentRuleIndex = (currentRuleIndex - 1 + rulePanels.Length) % rulePanels.Length;
        ShowRulePanel(currentRuleIndex);
    }

    public void OnRuleExitClicked()
    {
        PlaySound("Click");
        rulePanels[currentRuleIndex].SetActive(false);
        currentRuleIndex = 0;
        // Return to main view (menu button area)
    }
    private void ShowRulePanel(int index)
    {
        for (int i = 0; i < rulePanels.Length; i++)
        {
            rulePanels[i].SetActive(i == index);
        }
    }
    private void OnDestroy()
    {
        //Screen.orientation = ScreenOrientation.LandscapeLeft;
        musicToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }
    public void OnToggleValueChanged(bool isOn)
    {
        if (musicToggleImage != null)
        {

            if (isOn)
            {
                PlaySound("Click");
                PlayMusic("Background");
                musicToggleImage.sprite = musicOnSprite;
            }
            else
            {
                StopMusic("Background");
                musicToggleImage.sprite = musicOffSprite;
            }
        }
    }
    private void OnForceWinToggle(bool isOn)
    {
        if (isOn)
        {
            gameManager.forceWin = true;
            gameManager.forceLose = false;
            gameManager.forceTie = false;

            forceLoseToggle.isOn = false;
            forceTieToggle.isOn = false;
        }
        else if (!forceLoseToggle.isOn && !forceTieToggle.isOn)
        {
            gameManager.forceWin = false;
        }
    }

    private void OnForceLoseToggle(bool isOn)
    {
        if (isOn)
        {
            gameManager.forceWin = false;
            gameManager.forceLose = true;
            gameManager.forceTie = false;

            forceWinToggle.isOn = false;
            forceTieToggle.isOn = false;
        }
        else if (!forceWinToggle.isOn && !forceTieToggle.isOn)
        {
            gameManager.forceLose = false;
        }
    }

    private void OnForceTieToggle(bool isOn)
    {
        if (isOn)
        {
            gameManager.forceWin = false;
            gameManager.forceLose = false;
            gameManager.forceTie = true;

            forceWinToggle.isOn = false;
            forceLoseToggle.isOn = false;
        }
        else if (!forceWinToggle.isOn && !forceLoseToggle.isOn)
        {
            gameManager.forceTie = false;
        }
    }

    private void OnStopSpinToggle(bool isOn)
    {
        wheelManager.stopSpin = isOn;
    }

    private void OnChosenIndexChanged(string input)
    {
        input = input.Trim();
        if (int.TryParse(input, out int value) && value >= 0 && value <= 19)
        {
            gameManager.chosen = value;
            playButton.interactable = true;
        }
        else
        {
            gameManager.chosen = -1;
            playButton.interactable = false;
        }
    }

    public void AnimateWinning(float amount, System.Action onComplete = null)
    {
        if (winAnimationCoroutine != null)
            StopCoroutine(winAnimationCoroutine);
        winAnimationCoroutine = StartCoroutine(AnimateWinningAmount(amount, onComplete));
    }
    private IEnumerator AnimateWinningAmount(float amount, System.Action onComplete)
    {
        float duration = 1.5f; // Adjust animation time
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float displayAmount = Mathf.Lerp(0f, amount, t);
            if (WinningAmountText != null)
                WinningAmountText.text = displayAmount.ToString("F2");
            yield return null;
        }

        if (WinningAmountText != null)
            WinningAmountText.text = amount.ToString("F2");

        onComplete?.Invoke();
    }
    public void ResetWinningAmount()
    {
        if (winAnimationCoroutine != null)
            StopCoroutine(winAnimationCoroutine);
        if (WinningAmountText != null)
            WinningAmountText.text = "0.00";
    }
}
