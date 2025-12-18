using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CleopatraSlotMachine : BaseSlotMachine
{
    #region Variables

    public static CleopatraSlotMachine Instance;

    

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public CleopatraGameSettings settings;
    public List<CleopatraReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public CleopatraSlotType[,] resultMatrix;

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
    private bool _firstAutoSpin = true;
    private bool isSettingResult;

    // Free Spin Game
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int scatterCount;

    // Coins Variables
    private float winAmount;

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
        CleopatraGameSettings.UpdateLayout += UpdateLayout;
        CleopatraGameSettings.UpdateScale += UpdateScale;
        CleopatraReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        // Update Settings
        UpdateSettings();

        // Initialize Variables
        InSpin = false;
        isFreeGameReady = false;
        isFreeGame = false;

        forcedWin = true;
        ForcedToggle();
        forcedButton.onClick.AddListener(ForcedToggle);
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
        CleopatraGameSettings.UpdateLayout -= UpdateLayout;
        CleopatraGameSettings.UpdateScale -= UpdateScale;
        CleopatraReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        this.resultMatrix = new CleopatraSlotType[reels.Count, 3];
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
    [Header("Fake Scatter")]
    public int fakeScatterCount;

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

        if (currentSpinResult.scatterCount >= 2)
            scatterCount = currentSpinResult.scatterCount;
        else
            scatterCount = fakeScatterCount;    

        if (scatterCount > 2)
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
            }
            CleopatraUIManager.Instance.freeGameSpinCount = 3;
            //CleopatraUIManager.Instance.freeGameSpinCount += currentSpinResult.freeSpinCount;
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

        CleopatraUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        CleopatraPaylineController.Instance.StopPaylines();
        CleopatraPaylineController.Instance.ClearPaylineData();
        CleopatraUIManager.Instance.UpdateWinAmount(0f);
        //SlotSpinService.Instance.isCoinUpdaterOrNot = false;
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

        CleopatraUIManager.Instance.SetStopInteractable(false);

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? CleopatraGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? CleopatraGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = CleopatraGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == CleopatraSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == CleopatraSpinType.Single)
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
        if (settings.spinSettings.endSpin == CleopatraSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == CleopatraSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !CleopatraAutoSpinController.isAutoSpinning)
        {
            CleopatraUIManager.Instance.UpdateButtons("Single Stop");
            _firstAutoSpin = true;

            //if (!CleopatraAutoSpinController.isAutoSpinning)
            //{
            //    CleopatraSoundManager.Instance.StopSpinMusic("Spin");
            //}
            CleopatraUIManager.Instance.StopSpinMusic("Spin");
        }
    }

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
                CleopatraGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (CleopatraAutoSpinController.isAutoSpinning)
            {
                CleopatraUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                CleopatraUIManager.Instance.UpdateButtons("Default");
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
            //SlotSpinService.Instance.isCoinUpdaterOrNot = true;
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
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        InSpin = false;

        ProcessSpinResult();
        CleopatraUIManager.Instance.StopSpinMusic("Spin");
        if (!CleopatraAutoSpinController.isAutoSpinning)
        {
            CleopatraUIManager.Instance.UpdateButtons("Single Stop");
            
        }

        isSpinAgain = true;
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    
    public Button forcedButton;
    public TMP_InputField forcedInputField;

    public void ForcedToggle()
    {
        forcedWin = !forcedWin;

        if (forcedWin)
        {
            forcedButton.image.color = Color.green;
        }
        else
        {
            forcedButton.image.color = Color.red;
        }
    }

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            Debug.LogWarning("❌ Spin result is invalid or failed.");
            return;
        }

        if (forcedWin)
        {
            try
            {
                winAmount = float.Parse(forcedInputField.text);
            }
            catch
            {
                winAmount = currentSpinResult.totalWin;
            }
        }
        else
        {
            winAmount = currentSpinResult.totalWin;
        }

        if (isFreeGame && winAmount > 0)
        {
            CleopatraUIManager.Instance.PlayFreeGameWinAnimation(winAmount);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = CleopatraUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount >= 2)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    CleopatraPaylineResult result = new CleopatraPaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
                    CleopatraPaylineController.Instance.AddPaylineData(result);
                }
            }

            Invoke("ShowPaylines", 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }
    }

    private void ShowPaylines()
    {
        CleopatraPaylineController.Instance.StartPayline(scatterCount);
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

    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();
    }

    public static CleopatraSlotResource? GetResourceById(string id)
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
        if (forcedWin)
        {
            try
            {
                return float.Parse(forcedInputField.text);
            }
            catch
            {
                return 0f;
            }
        }
        else
        {
            return currentSpinResult.totalWin;
        }
    }

    #endregion
}
