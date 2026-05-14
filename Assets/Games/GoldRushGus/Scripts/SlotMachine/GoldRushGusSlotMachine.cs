using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoldRushGusSlotMachine : BaseSlotMachine
{
    #region Variables
    public static GoldRushGusSlotMachine Instance;

    [Header("Machine References")]
    public GoldRushGusGameSettings settings;
    public List<GoldRushGusReelScript> reels;
    [SerializeField] private GoldRushGusBetController betController;

    //Character Animator
    public Animator villainAnimator;
    public Animator characterAnimator;
    [SerializeField] private float winReturnDelay = 1.5f;
    private Coroutine winReturnCoroutine;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    
    public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    public bool isFreeGameReady;
    public int freeSpinCount;
    public float freeSpinWinAmount;
    public int freeSpinMultiplier;

    //Mini Game
    public bool isMiniGame;
    public bool isMiniGameReady;
    public bool isMiniGameRunning;
    public int miniGameMultiplier;
    public bool instantWin;
    public bool progressiveWin;
    public float treasureChestWinAmount;
    // Win
    private float winAmount = 0f;
    [SerializeField] private TMP_Text winText;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public GoldRushGusSlotType[,] resultMatrix;
    public bool hasSymbol = false;

    [Header("Random Respin")]
    private bool enableRandomRespin;
    private bool isRespinRunning;
    public List<int> reSpinReels = new List<int>();
    private bool respinResultArrived;

    public GameObject reSpinReelEffect;
    private Animator[] reSpinReelAnimator;

    public GameObject lastReelEffect;
    private Animator lastReelAnimator;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        UpdateSlotServicesGameName();
        Initialize();
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        InSpin = false;
        SetHeroCharacterNormal();
        CacheCharacterOriginalPositions();
        CacheRespinAnimators();
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (SpinResultController.Instance != null)
            SpinResultController.Instance.OnSpinResultReceived -= OnSpinResultReceived;
    }

    #endregion

    #region Machine Registery

    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);

        GameSlotRegistry.Register(sceneName, this);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }

    #endregion

    #region Machine Settings
    private void Initialize()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.Initialize();
            }
        }
    }
    #endregion

    #region Spin Result Receive
    [Header("Fake Scatter")]
    public bool isFakeFreeGame;
    public bool isFakeMiniGame;
    public string fakeWinType;
    public float fakeTreasureChestWinAmount;
    public int fakeMiniGameMultiplierIndex;
    public bool isTreasureChestAnimationRunning;
    public void SetSpinResult(SpinResult spinResult)
    {
        currentSpinResult = spinResult;
    }
    public int scatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        //Random Respin
        if (!isRespinRunning)
        {
            reSpinReels.Clear();
            enableRandomRespin = currentSpinResult.isRandomRespinTriggered;

            if (enableRandomRespin && currentSpinResult.respinReels != null && currentSpinResult.respinReels.Count > 0)
            {
                reSpinReels.AddRange(currentSpinResult.respinReels);
            }
        }
        else
        {
            respinResultArrived = true;
        }

        //Free Spin
        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
            freeSpinMultiplier = currentSpinResult.freeSpinMultiplier;
        }
        //Fake FreeGame
        if (isFakeFreeGame)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = 1;
            freeSpinMultiplier = 3;
        }

        //MiniGame
        if (currentSpinResult.isTreasureChestTriggered)
        {
            if (currentSpinResult.treasureChestResult.type == "ProgressiveJackpot")
            {
                isTreasureChestAnimationRunning = true;
                progressiveWin = true;
                treasureChestWinAmount = currentSpinResult.treasureChestResult.amount;
            }
            if (currentSpinResult.treasureChestResult.type == "InstantWin")
            {
                isTreasureChestAnimationRunning = true;
                instantWin = true;
                treasureChestWinAmount = currentSpinResult.treasureChestResult.amount;
            }
            if (currentSpinResult.treasureChestResult.type == "CoinGamble")
            {
                isTreasureChestAnimationRunning = true;
                if (!isMiniGame)
                    isMiniGameReady = true;

                miniGameMultiplier = currentSpinResult.treasureChestResult.coinMultiplier;
            }
        }
        //FakeMiniGame
        if (isFakeMiniGame)
        {
            if (fakeWinType == "ProgressiveJackpot")
            {
                isTreasureChestAnimationRunning = true;
                progressiveWin = true;
                treasureChestWinAmount = fakeTreasureChestWinAmount;
            }
            if (fakeWinType == "InstantWin")
            {
                isTreasureChestAnimationRunning = true;
                instantWin = true;
                treasureChestWinAmount = fakeTreasureChestWinAmount;
            }
            if(fakeWinType == "CoinGamble")
            {
                isTreasureChestAnimationRunning = true;
                if (!isMiniGame)
                    isMiniGameReady = true;

                miniGameMultiplier = fakeMiniGameMultiplierIndex;
            }
        }
        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();
        int reelIndex = 0;
        bool hasFreeSpinOnReel0 = false;
        bool hasFreeSpinOnReel2 = false;
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                if (!isRespinRunning)
                {
                    if (reelIndex == 0 || reelIndex == 2)
                    {
                        var res = GetResourceById(symbol.id);
                        if (res.HasValue && isFreeSpinSlot(res.Value.slotType))
                        {
                            if (reelIndex == 0)
                                hasFreeSpinOnReel0 = true;

                            if (reelIndex == 2)
                                hasFreeSpinOnReel2 = true;
                        }
                    }
                }
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }
        hasSymbol = hasFreeSpinOnReel0 && hasFreeSpinOnReel2;

        GoldRushGusUIManager.Instance.SetStopInteractable(true);
    }
    #endregion

    #region Spin
    public override void Spin()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
        }

        // Start the spin
        spinCoroutine = StartCoroutine(StartSpin());
    }

    private IEnumerator StartSpin()
    {
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            GoldRushGusUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        SetHeroCharacterSpin();

        // Reset Variables
        instantWin = false;
        progressiveWin = false;
        treasureChestWinAmount = 0;
        hasSymbol = false;
        isMiniGame = false;
        isMiniGameReady = false;
        isMiniGameRunning = false;
        miniGameMultiplier = 0;
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        GoldRushGusUIManager.Instance.winAnimationCompleted = true;
        GoldRushGusUIManager.Instance.isTreasureAnimationCompleted = true;
        GoldRushGusUIManager.Instance.StopCurrentSFX();
        GoldRushGusUIManager.Instance.PlaySpinMusic("Spin");

        ClearPaylines();
        GoldRushGusPaylineController.Instance.StopPaylines();
        GoldRushGusPaylineController.Instance.ClearPaylineData();
        GoldRushGusUIManager.Instance.SetStopInteractable(false);

        HashSet<int> activeReels = new HashSet<int>();
        if (isRespinRunning)
        {
            SetReSpinAnimatorForReels(reSpinReels, true);
            for (int i = 0; i < reSpinReels.Count; i++)
                activeReels.Add(reSpinReels[i]);
        }
        else
        {
            //SetReSpinAnimators(false);
            for (int i = 0; i < reels.Count; i++)
                activeReels.Add(i);
        }

        if (settings.spinSettings.startSpin == GoldRushGusSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (!activeReels.Contains(i)) continue;

                var reel = reels[i];
                if (reel != null)
                {
                    SetReelDirection(reel);
                    isResultReceived = false;
                    reel.StartSpin();
                }
            }
        }
        else
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (!activeReels.Contains(i)) continue;

                var reel = reels[i];
                if (reel != null)
                {
                    SetReelDirection(reel);
                    isResultReceived = false;
                    reel.StartSpin();
                    yield return new WaitForSeconds(settings.spinSettings.ReelStartDelay);
                }
            }
        }
        yield return StartCoroutine(WaitForAllReelsToBeSpinning());

        yield return new WaitForSeconds(settings.slotSettings.MinSpinDuration);

        StartSpinWithBackendResult();
    }

    public void StartSpinWithBackendResult()
    {
        StartCoroutine(WaitUntilResultAndThenStop());
    }
    #endregion

    #region Stop & Backend Result
    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 12f;
        float elapsed = 0f;

        // Wait until result is received
        while ((currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult(); // fallback
            if (isFreeGame)
            {
                GoldRushGusFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (GoldRushGusAutoSpinController.isAutoSpinning)
            {
                GoldRushGusUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                GoldRushGusUIManager.Instance.UpdateButtons("Stop");
            }
            GoldRushGusUIManager.Instance.StopSpinMusic("Spin");
            GoldRushGusUIManager.Instance.StopCurrentSFX();
            isSpinAgain = true;
            yield break;
        }

        // Optional: small delay for visual pacing
        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }

    public void StopWithResult() => Stop();
    public void Stop()
    {
        if (InSpin == false) { return; }
        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            InSpin = false;
            foreach (var reel in reels)
            {
                isResultReceived = false;
                reel.ForceStopSpin();
            }
            if (!GoldRushGusAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                GoldRushGusUIManager.Instance.UpdateButtons("Stop");
            }
            return;
        }

        if (isSettingResult)
            return;

        isSettingResult = true;
        stopCoroutine = StartCoroutine(StopReelsWithResultRoutine());
    }
    private IEnumerator StopReelsWithResultRoutine()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }
        if (isRespinRunning)
        {
            SetReSpinAnimators(false);
        }
        lastReelAnimator = lastReelEffect.GetComponent<Animator>();
        if (settings.spinSettings.endSpin == GoldRushGusSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (isRespinRunning && !reSpinReels.Contains(i))
                    continue;
                if (reels[i] != null)
                {
                    if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    {
                        lastReelEffect.SetActive(true);
                        lastReelAnimator.SetBool("LastReel", true);
                        yield return new WaitForSeconds(1.25f);
                        lastReelAnimator.SetBool("LastReel", false);
                        lastReelEffect.SetActive(false);
                        yield return new WaitForSeconds(0.3f);
                        reels[i].ApplyFinalResult(i);
                        reels[i].StopSpin();
                        continue;
                    }

                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            GoldRushGusUIManager.Instance.PlaySound("ReelStop");

        }
        else // SpinOneByOne mode
        {
            // Stop reels one by one with delays
            for (int i = 0; i < reels.Count; i++)
            {
                if (isRespinRunning && !reSpinReels.Contains(i))
                    continue;
                if (reels[i] != null)
                {
                    if (isStopBtnPressed)
                        break;

                    if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    {
                        lastReelEffect.SetActive(true);
                        lastReelAnimator.SetBool("LastReel", true);
                        yield return new WaitForSeconds(1.25f);
                        lastReelAnimator.SetBool("LastReel", false);
                        lastReelEffect.SetActive(false);
                        yield return new WaitForSeconds(0.3f);
                        reels[i].ApplyFinalResult(i);
                        reels[i].StopSpin();
                        continue;
                    }
                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
                GoldRushGusUIManager.Instance.PlaySound("ReelStop");
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        GoldRushGusUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());
            
        ForceAllReelsToFinalPosition();

        GoldRushGusUIManager.Instance.StopSpinMusic("Spin");
        GoldRushGusUIManager.Instance.StopCurrentSFX();
        ProcessSpinResult();
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            if (isRespinRunning && !reSpinReels.Contains(i))
                continue;
            reels[i].ApplyFinalResult(i);
            reels[i].StopSpin();
        }
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            return;
        }

        winAmount = forcedWin ? forcedPrize : currentSpinResult.totalWin;

        if (!isRespinRunning && enableRandomRespin && winAmount <= 0f)
        {
            StartCoroutine(RandomRespinRoutine());
            return;
        }
        if (!isFreeGame)
        {
            if (winAmount > 0f)
                SetHeroCharacterWin();
            else
                SetHeroCharacterNormal();
        }

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            SetVillainCharacterWin();
            GoldRushGusUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = GoldRushGusUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || isMiniGameReady || instantWin || progressiveWin || isFreeGameReady)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                GoldRushGusPaylineResult result = new GoldRushGusPaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
                GoldRushGusPaylineController.Instance.AddPaylineData(result);
            }

           ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();

        }
        InSpin = false;
        isSpinAgain = true;

        if (isRespinRunning)
        {
            isRespinRunning = false;
            reSpinReels.Clear();
        }
        if(GoldRushGusAutoSpinController.isAutoSpinning && (instantWin || progressiveWin || isMiniGameReady))
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Auto Jackpot Animation");
        }
        else if (isFreeGameReady || instantWin || progressiveWin || isMiniGameReady)
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!GoldRushGusAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            GoldRushGusUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        GoldRushGusPaylineController.Instance.StartPayline(isFreeGameReady);
        winText.text = ToSpriteDigits(winAmount);
        if(winAmount > 0f)
        {
            GoldRushGusUIManager.Instance.StopCurrentSFX();
            GoldRushGusUIManager.Instance.PlaySound("Win");
            PopupAnimation(winText.gameObject, 1.1f, 1f, true);
        }
        if (instantWin)
        {
            GoldRushGusUIManager.Instance.PlayInstantWinAnimation(treasureChestWinAmount);
        }
        if (progressiveWin)
        {
            GoldRushGusUIManager.Instance.PlayProgressiveWinAnimation(treasureChestWinAmount);
        }
        if (isMiniGameReady)
        {
            GoldRushGusUIManager.Instance.PlayCoinGambleAnimation(treasureChestWinAmount);
            Invoke(nameof(InvokingMiniGame), 1.5f);
        }
    }
    public void InvokingMiniGame()
    {
        GoldRushGusMiniGameController.Instance.StartMiniGameTransition();
    }
    private IEnumerator RandomRespinRoutine()
    {
        isRespinRunning = true;
        respinResultArrived = false;
        yield return new WaitForSeconds(1.25f);
        SlotSpinService.Instance.Spin(CurrentBet());
    }
    private void CacheRespinAnimators()
    {
        if (reSpinReelEffect == null) return;

        int childCount = reSpinReelEffect.transform.childCount;
        reSpinReelAnimator = new Animator[childCount];

        for (int i = 0; i < childCount; i++)
        {
            var child = reSpinReelEffect.transform.GetChild(i);
            reSpinReelAnimator[i] = child.GetComponent<Animator>();
        }
    }
    private void SetReSpinAnimators(bool flag)
    {
        if (reSpinReelAnimator == null) return;

        for (int i = 0; i < reSpinReelAnimator.Length; i++)
        {
            var anim = reSpinReelAnimator[i];
            if (anim == null) continue;

            if (flag)
            {
                if (!anim.gameObject.activeSelf)
                    anim.gameObject.SetActive(true);

                anim.enabled = true;
                anim.SetBool("Play", true);
            }
            else
            {
                anim.SetBool("Play", false);
                anim.enabled = false;
                anim.gameObject.SetActive(false);
            }
        }
    }
    private void SetReSpinAnimatorForReels(List<int> reelIndexes, bool flag)
    {
        if (reSpinReelAnimator == null) return;

        // First turn everything off
        SetReSpinAnimators(false);

        if (!flag || reelIndexes == null) return;

        foreach (int reelIndex in reelIndexes)
        {
            if (reelIndex < 0 || reelIndex >= reSpinReelAnimator.Length)
                continue;

            var anim = reSpinReelAnimator[reelIndex];
            if (anim == null) continue;

            if (!anim.gameObject.activeSelf)
                anim.gameObject.SetActive(true);

            anim.enabled = true;
            anim.SetBool("Play", true);
        }
    }

    #endregion

    #region Hero & Villain Character Animation
    public void PlayHitAnimation()
    {
        StopCoroutine("PlayHitAnimation_Coroutine");
        StartCoroutine("PlayHitAnimation_Coroutine");
    }

    private IEnumerator PlayHitAnimation_Coroutine()
    {
        villainAnimator.gameObject.SetActive(true);
        villainAnimator.enabled = true;
        SetVillainCharacterHit();
        //villainAnimator.SetBool("Hit", true);

        yield return new WaitForSeconds(0.5f);
        characterAnimator.enabled = true;
        SetHeroCharacterStunned();
        //characterAnimator.SetBool("Stunned", true);
        yield return new WaitForSeconds(2f);
        characterAnimator.gameObject.SetActive(false);
    }
    public void SetVillainCharacterNormal()
    {
        if (!villainAnimator) return;
        StopWinCoroutine();
        villainAnimator.SetBool("Normal", true);
        villainAnimator.SetBool("Hit", false);
        villainAnimator.SetBool("Win", false);
    }
    public void SetVillainCharacterHit()
    {
        if (!villainAnimator) return;
        StopWinCoroutine();
        villainAnimator.SetBool("Normal", false);
        villainAnimator.SetBool("Hit", true);
        villainAnimator.SetBool("Win", false);
    }

    public void SetVillainCharacterWin()
    {
        if (!villainAnimator) return;
        StopWinCoroutine();
        villainAnimator.SetBool("Normal", false);
        villainAnimator.SetBool("Hit", false);
        villainAnimator.SetBool("Win", true);
        winReturnCoroutine = StartCoroutine(ReturnVillainToNormalAfterWin());
    }
    private IEnumerator ReturnVillainToNormalAfterWin()
    {
        yield return new WaitForSeconds(winReturnDelay);
        SetVillainCharacterNormal();
    }
    public void SetHeroCharacterNormal()
    {
        if (!characterAnimator) return;
        StopWinCoroutine();
        characterAnimator.SetBool("Normal", true);
        characterAnimator.SetBool("Spin", false);
        characterAnimator.SetBool("Win", false);
        characterAnimator.SetBool("Stunned", false);
    }
    private void SetHeroCharacterStunned()
    {
        if (!characterAnimator) return;
        StopWinCoroutine();
        characterAnimator.SetBool("Stunned", true);
        characterAnimator.SetBool("Normal", false);
        characterAnimator.SetBool("Spin", false);
        characterAnimator.SetBool("Win", false);
    }
    private void SetHeroCharacterSpin()
    {
        if (!characterAnimator) return;
        StopWinCoroutine();
        characterAnimator.SetBool("Normal", false);
        characterAnimator.SetBool("Spin", true);
        characterAnimator.SetBool("Win", false);
        characterAnimator.SetBool("Stunned", false);
        winReturnCoroutine = StartCoroutine(ReturnToNormalAfterWin());
    }

    private void SetHeroCharacterWin()
    {
        if (!characterAnimator) return;
        StopWinCoroutine();
        characterAnimator.SetBool("Normal", false);
        characterAnimator.SetBool("Spin", false);
        characterAnimator.SetBool("Win", true);
        characterAnimator.SetBool("Stunned", false);
        winReturnCoroutine = StartCoroutine(ReturnToNormalAfterWin());
    }
    private IEnumerator ReturnToNormalAfterWin()
    {
        yield return new WaitForSeconds(winReturnDelay);
        SetHeroCharacterNormal();
    }
    private void StopWinCoroutine()
    {
        if (winReturnCoroutine != null)
        {
            StopCoroutine(winReturnCoroutine);
            winReturnCoroutine = null;
        }
    }

    [Header("Hero/Villain Transition")]
    public float swapOffsetX = 5f;     
    public float swapDuration = 1f;
    public Ease swapEase = Ease.InOutSine;
    private Sequence _swapSeq;
    private float villainOriginalPosX;
    private float heroOriginalPosX;
    private bool cachedOriginalPositions;

    public void CacheCharacterOriginalPositions()
    {
        if (villainAnimator != null) villainOriginalPosX = villainAnimator.transform.localPosition.x;
        if (characterAnimator != null) heroOriginalPosX = characterAnimator.transform.localPosition.x;
        cachedOriginalPositions = true;
    }
    public IEnumerator SwapCharacters()
    {
        if (villainAnimator == null || characterAnimator == null) yield break;

        if (!cachedOriginalPositions) CacheCharacterOriginalPositions();

        Transform vT = villainAnimator.transform;
        Transform hT = characterAnimator.transform;

        // Kill any old tweens
        _swapSeq?.Kill();
        vT.DOKill();
        hT.DOKill();

        characterAnimator.gameObject.SetActive(true);
        characterAnimator.enabled = false;
        // Put hero at left start position
        Vector3 hPos = hT.localPosition;
        hT.localPosition = new Vector3(-12.5f, hPos.y, hPos.z); // PosX manually set

        // Build sequence: both move together
        _swapSeq = DOTween.Sequence();
        _swapSeq.Join(vT.DOLocalMoveX(villainOriginalPosX - swapOffsetX, swapDuration).SetEase(swapEase));
        _swapSeq.Join(hT.DOLocalMoveX(heroOriginalPosX, swapDuration).SetEase(swapEase));

        SetHeroCharacterNormal();
        float enableAt = swapDuration * 0.7f;
        _swapSeq.InsertCallback(enableAt, () =>
        {
            if (characterAnimator != null)
                characterAnimator.enabled = true;
        });
        _swapSeq.OnComplete(() =>
        {
            villainAnimator.gameObject.SetActive(false);

            Vector3 vPos = vT.localPosition;
            vT.localPosition = new Vector3(villainOriginalPosX, vPos.y, vPos.z);
        });

        yield return _swapSeq.WaitForCompletion();
    }
    #endregion

    #region Helper Functions
    private IEnumerator WaitForAllReelsToStop()
    {
        bool allStopped = false;

        while (!allStopped)
        {
            allStopped = true;

            for (int i = 0; i < reels.Count; i++)
            {
                if (isRespinRunning && !reSpinReels.Contains(i))
                    continue;

                if (reels[i] != null && reels[i].IsSpinning)
                {
                    allStopped = false;
                    break;
                }
            }

            if (!allStopped)
                yield return null;
        }
    }

    private void SetReelDirection(GoldRushGusReelScript reel)
    {
        GoldRushGusSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == GoldRushGusSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? GoldRushGusSpinDirection.Up : GoldRushGusSpinDirection.Down;
        }

        reel.SetSpinDirection(direction);
    }

    public void ForceAllReelsToFinalPosition()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                // Always clamp to top position regardless of spin direction
                reel.ForceClampToTop();
            }
        }
    }

    public void ForceAllReelsToTop()
    {
        foreach (GoldRushGusReelScript reel in reels)
        {
            if (reel != null)
            {
                reel.ForceClampToTop();
            }
        }
    }
    private IEnumerator WaitForAllReelsToBeSpinning()
    {
        bool allSpinning = false;
        while (!allSpinning)
        {
            allSpinning = true;

            for (int i = 0; i < reels.Count; i++)
            {
                if (isRespinRunning && !reSpinReels.Contains(i))
                    continue;

                if (reels[i] != null && !reels[i].IsSpinning)
                {
                    allSpinning = false;
                    break;
                }
            }

            if (!allSpinning)
                yield return new WaitForSeconds(0.05f);
        }
    }
    public static GoldRushGusSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        foreach (var res in Instance.settings.slotResources)
        {
            if (res.slotType.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }
        return null;
    }
    public string s;
    public string ToSpriteDigits(double value)
    {
        if (GoldRushGusFreeGameTransitionController.Instance.flag)
        {
            double floored = value;
            s = floored.ToString();
        }
        else
        {
            double floored = System.Math.Floor(value * 100) / 100;
            s = floored.ToString("0.00", CultureInfo.InvariantCulture);
        }
            
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
    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        GameObject parent = rt.parent.gameObject;
        parent.SetActive(state);

        Vector3 originalScale = rt.localScale;
        rt.DOKill();
        rt.localScale = Vector3.zero;

        Sequence seq = DOTween.Sequence();

        // Scale IN
        seq.Append(
            rt.DOScale(scale, duration)
              .SetEase(Ease.OutBack)
        );

        // Stay visible
        seq.AppendInterval(0.3f);

        // Scale OUT
        seq.Append(
            rt.DOScale(originalScale, duration * 0.8f)
              .SetEase(Ease.InBack)
        );
        seq.OnComplete(() =>
        {
            parent.SetActive(!state);
            rt.localScale = originalScale; // hard reset (important)
        });
    }
    private void SetSlotAnimationCompleted()
    {
        isSpinAgain = true;
        isSlotAnimationCompleted = true;
    }
    public override void ClearPaylines()
    {

    }
    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();
    }
    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }
    public float GetWinAmount()
    {
        winAmount = forcedWin ? forcedPrize : currentSpinResult.totalWin;
        return winAmount;
    }
    public bool isFreeSpinSlot(GoldRushGusSlotType slotType)
    {
        if (slotType == GoldRushGusSlotType.FreeSpin)
        {
            return true;
        }

        return false;
    }
    #endregion
}