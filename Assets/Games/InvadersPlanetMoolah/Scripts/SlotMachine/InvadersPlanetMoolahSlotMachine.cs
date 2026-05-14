using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InvadersPlanetMoolahSlotMachine : BaseSlotMachine
{
    #region Variables

    public static InvadersPlanetMoolahSlotMachine Instance;

    [Header("Machine References")]
    public InvadersPlanetMoolahGameSettings settings;
    public List<InvadersPlanetMoolahReelScript> reels;
    [SerializeField] private InvadersPlanetMoolahBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    public bool isFreeGameReady;
    public int freeSpinCount;
    public float freeSpinWinAmount;

    private float winAmount = 0f;

    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public InvadersPlanetMoolahSlotType[,] resultMatrix;

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

    private void UpdateSlotServicesGameName()
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
    public int fakeFreeSpinCount;
    public int scatterCount;

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

        if (currentSpinResult.isFreeSpin)
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

        InvadersPlanetMoolahUIManager.Instance.SetStopInteractable(true);
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
            InvadersPlanetMoolahUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;

        InvadersPlanetMoolahUIManager.Instance.winAnimationCompleted = true;
        InvadersPlanetMoolahUIManager.Instance.StopCurrentSFX();

        ClearPaylines();

        InvadersPlanetMoolahPaylineController.Instance.StopPaylines();
        InvadersPlanetMoolahPaylineController.Instance.ClearPaylineData();

        InvadersPlanetMoolahUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == InvadersPlanetMoolahSpinMode.SpinAll)
        {
            foreach (InvadersPlanetMoolahReelScript reel in reels)
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
            foreach (InvadersPlanetMoolahReelScript reel in reels)
            {
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
                //InvadersPlanetMoolahFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (InvadersPlanetMoolahAutoSpinController.isAutoSpinning)
            {
                InvadersPlanetMoolahUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Stop");
            }

            InvadersPlanetMoolahUIManager.Instance.StopCurrentSFX();

            isSpinAgain = true;
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }

    public void StopWithResult() => Stop();

    public void Stop()
    {
        if (InSpin == false) return;

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            InSpin = false;

            foreach (var reel in reels)
            {
                isResultReceived = false;
                reel.ForceStopSpin();
            }

            if (!InvadersPlanetMoolahAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == InvadersPlanetMoolahSpinMode.SpinAll)
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

        InvadersPlanetMoolahUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        InvadersPlanetMoolahUIManager.Instance.StopCurrentSFX();

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
            InvadersPlanetMoolahUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = InvadersPlanetMoolahUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        bool hasPaylines = currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0;

        if (hasPaylines)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                InvadersPlanetMoolahPaylineResult result = new InvadersPlanetMoolahPaylineResult(
                    payline.paylineIndex,
                    payline.count,
                    payline.winAmount
                );

                InvadersPlanetMoolahPaylineController.Instance.AddPaylineData(result);
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

        if (isFreeGameReady)
        {
            InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!InvadersPlanetMoolahAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            InvadersPlanetMoolahUIManager.Instance.UpdateButtons("Free Spin");
        }
    }

    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private void ShowPaylines()
    {
        InvadersPlanetMoolahPaylineController.Instance.StartPayline(isFreeGameReady);
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

    private void SetReelDirection(InvadersPlanetMoolahReelScript reel)
    {
        InvadersPlanetMoolahSpinDirection direction = settings.spinSettings.spinDirection;

        if (direction == InvadersPlanetMoolahSpinDirection.Random)
        {
            direction = Random.value > 0.5f
                ? InvadersPlanetMoolahSpinDirection.Up
                : InvadersPlanetMoolahSpinDirection.Down;
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
        foreach (InvadersPlanetMoolahReelScript reel in reels)
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

            foreach (InvadersPlanetMoolahReelScript reel in reels)
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

    public static InvadersPlanetMoolahSlotResource? GetResourceById(string id)
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
        return forcedWin ? forcedPrize : currentSpinResult.totalWin;
    }

    #endregion
}