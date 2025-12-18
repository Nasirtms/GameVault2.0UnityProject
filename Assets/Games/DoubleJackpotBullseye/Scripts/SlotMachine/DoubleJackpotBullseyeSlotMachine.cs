using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DoubleJackpotBullseyeSlotMachine : BaseSlotMachine
{
    #region Variables

    public static DoubleJackpotBullseyeSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public DoubleJackpotBullseyeGameSettings settings;
    public List<DoubleJackpotBullseyeReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public DoubleJackpotBullseyeSlotType[,] resultMatrix;

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

    [HideInInspector] public float freeSpinWinAmount = 0f;
    [HideInInspector] public bool isFreeGame = false;
    [HideInInspector] public bool isFreeGameReady = false;
    [HideInInspector] public bool playingwinanimation;
    [HideInInspector] public int bullseyeCount = 0;
    //public bool HasBullseyeOnMiddleReel { get; private set; }

    // --- TEST ONLY ---
    //[Header("TEST ONLY")]
    //[SerializeField] private bool testMode = true;      // enable/disable this whole injection
    //[SerializeField] private bool testOnce = true;      // auto-disable after first trigger
    //[SerializeField] private string testBullseyeId = "DoubleJackpotBullseye"; // backend id
    //private bool testHasTriggered = false;              // internal guard

    // Coins Variables
    private float winAmount;
    public Coroutine AnimateToValueCoroutine;

    // Events
    public event Action StopReelProcess;

    // 3 Reels Slot
    public static List<DoubleJackpotBullseyeSlotResource> CachedRealSymbols { get; private set; }
    public static DoubleJackpotBullseyeSlotResource? CachedEmptySymbol { get; private set; }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        // Adding Game to Registry
        UpdateSlotServicesGameName();

        // Subscribing Events
        DoubleJackpotBullseyeGameSettings.UpdateLayout += UpdateLayout;
        DoubleJackpotBullseyeGameSettings.UpdateScale += UpdateScale;
        DoubleJackpotBullseyeReelScript.OnSpinComplete += OnReelSpinComplete;
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
        DoubleJackpotBullseyeGameSettings.UpdateLayout -= UpdateLayout;
        DoubleJackpotBullseyeGameSettings.UpdateLayout -= UpdateLayout;
        DoubleJackpotBullseyeReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != DoubleJackpotBullseyeSlotType.Empty);
        CachedEmptySymbol = settings.resourcesList.Find(r => r.type == DoubleJackpotBullseyeSlotType.Empty);

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
        this.resultMatrix = new DoubleJackpotBullseyeSlotType[reels.Count, 3];
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

            //if (testMode && !isFreeGame && !testHasTriggered && currentSpinResult != null && currentSpinResult.reels != null)
            //{
            //    // Safety: make sure reel/row exists (reel index 1 = middle reel, row index 1 = middle row)
            //    if (currentSpinResult.reels.Count > 1 && currentSpinResult.reels[1].Count > 1)
            //    {
            //        // 1) Force the bullseye symbol in the JSON result (doesn't touch your visual process)
            //        currentSpinResult.reels[1][1].id = testBullseyeId;

            //        // 2) Pre-mark detection so we don't depend on UpdateMatrix timing for the test
            //        bullseyeCount = 1;
            //        HasBullseyeOnMiddleReel = true;

            //        // 3) Only arm the feature if this backend spin actually has a payline win
            //        isFreeGameReady = true;

            //        // Optional: log so you can see the condition in the Console
            //        Debug.Log($"[TEST] Bullseye injected. paylineWins={(currentSpinResult.paylineWins?.Count ?? 0)}, isFreeGameReady={isFreeGameReady}");

            //        // If you want this to be a one-and-done test, mark it
            //        if (isFreeGameReady && testOnce) { testHasTriggered = true; testMode = false; }
            //    }
            //}
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

        if (isFreeGame && reels.Count > 1)
        {
            var mid = reels[1];
            var s0 = mid.GetSlotType(0);
            var s1 = mid.GetSlotType(1);
            var s2 = mid.GetSlotType(2);

            if (spinSymbolMatrix.Count > 1 && spinSymbolMatrix[1].Count >= 3)
            {
                spinSymbolMatrix[1][0].id = s0.ToString();
                spinSymbolMatrix[1][1].id = s1.ToString();
                spinSymbolMatrix[1][2].id = s2.ToString();
            }
        }
        DoubleJackpotBullseyeUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        DoubleJackpotBullseyePaylineController.Instance.StopPaylineLoop();
        DoubleJackpotBullseyePaylineController.Instance.ClearPaylineResults();
        if (!isFreeGame) DoubleJackpotBullseyeUIManager.Instance.UpdateWinAmount(0f);
        DoubleJackpotBullseyeUIManager.Instance.SetStopInteractable(false);

        // Reset Variables and Functions State
        winAmount = 0;
        if (!isFreeGame) freeSpinWinAmount = 0f;
        isSettingResult = false;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isPaylineCompleted = false;
        isSpinAgain = false;
        InSpin = true;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        ClearPaylines();

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? DoubleJackpotBullseyeGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? DoubleJackpotBullseyeGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = DoubleJackpotBullseyeGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        bool freeGame = isFreeGame;

        if (freeGame)
        {
            // Freeze reel-1 (index 1), spin only 0 and 2
            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].ResetShape();
                if (i == 1) continue; // don't spin the bullseye reel
                reels[i].Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == DoubleJackpotBullseyeSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == DoubleJackpotBullseyeSpinType.Single)
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

    private void OnReelSpinComplete(int index)
    {
        if (settings.spinSettings.endSpin == DoubleJackpotBullseyeSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == DoubleJackpotBullseyeSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !DoubleJackpotBullseyeAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Stop");
            DoubleJackpotBullseyeUIManager.Instance.StopSpinMusic("Spin");
        }
    }
    public bool errorFreeSpin;
    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 5f;
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
                errorFreeSpin = true;
                //DoubleJackpotBullseyeFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (DoubleJackpotBullseyeAutoSpinController.isAutoSpinning)
            {
                DoubleJackpotBullseyeUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Stop");
            }

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

    #endregion

    #region Result Stop

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].canStopReel = true;
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        DoubleJackpotBullseyeUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
                break;

            if (isFreeGame && i == 1)
                continue;

            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        ProcessSpinResult();
        DoubleJackpotBullseyeUIManager.Instance.StopSpinMusic("Spin");
        //InSpin = false;
        //isSpinAgain = true;

        //if (!DoubleJackpotBullseyeAutoSpinController.isAutoSpinning)
        //{
        //    DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Stop");
        //}
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    public bool hasaniamtion;
    private void ProcessSpinResult()
    {
        bool justEnteredFreeGame = (!isFreeGame && currentSpinResult.isFreeSpin);

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

        if (!isFreeGame)
        {
            isFreeGame = (currentSpinResult != null && currentSpinResult.isFreeSpin);
            isFreeGameReady = isFreeGame;
        }
        else if(isFreeGame && winAmount > 0)
        {
            isFreeGame = true;
            isFreeGameReady = false;
        }
        else if(isFreeGame && winAmount <= 0)
        {
            isFreeGame = false;
            isFreeGameReady = false;
        }

        if (winAmount > 0)
        {
            if (!isFreeGame || justEnteredFreeGame)
            {
                float betAmount = DoubleJackpotBullseyeUIManager.Instance.CurrentBet();

                if (winAmount >= (betAmount * 5000))
                {
                    DoubleJackpotBullseyeUIManager.Instance.PlayJackpotWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 500))
                {
                    DoubleJackpotBullseyeUIManager.Instance.PlaySuperWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 100))
                {
                    DoubleJackpotBullseyeUIManager.Instance.PlayMegaWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 50))
                {
                    DoubleJackpotBullseyeUIManager.Instance.PlayBigWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 10))
                {
                    DoubleJackpotBullseyeUIManager.Instance.PlayNiceWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else
                {
                    DoubleJackpotBullseyeUIManager.Instance.UpdateWinAmount(winAmount);
                }

            }
            else
            {
                DoubleJackpotBullseyeUIManager.Instance.StartCoroutine(DoubleJackpotBullseyeUIManager.Instance.AnimateValue(freeSpinWinAmount, (freeSpinWinAmount+winAmount), 0.7f, DoubleJackpotBullseyeUIManager.Instance.winAmount));
                freeSpinWinAmount += winAmount;
            }
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                DoubleJackpotBullseyePaylineResult result = new DoubleJackpotBullseyePaylineResult(payline.paylineIndex);
                DoubleJackpotBullseyePaylineController.Instance.AddPaylineResult(result);
            }

            Invoke("ShowPaylines", 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Transition");
        }
        else if (!DoubleJackpotBullseyeAutoSpinController.isAutoSpinning && !isFreeGame && DoubleJackpotBullseyeUIManager.Instance.winAnimationCompleted)
        {
            DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            DoubleJackpotBullseyeUIManager.Instance.UpdateButtons("enterfreeSpin");
        }
    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        DoubleJackpotBullseyePaylineController.Instance.StartPaylineLoop();
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

    public static DoubleJackpotBullseyeSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            Debug.LogWarning("Settings or resourcesList is null.");
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
