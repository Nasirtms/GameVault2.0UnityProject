using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FruitMarySlotMachine : BaseSlotMachine
{
    #region Variables
    public static FruitMarySlotMachine Instance;
    public List<FruitMarySlotScript> BorderSlots = new List<FruitMarySlotScript>();

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public FruitMaryGameSettings settings;
    public List<FruitMaryReelScript> reels;
    public FruitMaryGameTransitionController gameTransitionController;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    public FruitMaryAutoSpinController fruitMaryAutoSpinController;

    // Spin Variables
    private float _timeCounter;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    [Header("Result")]
    [ShowInInspector][ReadOnly] public FruitMarySlotType[,] resultMatrix;

    // Machine Variables
    private int _reelsCount;
    private int _reelIndex;
    private float _delayAmongReel;

    // State Variables
    //[HideInInspector] public bool InSpin;
    private bool _isSingleSpin;
    private float _acceleration;
    private float _speed;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isResultReceived;
    [HideInInspector] public bool isSpinAgain = false;
    private bool isSettingResult;
    public bool isPaylineCompleted;

    public float winAmount;
    public event Action StopReelProcess;
    private bool firstAutoSpin = true;

    //Free Spin 
    [HideInInspector] public bool forceWildPayline;
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    [SerializeField] public int scatterCount;
    [HideInInspector] public bool firstFreeSpin;
    [HideInInspector] public bool lastFreeSpin = false;
    #endregion

    #region Unity Methods
    public void InvokeStop()
    {
        StopReelProcess?.Invoke();
    }
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        FruitMaryGameSettings.UpdateLayout += UpdateLayout;
        FruitMaryGameSettings.UpdateScale += UpdateScale;
        FruitMaryReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;
        InSpin = false;
        UpdateSettings();
        winAmount = 0;
        UpdateSlotServicesGameName();

    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        FruitMaryGameSettings.UpdateLayout -= UpdateLayout;
        FruitMaryGameSettings.UpdateScale -= UpdateScale;
        FruitMaryReelScript.OnSpinComplete -= OnReelSpinComplete;

        if (SpinResultController.Instance != null)
            SpinResultController.Instance.OnSpinResultReceived -= OnSpinResultReceived;
    }
    #endregion

    #region Machine Layout
    private void UpdateSettings()
    {
        for (var i = 0; i < reels.Count; i++)
            reels[i].Initialize(i);

        UpdateScale();
        UpdateLayout();
    }

    private void UpdateScale()
    {
        foreach (var reel in reels)
            reel.UpdateSlotScale(settings.slotScale);
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

    private void UpdateLayout()
    {
        var lastStatus = horizontalLayout.enabled;
        horizontalLayout.enabled = true;
        horizontalLayout.spacing = settings.horizontalLayout;

        foreach (var reel in reels)
            reel.UpdateVerticalLayout(settings.verticalLayout, settings.paddingTop);

        horizontalLayout.enabled = lastStatus;
    }
    
    private void UpdateMatrix()
    {
        horizontalLayout.enabled = true;
        this.resultMatrix = new FruitMarySlotType[reels.Count, 3];
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < reels.Count; x++)
            {
                this.resultMatrix[x, y] = reels[x].GetSlotType(y);
            }
        }
    }
    #endregion

    #region Machine Registry
    void UpdateSlotServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        //Debug.Log("Scene Name : " + sceneName);
        GameSlotRegistry.Register(sceneName, this);

        SceneManagement.UpdateCurrentSceneName(sceneName);
    }
    public bool isFruitMaryGameReady;
    #endregion

    #region Spin Result Received

    public int fruitMaryGameCount;
    public bool isBonusGameCompleted;
    [Header("Fake Scatter")]
    public int fakeScatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }
        if (result.scatterCount > 2)
            scatterCount = result.scatterCount;
        else
            scatterCount = fakeScatterCount;


        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
            }
            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        else if (fakeScatterCount > 2)
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
            }

            freeSpinCount = 3;
        }

        if (currentSpinResult.isBonusGame && currentSpinResult.jackpotWin.type.Contains("wildBonus"))
        {
            fruitMaryGameCount = (int)currentSpinResult.jackpotWin.amount;
            isFruitMaryGameReady = true;
            isBonusGameCompleted = false;
        }
        Debug.Log("📩 SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
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

        FruitMaryUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin
    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        FruitMaryPaylineController.Instance.StopPaylines();
        FruitMaryPaylineController.Instance.ClearPaylineData();

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            FruitMaryUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        foreach (var reel in reels)
        {
            foreach (var slot in reel.slots)
            {
                slot?.StopAnimation();
            }
        }

        fruitMaryAutoSpinController.cancelRequested = false;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isPaylineCompleted = false;
        isSpinAgain = false;
        BorderSlots.Clear();
        winningIndices.Clear();
        ClearPaylines();
        isSettingResult = false;
        InSpin = true;
        horizontalLayout.enabled = false;
        FruitMaryUIManager.Instance.winAnimationCompleted = true;
        isBonusGameCompleted = true;
        freeSpinCount = 0;
        FruitMaryUIManager.Instance.SetStopInteractable(false);

        isFruitMaryGameReady = false;
        fruitMaryGameCount = 0;

        _acceleration = settings.spinSettings.useSameAcceleration
            ? FruitMaryGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? FruitMaryGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = FruitMaryGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        foreach (var reel in reels)
        {
            reel.ResetShape();
            reel.Spin(_delayAmongReel, _acceleration, _speed);
        }
        StartSpinWithBackendResult();
    }
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
        if (currentSpinResult.isFreeSpin)
        {
            FruitMaryUIManager.Instance.spinButton.GetButtonComponent().interactable = false;
        }
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

        FruitMaryUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
                break;

            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
        }
        if (isStopBtnPressed)
            StopButtonPressed();

        //FruitMaryUIManager.Instance.SetStopInteractable(false);
        yield return new WaitUntil(() => reels.All(r => r.IsClamped()));
        
        ProcessSpinResult();
        InSpin = false;
        isSpinAgain = true;
    }

    public List<int> winningIndices = new();

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    public void StartSpinWithBackendResult()
    {
        StartCoroutine(WaitUntilResultAndThenStop());
    }
    #endregion

    #region Stop
    private void OnReelSpinComplete(int index)
    {
        if (settings.spinSettings.endSpin == FruitMarySpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == FruitMarySpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !FruitMaryAutoSpinController.isAutoSpinning)
        {
            if (isFreeGame)
            {
                FruitMaryUIManager.Instance.UpdateButtons("Free Spin");
                return;
            }
            else
            {
                FruitMaryUIManager.Instance.UpdateButtons("Stop");
            }
            firstAutoSpin = true;
        }
    }

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
            FruitMaryUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = FruitMaryUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount > 2)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            { 
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    FruitMaryPaylineResult result = new FruitMaryPaylineResult(payline.paylineIndex, payline.count);
                    FruitMaryPaylineController.Instance.AddPaylineData(result);
                }
            }
            ShowPaylines();
        }
        else
        {
            isPaylineCompleted = true;
        }

        InSpin = false;
        isSpinAgain = true;
        if (isFreeGameReady)
        {
            FruitMaryUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!FruitMaryAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            FruitMaryUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            FruitMaryUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
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
            StopWithResult(); // fallback
            if (isFreeGame)
            {
                FruitMaryGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (FruitMaryAutoSpinController.isAutoSpinning)
            {
                FruitMaryUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                FruitMaryUIManager.Instance.UpdateButtons("Stop");
            }
            FruitMaryUIManager.Instance.StopCurrentSFX();
            isSpinAgain = true;
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }
    private void ShowPaylines()
    {
        FruitMaryPaylineController.Instance.ShowCollectedPaylines(scatterCount);
    }
    #endregion

    #region Cleanup
    public override void ClearPaylines() { }
    public void StopWithResult() => Stop();
    #endregion

    #region Slot Result Data
    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        FruitMaryUIManager.Instance.ToggleSpinButton();
        StopWithResult();
    }
    public static FruitMarySlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            //Debug.LogWarning("Settings or resourcesList is null.");
            return null;
        }

        var normalizedId = id.ToLowerInvariant();
        if (normalizedId == "lime") normalizedId = "lemon";

        foreach (var res in Instance.settings.resourcesList)
        {
            if (res.type.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }
        return null;
    }
    #endregion

    #region Helper Functions
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