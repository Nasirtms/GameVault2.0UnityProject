using DG.Tweening;
using DG.Tweening.Core.Easing;
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
using Random = UnityEngine.Random;

public class SuperBallKeno : MonoBehaviour
{
    #region In Game References
    public static SuperBallKeno Instance;
    public SuperBallGameSettings settings;

    [SerializeField] private SuperBallKenoDrawnBallPool ballPool;
    [SerializeField] private SuperBallKenoResponse currentSpinResult;

    [Header("Grid of 80 Toggles (assign in inspector)")]
    public List<SuperBallKenoButton> numberButtons;
    public GameObject btnPrefab;

    [SerializeField] private float bounceHeight = 80f;
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private float bounceDuration = 0.5f;

    private Tween tween;
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("UI Buttons")]
    public Button playButton;
    public Button stopButton;
    public Button autoPickButton;
    public Button wipeButton;
    public Button increaseBetButton;
    public Button decreaseBetButton;
    public Button openInfoButton;
    public Button Exit;
    public Button ExitButton1;
    public Button ExitButton2;
    public Button InfoPanalBetIncrease;
    public Button InfoPanalBetDecrease;
    public Button PreviousPage;
    public Button NextPage;

    [Header("Panels / Displays")]
    public GameObject infoPanelPage1;
    public GameObject infoPanelPage2;
    public TMP_Text betText;
    public TMP_Text picksCountText;
    public TMP_Text infoPanelBetText;

    [Header("Balance")]
    [SerializeField] public TMP_Text balanceText;
    [SerializeField] private float balance;
    [SerializeField]  public TMP_Text winAmount_Text;
    public Transform hitPayContainer;
    public GameObject hitPayRowPrefab;
    public Transform fullPayTableContainer;
    public GameObject payRowPrefab;
    public Transform drawnBallContainer;
    public GameObject drawnBallPrefab;
    public Image lastDrawBg;
    public TMP_Text ballText;

    public Transform GridParent;
    GameObject drawsLastObject;

    private Coroutine drawCoroutine;
    private bool isDrawStopped = false;
    private bool isGameReady = true;
    public bool canAddPick = true;
    private bool isRoundInProgress = false;
    private bool wipedForAutoPick = false;


    public Transform target1;
    public Transform target2;
    public Transform target3;
    public Coroutine textAnimationCoroutine;



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
        {2, new Dictionary<int,float>{{0,0},{1,1},{2,6}}},
        {3, new Dictionary<int,float>{{0,0},{1,0},{2,2},{3,27}}},
        {4, new Dictionary<int,float>{{0,0},{1,0},{2,1},{3,6},{4,50}}},
        {5, new Dictionary<int,float>{{0,0},{1,0},{2,1},{3,2},{4,10},{5,100}}},
        {6, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,2},{4,7},{5,31},{6,150}}},
        {7, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,1},{4,4},{5,14},{6,60},{7,200}}},
        {8, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,1},{4,2},{5,6},{6,21},{7,100},{8,500}}},
        {9, new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,0},{4,2},{5,4},{6,16},{7,90},{8,200},{9,1000}}},
        {10,new Dictionary<int,float>{{0,0},{1,0},{2,0},{3,0},{4,1},{5,4},{6,8},{7,25},{8,125},{9,500},{10,1000}}}
    };
