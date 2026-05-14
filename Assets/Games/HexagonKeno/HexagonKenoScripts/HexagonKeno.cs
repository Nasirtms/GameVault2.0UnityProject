using DG.Tweening;
using Sirenix.OdinInspector;
using System;
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

public class HexagonKeno : MonoBehaviour
{
    #region Scene Refrences
    public static HexagonKeno Instance;
    public HexagonKenoGameSettings settings;
    public HexaKenoResponse currentSpinResult;

    [SerializeField] private HexagonKenoDrawnBallPool ballPool;

    [Header("Grid of 80 Toggles (assign in inspector)")]
    public List<HexagonKenoButton> numberButtons;
    public GameObject btnPrefab;

    //Feedback-01 Deepak 17/10/2025
    [Header("Drawn Ball Speed; Previous:0.09, New:0.03")]
    [SerializeField] private float moveDuration = 0.6f;
    //Feedback-01 Deepak 17/10/2025

    [Header("Loader")]
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float rotationSpeed = 180f;
    //Feedback-01

    [Header("Loader")]
    private Tween tween;

    [Header("UI Buttons")]
    public Button playButton;
    public Button stopButton;
    public Button autoPickButton;
    public Button wipeButton;
    public Button increaseBetButton;
    public Button decreaseBetButton;
    public Button maxBetButton;
    public Button openInfoButton;
    public Button Exit;
    public Button ExitButton1;


    [Header("Panels / Displays")]
    public GameObject infoPanelPage1;
    public TMP_Text betText;
    public TMP_Text picksCountText;
    public TMP_Text winAmount_Text;
    public TMP_Text instructions;

    [Header("Balance")]
    [SerializeField] public TMP_Text balanceText;
    [SerializeField] public float startingBalance;
    [SerializeField] private float balance;
    [SerializeField] private float winning;
    public Transform hitPayContainer;
    public GameObject hitPayRowPrefab;
    public Transform drawnBallContainer;
    public GameObject drawnBallPrefab;

    public Transform GridParent;
    GameObject drawsLastObject;
    public Coroutine textAnimationCoroutine;

    private Coroutine drawCoroutine;
    private bool isDrawStopped = false;
    private bool isGameReady = true;
    private bool isRoundInProgress = false;
    public bool canAddPick = true;
    public Transform target1;
    public Transform target2;
    public Transform target3;
    private Transform baseTarget2;
    private Transform baseTarget3;
    private Transform initialTarget2;
    private Transform initialTarget3;

    private readonly float[] betOptions = new float[]
    {
        0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f,
        2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f, 9.0f, 10.0f
    };
    private int currentBetIndex = 0;
    private float CurrentBet => betOptions[currentBetIndex];
    [SerializeField]
    private List<int> currentPicks = new List<int>();
    private List<GameObject> currentPikGameObj = new List<GameObject>();
    private const int minPicks = 2;
    private const int maxPicks = 10;

