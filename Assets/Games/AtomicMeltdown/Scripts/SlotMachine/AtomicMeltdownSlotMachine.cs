using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AtomicMeltdownSlotMachine : BaseSlotMachine
{
    #region Variables

    public static AtomicMeltdownSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public AtomicMeltdownGameSettings settings;
    public List<AtomicMeltdownReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public AtomicMeltdownSlotType[,] resultMatrix;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // Spin Variables
    private float _timeCounter;
    private float _delayAmongReel;
    private float _acceleration;
    private float _speed;

    // Machine Variables
    private int _reelsCount;
    private int _reelIndex;

    // State Variables
    //[HideInInspector] public bool InSpin;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public bool firstFreeSpin;

    // Coins Variables
    private float winAmount;

    // Events
    public event Action StopReelProcess;

    // 3 Reels Slot
    public static List<AtomicMeltdownSlotResource> CachedRealSymbols { get; private set; }
    public static AtomicMeltdownSlotResource? CachedEmptySymbol { get; private set; }

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
        // Adding Game to Registry
        UpdateSlotServicesGameName();

        // Subscribing Events
        AtomicMeltdownGameSettings.UpdateLayout += UpdateLayout;
        AtomicMeltdownGameSettings.UpdateScale += UpdateScale;
        //AtomicMeltdownReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        // Update Settings
        UpdateSettings();

        // Initialize Variables
        InSpin = false;
    }

    private void Update()
    {
        if (!_isSingleSpin) return;

        if (_timeCounter >= _delayAmongReel)
        {
            if (_reelIndex >= reels.Count)
            {
                _isSingleSpin = false;
                return;
            }
            _timeCounter = 0;
            reels[_reelIndex].ResetShape();
            reels[_reelIndex].Spin(_delayAmongReel, _acceleration, _speed);
            _reelIndex++;
        }
        else
        {
            _timeCounter += Time.deltaTime;
        }
    }

    private void OnDestroy()
    {
        // Clearing Instance
        if (Instance == this)
            Instance = null;

        // Unsubscribing Events
        AtomicMeltdownGameSettings.UpdateLayout -= UpdateLayout;
        AtomicMeltdownGameSettings.UpdateScale -= UpdateScale;
        //AtomicMeltdownReelScript.OnSpinComplete -= OnReelSpinComplete;

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

    #region Machine Layout

    private void UpdateSettings()
    {
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != AtomicMeltdownSlotType.Empty);
        CachedEmptySymbol = settings.resourcesList.Find(r => r.type == AtomicMeltdownSlotType.Empty);

        for (var i = 0; i < this.reels.Count; i++)
        {
            this.reels[i].Initialize(i);
        }

        UpdateScale();
        UpdateLayout();
    }

    private void UpdateScale()
    {
        foreach (var reel in reels)
        {
            reel.UpdateSlotScale(settings.slotScale);
        }
    }

    private void UpdateLayout()
    {
        var lastStatus = horizontalLayout.enabled;
        horizontalLayout.enabled = true;
        horizontalLayout.spacing = settings.horizontalLayout;

        foreach (var reel in reels)
        {
            reel.UpdateVerticalLayout(settings.verticalLayout, settings.paddingTop);
        }
        horizontalLayout.enabled = lastStatus;
    }

    private void UpdateMatrix()
    {
        horizontalLayout.enabled = true;
        this.resultMatrix = new AtomicMeltdownSlotType[reels.Count, 3];
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < reels.Count; x++)
            {
                this.resultMatrix[x, y] = reels[x].GetSlotType(y);
            }
        }
    }

    #endregion

    #region Spin Result Received

    //public void SetSpinResult(BaseSpinResult spinResult)
    //{
    //    if (spinResult is SpinResult normalSpin)
    //    {
    //        currentSpinResult = normalSpin;
    //    }
    //}

    [Header("Fake Free Spin")]
    public int fakeFreeSpins;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        if (currentSpinResult.freeSpinCount > 0)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        else if (fakeFreeSpins > 0)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = fakeFreeSpins;
        }

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
            }
            spinSymbolMatrix.Add(symbols);
        }

        AtomicMeltdownUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            AtomicMeltdownUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0f;
            winAmount = 0f;
        }

        AtomicMeltdownUIManager.Instance.PlaySpinMusic("Spin");
        StopAllCoroutines();
        AtomicMeltdownPaylineController.Instance.StopPaylineLoop();
        AtomicMeltdownPaylineController.Instance.ClearPaylineResults();
        AtomicMeltdownUIManager.Instance.SetStopInteractable(false);

        // Reset Variables and Functions State
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isPaylineCompleted = false;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        AtomicMeltdownUIManager.Instance.winAnimationCompleted = true;
        ClearPaylines();

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? AtomicMeltdownGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? AtomicMeltdownGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = AtomicMeltdownGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == AtomicMeltdownSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == AtomicMeltdownSpinType.Single)
        {
            //start spin the first reel
            reels[0].ResetShape();
            reels[0].Spin(_delayAmongReel, _acceleration, _speed);

            //init delay variables

            _timeCounter = 0;
            _reelIndex = 1;
            _isSingleSpin = true;
        }

        // Wait for backend and then stop
        StartSpinWithBackendResult();
    }

    public void StartSpinWithBackendResult()
    {
        StartCoroutine(WaitUntilResultAndThenStop());
    }

    #endregion

    #region Stop

    //private void OnReelSpinComplete(int index)
    //{
    //    if (settings.spinSettings.endSpin == AtomicMeltdownSpinType.Single && index == reels.Count - 1)
    //    {
    //        InSpin = false;
    //    }
    //    else if (settings.spinSettings.endSpin == AtomicMeltdownSpinType.All)
    //    {
    //        InSpin = false;
    //    }

    //    if (index == reels.Count - 1 && !AtomicMeltdownAutoSpinController.isAutoSpinning)
    //    {
    //        AtomicMeltdownUIManager.Instance.UpdateButtons("Stop");
    //        AtomicMeltdownUIManager.Instance.StopSpinMusic("Spin");
    //        //AtomicMeltdownUIManager.Instance.PlaySound("SpinStop");
    //    }
    //}

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
                AtomicMeltdownFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (AtomicMeltdownAutoSpinController.isAutoSpinning)
            {
                AtomicMeltdownUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                AtomicMeltdownUIManager.Instance.UpdateButtons("Default");
            }
            
            isSpinAgain = true;
            AtomicMeltdownUIManager.Instance.StopSpinMusic("Spin");
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
                reel.ResetShape();
                reel.ForceStop();
            }
            UpdateMatrix();

            return;
        }

        if (isSettingResult)
            return;

        isSettingResult = true;
        StartCoroutine(StopReelsWithResultRoutine());
    }

    #endregion

    #region Result Stop

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].canStopReel = true;
        }

        AtomicMeltdownUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        if (settings.spinSettings.endSpin == AtomicMeltdownSpinType.All)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isStopBtnPressed)
                    break;

                reels[i].canStopReel = true;
            }
        }
        else
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isStopBtnPressed)
                    break;

                yield return new WaitForSeconds(_delayAmongReel);

                reels[i].canStopReel = true;
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        AtomicMeltdownUIManager.Instance.StopSpinMusic("Spin");
        ProcessSpinResult();
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            //Debug.LogWarning("❌ Spin result is invalid or failed.");
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

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            AtomicMeltdownUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0f)
        {
            float betAmount = AtomicMeltdownUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || freeSpinCount > 0)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                AtomicMeltdownPaylineResult result = new AtomicMeltdownPaylineResult(payline.paylineIndex);
                AtomicMeltdownPaylineController.Instance.AddPaylineResult(result);
            }

            Invoke("ShowPaylines", 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }
        if (winAmount > 0f)
        {
            AtomicMeltdownUIManager.Instance.PlaySound("Win");
        }

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            AtomicMeltdownUIManager.Instance.UpdateButtons("Transition");
        }
        else if (!AtomicMeltdownAutoSpinController.isAutoSpinning && !isFreeGame && AtomicMeltdownUIManager.Instance.winAnimationCompleted)
        {
            AtomicMeltdownUIManager.Instance.UpdateButtons("Default");
        }
        else if (isFreeGame)
        {
            AtomicMeltdownUIManager.Instance.UpdateButtons("FreeSpin");
        }
    }

    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        AtomicMeltdownPaylineController.Instance.StartPaylineLoop(freeSpinCount);
    }

    #endregion

    #region Cleanup

    public override void ClearPaylines() { }

    #endregion

    #region Helper Functions

    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();
    }

    public static AtomicMeltdownSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            //Debug.LogWarning("Settings or resourcesList is null.");
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        //Manually find match and return nullable
        foreach (var res in Instance.settings.resourcesList)
        {
            if (res.type.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }

        return null;
    }

    public void InvokeStop()
    {
        StopReelProcess?.Invoke();
    }

    public float GetWinAmount()
    {
        return currentSpinResult.totalWin;
    }

    #endregion
}
