using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuffaloXtraReelPowerSlotMachine : BaseSlotMachine
{
    #region Variables
    public static BuffaloXtraReelPowerSlotMachine Instance;

    [Header("Machine References")]
    public BuffaloXtraReelPowerGameSettings settings;
    public List<BuffaloXtraReelPowerReelScript> reels;
    [SerializeField] private BuffaloXtraReelPowerBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public BuffaloXtraReelPowerSpinResult currentSpinResult;

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

    // Win
    private float winAmount = 0f;

    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public BuffaloXtraReelPowerSlotType[,] resultMatrix;
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

    //public void SetSpinResult(SpinResult spinResult)
    //{
    //    currentSpinResult = spinResult;
    //}

    public int scatterCount;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is BuffaloXtraReelPowerSpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        //if (currentSpinResult.isFreeSpin)
        if (currentSpinResult.scatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }

        if (isFakeFreeGame)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = fakeFreeSpinCount;
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

        BuffaloXtraReelPowerUIManager.Instance.SetStopInteractable(true);
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

        spinCoroutine = StartCoroutine(StartSpin());
    }

    private IEnumerator StartSpin()
    {
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            BuffaloXtraReelPowerUIManager.Instance.UpdateWinAmount(0f);
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

        BuffaloXtraReelPowerUIManager.Instance.winAnimationCompleted = true;
        BuffaloXtraReelPowerUIManager.Instance.StopCurrentSFX();

        ClearPaylines();
        //BuffaloXtraReelPowerPaylineController.Instance.StopPaylines();
        //BuffaloXtraReelPowerPaylineController.Instance.ClearPaylineData();
        BuffaloXtraReelPowerUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == BuffaloXtraReelPowerSpinMode.SpinAll)
        {
            foreach (BuffaloXtraReelPowerReelScript reel in reels)
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
            foreach (BuffaloXtraReelPowerReelScript reel in reels)
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

        while ((currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult();

            if (isFreeGame)
            {
                //BuffaloXtraReelPowerFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (BuffaloXtraReelPowerAutoSpinController.isAutoSpinning)
            {
                BuffaloXtraReelPowerUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Stop");
            }

            BuffaloXtraReelPowerUIManager.Instance.StopCurrentSFX();
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

            if (!BuffaloXtraReelPowerAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == BuffaloXtraReelPowerSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
        }
        else
        {
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
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        BuffaloXtraReelPowerUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        BuffaloXtraReelPowerUIManager.Instance.StopCurrentSFX();
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
            BuffaloXtraReelPowerUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = BuffaloXtraReelPowerUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        bool hasPaylines = currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0;

        if (hasPaylines)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                BuffaloXtraReelPowerPaylineResult result = new BuffaloXtraReelPowerPaylineResult(payline.paylineIndex);
                BuffaloXtraReelPowerPaylineController.Instance.AddPaylineData(result);
            }
        }

        if (hasPaylines || scatterCount >= 3)
        {
            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!BuffaloXtraReelPowerAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            BuffaloXtraReelPowerUIManager.Instance.UpdateButtons("Free Spin");
        }
    }

    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private void ShowPaylines()
    {
        BuffaloXtraReelPowerPaylineController.Instance.StartPayline(scatterCount);
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

    private void SetReelDirection(BuffaloXtraReelPowerReelScript reel)
    {
        BuffaloXtraReelPowerSpinDirection direction = settings.spinSettings.spinDirection;

        if (direction == BuffaloXtraReelPowerSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? BuffaloXtraReelPowerSpinDirection.Up : BuffaloXtraReelPowerSpinDirection.Down;
        }

        reel.SetSpinDirection(direction);
    }

    public void ForceAllReelsToFinalPosition()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.ForceClampToTop();
            }
        }
    }

    public void ForceAllReelsToTop()
    {
        foreach (BuffaloXtraReelPowerReelScript reel in reels)
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

            foreach (BuffaloXtraReelPowerReelScript reel in reels)
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

    public static BuffaloXtraReelPowerSlotResource? GetResourceById(string id)
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

    public override void ClearPaylines() { }

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

    #endregion
}