    // payTable[picks][hits] = multiplier
    private readonly Dictionary<int, Dictionary<int, float>> payTable = new Dictionary<int, Dictionary<int, float>>
    {
        {2, new Dictionary<int,float>{{0,0},{1,1},{2,9}}},
        {3, new Dictionary<int,float>{{0,0},{1,0},{2,3},{3,36}}},
        {4, new Dictionary<int,float>{{0,0},{1,0},{2,2},{3,6},{4,77}}},
        {5, new Dictionary<int,float>{{0,0},{1,0},{2,1},{3,3},{4,22},{5,204}}},
        {6, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,3},{4,12},{5,40},{6,500}}},
        {7, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,2},{4,5},{5,24},{6,114},{7,750}}},
        {8, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,1},{4,3},{5,14},{6,70},{7,215},{8,1000}}},
        {9, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,0},{4,3},{5,8},{6,39},{7,130},{8,500},{9,1500}}},
        {10,new Dictionary<int,float>{{0,1},{1,0},{2,0},{3,0},{4,2},{5,5},{6,15},{7,65},{8,300},{9,750},{10,2000}}}
    };
    #endregion

    #region Awake / Update
    private void Awake()
    {
        SetGameButtons();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }


        ApiHandler.instance?.GameStarted(SceneManagement.currentGameID);
    }
    #endregion

    #region Start / Initialization
    void Start()
    {
        GameBetServices.Instance.SetActiveUI(this, balanceText, UpdateCoins);
        winAmount_Text.text = "0.00";
        instructions.text = "Select at least two numbers";
        UpdateSlotServicesGameName();
        balance = UserManager.Instance.Coins;
        UserManager.Instance.currentBetAmount = 0;
        UpdateCoins();
        playButton.interactable = false;
        stopButton.gameObject.SetActive(false);
        stopButton.onClick.AddListener(() =>
        {
            PlaySound("UnPick");
            isDrawStopped = true;
        });

        // hook buttons
        autoPickButton.onClick.AddListener(() =>
        {
            AutoPick();
        });
        increaseBetButton.onClick.AddListener(IncreaseBet);
        decreaseBetButton.onClick.AddListener(DecreaseBet);
        maxBetButton.onClick.AddListener(MaxBet);
        playButton.onClick.AddListener(() =>
        {
            if (textAnimationCoroutine != null)
            {
                StopCoroutine(textAnimationCoroutine);
            }
            StartRound();

        });
        wipeButton.onClick.AddListener(() =>
        {
            PlaySound("Clear_Table");
            isGameReady = true;
            isSamePicks = false;
            WipePicks();
            playButton.interactable = true;
            autoPickButton.interactable = true;
            increaseBetButton.interactable = true;
            decreaseBetButton.interactable = true;
            maxBetButton.interactable = true;
        });
        openInfoButton.onClick.AddListener(() =>
        {
            PlaySound("UnPick");
            infoPanelPage1.SetActive(true);
        });
        ExitButton1.onClick.AddListener(() =>
        {
            PlaySound("UnPick");
            infoPanelPage1.SetActive(false);
        });

        // initial UI
        infoPanelPage1.SetActive(false);
        UpdateBetDisplay();
        UpdatePickCountDisplay();
        UpdatePlayButton();
        UpdatePayTableDisplay();
        PlayMusic("OctagonKeno_Music");
        Exit.onClick.AddListener(OnExit);
        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }
    private void OnDestroy()
    {
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;
    }

    public void PlaySound(string soundName)
    {
        if (soundName == null) return;
        if (!HexagonKenoSoundManager.Instance.IsSoundMute())
            HexagonKenoSoundManager.Instance.PlaySFX(soundName);
    }

    public void PlayMusic(string soundName)
    {
        if (soundName == null) return;

        if (HexagonKenoSoundManager.Instance.IsMusicMute()) return;

        if (!HexagonKenoSoundManager.Instance.IsMusicPlaying(soundName))
            HexagonKenoSoundManager.Instance.PlayMusic(soundName);
    }
    #endregion

    #region Machine Registery


    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }

    #endregion

    #region Game Listners
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
    void SetGameButtons()
    {
        int rows = 8;
        int cols = 10;

        for (int i = 0; i < rows * cols; i++)
        {
            int number = i + 1;
            int col = i % cols;       // determines the column index (0–9)
            int row = i / cols;       // determines the row index (0–7)

            Transform parent = GridParent.GetChild(col); // Place in Column1, Column2, ...

            GameObject go = Instantiate(btnPrefab, parent);
            var hexButton = go.GetComponent<HexagonKenoButton>();

            go.name = "Button" + number;
            hexButton.number = number;
            hexButton.numberText.text = number.ToString();
            hexButton.button.onClick.AddListener(() => OnNumberClicked(number));
            numberButtons.Add(hexButton);
        }
    }
    private void SetTargert()
    {
        baseTarget2 = new GameObject("BaseTarget2").transform;
        baseTarget3 = new GameObject("BaseTarget3").transform;
        initialTarget2 = new GameObject("InitialTarget2").transform;
        initialTarget3 = new GameObject("InitialTarget3").transform;

        baseTarget2.SetParent(target2.parent);
        baseTarget3.SetParent(target3.parent);
        initialTarget2.SetParent(target2.parent);
        initialTarget3.SetParent(target3.parent);

        baseTarget2.localPosition = target2.localPosition;
        baseTarget3.localPosition = target3.localPosition;
        initialTarget2.localPosition = target2.localPosition;
        initialTarget3.localPosition = target3.localPosition;
    }

    public void UpdateCoins()
    {
        if (UserManager.Instance != null)
        {
            balanceText.text = UserManager.Instance.FormatCoins(UserManager.Instance.Coins);
        }
        UpdatePlayButton(); // keep Play in sync when balance changes
    }
    private void ResetButtonStates()
    {
        for (int i = 0; i < numberButtons.Count; i++)
        {
            int number = i + 1;
            if (currentPicks.Contains(number))
                numberButtons[i].SetState(HexagonKenoButtonState.Selected);
            else
                numberButtons[i].SetState(HexagonKenoButtonState.Unselected);
        }
    }
    #endregion

    #region AutoPick
    bool isAutoPicking = false;
    public void AutoPick()
    {
        if (isAutoPicking || isRoundInProgress || !canAddPick) return;
        isAutoPicking = true;
        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        playButton.interactable = false;
        StartCoroutine(AutoPickRoutine());
    }

    private IEnumerator AutoPickRoutine()
    {
        if (!canAddPick) yield break;

        int count = currentPicks.Count;
        if (count >= maxPicks)
        {
            yield return StartCoroutine(WipeRoutine(true));
            yield return new WaitForSeconds(0.15f);

            HashSet<int> fresh = new HashSet<int>();
            while (fresh.Count < maxPicks)
                fresh.Add(Random.Range(1, 81));

            foreach (int n in fresh)
            {
                OnNumberClicked(n);                 
                yield return new WaitForSeconds(0.05f);
            }
        }
        else
        {
            HashSet<int> chosen = new HashSet<int>(currentPicks);
            List<int> toAdd = new List<int>();

            while (chosen.Count < maxPicks)
            {
                int n = Random.Range(1, 81);
                if (chosen.Add(n))
                    toAdd.Add(n); 
            }

            foreach (int n in toAdd)
            {
                OnNumberClicked(n);
                yield return new WaitForSeconds(0.05f);
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
    #endregion

    #region WipePicks
    public bool isSamePicks;
    public void WipePicks()
    {
        EnterWipingState();
        StartCoroutine(WipeRoutine(true));
    }

    private IEnumerator WipeRoutine(bool restoreIdleAtEnd)
    {
        try
        {
            if (currentPicks.Count > 0)
            {
                List<Transform> balls = new List<Transform>();
                foreach (Transform child in drawnBallContainer) balls.Add(child);

                int maxCount = Mathf.Max(currentPikGameObj.Count, balls.Count);
                for (int i = 0; i < maxCount; i++)
                {
                    PlaySound("UnPick");

                    if (!isSamePicks && i < currentPikGameObj.Count)
                    {
                        currentPikGameObj[i].GetComponent<HexagonKenoButton>()
                            .SetState(HexagonKenoButtonState.Unselected);
                        yield return new WaitForSeconds(0.05f);
                    }

                    if (i < balls.Count)
                    {
                        Transform ball = balls[i];
                        ball.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack);
                        yield return new WaitForSeconds(0.05f);
                        ballPool.Release(ball.gameObject);
                    }
                }
                if (hitPayContainer != null) 
                {
                    foreach (Transform hitAndPayRow in hitPayContainer)
                    {
                        if (!isSamePicks)
                        {
                            Destroy(hitAndPayRow.gameObject);
                        }
                    }
                }
                

                foreach (Transform hitAndPayRow in hitPayContainer)
                {
                    if (!isSamePicks)
                    {
                        Destroy(hitAndPayRow.gameObject);
                    }
                }

                if (drawsLastObject != null)
                {
                    drawsLastObject.transform.GetChild(1).gameObject.SetActive(false);
                }

                if (!isSamePicks)
                {
                    currentPikGameObj.Clear();
                    currentPicks.Clear();
                }
            }

            ResetButtonStates();
            winAmount_Text.text = "0.00";
            UpdatePickCountDisplay();
            UpdatePlayButton();
        }
        finally
        {
            if (restoreIdleAtEnd)
            {
                canAddPick = true;
                EnterIdleState();
            }
            else
            {
                canAddPick = false;
                EnterPlayingState();
            }
        }
    }
    #endregion

    #region Bet Adjustment
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

    public void MaxBet()
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
        playButton.interactable = picksOK && !isRoundInProgress && playButton.gameObject.activeSelf;
    }
    private void UpdatePayTableDisplay()
    {
        foreach (Transform child in hitPayContainer)
        {
            Destroy(child.gameObject);
        }

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

                    GameObject rowObj = Instantiate(hitPayRowPrefab, hitPayContainer);
                    rowObj.SetActive(true);

                    var hitText = rowObj.transform.GetChild(0).GetComponent<TMP_Text>();
                    if (hitText != null)
                        hitText.text = hits.ToString();

                    var payText = rowObj.transform.GetChild(1).GetComponent<TMP_Text>();
                    if (payText != null)
                        payText.text = payout.ToString("0.00");
                }
            }
        }
    }

    public List<int> draws = new List<int>();
    #endregion

    #region Play

    private IEnumerator PlayRoutine()
    {
        instructions.text = "Good Luck!";
        if (drawCoroutine != null)
            StopCoroutine(drawCoroutine);

        isDrawStopped = false;

        if (currentPicks.Count < minPicks)
        {
            PlaySound("Invalid_Pick");
            yield return null;
        }

        ResetButtonStates();

        // build a pool of all non-picked numbers
        var missPool = Enumerable.Range(1, 80)
                                 .Where(n => !currentPicks.Contains(n))
                                 .ToList();
        if (!isMakeDraws)
        {
            draws.Clear();
            for (int i = 0; i < 1; i++)
            {
                int idx = Random.Range(0, missPool.Count);
                draws.Add(missPool[idx]);
                missPool.RemoveAt(idx);
            }

            // 2) Last 2 draws: truly random from what's left (can hit or miss),
            //    but still unique overall
            var remainingPool = Enumerable.Range(1, 80)
                                          .Except(draws)
                                          .ToList();
            for (int i = 0; i < 19; i++)
            {
                int idx = Random.Range(0, remainingPool.Count);
                draws.Add(remainingPool[idx]);
                remainingPool.RemoveAt(idx);
            }
        }


        // 3) Display and color
        drawCoroutine = StartCoroutine(AnimateDrawSequence(draws));


        // 4) Lookup multiplier & apply “last‐ball” rule
    }



    bool isMakeDraws = false;
    [ContextMenu("ResponseAnimateDrawSequence")]
    public void ResponseAnimateDrawSequence(List<int> responseDraws)
    {
        draws.Clear();
        isMakeDraws = true;
        draws.AddRange(responseDraws);

        if (!EnsureDrawDeps()) return;
        EnterPlayingState();

        StartCoroutine(PlayRoutine());
    }


    private IEnumerator AnimateDrawSequence(List<int> draws)
    {
        StopRotation();
        if (!EnsureDrawDeps())
        {
            Debug.Log("Deepak -- Broken");
            yield break;
        }
        stopButton.interactable = true;
        int currentIndex = 0;
        //KenoSoundManager.Instance.PlayMusic("Drawing_Balls");
        foreach (int n in draws)
        {
            currentIndex++;

            if (isDrawStopped)
            {
                for (int i = currentIndex - 1; i < draws.Count; i++)
                {
                    int drawNumber = draws[i];

                    GameObject go = ballPool.SpawnAt(target1.position, drawnBallContainer.parent);
                    var ballText = go.GetComponentInChildren<TMP_Text>(true);
                    ballText.text = drawNumber.ToString();

                    bool isHit = currentPicks.Contains(drawNumber);
                    numberButtons[drawNumber - 1].SetState(isHit ? HexagonKenoButtonState.Hit : HexagonKenoButtonState.Drawn);

                    var anim = go.GetComponent<Animator>();
                    if (isHit)
                    {
                        ballText.color = Color.yellow;
                        if (anim) anim.enabled = true;
                    }

                    ballPool.FinalizeIntoGrid(go);
                }
                TogglePlayBUtton();
                Payout();
                UpdateCoinIntoBD();
                yield break;
            }

            // Instantiate the ball at target1
            GameObject db = ballPool.SpawnAt(target1.position, drawnBallContainer.parent);

            var text = db.GetComponentInChildren<TMP_Text>(true);
            text.text = n.ToString();
            db.name = "DrawnBall" + n;

            var animator = db.GetComponent<Animator>();
            if (animator) animator.enabled = false;

            bool ishit = currentPicks.Contains(n);
            if (ishit)
            {
                text.color = Color.yellow;
                if (animator) animator.enabled = true;
            }

            // Temporarily move to layout container to get target position
            db.transform.SetParent(drawnBallContainer);
            db.transform.localScale = Vector3.one;

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)drawnBallContainer);

            db.transform.SetParent(drawnBallContainer.parent);
            db.transform.position = target1.position;


            if (currentIndex != 21)
            {
                //PlaySound("Each_Hit");
                yield return db.transform.DOMove(target2.position, moveDuration)
                                            .SetEase(Ease.InOutSine).WaitForCompletion();
                yield return db.transform.DOMove(target3.position, moveDuration)
                                            .SetEase(Ease.InOutSine).WaitForCompletion();
                if (currentIndex % 3 == 0)
                {
                    target2.position = target3.position + new Vector3(0, 0.72f, 0);
                    target3.position = baseTarget3.position;
                    target3.position = target3.position + new Vector3(0, 0.72f, 0);
                    baseTarget3.position = target3.position;
                }
                else
                {
                    target3.position = target3.position + new Vector3(0.815f, 0, 0);
                }
            }
            // Final placement
            db.transform.SetParent(drawnBallContainer);
            db.transform.localScale = Vector3.one;

            numberButtons[n - 1].SetState(currentPicks.Contains(n) ? HexagonKenoButtonState.Hit : HexagonKenoButtonState.Drawn);


            yield return new WaitForSeconds(0.01f);
        }

        TogglePlayBUtton();
        Payout();
        UpdateCoinIntoBD();
    }

    private void UpdateCoinIntoBD()
    {
        if (currentSpinResult.winAmount > 0)
        {
            UpdateWinAmount(currentSpinResult.winAmount);
            balance += currentSpinResult.winAmount;
        }
        else
        {
            winAmount_Text.text = $"0.00";
        }
    }

    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private float currentSpinWin;
    public void UpdateWinAmount(float winAmount)
    {
        if (winAmount > 0)
        {
            currentSpinWin = winAmount;
            PlayTextAnimation(currentSpinWin);
            UpdateGameCoin();

        }
        else
        {
            currentSpinWin = 0;
            this.winAmount_Text.text = "0.00";
        }
        UpdateGameCoin();
    }


    private void PlayTextAnimation(float winAmount)
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);

        textAnimationCoroutine = StartCoroutine(AnimateToValue(winAmount, 1f, this.winAmount_Text, false));
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
            textToAnimate.text = displayed.ToString("0.00");

            timer += Time.deltaTime;
            yield return null;
        }

        textToAnimate.text = target.ToString("0.00");
    }
    #endregion

    #region Picking Numbers
    public void OnNumberClicked(int number)
    {
        if (!canAddPick) return;

        var btnGO = numberButtons[number - 1].gameObject;

        if (currentPicks.Contains(number))
        {
            PlaySound("UnPick");
            currentPicks.Remove(number);

            // remove first matching GO if present
            int idx = currentPikGameObj.IndexOf(btnGO);
            if (idx >= 0) currentPikGameObj.RemoveAt(idx);

            numberButtons[number - 1].SetState(HexagonKenoButtonState.Unselected);
        }
        else if (currentPicks.Count < maxPicks)
        {
            PlaySound("Pick");
            currentPicks.Add(number);

            // avoid duplicates in GO list
            if (!currentPikGameObj.Contains(btnGO))
                currentPikGameObj.Add(btnGO);

            numberButtons[number - 1].SetState(HexagonKenoButtonState.Selected);
        }
        else
        {
            PlaySound("Invalid_Pick");
        }

        UpdatePickCountDisplay();
        UpdatePlayButton();
        UpdatePayTableDisplay();
    }
    #endregion

    #region Play Button State
    void TogglePlayBUtton()
    {
        EnterResultState();
    }
    #endregion

    #region Payout
    private void Payout()
    {
        int hits = draws.Count(n => currentPicks.Contains(n));
        float mult = 0f;
        if (payTable.TryGetValue(currentPicks.Count, out var row) &&
            row.TryGetValue(hits, out var baseMult))
        {
            mult = baseMult;

            foreach (Transform rowTransform in hitPayContainer)
            {
                var hitText = rowTransform.GetChild(0).GetComponent<TMP_Text>();
                var glowImage = rowTransform.GetComponent<Image>();

                if (hitText != null && glowImage != null)
                {
                    bool isHit = hitText.text == hits.ToString();
                    glowImage.DOKill();

                    if (isHit)
                    {
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

        float betAmt = betOptions[currentBetIndex];
        balance -= betAmt;
    }
    #endregion

    #region Game States
    private void EnterIdleState()
    {
        // Ready to pick/adjust
        canAddPick = true;
        playButton.gameObject.SetActive(true);
        playButton.interactable = currentPicks.Count >= minPicks && currentPicks.Count <= maxPicks;

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
        stopButton.interactable = false;

        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        increaseBetButton.interactable = false;
        decreaseBetButton.interactable = false;
        maxBetButton.interactable = false;
        openInfoButton.interactable = false;
        Exit.interactable = false;
    }

    private void EnterErrorState()
    {
        canAddPick = true;
        isRoundInProgress = false;
        isGameReady = false;

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


    private void EnterResultState()
    {
        // After round settles; keep picks frozen until wipe
        canAddPick = false;
        isRoundInProgress = false;

        playButton.gameObject.SetActive(true);
        // Only enable if user still meets conditions (e.g., keeping same picks)
        playButton.interactable = currentPicks.Count >= minPicks && currentPicks.Count <= maxPicks;

        stopButton.gameObject.SetActive(false);
        stopButton.interactable = false;

        autoPickButton.interactable = true;
        wipeButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
        maxBetButton.interactable = true;
        openInfoButton.interactable = true;
        Exit.interactable = true;

        // restore moving targets if they were created
        if (initialTarget3 != null && initialTarget2 != null)
        {
            target2.position = initialTarget2.position;
            target3.position = initialTarget3.position;
        }
        if (baseTarget2 != null && baseTarget3 != null && initialTarget2 != null && initialTarget3 != null)
        {
            Destroy(baseTarget2.gameObject);
            Destroy(baseTarget3.gameObject);
            Destroy(initialTarget2.gameObject);
            Destroy(initialTarget3.gameObject);
        }

        isGameReady = true;
    }

    private void StartRound()
    {
        if (isRoundInProgress) return;
        if (currentPicks.Count < minPicks || currentPicks.Count > maxPicks)
        {
            PlaySound("Invalid_Pick");
            instructions.text = $"Pick between {minPicks} and {maxPicks} numbers.";
            return;
        }

        SetTargert();
        instructions.text = "Good Luck!";
        EnterPlayingState();
        SpinHexaKeno();
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
    #endregion

    #region APICallForRsult

    public void SpinHexaKeno()
    {
        if (string.IsNullOrEmpty(SceneManagement.currentGameID))
        {
            return;
        }
        if (balance < betOptions[currentBetIndex])
        {
            StartCoroutine(wipeIt());
        }
        else
        {
            GameBetServices.Instance.TrySpinWithCurrentBet(betOptions[currentBetIndex]);
            UpdateCoins();

            HexaKenoRequest requestData = new HexaKenoRequest
            {
                requestId = Guid.NewGuid().ToString(),
                gameId = SceneManagement.currentGameID,
                betAmount = betOptions[currentBetIndex],
                playerPicks = currentPicks,
            };
            
            StartCoroutine(SendHexaKenoSpinRequest(requestData));
        }
    }

    IEnumerator wipeIt()
    {
        stopButton.interactable = false;
        isSamePicks = true;
        yield return StartCoroutine(WipeRoutine(false));
        foreach(Transform row in hitPayContainer)
        {
            var glow = row.GetComponent<Image>();
            if (glow != null && glow.enabled)
            {
                glow.enabled = false;
            }
        }
        stopButton.interactable = true;

        CasinoUIManager.Instance.ShowErrorCanvas(1, "Insufficient Balance");
        EnterErrorState();
        yield return null;
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
    public bool responseReceived;
    public void StopRotation()
    {
        tween?.Kill();
        spinnerImage.gameObject.SetActive(false);
    }

    IEnumerator SendHexaKenoSpinRequest(HexaKenoRequest data)
    {


        stopButton.interactable = false;
        responseReceived = false;
        isSamePicks = true;
        yield return StartCoroutine(WipeRoutine(false));
        foreach (Transform row in hitPayContainer)
        {
            var glow = row.GetComponent<Image>();
            if (glow != null && glow.enabled)
            {
                glow.enabled = false;
            }
        }
        instructions.text = "Loading Your Play Request";
        string json = JsonUtility.ToJson(data);
        Debug.Log("📦 Sending payload: " + json);

        UnityWebRequest request = new UnityWebRequest(ApiEndpoints.hexagonekeno, "POST");
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
            yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.hexagonekeno, json, "POST", ()=> SendHexaKenoSpinRequest(data));
            yield break;
        }
        if (request.result == UnityWebRequest.Result.Success)
        {
            responseReceived = true;
            Debug.Log("✅ Spin result received:");
            Debug.Log(request.downloadHandler.text);

            currentSpinResult = JsonUtility.FromJson<HexaKenoResponse>(request.downloadHandler.text);
            Debug.Log($"🎯 Hit Count: {currentSpinResult.hitCount}, Win: {currentSpinResult.isWin}, Win Amount: {currentSpinResult.winAmount}");

            winning = 0;
            if (currentSpinResult.winAmount > 0)
            {
                winning = currentSpinResult.winAmount;
            }
            ResponseAnimateDrawSequence(currentSpinResult.drawnNumbers);
        }
        else
        {
            StopRotation();
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network error. Please try again.");
            ReturnBetAmount();
            isRoundInProgress = false;
            EnterIdleState();
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
    private bool EnsureDrawDeps()
    {
        if (ballPool == null)
        {
            ballPool = GetComponentInChildren<HexagonKenoDrawnBallPool>(includeInactive: true);
            if (ballPool == null)
            {
                return false;
            }
        }

        if (target1 == null || target2 == null || target3 == null)
        {
            return false;
        }

        if (baseTarget2 == null || baseTarget3 == null || initialTarget2 == null || initialTarget3 == null)
            SetTargert();

        return true;
    }

    #endregion
}


[Serializable]
public class HexaKenoRequest
{
    public string requestId;
    public string gameId;
    public float betAmount;
    public List<int> playerPicks;
}

[Serializable]
public class HexaKenoResponse
{
    public string requestId;
    public string gameId;
    public List<int> playerPicks;
    public List<int> drawnNumbers;
    public int hitCount;
    public float betAmount;
    public float winAmount;
    public float totalWin;
    public float newBalance;
    public bool isWin;
    public string timestamp;
}