#endregion

    #region Unity Methods
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
    }

    void Start()
    {
        GameBetServices.Instance.SetActiveUI(this, balanceText, UpdateCoins);
        winAmount_Text.text = "0.00";
        lastDrawBg.gameObject.SetActive(false);
        UpdateSlotServicesGameName();
        balance = UserManager.Instance.Coins;
        UserManager.Instance.currentBetAmount = 0;
        UpdateCoins();
        playButton.interactable = false;
        stopButton.gameObject.SetActive(false);


        stopButton.onClick.AddListener(() =>
        {
            isDrawStopped = true;
        });

        // hook buttons
        autoPickButton.onClick.AddListener(() =>
        {
            AutoPick();
            PlaySound("Wipe");
        });
        increaseBetButton.onClick.AddListener(IncreaseBet);
        decreaseBetButton.onClick.AddListener(DecreaseBet);
        InfoPanalBetIncrease.onClick.AddListener(() =>
        {
            InfoPanalBetInc();
            PlaySound("Help_PanelButton");
        });
        InfoPanalBetDecrease.onClick.AddListener(() =>
        {
            InfoPanalBetDec();
            PlaySound("Help_PanelButton");
        });
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
            PlaySound("Wipe");
            isGameReady = true;
            autoPickButton.interactable = false;
            isSamePicks = false;
            WipePicks();
            playButton.interactable = true;
            autoPickButton.interactable = true;
            increaseBetButton.interactable = true;
            decreaseBetButton.interactable = true;
        });
        openInfoButton.onClick.AddListener(() =>
        {
            PlaySound("Help_Exit");
            infoPanelPage1.SetActive(true);
        });
        NextPage.onClick.AddListener(() =>
        {
            PlaySound("Help_PanelButton");
            FullPayTable();
            InfoPanalBetInc();
            InfoPanalBetDec();
            infoPanelPage2.SetActive(true);
            infoPanelPage1.SetActive(false);
        });
        PreviousPage.onClick.AddListener(() =>
        {
            PlaySound("Help_PanelButton");
            infoPanelPage1.SetActive(true);
            infoPanelPage2.SetActive(false);
        });
        ExitButton1.onClick.AddListener(() =>
        {
            PlaySound("Help_Exit");
            infoPanelPage1.SetActive(false);
        });
        ExitButton2.onClick.AddListener(() =>
        {
            PlaySound("Help_Exit");
            infoPanelPage2.SetActive(false);
        });

        // initial UI
        infoPanelPage1.SetActive(false);
        infoPanelPage2.SetActive(false);
        UpdateBetDisplay();
        UpdatePickCountDisplay();
        UpdatePlayButton();
        UpdatePayTableDisplay();
        FullPayTable();
        Exit.onClick.AddListener(OnExit);

        UserManager.Instance.UpdateGameCoins += UpdateCoins;
    }
    private void OnDestroy()
    {
        UserManager.Instance.UpdateGameCoins -= UpdateCoins;
    }
    #endregion

    #region Listener Methods
    public void PlaySound(string soundName)
    {
        if (soundName == null) return;
        if (!KenoSoundManager.Instance.IsSoundMute())
            KenoSoundManager.Instance.PlaySFX(soundName);
    }

    #region Machine Registery


    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }

    #endregion

    void OnExit()
    {
        if (isRoundInProgress) return;
        PlaySound("Help_Exit");
        Destroy(gameObject);
        if (UserManager.Instance != null)
        {
            UserManager.Instance.StartUpdateCanAddCoin(true);
        }
        SceneManager.LoadScene("Main");
    }
