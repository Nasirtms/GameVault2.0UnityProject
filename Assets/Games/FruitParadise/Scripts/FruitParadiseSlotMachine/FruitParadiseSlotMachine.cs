using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.UI.Image;
//using Newtonsoft.Json.Linq; //hdr

public class FruitParadiseSlotMachine : BaseSlotMachine
{
    #region Variables
    public static FruitParadiseSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public FruitParadiseGameSettings settings;
    public List<FruitParadiseReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    public FruitParadiseAutoSpinController fruitParadiseAutoSpinController;
    [Header("Result")]
    [ShowInInspector][ReadOnly] public FruitParadiseSlotType[,] resultMatrix;

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
    [HideInInspector] public bool InSpin;
    [HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    //Free Spin 
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    [SerializeField] public int scatterCount;
    [HideInInspector] public bool firstFreeSpin;

    public float winAmount;
    private bool firstAutoSpin = true;
    public event Action StopReelProcess;

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
        FruitParadiseGameSettings.UpdateLayout += UpdateLayout;
        FruitParadiseGameSettings.UpdateScale += UpdateScale;
        //FruitParadiseReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;
        InSpin = false;
        UpdateSettings();
        winAmount = 0;
        UpdateSlotServicesGameName();
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

        FruitParadiseGameSettings.UpdateLayout -= UpdateLayout;
        FruitParadiseGameSettings.UpdateScale -= UpdateScale;
        //FruitParadiseReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        this.resultMatrix = new FruitParadiseSlotType[reels.Count, 3];
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
    [Header("Fake Spins")]
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

        if (scatterCount >= 3)
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
       
        Debug.Log("📩 SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
        Debug.Log($" New Balance : {currentSpinResult}");
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
        FruitParadiseUIManager.Instance.SetStopInteractable(true);
    }
    #endregion

    #region Spin
    public override void Spin()
    {
        if (InSpin) return;
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            FruitParadiseUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        StopAllCoroutines();
        FruitParadisePaylineController.Instance.ClearPaylineData();
        FruitParadiseUIManager.Instance.SetStopInteractable(false);

        //FruitParadiseUIManager.Instance.winAmount.text = "0.00";

        fruitParadiseAutoSpinController.cancelRequested = false;
        freeSpinCount = 0;
        scatterCount = 0;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isPaylineCompleted = false;
        isSettingResult = false;
        isSpinAgain = false;
        InSpin = true;
        horizontalLayout.enabled = false;
        FruitParadiseUIManager.Instance.winAnimationCompleted = true;
        winningIndices.Clear();
        ClearPaylines();

        _acceleration = settings.spinSettings.useSameAcceleration
            ? FruitParadiseGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? FruitParadiseGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = FruitParadiseGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        foreach (var reel in reels)
        {
            reel.ResetShape();
            reel.Spin(_delayAmongReel, _acceleration, _speed);
        }

        //if (firstAutoSpin && FruitParadiseAutoSpinController.isAutoSpinning)
        //{
        //    FruitParadiseUIManager.Instance.SetSpinInProgress(true);
        //    firstAutoSpin = false;
        //}
        //else if (!FruitParadiseAutoSpinController.isAutoSpinning)
        //{
        //    FruitParadiseUIManager.Instance.SetSpinInProgress(true);
        //}
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
    //    if (settings.spinSettings.endSpin == FruitParadiseSpinType.Single && index == reels.Count - 1)
    //    {
    //        InSpin = false;
    //        UpdateMatrix();
    //    }
    //    else if (settings.spinSettings.endSpin == FruitParadiseSpinType.All)
    //    {
    //        InSpin = false;
    //        UpdateMatrix();
    //    }

    //    if (index == reels.Count - 1 && !FruitParadiseAutoSpinController.isAutoSpinning)
    //    {
    //        if (!FruitParadiseAutoSpinController.isAutoSpinning)
    //        {
    //            //FruitParadiseUIManager.Instance.SetSpinInProgress(false);
    //            if (isFreeGame)
    //            {
    //                FruitParadiseUIManager.Instance.UpdateButtons("Free Spin");
    //                return;
    //            }
    //            else
    //            {
    //                //FruitParadiseUIManager.Instance.UpdateButtons("Stop");
    //            }
    //        }
    //        firstAutoSpin = true;
    //    }
    //}
    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while ((currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            StopWithResult();
            if (isFreeGame)
            {
                FruitParadiseFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (FruitParadiseAutoSpinController.isAutoSpinning)
            {
                FruitParadiseUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                FruitParadiseUIManager.Instance.UpdateButtons("Default");
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
        if (!InSpin) { return; }
        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            InSpin = false;
            ClearPaylines();
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

        FruitParadiseUIManager.Instance.SetStopInteractable(false);
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

        ProcessSpinResult();

        InSpin = false;
        isSpinAgain = true;
    }

    public List<int> winningIndices = new();
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
            FruitParadiseUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = FruitParadiseUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount >= 3)
        {
            //if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            //{
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    FruitParadisePaylineResult result = new FruitParadisePaylineResult(payline.paylineIndex, payline.count);
                    FruitParadisePaylineController.Instance.AddPaylineData(result);
                }
            //}

            Invoke(nameof(ShowPaylinesWrapper), 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }

        InSpin = false;
        isSpinAgain = true;
        if (isFreeGameReady)
        {
            FruitParadiseUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!FruitParadiseAutoSpinController.isAutoSpinning && !isFreeGame && FruitParadiseUIManager.Instance.winAnimationCompleted)
        {
            FruitParadiseUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            FruitParadiseUIManager.Instance.UpdateButtons("Free Spin");
        }
    }


    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylinesWrapper()
    {
        var plc = FruitParadisePaylineController.Instance;
        if (plc != null) plc.ShowCollectedPaylines(scatterCount);
        else isPaylineCompleted = true;
    }

    #endregion
    
    #region Cleanup

    public override void ClearPaylines() { }

    #endregion


    #region Slot Result Data
    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        FruitParadiseUIManager.Instance.ToggleSpinButton();
        StopWithResult();
    }

    public static FruitParadiseSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
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