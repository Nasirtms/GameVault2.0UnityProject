using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuickHitVolcanoSlotMachine : BaseSlotMachine
{
    #region Variables

    public static QuickHitVolcanoSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public QuickHitVolcanoGameSettings settings;
    public List<QuickHitVolcanoReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public QuickHitVolcanoSlotType[,] resultMatrix;

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
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool _firstAutoSpin = true;
    private bool isSettingResult;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public bool extraFreeGame;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public int scatterCount;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public bool firstFreeSpin;

    // Coins Variables
    private float winAmount;

    // Quick Hit
    [HideInInspector] public int quickHitCount;

    // Events
    public event Action StopReelProcess;

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
        QuickHitVolcanoGameSettings.UpdateLayout += UpdateLayout;
        QuickHitVolcanoGameSettings.UpdateScale += UpdateScale;
        //QuickHitVolcanoReelScript.OnSpinComplete += OnReelSpinComplete;
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
        QuickHitVolcanoGameSettings.UpdateLayout -= UpdateLayout;
        QuickHitVolcanoGameSettings.UpdateScale -= UpdateScale;
        //QuickHitVolcanoReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        this.resultMatrix = new QuickHitVolcanoSlotType[reels.Count, 3];
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < reels.Count; x++)
            {
                this.resultMatrix[x, y] = reels[x].GetSlotType(y);
            }
        }
    }

    #endregion

    #region Spin Result Receive

    public void SetSpinResult(SpinResult spinResult)
    {
        currentSpinResult = spinResult;
    }

    [Header("Fake Scatter")]
    public bool fakeScatter;
    public int fakeScatterCount;
    public int fakeFreeSpinCount;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        if (fakeScatter)
        {
            scatterCount = fakeScatterCount;
        }
        else if (currentSpinResult.scatterCount >= 3)
        {
            scatterCount = currentSpinResult.scatterCount;
        }
        
        if (scatterCount >= 3)
        {
            if (isFreeGame)
            {
                extraFreeGame = true;
            }
            else
            {
                isFreeGameReady = true;
            }

            if (fakeScatter)
            {
                freeSpinCount = fakeFreeSpinCount;
            }
            else
            {
                freeSpinCount = currentSpinResult.freeSpinCount;
            }
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                if (symbol.id.Equals("QuickHit") || symbol.id.Equals("QuickHitWild"))
                {
                    quickHitCount++;
                }

                //Debug.Log("Symbol ID: " + symbol.id);
            }
            spinSymbolMatrix.Add(symbols);
        }

        QuickHitVolcanoUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            QuickHitVolcanoUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        StopAllCoroutines();
        QuickHitVolcanoPaylineController.Instance.StopPaylines();
        QuickHitVolcanoPaylineController.Instance.ClearPaylineData();
        QuickHitVolcanoUIManager.Instance.PlaySpinMusic("Spin");
        // Reset Variables and Functions State
        extraFreeGame = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isSlotAnimationCompleted = false;
        isSpinAgain = false;
        InSpin = true;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        scatterCount = 0;
        quickHitCount = 0;
        ClearPaylines();

        QuickHitVolcanoUIManager.Instance.winAnimationCompleted = true;
        QuickHitVolcanoUIManager.Instance.SetStopInteractable(false);

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? QuickHitVolcanoGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? QuickHitVolcanoGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = QuickHitVolcanoGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == QuickHitVolcanoSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == QuickHitVolcanoSpinType.Single)
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
                QuickHitVolcanoGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (QuickHitVolcanoAutoSpinController.isAutoSpinning)
            {
                QuickHitVolcanoUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                QuickHitVolcanoUIManager.Instance.UpdateButtons("Default");
            }
            QuickHitVolcanoUIManager.Instance.StopSpinMusic("Spin");
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

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].canStopReel = true;
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        QuickHitVolcanoUIManager.Instance.SetStopInteractable(false);
    }

    #endregion

    #region Result Stop

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
                break;

            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        QuickHitVolcanoUIManager.Instance.SetStopInteractable(false);
        QuickHitVolcanoUIManager.Instance.StopSpinMusic("Spin");
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

        //winAmount = currentSpinResult.totalWin;
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
            QuickHitVolcanoUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = QuickHitVolcanoUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }

        //Debug.Log("Quick Hit Count: " + quickHitCount);

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3 || quickHitCount >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    QuickHitVolcanoPaylineResult result = new QuickHitVolcanoPaylineResult(payline.paylineIndex, payline.count);
                    QuickHitVolcanoPaylineController.Instance.AddPaylineData(result);
                }
            }

            ShowPaylines();
        }
        else
        {
            isSlotAnimationCompleted = true;
        }

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady || extraFreeGame)
        {
            QuickHitVolcanoUIManager.Instance.UpdateButtons("Transition");
        }
        else if (!QuickHitVolcanoAutoSpinController.isAutoSpinning && !isFreeGame && QuickHitVolcanoUIManager.Instance.winAnimationCompleted)
        {
            QuickHitVolcanoUIManager.Instance.UpdateButtons("Default");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        QuickHitVolcanoPaylineController.Instance.StartPayline(scatterCount, quickHitCount);
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

    public static QuickHitVolcanoSlotResource? GetResourceById(string id)
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