#endregion

    #region Buttton States
    void SetGameButtons()
    {
        for (int i = 0; i < 80; i++)
        {
            if (i < 40)
            {
                GameObject go = Instantiate(btnPrefab, GridParent.GetChild(0));
                numberButtons.Add(go.GetComponent<SuperBallKenoButton>());
                var superBallKenoButton = go.GetComponent<SuperBallKenoButton>();
                int number = i + 1;

                go.name = "Button" + number;
                superBallKenoButton.number = number;
                superBallKenoButton.numberText.text = number + "";
                superBallKenoButton.numberTextShadow.text = number + "";

                go.GetComponent<SuperBallKenoButton>().button.onClick.AddListener(() => OnNumberClicked(number));
            }
            else
            {
                GameObject go = Instantiate(btnPrefab, GridParent.GetChild(1));
                numberButtons.Add(go.GetComponent<SuperBallKenoButton>());
                var superBallKenoButton = go.GetComponent<SuperBallKenoButton>();

                int number = i + 1;
                go.name = "Button" + number;
                superBallKenoButton.number = number;
                superBallKenoButton.numberText.text = number + "";
                superBallKenoButton.numberTextShadow.text = number + "";
                superBallKenoButton.button.onClick.AddListener(() => OnNumberClicked(number));
            }

        }
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
                numberButtons[i].SetState(KenoButtonState.Selected);
            else
                numberButtons[i].SetState(KenoButtonState.Unselected);
        }
    }
    #endregion

    #region Auto Picks
    public void AutoPick()
    {
        if (isRoundInProgress || !canAddPick) return;
        autoPickButton.interactable = false;
        wipeButton.interactable = false;
        playButton.interactable = false;

        StartCoroutine(AutoPickRoutine());
    }

    private IEnumerator AutoPickRoutine()
    {
        int currentCount = Mathf.Clamp(currentPicks.Count, 0, 10);
        int targetCount = (currentCount == 0) ? 10 : currentCount;

        isSamePicks = false;

        if (currentPicks.Count > 0)
        {
            EnterWipingState();
            yield return StartCoroutine(WipeRoutine(true));
        }

        yield return new WaitForSeconds(0.15f);

        HashSet<int> picks = new HashSet<int>();
        while (picks.Count < targetCount)
        {
            picks.Add(Random.Range(1, 81));
        }

        currentPicks.Clear();

        foreach (int n in picks)
        {
            OnNumberClicked(n);
            yield return new WaitForSeconds(0.05f);
        }

        UpdatePickCountDisplay();
        UpdatePlayButton();
        UpdatePayTableDisplay();

        wipeButton.interactable = true;
        playButton.interactable = true;
        autoPickButton.interactable = true;
    }
    #endregion

    #region Wipe Picks
    public bool isSamePicks;
    public void WipePicks()
    {
        EnterWipingState();
        StartCoroutine(WipeRoutine(true));
    }

    private IEnumerator WipeRoutine(bool restoreIdleAtEnd)
    {
        lastDrawBg.gameObject.SetActive(false);

        try
        {
            if (currentPicks.Count > 0)
            {
                // --- Cache ball objects ---
                List<Transform> balls = new List<Transform>(drawnBallContainer.childCount);
                foreach (Transform child in drawnBallContainer)
                    balls.Add(child);

                int pickCount = currentPikGameObj.Count;
                int ballCount = balls.Count;
                int maxCount = pickCount > ballCount ? pickCount : ballCount;

                for (int i = 0; i < maxCount; i++)
                {
                    PlaySound("Pick_Number");

                    // --- Hide Drawn Balls ---
                    if (i < ballCount)
                    {
                        Transform ball = balls[i];
                        ball.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack);
                        yield return new WaitForSeconds(0.05f);

                        ballPool.Release(ball.gameObject);
                    }

                    // --- Reset Picked Buttons ---
                    if (!isSamePicks && i < pickCount)
                    {
                        SuperBallKenoButton btn = currentPikGameObj[i].GetComponent<SuperBallKenoButton>();
                        if (btn != null)
                            btn.SetState(KenoButtonState.Unselected);

                        yield return new WaitForSeconds(0.05f);
                    }
                }

                // --- Hide last object icon ---
                if (drawsLastObject != null && drawsLastObject.transform.childCount > 1)
                {
                    Transform child = drawsLastObject.transform.GetChild(1);
                    if (child != null)
                        child.gameObject.SetActive(false);
                }

                // --- Clean Lists ---
                if (!isSamePicks)
                {
                    currentPikGameObj.Clear();
                    currentPicks.Clear();
                }
            }

            ResetButtonStates();
            winAmount_Text.text = "0.00";
            UpdatePickCountDisplay();
            UpdatePayTableDisplay();
        }
        finally
        {
            // --- Restore State ---
            canAddPick = restoreIdleAtEnd;

            if (restoreIdleAtEnd)
            {
                wipedForAutoPick = false;
                EnterIdleState();
            }
            else
            {
                EnterPlayingState();
            }
        }
    }


    #endregion

    #region Bet Management
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
    public void InfoPanalBetInc()
    {
        currentBetIndex = (currentBetIndex + 1) % betOptions.Length;
        infoPanelBetText.text = $"{betOptions[currentBetIndex]:0.00}";
        FullPayTable();
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
    public void InfoPanalBetDec()
    {
        currentBetIndex = (currentBetIndex - 1 + betOptions.Length) % betOptions.Length;
        infoPanelBetText.text = $"{betOptions[currentBetIndex]:0.00}";
        FullPayTable();
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
#endregion

    #region Pay Table Displays
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

            foreach (var kv in row.OrderByDescending(k => k.Key))
            {
                int hits = kv.Key;
                float mult = kv.Value;

                if (mult > 0f)
                {
                    float payout = mult * betOptions[currentBetIndex];

                    GameObject rowObj = Instantiate(hitPayRowPrefab, hitPayContainer);
                    rowObj.SetActive(true);

                    var hitText = rowObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (hitText != null)
                        hitText.text = hits.ToString();

                    var payText = rowObj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (payText != null)
                        payText.text = payout.ToString("0.00");
                }
            }
        }
    }
    private void FullPayTable()
    {

        foreach (Transform child in fullPayTableContainer)
        {
            Destroy(child.gameObject);
        }
        float[,] payouts = new float[11, 9];

        for (int hit = 0; hit <= 10; hit++)
        {
            for (int i = 0; i <= 8; i++)
            {
                int picks = i + 2;
                if (payTable.TryGetValue(picks, out var row) &&
                    row.TryGetValue(hit, out var mult) &&
                    mult > 0f)
                {
                    payouts[hit, i] = mult * betOptions[currentBetIndex];
                }
            }
        }
        int[] maxHitWithValue = new int[9];
        for (int col = 0; col <= 8; col++)
        {
            for (int row = 10; row >= 0; row--)
            {
                if (payouts[row, col] > 0f)
                {
                    maxHitWithValue[col] = row;
                    break;
                }
            }
        }
        for (int hit = 0; hit <= 10; hit++)
        {
            GameObject rowObj = Instantiate(payRowPrefab, fullPayTableContainer);
            rowObj.SetActive(true);

            for (int i = 0; i <= 8; i++) // Picks 2–10
            {
                var textComponent = rowObj.transform.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (textComponent == null) continue;

                float payout = payouts[hit, i];
                if (payout > 0f)
                {
                    textComponent.text = payout.ToString("0.00");
                    textComponent.color = Color.green;
                }
                else if (hit <= maxHitWithValue[i])
                {
                    textComponent.text = "0.00";
                    textComponent.color = Color.yellow;
                }
                else
                {
                    textComponent.text = "";
                }
            }
        }
    }

    public List<int> draws = new List<int>();
