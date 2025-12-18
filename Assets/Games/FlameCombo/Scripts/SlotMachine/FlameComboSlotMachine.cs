using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlameComboSlotMachine : BaseSlotMachine
{
    #region Variables

    public static FlameComboSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public FlameComboGameSettings settings;
    public List<FlameComboReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    //public GoldenDragonAutoSpinController goldenDragonAutoSpinController;
    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public FlameComboSlotType[,] resultMatrix;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // Spin Variables
    private float _timeCounter;
    private float _delayAmongReel;
    private float _acceleration;
    private float _speed;

    // Machine Variables
    private float _reelsCount;
    private int _reelIndex;

    // State Variables
    [HideInInspector] public bool InSpin;
    [HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    public bool isMiniGame;
    // Free Spin Game
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public bool firstFreeSpin;
    [HideInInspector] public int freeSpinCount;

    // Coins Variables
    public float winAmount;
    // Events
    public event Action StopReelProcess;

    public int hit_Count;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        UpdateSlotServicesGameName();

        FlameComboGameSettings.UpdateLayout += UpdateLayout;
        FlameComboGameSettings.UpdateScale += UpdateScale;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        UpdateSettings();
        winAmount = 0;
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
        if (Instance == this)
            Instance = null;

        FlameComboGameSettings.UpdateLayout -= UpdateLayout;
        FlameComboGameSettings.UpdateScale -= UpdateScale;

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
        this.resultMatrix = new FlameComboSlotType[reels.Count, 3];
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
    [SerializeField] public int scatterCount;
    public bool isFakeSpins;
    public int fakeScatterCount;
    public int fakeFreeSpins;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        if (isFakeSpins)
        {
            scatterCount = fakeScatterCount;
        }
        else
        {
            scatterCount = currentSpinResult.scatterCount;
        }

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        else if (isFakeSpins)
        {
            if (!isFreeGame)
                isFreeGameReady = true;
            freeSpinCount = fakeFreeSpins;
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                var res = GetResourceById(symbol.id);

                if (res.HasValue)
                {
                    if (isHitSlot(res.Value.type))
                    {
                        hit_Count++;
                    }
                }
            }
            spinSymbolMatrix.Add(symbols);
        }

        FlameComboUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            FlameComboUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0f;
            winAmount = 0f;
        }

        StopAllCoroutines();
        FlameComboPaylineController.Instance.ClearPaylineData();
        FlameComboUIManager.Instance.SetStopInteractable(false);

        // Reset Variables and Functions State
        freeSpinCount = 0;
        scatterCount = 0;
        hit_Count = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isPaylineCompleted = false;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        FlameComboUIManager.Instance.winAnimationCompleted = true;
        ClearPaylines();
        isMiniGame = false;
        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? FlameComboGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? FlameComboGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = FlameComboGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == FlameComboSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == FlameComboSpinType.Single)
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
        float timeout = 7f;
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
                FlameComboFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (FlameComboAutoSpinController.isAutoSpinning)
            {
                FlameComboUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                FlameComboUIManager.Instance.UpdateButtons("Stop");
            }

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

        FlameComboUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        if (settings.spinSettings.endSpin == FlameComboSpinType.All)
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

        ProcessSpinResult();
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            Debug.LogWarning("❌ Spin result is invalid or failed.");
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
            FlameComboUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0f)
        {
            float betAmount = FlameComboUIManager.Instance.CurrentBet();
            Invoke(nameof(UpdateGameCoin), 1f);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount >= 3 || hit_Count >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    FlameComboPaylineResult result = new FlameComboPaylineResult(payline.paylineIndex, payline.count);
                    FlameComboPaylineController.Instance.AddPaylineData(result);
                }
            }

            Invoke(nameof(ShowPaylines), 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }

        //GoldenDragonUIManager.Instance.StopSpinMusic("Spin");

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            FlameComboUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!FlameComboAutoSpinController.isAutoSpinning && !isFreeGame && FlameComboUIManager.Instance.winAnimationCompleted)
        {
            FlameComboUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            FlameComboUIManager.Instance.UpdateButtons("Free Spin");
        }
    }

    private void ShowPaylines()
    {
        FlameComboPaylineController.Instance.ShowCollectedPaylines(scatterCount);

    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region Cleanup

    public override void ClearPaylines() { }

    #endregion

    #region Helper Functions
    public void InvokeStop()
    {
        StopReelProcess?.Invoke();
    }
    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();

    }
    public bool isHitSlot(FlameComboSlotType slotType)
    {
        if (slotType == FlameComboSlotType.HIT)
        {
            return true;
        }

        return false;
    }
    public static FlameComboSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            Debug.LogWarning("Settings or resourcesList is null.");
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        foreach (var res in Instance.settings.resourcesList)
        {
            if (res.type.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }

        return null;
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