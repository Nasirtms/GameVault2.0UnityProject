using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LifeOfLuxurySlotMachine : BaseSlotMachine
{
    #region Variables
    public static LifeOfLuxurySlotMachine Instance;

    [Header("Machine References")]
    public LifeOfLuxuryGameSettings settings;
    public List<LifeOfLuxuryReelScript> reels;
    [SerializeField] private LifeOfLuxuryBetController betController;

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
    public int freeSpinLineMultiplier;
    public int remainingFreeSpins;
    public float freeSpinWinAmount;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public LifeOfLuxurySlotType[,] resultMatrix;
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
    [Header("Fake FreeSpin")]
    public bool isFakeFreeGame;
    public int fakeFreeSpinCount;
    public int fakeLineMuiltiplier;
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

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
            if (currentSpinResult.lifeOfLuxuryFreeSpinState != null)
            {
                Debug.Log("FreeSpinLineMultiplier : " + currentSpinResult.lifeOfLuxuryFreeSpinState.lineMultiplier);
                remainingFreeSpins = currentSpinResult.lifeOfLuxuryFreeSpinState.remainingSpins;
                freeSpinLineMultiplier = currentSpinResult.lifeOfLuxuryFreeSpinState.lineMultiplier;
            }
            else
            {
                Debug.Log("LifeOfLuxuryFreeSpinState is null in OnSpinResultReceived.");
            }
        }
        if (isFakeFreeGame)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = fakeFreeSpinCount;
            freeSpinLineMultiplier = fakeLineMuiltiplier;

        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
            }
            spinSymbolMatrix.Add(symbols);
        }

        LifeOfLuxuryUIManager.Instance.SetStopInteractable(true);
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
            LifeOfLuxuryUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        // Reset Variables
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        LifeOfLuxuryUIManager.Instance.winAnimationCompleted = true;
        LifeOfLuxuryUIManager.Instance.StopCurrentSFX();
        //IrishPotLuckUIManager.Instance.PlaySound("Spin");
        ClearPaylines();
        LifeOfLuxuryPaylineController.Instance.StopPaylines();
        LifeOfLuxuryPaylineController.Instance.ClearPaylineData();
        LifeOfLuxuryUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == LifeOfLuxurySpinMode.SpinAll)
        {
            foreach (LifeOfLuxuryReelScript reel in reels)
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
            foreach (LifeOfLuxuryReelScript reel in reels)
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
                LifeOfLuxuryFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (LifeOfLuxuryAutoSpinController.isAutoSpinning)
            {
                LifeOfLuxuryUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                LifeOfLuxuryUIManager.Instance.UpdateButtons("Stop");
            }
            //IrishPotLuckUIManager.Instance.StopSpinMusic("Spin");
            LifeOfLuxuryUIManager.Instance.StopCurrentSFX();
            isSpinAgain = true;
            yield break;
        }

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
            if (!LifeOfLuxuryAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                LifeOfLuxuryUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == LifeOfLuxurySpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            //IrishPotLuckUIManager.Instance.PlaySound("ReelStop"); 
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
                    reels[i].StopSpin();
                }
                //IrishPotLuckUIManager.Instance.PlaySound("ReelStop");
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        LifeOfLuxuryUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        //IrishPotLuckUIManager.Instance.StopSpinMusic("Spin");
        LifeOfLuxuryUIManager.Instance.StopCurrentSFX();
        ProcessSpinResult();
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
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
            return;

        winAmount = forcedWin ? forcedPrize : currentSpinResult.totalWin;

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            LifeOfLuxuryUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = LifeOfLuxuryUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }
        bool hasPaylines = currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0;

        if (hasPaylines)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                LifeOfLuxuryPaylineResult result = new LifeOfLuxuryPaylineResult(
                    payline.paylineIndex,
                    payline.count,
                    payline.winAmount
                );

                LifeOfLuxuryPaylineController.Instance.AddPaylineData(result);
            }
        }

        if (hasPaylines || isFreeGameReady)
        {
            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGame)
        {
            if (currentSpinResult.lifeOfLuxuryFreeSpinState != null)
            {
                freeSpinLineMultiplier = currentSpinResult.lifeOfLuxuryFreeSpinState.lineMultiplier;
                LifeOfLuxuryFreeGameTransitionController.Instance.UpdateLineMultiplierText();
                remainingFreeSpins = currentSpinResult.lifeOfLuxuryFreeSpinState.remainingSpins;
            }
        }

        if (isFreeGameReady)
        {
            LifeOfLuxuryUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!LifeOfLuxuryAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            LifeOfLuxuryUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            LifeOfLuxuryUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        LifeOfLuxuryPaylineController.Instance.StartPayline(isFreeGameReady);
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

    private void SetReelDirection(LifeOfLuxuryReelScript reel)
    {
        LifeOfLuxurySpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == LifeOfLuxurySpinDirection.Random)
        {
            direction = Random.value > 0.5f ? LifeOfLuxurySpinDirection.Up : LifeOfLuxurySpinDirection.Down;
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
        foreach (LifeOfLuxuryReelScript reel in reels)
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
            foreach (LifeOfLuxuryReelScript reel in reels)
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
    public static LifeOfLuxurySlotResource? GetResourceById(string id)
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
        if (forcedWin)
        {
            return forcedPrize;
        }
        else
        {
            return currentSpinResult.totalWin;
        }
    }
    #endregion
}