#endregion

    #region Play Round
    private IEnumerator PlayRoutine()
    {
        SpriteManager.Instance.OnPlayButtonClicked();
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

        int hits = draws.Count(n => currentPicks.Contains(n));

        drawCoroutine = StartCoroutine(AnimateDrawSequence(draws));

        float mult = 0f;
        if (payTable.TryGetValue(currentPicks.Count, out var row) &&
            row.TryGetValue(hits, out var baseMult))
            mult = baseMult;

        if (currentPicks.Contains(draws[19]))
        {
            PlaySound("LastBall_Draw");
            SpriteManager.Instance.ShowWinner();
            mult *= 4f;
        }

        float betAmt = betOptions[currentBetIndex];
        balance -= betAmt;
    }


    void LastHitBtn(GameObject go)
    {
        go.transform.GetChild(1).gameObject.SetActive(true);
        go.transform.GetChild(1).GetComponent<Animator>().SetTrigger("LastHit");
        go.transform.GetChild(1).GetComponent<Image>().enabled = true;
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
        int currentIndex = 0;
        KenoSoundManager.Instance.PlayMusic("Drawing_Balls");
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
                    go.name = "DrawnBall" + drawNumber;
                    go.transform.localScale = Vector3.one;

                    ballPool.FinalizeIntoGrid(go);
                    numberButtons[drawNumber - 1].SetState(currentPicks.Contains(drawNumber) ? KenoButtonState.Hit : KenoButtonState.Drawn);

                    if (i == draws.Count - 1)
                    {
                        KenoSoundManager.Instance.StopMusic("Drawing_Balls");
                        lastDrawBg.gameObject.SetActive(true);
                        drawsLastObject = numberButtons[drawNumber - 1].gameObject;
                        LastHitBtn(drawsLastObject);
                        if (numberButtons[i]._state == KenoButtonState.Hit)
                        {
                            PlaySound("Super_Hit");
                        }
                    }
                }
                TogglePlayBUtton();
                UpdateCoinIntoBD();
                yield break;
            }

            // Instantiate the ball at target1
            GameObject db = ballPool.SpawnAt(target1.position, drawnBallContainer.parent);

            var text = db.GetComponentInChildren<TMP_Text>(true);
            text.text = n.ToString();
            db.name = "DrawnBall" + n;

            // Temporarily move to layout container to get target position
            db.transform.SetParent(drawnBallContainer);

            db.transform.localScale = Vector3.one;

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)drawnBallContainer);
            Vector3 finalWorldPos;
            //if (isSamePicks && currentIndex == 1)
            //{
            //    finalWorldPos = new Vector3(3.96f, -4.20f, 90.00f);
            //}
            //else
            //{
               finalWorldPos = db.transform.position;
            //}
            db.transform.SetParent(drawnBallContainer.parent);
            db.transform.position = target1.position;

            if (currentIndex == 20)
            {
                KenoSoundManager.Instance.StopMusic("Drawing_Balls");
                lastDrawBg.gameObject.SetActive(true);
                yield return db.transform.DOMove(numberButtons[draws[currentIndex - 1] - 1].transform.position, moveDuration)
                                         .SetEase(Ease.InOutSine).WaitForCompletion();
                drawsLastObject = numberButtons[draws[currentIndex - 1] - 1].gameObject;
                LastHitBtn(drawsLastObject);
            }

            if (currentIndex != 20)
            {
                //Debug.Log("Current Index: " + currentIndex);
                if (currentIndex % 2 == 0)
                {
                    PlaySound("Each_Hit");
                    yield return db.transform.DOMove(target2.position, moveDuration)
                                             .SetEase(Ease.InOutSine).WaitForCompletion();
                }
                else
                {
                    PlaySound("Each_Hit");
                    yield return db.transform.DOMove(target3.position, moveDuration)
                                             .SetEase(Ease.InOutSine).WaitForCompletion();
                }

            }

                yield return db.transform.DOMove(finalWorldPos, moveDuration)
                         .SetEase(Ease.InOutSine).WaitForCompletion();

            float baseBounceHeight = bounceHeight;
            if (currentIndex != 20)
            {
                // Step 3: Bounce in place after landing
                float adjustedBounceHeight = baseBounceHeight * Mathf.Lerp(1f, 0.1f, currentIndex / 20f);
                Sequence bounce = DOTween.Sequence();
                bounce.Append(db.transform.DOMoveY(finalWorldPos.y + adjustedBounceHeight, bounceDuration / 2f).SetEase(Ease.OutSine));
                bounce.Append(db.transform.DOMoveY(finalWorldPos.y, bounceDuration / 2f).SetEase(Ease.InSine));

                yield return bounce.WaitForCompletion();
            }
            // Final placement
            db.transform.SetParent(drawnBallContainer);
            db.transform.localScale = Vector3.one;

            numberButtons[n - 1].SetState(currentPicks.Contains(n) ? KenoButtonState.Hit : KenoButtonState.Drawn);


            yield return new WaitForSeconds(0.01f);
        }

        TogglePlayBUtton();
        UpdateCoinIntoBD();
    }

    private void UpdateCoinIntoBD()
    {
        if (currentSpinResult.winAmount> 0)
        {
            UpdateWinAmount(currentSpinResult.winAmount);
            balance += currentSpinResult.winAmount;
        }
        else
        {
            winAmount_Text.text = $"0.00";
        }
    }
    #endregion

    #region Text Animation

    private float currentSpinWin;

    public void UpdateWinAmount(float winAmount)
    {
        if (winAmount > 0)
        {
            currentSpinWin = winAmount;
            PlayTextAnimation(currentSpinWin);
            Invoke(nameof(UpdateGameCoin), 1f);
        }
        else
        {
            currentSpinWin = 0;
            this.winAmount_Text.text = "0.00";
        }
        Invoke(nameof(UpdateGameCoin), 1f);
    }

    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
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
    #region Button Handlers
    public void OnNumberClicked(int number)
    {
        if (!canAddPick) return;

        var btnGO = numberButtons[number - 1].gameObject;

        if (currentPicks.Contains(number))
        {
            PlaySound("Pick_Number");
            currentPicks.Remove(number);
            int idx = currentPikGameObj.IndexOf(btnGO);
            if (idx >= 0) currentPikGameObj.RemoveAt(idx);
            numberButtons[number - 1].SetState(KenoButtonState.Unselected);
        }
        else if (currentPicks.Count < maxPicks)
        {
            PlaySound("Pick_Number");
            currentPicks.Add(number);
            if (!currentPikGameObj.Contains(btnGO))
                currentPikGameObj.Add(btnGO);
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
    void TogglePlayBUtton()
    {
        EnterResultState();
    }
    #endregion

    #region APICallForRsult

    public void SpinKeno()
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

            SuperBallKenoRequest requestData = new SuperBallKenoRequest
            {
                requestId = Guid.NewGuid().ToString(),
                gameId = SceneManagement.currentGameID,
                betAmount = betOptions[currentBetIndex],
                playerPicks = currentPicks,
            };

            StartCoroutine(SendKenoSpinRequest(requestData));
        }
    }
    IEnumerator wipeIt()
    {
        stopButton.interactable = false;
        isSamePicks = true;
        yield return StartCoroutine(WipeRoutine(false));
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


    public void StopRotation()
    {
        tween?.Kill();
        spinnerImage.gameObject.SetActive(false);
    }
    public bool responseReceived;
    IEnumerator SendKenoSpinRequest(SuperBallKenoRequest data)
    {

        stopButton.interactable = false;
        responseReceived = false;
        isSamePicks = true;
        yield return StartCoroutine(WipeRoutine(false));

        string json = JsonUtility.ToJson(data);
        Debug.Log("📦 Sending payload: " + json);

        UnityWebRequest request = new UnityWebRequest(ApiEndpoints.superBallKeno, "POST");
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
            yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.superBallKeno, json, "POST",()=> SendKenoSpinRequest(data));
            yield break;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            responseReceived = true;
            Debug.Log("✅ Spin result received:");
            Debug.Log(request.downloadHandler.text);

            SuperBallKenoResponse response = JsonUtility.FromJson<SuperBallKenoResponse>(request.downloadHandler.text);
            currentSpinResult = response;
            //Debug.Log($"🎯 Hit Count: {currentSpinResult.hitCount}, Win: {currentSpinResult.isWin}, Win Amount: {currentSpinResult.winAmount}");
            ResponseAnimateDrawSequence(currentSpinResult.drawnNumbers);
        }
        else
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network error. Please try again.");
            ReturnBetAmount();
            wipedForAutoPick = true;
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

    #endregion

    #region Game States
    // --- NEW: central UI states (no enums)
    private void EnterIdleState()
    {
        canAddPick = true;
        isRoundInProgress = false;

        playButton.gameObject.SetActive(true);
        playButton.interactable = currentPicks.Count >= minPicks &&
                                  currentPicks.Count <= maxPicks;

        stopButton.gameObject.SetActive(false);
        stopButton.interactable = false;
        if (wipedForAutoPick)
        {
            autoPickButton.interactable = true;
        }
        wipeButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
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
        openInfoButton.interactable = true;
        Exit.interactable = true;
    }

    private void EnterResultState()
    {
        canAddPick = false;          
        isRoundInProgress = false;
        isGameReady = true;

        playButton.gameObject.SetActive(true);
        playButton.interactable = currentPicks.Count >= minPicks &&
                                  currentPicks.Count <= maxPicks;

        stopButton.gameObject.SetActive(false);
        stopButton.interactable = false;

        autoPickButton.interactable = true;
        wipeButton.interactable = true;
        increaseBetButton.interactable = true;
        decreaseBetButton.interactable = true;
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
        openInfoButton.interactable = false;
        Exit.interactable = false;
    }
    private void StartRound()
    {
        if (isRoundInProgress) return;
      
        if (currentPicks.Count < minPicks || currentPicks.Count > maxPicks)
        {
            PlaySound("Invalid_Pick");
            return;
        }
        EnterPlayingState();
        SpinKeno();
    }

    private bool EnsureDrawDeps()
    {
        if (ballPool == null)
        {
            ballPool = GetComponentInChildren<SuperBallKenoDrawnBallPool>(includeInactive: true);
            if (ballPool == null)
            {
                return false;
            }
        }

        if (target1 == null || target2 == null || target3 == null)
        {
            return false;
        }
        return true;
    }

    #endregion
}


[Serializable]
public class SuperBallKenoRequest
{
    public string requestId;
    public string gameId;
    public float betAmount;
    public List<int> playerPicks;
}

[Serializable]
public class SuperBallKenoResponse
{
    public string requestId;
    public string gameId;
    public List<int> playerPicks;
    public List<int> drawnNumbers;
    public int superBall;
    public int hitCount;
    public bool superBallHit;
    public float betAmount;
    public float winAmount;
    public float totalWin;
    public float newBalance;
    public bool isWin;
    public string timestamp;
}

