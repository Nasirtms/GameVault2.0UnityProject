using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MonkeyMadnessSlotMachine : BaseSlotMachine
{
    #region Variables

    public static MonkeyMadnessSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public MonkeyMadnessGameSettings settings;
    public List<MonkeyMadnessReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public MonkeyMadnessSlotType[,] resultMatrix;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] private SpinResult currentSpinResult;

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
    private bool _firstAutoSpin = true;
    private bool isSettingResult;

    // Coins Variables
    private float winAmount;

    // Events
    public event Action StopReelProcess;

    // 3 Reels
    public static List<MonkeyMadnessSlotResource> CachedRealSymbols { get; private set; }
    public static MonkeyMadnessSlotResource? CachedEmptySymbol { get; private set; }

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
        MonkeyMadnessGameSettings.UpdateScale += UpdateScale;
        MonkeyMadnessReelScript.OnSpinComplete += OnReelSpinComplete;
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
        MonkeyMadnessGameSettings.UpdateLayout -= UpdateLayout;
        MonkeyMadnessGameSettings.UpdateScale -= UpdateScale;
        MonkeyMadnessReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != MonkeyMadnessSlotType.Empty);
        CachedEmptySymbol = settings.resourcesList.Find(r => r.type == MonkeyMadnessSlotType.Empty);

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
        this.resultMatrix = new MonkeyMadnessSlotType[reels.Count, 3];
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

        MonkeyMadnessUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin
    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        MonkeyMadnessPaylineController.Instance.StopPaylineLoop();
        MonkeyMadnessPaylineController.Instance.ClearPaylineResults();
        MonkeyMadnessUIManager.Instance.UpdateWinAmount(0f);
        MonkeyMadnessUIManager.Instance.SetStopInteractable(false);

        // Reset Variables and Functions State
        winAmount = 0;
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
            ? MonkeyMadnessGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? MonkeyMadnessGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = MonkeyMadnessGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == MonkeyMadnessSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == MonkeyMadnessSpinType.Single)
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
        if (settings.spinSettings.endSpin == MonkeyMadnessSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == MonkeyMadnessSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !MonkeyMadnessAutoSpinController.isAutoSpinning)
        {
            MonkeyMadnessUIManager.Instance.UpdateButtons("Stop");

            if (!MonkeyMadnessAutoSpinController.isAutoSpinning)
            {
                MonkeyMadnessSoundManager.Instance.StopReelStopSFX();
            }
        }
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
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult(); // fallback
            if (MonkeyMadnessAutoSpinController.isAutoSpinning)
            {
                MonkeyMadnessUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                MonkeyMadnessUIManager.Instance.UpdateButtons("Stop");
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

        MonkeyMadnessUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
            {
                break;
            }

            yield return new WaitForSeconds(_delayAmongReel);
            reels[i].canStopReel = true;
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        //MonkeyMadnessUIManager.Instance.SetStopInteractable(false);

        ProcessSpinResult();
    }
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

        if (winAmount > 0)
        {
            float betAmount = MonkeyMadnessUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                MonkeyMadnessPaylineResult result = new MonkeyMadnessPaylineResult(payline.paylineIndex);
                MonkeyMadnessPaylineController.Instance.AddPaylineResult(result);
            }

            Invoke("ShowPaylines", 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }

        InSpin = false;
        isSpinAgain = true;

        if (winAmount > 0f)
            MonkeyMadnessUIManager.Instance.PlaySound("Win");

        if (!MonkeyMadnessAutoSpinController.isAutoSpinning)
        {
            MonkeyMadnessUIManager.Instance.UpdateButtons("Stop");

        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    #endregion

    #region Animation

    public void AnimateSlotsFromPattern(int[,] pattern, float flickerTime = 0.3f)
    {
        int reelCount = reels.Count;
        int rowCount = 3;

        for (int x = 0; x < reelCount; x++)
        {
            var reel = reels[x];
            for (int y = 0; y < rowCount; y++)
            {
                if (pattern[x, y] == 1)
                {
                    var slot = reel.slots[y + 1];
                    if (slot != null)
                        slot.PlayFlickerAnimation(flickerTime);
                }
            }
        }
    }

    private void ShowPaylines()
    {
        MonkeyMadnessPaylineController.Instance.StartPaylineLoop();
    }

    public void StopAllSlotAnimations()
    {
        foreach (var reel in reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                    slot.StopFlickerAnimation();
            }
        }
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

    public static MonkeyMadnessSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
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
