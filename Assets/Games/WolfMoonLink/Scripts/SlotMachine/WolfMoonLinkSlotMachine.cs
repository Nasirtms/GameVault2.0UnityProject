using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WolfMoonLinkSlotMachine : BaseSlotMachine
{
    #region Variables

    public static WolfMoonLinkSlotMachine Instance;

    [Header("Machine References")]
    public WolfMoonLinkGameSettings settings;
    public List<WolfMoonLinkReelScript> reels;
    [SerializeField] private WolfMoonLinkBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    [HideInInspector] public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;

    private bool isSettingResult;

    private float winAmount = 0f;

    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public WolfMoonLinkSlotType[,] resultMatrix;

    public static List<WolfMoonLinkSlotResource> CachedRealSymbols { get; private set; }
    public static WolfMoonLinkSlotResource? CachedEmptySymbol { get; private set; }

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
        CachedRealSymbols = settings.slotResources.FindAll(r => r.slotType != WolfMoonLinkSlotType.Empty);
        CachedEmptySymbol = settings.slotResources.Find(r => r.slotType == WolfMoonLinkSlotType.Empty);

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

    public void SetSpinResult(SpinResult spinResult)
    {
        currentSpinResult = spinResult;
    }

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
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

        WolfMoonLinkUIManager.Instance.SetStopInteractable(true);
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
        WolfMoonLinkUIManager.Instance.UpdateWinAmount(0f, false);
        WolfMoonLinkUIManager.Instance.winAnimationCompleted = true;

        winAmount = 0f;
        currentSpinResult = null;

        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;

        //WolfMoonLinkUIManager.Instance.PlaySpinMusic("Spin");
        ClearPaylines();

        WolfMoonLinkPaylineController.Instance.StopPaylineLoop();
        WolfMoonLinkPaylineController.Instance.ClearPaylineResults();
        WolfMoonLinkUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == WolfMoonLinkSpinMode.SpinAll)
        {
            foreach (WolfMoonLinkReelScript reel in reels)
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
            foreach (WolfMoonLinkReelScript reel in reels)
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

            if (WolfMoonLinkAutoSpinController.isAutoSpinning)
            {
                WolfMoonLinkUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                WolfMoonLinkUIManager.Instance.UpdateButtons("Stop");
            }

            //WolfMoonLinkUIManager.Instance.StopSpinMusic("Spin");
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

            if (!WolfMoonLinkAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                WolfMoonLinkUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == WolfMoonLinkSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }

            //WolfMoonLinkUIManager.Instance.PlaySound("Stop");
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

        //WolfMoonLinkUIManager.Instance.StopSpinMusic("Spin");

        if (isStopBtnPressed)
            StopButtonPressed();

        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

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

        if (winAmount > 0f)
        {
            float betAmount = WolfMoonLinkUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0))
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                WolfMoonLinkPaylineResult result = new WolfMoonLinkPaylineResult(payline.paylineIndex, payline.symbol, payline.count);
                WolfMoonLinkPaylineController.Instance.AddPaylineResult(result);
            }

            ShowPaylines();
        }
        else
        {
            isSlotAnimationCompleted = true;
        }

        if (winAmount > 0f)
            //WolfMoonLinkUIManager.Instance.PlaySound("Win");

        InSpin = false;
        isSpinAgain = true;

        if (!WolfMoonLinkAutoSpinController.isAutoSpinning && !isFreeGame && WolfMoonLinkUIManager.Instance.winAnimationCompleted)
        {
            WolfMoonLinkUIManager.Instance.UpdateButtons("Stop");
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

    private void SetReelDirection(WolfMoonLinkReelScript reel)
    {
        WolfMoonLinkSpinDirection direction = settings.spinSettings.spinDirection;

        if (direction == WolfMoonLinkSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? WolfMoonLinkSpinDirection.Up : WolfMoonLinkSpinDirection.Down;
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
        foreach (WolfMoonLinkReelScript reel in reels)
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

            foreach (WolfMoonLinkReelScript reel in reels)
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

    public static WolfMoonLinkSlotResource? GetResourceById(string id)
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

    private void ShowPaylines()
    {
        WolfMoonLinkPaylineController.Instance.StartPaylineLoop();
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