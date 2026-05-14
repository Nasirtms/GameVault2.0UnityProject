using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class GoldGobblersSlotMachine : BaseSlotMachine
{
    #region Variables

    public static GoldGobblersSlotMachine Instance;

    [Header("Machine References")]
    public GoldGobblersGameSettings settings;
    public List<GoldGobblersReelScript> reels;

    public List<GoldGobblersReelScript> threeByFiveReels;
    public List<GoldGobblersReelScript> fiveByFiveReels;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public GoldGobblersSpinResult currentSpinResult;

    // State Variables
    //public bool InSpin = false;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    private bool _isSingleSpin;
    public bool hasRedFreeGameStarted = false;
    public bool hasGreenFreeGameStarted = false;
    public bool hasBlueFreeGameStarted = false;
    public bool testMode;
    public string testFreeGameType;
    public string testMidFreeGameType;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    public bool hasNewFreeGameTriggeredInBetween;
    public bool firstFreeSpin;
    public bool isTunnelSlot;
    public bool showingTunnelSlotAnimation;
    public List<int> tunnelSlotFreeSpinCount = new List<int>();

    [ShowInInspector][ReadOnly] public string freeGameType;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        // Creating Instance
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        UpdateSlotServicesGameName();
        Initialize();

        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        InSpin = false;
    }

    private void OnDestroy()
    {
        // Clearing Instance    
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

    [Header("Fake Free Spin")]
    public int fakeScatterCount;
    public int fakeFreeSpins;
    public int scatterCount;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is GoldGobblersSpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        isTunnelSlot = false;
        tunnelSlotFreeSpinCount.Clear();

        if (currentSpinResult.scatterCount >= 3)
            scatterCount = currentSpinResult.scatterCount;
        else
            scatterCount = fakeScatterCount;

        if (!testMode)
        {
            if (currentSpinResult.isFreeSpin)
            {
                if (!isFreeGame)
                {
                    freeGameType = currentSpinResult.cardName.ToLower();
                    isFreeGameReady = true;
                    DetrmineTheFreeGameOccoured(freeGameType, true);
                    freeSpinCount = currentSpinResult.freeSpinCount;
                }
                else if (isFreeGame)
                {
                    if (currentSpinResult.cardName.ToLower() != "none")
                    {
                        if (currentSpinResult.cardName != freeGameType)
                        {
                            hasNewFreeGameTriggeredInBetween = true;
                            freeGameType = currentSpinResult.cardName.ToLower();
                        }
                    }
                    freeSpinCount = currentSpinResult.freeSpinCount;
                }
            }
        }

        if (testMode)
        {
            if (!isFreeGame)
            {
                freeGameType = testFreeGameType.ToLower();
                isFreeGameReady = true;
                DetrmineTheFreeGameOccoured(freeGameType, true);
                freeSpinCount = 8;
            }
            else if (isFreeGame)
            {
                if (!String.IsNullOrEmpty(testMidFreeGameType))
                {
                    if (testMidFreeGameType != freeGameType)
                    {
                        Debug.Log("i snunk here hehe");
                        hasNewFreeGameTriggeredInBetween = true;
                        freeGameType = testMidFreeGameType.ToLower();
                        freeSpinCount = 8;
                    }
                }
            }
        }

        if (fakeScatterCount >= 3)
        {
            freeSpinCount = fakeFreeSpins;
        }


        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
        //string json = JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented);
        //LogLarge("SpinResult (parsed)", json);.
 
        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                var res = GetResourceById(symbol.id);

                if (hasTunnelSlot(res.Value.slotType))
                {
                    isTunnelSlot = true;
                    showingTunnelSlotAnimation = true;
                    tunnelSlotFreeSpinCount.Add(symbol.symbolFreeSpinCount);
                }
            }
            spinSymbolMatrix.Add(symbols);
        }

        GoldGobblersUIManager.Instance.SetStopInteractable(true);
    }



    //void LogLarge(string prefix, string text, int chunkSize = 8000)
    //{
    //    if (string.IsNullOrEmpty(text))
    //    {
    //        return;
    //    }

    //    for (int i = 0; i < text.Length; i += chunkSize)
    //    {
    //        string chunk = text.Substring(i, Mathf.Min(chunkSize, text.Length - i));
    //    }
    //}

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
            GoldGobblersUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        GoldGobblersUIManager.Instance.SetStopInteractable(false);
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        GoldGobblersUIManager.Instance.winAnimationCompleted = true;
        GoldGobblersPaylineController.Instance.StopPaylines();
        GoldGobblersPaylineController.Instance.ClearPaylineData();
        GoldGobblersUIManager.Instance.PlaySpinMusic("Spin");

        if (settings.spinSettings.startSpin == GoldGobblersSpinMode.SpinAll)
        {
            foreach (GoldGobblersReelScript reel in reels)
            {
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
            foreach (GoldGobblersReelScript reel in reels)
            {
                if (reels != null)
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

    #region Stop and Backend Result

    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 12f;
        float elapsed = 0f;

        // Wait until result is received
        while (currentSpinResult == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult(); // fallback

            if (isFreeGame)
            {
                GoldGobblersFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (GoldGobblersAutoSpinController.isAutoSpinning)
            {
                GoldGobblersUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                GoldGobblersUIManager.Instance.UpdateButtons("Spin Stop");
            }

            isSpinAgain = true;
            GoldGobblersUIManager.Instance.StopSpinMusic("Spin");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        StopWithResult();
    }

    public void StopWithResult() => Stop();

    public void Stop()
    {
        if (InSpin == false) { return; }
        if (currentSpinResult == null)
        {
            InSpin = false;
            foreach (var reel in reels)
            {
                isResultReceived = false;
                reel.ForceStopSpin();
            }
            if (!GoldGobblersAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                GoldGobblersUIManager.Instance.UpdateButtons("Spin Stop");
            }

            return;
        }

        if (isSettingResult)
        {
            return;
        }

        isSettingResult = true;

        stopCoroutine = StartCoroutine(StopReelsWithResultRoutine());
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }

        // Stop reels based on end spin mode
        if (settings.spinSettings.endSpin == GoldGobblersSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    GoldGobblersUIManager.Instance.PlaySound("ReelStop");
                    reels[i].StopSpin(); // No delay for simultaneous stop
                    
                }
            }
        }
        else // SpinOneByOne mode
        {
            // Stop reels one by one with delays
            for (int i = 0; i < reels.Count; i++)
            {

                if (reels[i] != null)
                {
                    if (isStopBtnPressed)
                        break;

                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);
                    GoldGobblersUIManager.Instance.PlaySound("ReelStop");
                    reels[i].StopSpin();
                    
                }
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        GoldGobblersUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        // Force all reels to final position based on direction
        ForceAllReelsToFinalPosition();
        GoldGobblersUIManager.Instance.StopSpinMusic("Spin");

        ProcessSpinResult();

        if (!GoldGobblersAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            GoldGobblersUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].ApplyFinalResult(i);
            reels[i].StopSpin();
        }

        GoldGobblersUIManager.Instance.SetStopInteractable(false);
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

        if (forcedWin)
        {
            winAmount = forcedPrize;
        }
        else
        {
            winAmount = currentSpinResult.totalWin;
        }
        Debug.Log("winAmount : " + winAmount);
        Debug.Log("currentSpinResult.totalWin : " + currentSpinResult.totalWin);
        Debug.Log("newBalance : " + currentSpinResult.newBalance);
        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            GoldGobblersUIManager.Instance.UpdateWinAmount(winAmount, true);

        }
        else if (winAmount > 0)
        {
            float betAmount = GoldGobblersUIManager.Instance.CurrentBet();
            //Invoke(nameof(UpdateGameCoin), 1f);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || (scatterCount >= 3) || (isTunnelSlot))
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    GoldGobblersPaylineResult result = new GoldGobblersPaylineResult(payline.paylineIndex);
                    GoldGobblersPaylineController.Instance.AddPaylineData(result);
                }
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        // Spin complete
        InSpin = false;
        isSpinAgain = true;
        if(winAmount > 0)
        {
            GoldGobblersUIManager.Instance.PlaySound("Win");
        }

        if (!GoldGobblersAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            GoldGobblersUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region Helper Functions

    private IEnumerator WaitForAllReelsToStop()
    {
        bool allStopped = false;

        while (!allStopped)
        {
            allStopped = true;

            foreach (var reel in reels)
            {
                if (reel != null && reel.IsSpinning)
                {
                    allStopped = false;
                    break;
                }
            }

            if (!allStopped)
            {
                yield return null;
            }
        }
    }

    private void SetReelDirection(GoldGobblersReelScript reel)
    {
        GoldGobblersSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == GoldGobblersSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? GoldGobblersSpinDirection.Up : GoldGobblersSpinDirection.Down;
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
        foreach (GoldGobblersReelScript reel in reels)
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
            foreach (GoldGobblersReelScript reel in reels)
            {
                if (reel != null && !reel.IsSpinning)
                {
                    allSpinning = false;
                    break;
                }
            }
            if (!allSpinning)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void ShowPaylines()
    {
        GoldGobblersPaylineController.Instance.StartPayline(scatterCount, isTunnelSlot);
    }

    private void SetSlotAnimationCompleted()
    {
        isSpinAgain = true;
        isSlotAnimationCompleted = true;

        if (isFreeGameReady || (!isFreeGameReady && isFreeGame))
        {
            GoldGobblersPaylineController.Instance.FreeGame();
        }
    }

    public override void ClearPaylines()
    {

    }

    public override void StopSpinGettingError()
    {

    }

    public float GetWinAmount()
    {
        return currentSpinResult.totalWin;
    }

    public static GoldGobblersSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        //Manually find match and return nullable
        foreach (var res in Instance.settings.slotResources)
        {
            if (res.slotType.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }

        return null;
    }

    public void ChangeReels(bool isfreeGame)
    {
        if (isfreeGame)
        {
            reels.Clear();
            for(int i = 0; i < fiveByFiveReels.Count; i++)
            {
                reels.Add(fiveByFiveReels[i]);
            }
        }
        else
        {
            reels.Clear();
            for (int i = 0; i < threeByFiveReels.Count; i++)
            {
                reels.Add(threeByFiveReels[i]);
            }
        }
    }

    public void InitializeReels()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.Initialize();
            }
        }
    }

    public void ChangeSlotBottomPosition(bool isFreegame)
    {
        if (isFreegame)
        {
            settings.slotSettings.BottomYPosition = GoldGobblersUIManager.Instance.bottomYForFiveByFive;
        }
        else
        {
            settings.slotSettings.BottomYPosition = GoldGobblersUIManager.Instance.bottomYForThreeByFive;
        }
    }

    private void DetrmineTheFreeGameOccoured(string freeGameType, bool start)
    {
        if (start)
        {
            switch (freeGameType)
            {
                case "red":
                    hasRedFreeGameStarted = true;
                    break;
                case "green":
                    hasGreenFreeGameStarted = true;
                    break;
                case "blue":
                    hasBlueFreeGameStarted = true;
                    break;
                case "red&blue":
                    hasBlueFreeGameStarted = true;
                    hasRedFreeGameStarted = true;
                    break;
                case "red&green":
                    hasRedFreeGameStarted = true;
                    hasGreenFreeGameStarted = true;
                    break;
                case "green&blue":
                    hasGreenFreeGameStarted = true;
                    hasBlueFreeGameStarted = true;
                    break;
                case "red&green&blue":
                    hasRedFreeGameStarted = true;
                    hasBlueFreeGameStarted = true;
                    hasGreenFreeGameStarted = true;
                    break;
            }
        }
        else
        {
            hasRedFreeGameStarted = false;
            hasBlueFreeGameStarted = false;
            hasGreenFreeGameStarted = false;
        }
    }

    private bool hasTunnelSlot(GoldGobblersSlotType slotType)
    {
        if(slotType == GoldGobblersSlotType.MineEntrance)
        {
            return true;
        }
        else return false;
    }
    #endregion
}
