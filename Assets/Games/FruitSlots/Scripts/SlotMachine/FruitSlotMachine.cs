using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class FruitSlotMachine : BaseSlotMachine
{
    #region Variables
    public static FruitSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public FruitSlotGameSettings settings;
    public List<FruitSlotReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    public FruitSlotAutoSpinController fruitSlotAutoSpinController;

    [Header("Result")]
    [ShowInInspector][ReadOnly] public FruitSlotType[,] resultMatrix;

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
    //public bool InSpin;
    //public bool isStopBtnPressed = false;
    public bool isSpinAgain = false;
    public bool isPaylineCompleted;
    public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    public event Action StopReelProcess;
    public float winAmount;
    private bool firstAutoSpin = true;

    //Free Spin 
    //public bool isFreeGame;
    public bool isFreeGameReady;
    public int freeSpinCount;
    public float freeSpinWinAmount;
    public int scatterCount;
    public bool firstFreeSpin;
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
        FruitSlotGameSettings.UpdateLayout += UpdateLayout;
        FruitSlotGameSettings.UpdateScale += UpdateScale;
        //FruitSlotReelScript.OnSpinComplete += OnReelSpinComplete;
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

        FruitSlotGameSettings.UpdateLayout -= UpdateLayout;
        FruitSlotGameSettings.UpdateScale -= UpdateScale;
        //FruitSlotReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        this.resultMatrix = new FruitSlotType[reels.Count, 3];
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

        if (scatterCount >= 6)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            if (isFakeSpins)
            {
                freeSpinCount = fakeFreeSpins;
            }
            else
            {
                freeSpinCount = currentSpinResult.freeSpinCount;
            }
        }
        Debug.Log("📩 SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        // ✅ Clear and fill spin matrix from backend
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

        FruitSlotUIManager.Instance.SetStopInteractable(true);
    }
    #endregion

    #region Spin
    public override void Spin()
    {
        if (InSpin) return;
        StopAllCoroutines();
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            FruitSlotUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        FruitSlotUIManager.Instance.PlaySpinMusic("FruitSlot_Spin"); 
        FruitSlotPaylineController.Instance.ClearPaylineData();
        FruitSlotUIManager.Instance.SetStopInteractable(false);
        fruitSlotAutoSpinController.cancelRequested = false;
        freeSpinCount = 0;
        scatterCount = 0;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isPaylineCompleted = false;
        isSettingResult = false;
        isSpinAgain = false;
        winningIndices.Clear();
        ClearPaylines();
        InSpin = true;
        horizontalLayout.enabled = false;
        FruitSlotUIManager.Instance.winAnimationCompleted = true;
        _acceleration = settings.spinSettings.useSameAcceleration
            ? FruitSlotGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? FruitSlotGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = FruitSlotGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        foreach (var reel in reels)
        {
            reel.ResetShape();
            reel.Spin(8f, _acceleration, _speed);
        }
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

        while ((currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            //CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult(); // fallback
            if (isFreeGame)
            {
                FruitSlotGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (FruitSlotAutoSpinController.isAutoSpinning)
            {
                FruitSlotUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                FruitSlotUIManager.Instance.UpdateButtons("Stop");
            }
            FruitSlotUIManager.Instance.StopSpinMusic("FruitSlot_Spin");
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
            ClearPaylines();
            foreach (var reel in reels)
            {
                //Debug.Log("Force Stop : foreach Number ");
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

        FruitSlotUIManager.Instance.SetStopInteractable(false);
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

        //FruitSlotUIManager.Instance.SetStopInteractable(false);

        // 5️⃣ Wait until all reels are clamped
        //yield return new WaitUntil(() => reels.All(r => r.IsClamped()));
        FruitSlotUIManager.Instance.StopSpinMusic("FruitSlot_Spin");
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
            FruitSlotUIManager.Instance.UpdateWinAmount(winAmount,true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = FruitSlotUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        
        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount >= 6)
        {
            //if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            //{
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    FruitSlotPaylineResult result = new FruitSlotPaylineResult(payline.paylineIndex, payline.count);
                    FruitSlotPaylineController.Instance.AddPaylineData(result);
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
            FruitSlotUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!FruitSlotAutoSpinController.isAutoSpinning && !isFreeGame && FruitSlotUIManager.Instance.winAnimationCompleted)
        {
            FruitSlotUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            FruitSlotUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        if (currentSpinResult != null)
        {
            GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
        }
    }
    private void ShowPaylinesWrapper()
    {
        FruitSlotPaylineController.Instance.ShowCollectedPaylines(scatterCount);
        //var plc = FruitSlotPaylineController.Instance;
        //if (plc != null) plc.ShowCollectedPaylines(scatterCount);
        //else isPaylineCompleted = true;
    }
    #endregion

    #region Cleanup

    public override void ClearPaylines() { }

    #endregion

    #region Slot Result Data

   

    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        FruitSlotUIManager.Instance.ToggleSpinButton();
        StopWithResult();
    }

    public static FruitSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            return null;
        }

        var normalizedId = id.ToLowerInvariant();
        if (normalizedId == "lime") normalizedId = "lemon";

        //Debug.Log("NormalizedID: " + normalizedId);

        // Manually find match and return nullable
        foreach (var res in Instance.settings.resourcesList)
        {
            if (res.type.ToString().ToLowerInvariant() == normalizedId)
            {
                //Debug.Log("Resource found: " + res.type);
                return res;
            }
        }

        //Debug.LogWarning("Resource NOT found for ID: " + normalizedId);
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