using DG.Tweening;
using System;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;
using UnityEngine.EventSystems;
using UnityEngine.PlayerLoop;

public class OctagonKeno : MonoBehaviour
{
    public static OctagonKeno Instance;
    public OctagonKenoGameSettings Settings;
    [SerializeField] private OctagonKenoResponse _response;


    [Header("Grid of 80 Toggles (assign in inspector)")]
    public List<OctagonKenoButton> numberButtons;
    public GameObject btnPrefab;

    private Tween tween;
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("UI Buttons")]
    public Button playButton;
    public Button stopButton;
    private HoldableButton playHoldable;
    private HoldableButton stopHoldable;
    public Button autoPickButton;
    public Button wipeButton;
    public Button increaseBetButton;
    public Button decreaseBetButton;
    public Button maxBetButton;
    public Button openInfoButton;
    public Button Exit;
    public Button ExitButton1;
    public Button closeInstructions;
    public GameObject autoBetMenuPanel;
    public GameObject instructuinsPanal;
    public Button autoButton20, autoButton50, autoButton100, autoButton200, autoButton500;
    public Button autoButtonInfinity;
    public TMP_Text stopButtonText;
    public TMP_Text stopButtonText1;
    public TMP_Text instructions;

    [SerializeField] private bool isAutoBetting = false;
    private bool skipwipe = false;
    private int autoRoundTarget = 0;
    private int autoRoundsRemaining = 0;
    private float holdThreshold = 0.8f;

    [Header("Panels / Displays")]
    public GameObject infoPanelPage1;
    public TMP_Text betText;
    public TMP_Text picksCountText;

    [Header("Balance")]
    [SerializeField] public TMP_Text balanceText;
    [SerializeField] private float balance;
    public Transform hitPayContainer;
    public GameObject hitPayRowPrefab;
    public Transform GridParent;

    private Coroutine drawCoroutine;
    private bool isDrawStopped = false;
    private bool isGameReady = true;
    public bool canAddPick = true;
    private bool showInstructions;
    private string prefKey;
    private int animationCompleteCount = 0;
    private int totalDrawsThisRound = 0;
    private int hitCountThisRound = 0;
    private int completedVisualsCount = 0;

    // === Hexagon-like UI state flags ===
    private bool isRoundInProgress = false;
    private float CurrentBet => betOptions[currentBetIndex];

    [ContextMenu("Reset Instruction Pref")]
    private void ResetInstructionPref()
    {
        PlayerPrefs.DeleteKey(prefKey);
        PlayerPrefs.Save();
    }

    private readonly float[] betOptions = new float[]
    {
        0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f,
        2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f
    };
    private int currentBetIndex = 0;

    public List<int> currentPicks = new List<int>();
    private List<GameObject> currentPikGameObj = new List<GameObject>();
    private const int minPicks = 1;
    private const int maxPicks = 10;

    // Pay table mapping picks->(hits->multiplier)
    private readonly Dictionary<int, Dictionary<int, float>> payTable = new Dictionary<int, Dictionary<int, float>>
    {
        {1, new Dictionary<int,float>{{0,0.4f},{1,2.7f}}},
        {2, new Dictionary<int,float>{{0,0},{1,1.8f},{2,5.1f}}},
        {3, new Dictionary<int,float>{{0,0},{1,0},{2,2.8f},{3,50}}},
        {4, new Dictionary<int,float>{{0,0},{1,0},{2,1.7f},{3,10},{4,100}}},
        {5, new Dictionary<int,float>{{0,0},{1,0},{2,1.4f},{3,4f},{4,14},{5,390}}},
        {6, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,3},{4,9},{5,180},{6,710}}},
        {7, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,2},{4,7},{5,30},{6,400},{7,800}}},
        {8, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,2},{4,4},{5,11},{6,67},{7,400},{8,900}}},
        {9, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,2},{4,2.5f},{5,5},{6,15},{7,100},{8,500},{9,1000}}},
        {10,new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,1.6f},{4,2f},{5,4},{6,7},{7,26},{8,100},{9,500},{10,1000}}}
    };
     
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
        SetGameButtons();


        ApiHandler.instance?.GameStarted(SceneManagement.currentGameID);
    }

    void Start()
    {
        string userID = UserManager.Instance.UserId;
        prefKey = $"OctagonKenoShowInstructions_{userID}";

        GameBetServices.Instance.SetActiveUI(this, balanceText, UpdateCoins);


        showInstructions = PlayerPrefs.GetInt(prefKey, 1) == 1;
        if (showInstructions)
        {
            instructuinsPanal.SetActive(true);
            instructuinsPanal.transform.GetChild(0).DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
        else
        {
            instructuinsPanal.SetActive(false);
        }
        closeInstructions.onClick.AddListener(OnCloseInstructions);
        skipwipe = false;
        instructions.text = "Select 1-10 Numbers To Play!";
        balance = UserManager.Instance.Coins;
        UserManager.Instance.currentBetAmount = 0;
        UpdateCoins();
        UpdateSlotServicesGameName();

        // Basic UI state
        playButton.interactable = false;
        stopButton.gameObject.SetActive(false);
        stopButton.onClick.AddListener(() => { OnStop(); });

        autoPickButton.onClick.AddListener(AutoPick);
        increaseBetButton.onClick.AddListener(IncreaseBet);
        decreaseBetButton.onClick.AddListener(DecreaseBet);
        maxBetButton.onClick.AddListener(maxbet);

        // Auto-bet menu buttons
        autoButton20.onClick.AddListener(() => StartAutoBet(20));
        autoButton50.onClick.AddListener(() => StartAutoBet(50));
        autoButton100.onClick.AddListener(() => StartAutoBet(100));
        autoButton200.onClick.AddListener(() => StartAutoBet(200));
        autoButton500.onClick.AddListener(() => StartAutoBet(500));
        autoButtonInfinity.onClick.AddListener(() => StartAutoBet(-1));

        // Hold-to-open menu
        playHoldable = playButton.gameObject.AddComponent<HoldableButton>();
        playHoldable.Init(OnPlayHoldTriggered, holdThreshold);
        playHoldable.IsHoldEnabled = true;

        stopHoldable = stopButton.gameObject.AddComponent<HoldableButton>();
        stopHoldable.Init(OnStopHoldTriggered, holdThreshold);
        stopHoldable.IsHoldEnabled = false;

        autoBetMenuPanel.transform.localScale = Vector3.zero;
        autoBetMenuPanel.SetActive(false);

        playButton.onClick.AddListener(() =>
        {
            OnUserPlay();
         });


        wipeButton.onClick.AddListener(() =>
        {
            PlaySound("Clear_Table");
            OnUserWipe();
        });
        openInfoButton.onClick.AddListener(() =>
        {
            PlaySound("UnPick");
            infoPanelPage1.SetActive(true);
        });
        ExitButton1.onClick.AddListener(() => infoPanelPage1.SetActive(false));

        infoPanelPage1.SetActive(false);
        UpdateBetDisplay();
        UpdatePickCountDisplay();
        UpdatePlayButton();
        UpdatePayTableDisplay();
        PlayMusic("OctagonKeno_Music");
        Exit.onClick.AddListener(OnExit);

        // Hexagon-style initial state
        EnterIdleState();
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }

    public void OnDrawAnimationComplete()
    {
        completedVisualsCount++;
        if (completedVisualsCount >= totalDrawsThisRound && !isAutoBetting)
        {
            completedVisualsCount = 0;
            EnterResultState();
        }
    }

    private void OnDestroy()
    {
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;
    }
    void Update()
    {
        // Close auto-bet panel on any outside click (safe if EventSystem missing)
        if (autoBetMenuPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current == null) { HideAutoBetMenu(); return; }

            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            bool clickedOnPanel = results.Any(r => r.gameObject.transform.IsChildOf(autoBetMenuPanel.transform));
            if (!clickedOnPanel) HideAutoBetMenu();
        }
    }

    // User-triggered Play
    private void OnUserPlay()
    {
        if (UserManager.Instance.Coins < CurrentBet)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Insufficiant Balance");
        }

        if (isGameReady)
        {
            UpdateCoins();
            if (!isAutoBetting)
            {
                instructions.text = "Click 'START' to Play!";
                isSamePicks = true;
                WipePicks();
                stopButton.gameObject.SetActive(true);
                stopButton.interactable = false;
            }
            SpinOctaKeno();
        }
        else
        {
            return;
        }
    }

    void OnExit()
    {
        if (isRoundInProgress) return;
        PlaySound("UnPick");
        Destroy(gameObject);
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManagement.GoBackToMainMenu();    // SceneManager.LoadScene("Main");
    }

    public void PlaySound(string soundName)
    {
        if (soundName == null) return;
        if (!OctagonKenoSoundManager.Instance.IsSoundMute())
            OctagonKenoSoundManager.Instance.PlaySFX(soundName);
    }
    public void PlayMusic(string soundName)
    {
        if (soundName == null) return;

        if (OctagonKenoSoundManager.Instance.IsMusicMute()) return;

        if (!OctagonKenoSoundManager.Instance.IsMusicPlaying(soundName))
            OctagonKenoSoundManager.Instance.PlayMusic(soundName);
    }

    private void OnStop()
    {
        PlaySound("UnPick");
        isDrawStopped = true;
        if (isAutoBetting)
        {
            isAutoBetting = false;
            skipwipe = true;
            EnterResultState();
        }

        stopButtonText.text = "STOP";
        stopButtonText1.text = "Hold For Autobet";

        foreach (var btn in numberButtons)
        {
            if (btn != null && btn.animator != null)
            {
                btn.animator.speed = 10f;
            }
        }
    }

    public bool isSamePicks;

    private void OnUserWipe()
    {
        PlaySound("Clear_Table");
        isGameReady = true;
        isSamePicks = false;

        EnterWipingState();
        WipePicks(); 
    }

    void SetGameButtons()
    {
        for (int i = 0; i < 40; i++)
        {
            GameObject go = Instantiate(btnPrefab, GridParent);
            var btn = go.GetComponent<OctagonKenoButton>();
            int num = i + 1;
            go.name = "Button" + num;
            btn.number = num;
            btn.numberText.text = num.ToString();
            btn.button.onClick.AddListener(() => OnNumberClicked(num));
            numberButtons.Add(btn);
        }
    }

    #region Machine Registery
    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }
    #endregion

    // === Hexagon-like state helpers ===
    private void EnterIdleState()
    {
        canAddPick = true;
        isRoundInProgress = false;

        playButton.gameObject.SetActive(true);
        playButton.interactable = currentPicks.Count >= minPicks &&
                                  currentPicks.Count <= maxPicks;

        stopButton.gameObject.SetActive(false);
        stopButton.interactable = false;

        autoPickButton.interactable = true;
        wipeButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
        maxBetButton.interactable = true;
        openInfoButton.interactable = true;
        Exit.interactable = true;
    }

    private void EnterPlayingState()
    {
        canAddPick = false;
        isRoundInProgress = true;

        playButton.gameObject.SetActive(false);
        playButton.interactable = false;

        stopButton.gameObject.SetActive(true);
        stopButton.interactable = true;

        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;
        maxBetButton.interactable = false;
        openInfoButton.interactable = false;
        Exit.interactable = false;
    }

    private void EnterResultState()
    {
        canAddPick = false;
        isRoundInProgress = false;

        playButton.gameObject.SetActive(true);
        playButton.interactable = currentPicks.Count >= minPicks &&
                                  currentPicks.Count <= maxPicks;

        stopButton.gameObject.SetActive(false);
        stopButton.interactable = false;

        autoPickButton.interactable = true;
        wipeButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
        maxBetButton.interactable = true;
        openInfoButton.interactable = true;
        Exit.interactable = true;

        isGameReady = true;
    }
    private void EnterErrorState()
    {
        canAddPick = true;
        isRoundInProgress = false;
        isGameReady = false;
        if (isAutoBetting)
        {
            isAutoBetting = false;
        }

        playButton.gameObject.SetActive(true);
        playButton.interactable = true;

        stopButton.gameObject.SetActive(false);
        stopButton.interactable = false;

        autoPickButton.interactable = true;
        wipeButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
        maxBetButton.interactable = true;
        openInfoButton.interactable = true;
        Exit.interactable = true;
    }

    private void EnterWipingState()
    {
        canAddPick = false;
        playButton.interactable = false;
        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        stopButton.interactable = false;
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;
        maxBetButton.interactable = false;
        openInfoButton.interactable = false;
        Exit.interactable = false;
    }

    private void EnterPlayUI()
    {
        if (!isGameReady) return;

        // Switch to Playing UI state (Hexagon style)
        EnterPlayingState();

        if (!isAutoBetting)
        {
            Play();
        }

        isGameReady = false;
    }

    private void OnCloseInstructions()
    {
        PlaySound("UnPick");
        PlayerPrefs.SetInt(prefKey, 0);
        PlayerPrefs.Save();
        instructuinsPanal.transform.GetChild(0).DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack)
            .OnComplete(() => instructuinsPanal.SetActive(false));
    }
  

    // Update balance text (Hexagon-style: also refresh playButton)
    public void UpdateCoins()
    { 
        if (UserManager.Instance != null)
        {
            balanceText.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }
        UpdatePlayButton();
    }

    // Reset button visuals based on current picks
    private void ResetButtonStates()
    {
        for (int i = 0; i < numberButtons.Count; i++)
        {
            int num = i + 1;
            var state = currentPicks.Contains(num)
                ? KenoButtonState.Selected
                : KenoButtonState.Unselected;
            numberButtons[i].SetState(state);
        }
    }

    private void HideAutoBetMenu()
    {
        playButton.interactable = true;
        autoBetMenuPanel.transform.DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() => autoBetMenuPanel.SetActive(false));
    }

    // Start auto betting for a given number of rounds (-1 = infinity)
    private void StartAutoBet(int roundCount)
    {
        PlaySound("UnPick");
        if (currentPicks.Count < minPicks)
        {
            instructions.text = "Select 1-10 Numbers To Play!";
            return;
        }
        if (UserManager.Instance.Coins < betOptions[currentBetIndex])
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Insufficiant Balance");
            EnterErrorState();
            return;
        }
        autoRoundTarget = roundCount;
        autoRoundsRemaining = (roundCount == -1 ? int.MaxValue : roundCount);

        //Debug.Log($" nasir  : Starting auto bet for {autoRoundsRemaining} rounds");
        isAutoBetting = true;
        stopHoldable.IsHoldEnabled = false;
        isSamePicks = false;
        EnterPlayUI();
        HideAutoBetMenu();
        StartCoroutine(WipeRoutine(false));
    }

    private IEnumerator PlayInLoop()
    {
        if (isAutoBetting)
        {
            stopHoldable.IsHoldEnabled = false;
            yield return new WaitForSeconds(0.1f);
            foreach (int number in currentPicks)
            {
                numberButtons[number - 1].SetState(KenoButtonState.Selected);
            }
            yield return new WaitForSeconds(0.3f);
            SpinOctaKeno();
        }
        else
        {
            stopHoldable.IsHoldEnabled = true;
            isAutoBetting = false;
        }
    }

    bool isAutoPicking = false;
    public void AutoPick()
    {
        if (isAutoPicking) return;
        if (isRoundInProgress || !canAddPick) return;

        isAutoPicking = true;
        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        playButton.interactable = false;
        StartCoroutine(AutoPickRoutine());
    }

    private IEnumerator AutoPickRoutine()
    {
        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        playButton.interactable = false;

        int totalSlots = numberButtons.Count;
        int count = Mathf.Clamp(currentPicks.Count, 0, maxPicks);

        if (count == maxPicks)
        {
            isSamePicks = false;
            yield return StartCoroutine(WipeRoutine(true));
            count = 0;
        }

        int need = (count == 0) ? maxPicks : (maxPicks - count);

        yield return new WaitForSeconds(0.25f);

        HashSet<int> chosen = new HashSet<int>(currentPicks);
        List<int> toAdd = new List<int>();

        while (toAdd.Count < need)
        {
            int n = Random.Range(1, totalSlots + 1);
            if (chosen.Add(n))
                toAdd.Add(n);
        }

        foreach (int n in toAdd)
        {
            int index = n - 1;
            if (index >= 0 && index < numberButtons.Count)
            {
                OnNumberClicked(n);
                yield return new WaitForSeconds(0.05f);
            }
            else
            {
            }
        }

        UpdatePickCountDisplay();
        UpdatePlayButton();
        UpdatePayTableDisplay();

        isAutoPicking = false;
        autoPickButton.interactable = true;
        wipeButton.interactable = true;
        playButton.interactable = true;
    }

    public void WipePicks()
    {
        EnterWipingState();
        StartCoroutine(WipeRoutine(true));
    }

    private IEnumerator WipeRoutine(bool restoreIdleAtEnd)
    {
        if (isAutoBetting)
        {
            yield return new WaitForSeconds(3f);
        }
        if (!skipwipe)
        {
            foreach (var child in hitsGameObj)
            {
                if (child.transform.childCount > 0)
                {
                    child.transform.GetChild(0).localScale = Vector3.zero;
                    var anim = child.GetComponent<Animator>();
                    if (anim != null)
                    {
                        anim.enabled = true;
                        anim.ResetTrigger("Hit");
                        anim.SetTrigger("StillHit");
                    }
                }
            }
            if (!isSamePicks)
            {
                foreach (var obj in currentPikGameObj)
                {
                    PlaySound("UnPick");
                    var btn = obj.GetComponent<OctagonKenoButton>();
                    if (btn != null)
                        btn.SetState(KenoButtonState.Unselected);
                    yield return new WaitForSeconds(0.05f);
                }
            }
            hitsGameObj.Clear();
            draws.Clear();
            if (!isAutoBetting)
            {
                if (!isSamePicks)
                {
                    currentPicks.Clear();
                    currentPikGameObj.Clear();
                }
            }
            ResetButtonStates();
            UpdatePickCountDisplay();
            UpdatePlayButton();
            UpdatePayTableDisplay();
        }

        if (isAutoBetting)
        {
            autoRoundsRemaining -= 1;
            StartCoroutine(PlayInLoop());
        }
        skipwipe = false;

        if (restoreIdleAtEnd)
        {
            canAddPick = true;
            EnterIdleState();
        }
        else
        {
            canAddPick = false;

            if (!isDrawStopped)
            {
                EnterPlayingState();
            }
            isGameReady = true;
        }
    }

    public void IncreaseBet()
    {
        currentBetIndex = (currentBetIndex + 1) % betOptions.Length;
        if (currentBetIndex == betOptions.Length - 1)
        {
            PlaySound("Max_Bet");
        }
        else
        {
            PlaySound("Bet_Increase");
        }
        UpdateBetDisplay();
        UpdatePayTableDisplay();
    }

    public void DecreaseBet()
    {
        currentBetIndex = (currentBetIndex - 1 + betOptions.Length) % betOptions.Length;
        if (currentBetIndex == betOptions.Length - 1)
        {
            PlaySound("Max_Bet");
        }
        else
        {
            PlaySound("Bet_Increase");
        }
        UpdateBetDisplay();
        UpdatePayTableDisplay();
    }

    public void maxbet()
    {
        currentBetIndex = betOptions.Length - 1;
        PlaySound("Max_Bet");
        UpdateBetDisplay();
        UpdatePayTableDisplay();
    }

    private void UpdateBetDisplay()
    {
        betText.text = $"{betOptions[currentBetIndex]:0.00}";
    }

    private void UpdatePickCountDisplay()
    {
        picksCountText.text = $"{currentPicks.Count}";
    }

    private void UpdatePlayButton()
    {
        bool picksOK = currentPicks.Count >= minPicks && currentPicks.Count <= maxPicks;
        bool visible = playButton.gameObject.activeSelf;
        playButton.interactable = picksOK && !isRoundInProgress && visible;
    }

    private void UpdatePayTableDisplay()
    {
        foreach (Transform child in hitPayContainer)
            Destroy(child.gameObject);

        int picks = currentPicks.Count;
        if (payTable.ContainsKey(picks))
        {
            var row = payTable[picks];
            foreach (var kv in row.OrderBy(k => k.Key))
            {
                int hits = kv.Key;
                float mult = kv.Value;
                if (mult > 0f)
                {
                    float payout = mult * betOptions[currentBetIndex];
                    var rowObj = Instantiate(hitPayRowPrefab, hitPayContainer);
                    rowObj.SetActive(true);
                    var hitText = rowObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (hitText != null) hitText.text = hits.ToString();
                    var payText = rowObj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (payText != null) payText.text = payout.ToString("0.00");
                }
            }
        }
    }

    public List<int> draws = new List<int>();
    public List<GameObject> hitsGameObj = new List<GameObject>();

    public void Play()
    {
        foreach (var btn in numberButtons)
        {
            if (btn != null && btn.animator != null)
            {
                btn.animator.speed = 1f;
            }
        }

        if (drawCoroutine != null) { StopCoroutine(drawCoroutine); drawCoroutine = null; }
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        instructions.text = "GoodLuck!";
        if (drawCoroutine != null)
        {
            StopCoroutine(drawCoroutine);
        }
        isDrawStopped = false;
        if (currentPicks.Count < minPicks)
        {
            instructions.text = "Select 1-10 Numbers To Play!";
            PlaySound("Invalid_Pick");
            yield return null;
        }

        ResetButtonStates();
        var missPool = Enumerable.Range(1, numberButtons.Count)
            .Where(n => !currentPicks.Contains(n)).ToList();

        if (!isMakeDraws)
        {
            draws.Clear();
            // First pick one from miss pool
            int idx = Random.Range(0, missPool.Count);
            draws.Add(missPool[idx]);
            missPool.RemoveAt(idx);
            // Then fill to maxPicks-1 from remaining (unique)
            var rem = Enumerable.Range(1, numberButtons.Count)
                .Except(draws).ToList();
            for (int i = 0; i < maxPicks - 1; i++)
            {
                idx = Random.Range(0, rem.Count);
                draws.Add(rem[idx]);
                rem.RemoveAt(idx);
            }
        }

        drawCoroutine = StartCoroutine(AnimateDrawSequence(draws));
        if (isAutoBetting)
        {
            if (autoRoundsRemaining > 0 || autoRoundTarget == -1)
            {
                StartCoroutine(WipeRoutine(false));
                stopHoldable.IsHoldEnabled = false;
                stopButton.interactable = true;
                if (autoRoundTarget == -1)
                {
                    stopButtonText.text = "∞";
                }
                else
                {
                    stopButtonText.text = $"{autoRoundsRemaining}";
                }
                stopButtonText1.text = $"Stop auto bet";
            }
            else
            {
                isAutoBetting = false;
                stopHoldable.IsHoldEnabled = true;
                stopButtonText.text = "Stop";
                stopButtonText1.text = $"Hold for auto bet";
            }
        }
    }

    bool isMakeDraws = false;
    [ContextMenu("TestAnimateDrawSequence")]
    public void ResponseAnimateDrawSequence(List<int> responseDraws)
    {
        draws.Clear();
        isMakeDraws = true;
        draws.AddRange(responseDraws);
        //Debug.Log("In play game-1");
        EnterPlayUI();
        if (isAutoBetting)
        {
            Play();
        }
    }

    private IEnumerator AnimateDrawSequence(List<int> drawsList)
    {
        StopRotation();
        totalDrawsThisRound = drawsList.Count;
        hitCountThisRound = 0;
        completedVisualsCount = 0;

        for (int i = 0; i < drawsList.Count; i++)
        {
            int num = drawsList[i];
            if (isDrawStopped)
            {
                for (int j = i; j < drawsList.Count; j++)
                {
                    int rest = drawsList[j];
                    var st = currentPicks.Contains(rest)
                        ? KenoButtonState.Hit : KenoButtonState.Drawn;
                    numberButtons[rest - 1].SetState(st);
                    if (st == KenoButtonState.Hit)
                    {
                        hitCountThisRound++;
                        hitsGameObj.Add(numberButtons[rest - 1].gameObject);
                    }
                    else
                    {
                        StartCoroutine(RegisterVisualCompleteDelayed(0.1f));
                    }
                }
                if (!isAutoBetting)
                    TogglePlayButton();
                EvaluateRoundResults();
                UpdateCoinIntoBD();
                yield break;
            }

            var state = currentPicks.Contains(num)
                ? KenoButtonState.Hit : KenoButtonState.Drawn;
            numberButtons[num - 1].SetState(state);
            if (state == KenoButtonState.Hit)
            {
                hitCountThisRound++;
                hitsGameObj.Add(numberButtons[num - 1].gameObject);
            }
            else
            {
                StartCoroutine(RegisterVisualCompleteDelayed(0.1f));
            }

            yield return new WaitForSeconds(0.1f);
        }

        if (!isAutoBetting)
            TogglePlayButton();
        EvaluateRoundResults();
        UpdateCoinIntoBD();
    }
    private IEnumerator RegisterVisualCompleteDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnDrawAnimationComplete();
    }

    private void UpdateCoinIntoBD()
    {
        //Debug.Log("trying to update balance");
        if (_response.winAmount > 0)
        {
            GameBetServices.Instance.UpdateCoins(_response.newBalance);
        }
    }

    public void OnNumberClicked(int number)
    {
        if (canAddPick)
        {
            if (currentPicks.Contains(number))
            {
                PlaySound("UnPick");
                currentPicks.Remove(number);
                numberButtons[number - 1].SetState(KenoButtonState.Unselected);
                // remove first GO match if tracking
                var go = numberButtons[number - 1].gameObject;
                int idx = currentPikGameObj.IndexOf(go);
                if (idx >= 0) currentPikGameObj.RemoveAt(idx);
            }
            else if (currentPicks.Count < maxPicks)
            {
                PlaySound("Pick");
                currentPicks.Add(number);
                var go = numberButtons[number - 1].gameObject;
                if (!currentPikGameObj.Contains(go))
                    currentPikGameObj.Add(go);
                numberButtons[number - 1].SetState(KenoButtonState.Selected);
            }
            else
            {
                PlaySound("Invalid_Pick");
            }
            UpdatePickCountDisplay();
            UpdatePlayButton();
            UpdatePayTableDisplay();
        }
        else
        {
            return;
        }
    }

    void TogglePlayButton()
    {
        canAddPick = false;
    }

    // Hold triggers
    private void OnPlayHoldTriggered()
    {
        PlaySound("UnPick");
        playButton.interactable = false;
        ShowAutoBetMenu();
    }
    private void OnStopHoldTriggered() => ShowAutoBetMenu();

    // Show auto-bet panel
    private void ShowAutoBetMenu()
    {
        if (autoBetMenuPanel.activeSelf) return;
        autoBetMenuPanel.SetActive(true);
        autoBetMenuPanel.transform.localScale = Vector3.zero;
        autoBetMenuPanel.transform.DOScale(Vector3.one, 0.25f)
            .SetEase(Ease.OutBack);
    }

    private void EvaluateRoundResults()
    {
        int hitsCount = draws.Count(n => currentPicks.Contains(n));
        float mult = 0f;

        if (payTable.TryGetValue(currentPicks.Count, out var row) &&
            row.TryGetValue(hitsCount, out var baseMult))
        {
            mult = baseMult;

            foreach (Transform rowTransform in hitPayContainer)
            {
                var hitText = rowTransform.GetChild(0).GetComponent<TextMeshProUGUI>();
                var glowImage = rowTransform.GetComponent<Image>();

                if (hitText != null && glowImage != null)
                {
                    bool isHit = hitText.text == hitsCount.ToString();
                    glowImage.DOKill();

                    if (isHit)
                    {
                        instructions.text = $"YOU WON {_response.winAmount}";
                        glowImage.enabled = true;
                        var color = glowImage.color;
                        color.a = 0.3f;
                        glowImage.color = color;
                        glowImage.DOFade(1f, 0.5f)
                                 .SetLoops(-1, LoopType.Yoyo)
                                 .SetEase(Ease.InOutSine);
                    }
                    else
                    {
                        glowImage.enabled = false;
                        var color = glowImage.color;
                        color.a = 1f;
                        glowImage.color = color;
                    }
                }
            }
            PlaySound("Draw");
        }

        balance -= betOptions[currentBetIndex];
    }

    #region APICallForRsult
    public void SpinOctaKeno()
    {
        
        if (string.IsNullOrEmpty(SceneManagement.currentGameID))
        {
            return;
        }
        if (UserManager.Instance.Coins < betOptions[currentBetIndex])
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Insufficiant Balance");
            EnterErrorState();
            return;
        }
        else
        {
            if (!GameBetServices.Instance.TrySpinWithCurrentBet(betOptions[currentBetIndex])) return;
            //UserManager.Instance.UpdateCoins(betOptions[currentBetIndex], true);
            //UpdateCoinsAction();

            OctagonKenoRequest requestData = new OctagonKenoRequest
            {
                requestId = Guid.NewGuid().ToString(),
                gameId = SceneManagement.currentGameID,
                betAmount = betOptions[currentBetIndex],
                playerPicks = currentPicks,
            };

            StartCoroutine(SendOctaKenoSpinRequest(requestData));
        }
    }
    public void StartRotation()
    {
        if (responseReceived)
        {
            return;
        }
        else
        {
            if (spinnerImage == null) return;

            spinnerImage.gameObject.SetActive(true);
            tween?.Kill();

            float duration = 360f / rotationSpeed;

            tween = spinnerImage.transform.GetChild(0)
                .DORotate(new Vector3(0, 0, -360f), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
    }

    public void StopRotation()
    {
        tween?.Kill();
        spinnerImage.gameObject.SetActive(false);
    }
    public bool responseReceived;
    IEnumerator SendOctaKenoSpinRequest(OctagonKenoRequest data)
    {


        responseReceived = false;
        string json = JsonUtility.ToJson(data);
        Debug.Log("📦 Sending payload: " + json);

        UnityWebRequest request = new UnityWebRequest(ApiEndpoints.octagonekeno, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        foreach (var header in ApiEndpoints.GetAuthHeaders())
            request.SetRequestHeader(header.Key, header.Value);

        // ⭐ Start timeout check in parallel
        StartCoroutine(SpinnerDelayCheck(0.5f));

        yield return request.SendWebRequest();

        // Mark response received
        responseReceived = true;

        if (request.responseCode == 401)
        {
            yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.octagonekeno, json, "POST", () => SendOctaKenoSpinRequest(data));
            yield break;
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            responseReceived = true; // to dont start the rotation again
            //Debug.Log("✅ Spin result received:");
            Debug.Log(request.downloadHandler.text);

            OctagonKenoResponse response = JsonUtility.FromJson<OctagonKenoResponse>(request.downloadHandler.text);
            Debug.Log($"🎯 Hit Count: {response.hitCount}, ___ Win: {response.isWin} ___ Win Amount: {response.winAmount}, ___ newBalance: {response.newBalance}");


            _response = response;
            if (response.winAmount > 0)
            {
                _response.winAmount = response.winAmount;
            }
            ResponseAnimateDrawSequence(response.drawnNumbers);
        }
        else
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network error. Please try again.");
            ReturnBetAmount();
            isRoundInProgress = false;
            EnterIdleState();
            StopRotation();
        }
    }
    private IEnumerator SpinnerDelayCheck(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only show spinner if response has NOT arrived yet
        if (!responseReceived)
            StartRotation();
    }
    void ReturnBetAmount()
    {
        if (UserManager.Instance != null)
        {
            float coin = UserManager.Instance.Coins;
            float bet = UserManager.Instance.currentBetAmount;

            balanceText.text = UserManager.Instance.FormatCoins(coin + bet);
        }
        UpdatePlayButton();
    }
    #endregion

    [Serializable]
    public class OctagonKenoRequest
    {
        public string requestId;
        public string gameId;
        public float betAmount;
        public List<int> playerPicks;
    }

    [Serializable]
    public class OctagonKenoResponse
    {
        public string requestId;
        public string gameId;
        public List<int> playerPicks;
        public List<int> drawnNumbers;
        public int hitCount;
        public float betAmount;
        public float winAmount;
        public float newBalance;
        public float totalWin;
        public bool isWin;
        public string timestamp;
    }
}
