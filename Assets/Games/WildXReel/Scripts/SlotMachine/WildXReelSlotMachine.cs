using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Joins;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class WildXReelSlotMachine : BaseSlotMachine
{
    #region Variables

    public static WildXReelSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public WildXReelGameSettings settings;
    public List<WildXReelReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public WildXReelSlotType[,] resultMatrix;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    [Header("Animators")]
    public Animator paylineAnimator;

    // Spin Variables
    private float _timeCounter;
    private float _delayAmongReel;
    private float _acceleration;
    private float _speed;

    // Machine Variables
    private float _reelsCount;
    private int _reelIndex;

    // State Variables
    //[HideInInspector] public bool InSpin;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    // Coins Variables
    private float winAmount;
    public event Action StopReelProcess;
    public static List<WildXReelResource> CachedRealSymbols { get; private set; }
    public static WildXReelResource? CachedEmptySymbol { get; private set; }
    public bool firstSpin;
    public bool hasSymbol = false;

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

        WildXReelGameSettings.UpdateLayout += UpdateLayout;
        WildXReelGameSettings.UpdateScale += UpdateScale;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        UpdateSettings();
        InSpin = false;
        firstSpin = true;
        SetPaylineAnimator(false);
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

        WildXReelGameSettings.UpdateLayout -= UpdateLayout;
        WildXReelGameSettings.UpdateScale -= UpdateScale;

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
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != WildXReelSlotType.Empty);
        CachedEmptySymbol = settings.resourcesList.Find(r => r.type == WildXReelSlotType.Empty);

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
        this.resultMatrix = new WildXReelSlotType[reels.Count, 3];
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

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        int reelIndex = 0;
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            int slotIndex = 0;
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                if (slotIndex == 1 && (reelIndex == 0 || reelIndex == 1))
                {
                    var res = GetResourceById(symbol.id);

                    //if (res.HasValue && isImperialDiamondSlot(res.Value.type))
                    //{
                    //    hasSymbol = true;
                    //}
                }
                slotIndex++;
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        WildXReelUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        WildXReelUIManager.Instance.UpdateWinAmount(0f, false);
        winAmount = 0f;
        StopAllCoroutines();

        WildXReelPaylineController.Instance.StopPaylineLoop();
        WildXReelPaylineController.Instance.ClearPaylineResults();
        WildXReelUIManager.Instance.SetStopInteractable(false);
        WildXReelUIManager.Instance.StopCurrentSFX();
        WildXReelUIManager.Instance.PlaySpinMusic("Spin");

        SetPaylineAnimator(false);
        currentSpinResult = null;
        InSpin = true;
        hasSymbol = false;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isPaylineCompleted = false;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        WildXReelUIManager.Instance.winAnimationCompleted = true;
        ClearPaylines();

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? WildXReelGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? WildXReelGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = WildXReelGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == WildXReelSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == WildXReelSpinType.Single)
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

            if (WildXReelAutoSpinController.isAutoSpinning)
            {
                WildXReelUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                WildXReelUIManager.Instance.UpdateButtons("Stop");
            }

            isSpinAgain = true;
            WildXReelUIManager.Instance.StopSpinMusic("Spin");
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
        WildXReelUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();
        if (settings.spinSettings.endSpin == WildXReelSpinType.All)
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

        WildXReelUIManager.Instance.StopSpinMusic("Spin");
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

        winAmount = forcedWin ? forcedPrize : currentSpinResult.totalWin;

        if (winAmount > 0f)
        {
            float betAmount = WildXReelUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                WildXReelPaylineResult result = new WildXReelPaylineResult(payline.paylineIndex, payline.symbol, payline.count);
                WildXReelPaylineController.Instance.AddPaylineResult(result);
            }
            Invoke(nameof(ShowPaylines), 0.5f);
        }
        else
        {
            SetPaylineAnimator(false);
            isPaylineCompleted = true;
        }

        if (winAmount > 0f)
            WildXReelUIManager.Instance.PlaySound("Win");

        InSpin = false;
        isSpinAgain = true;

        if (!WildXReelAutoSpinController.isAutoSpinning && !isFreeGame && WildXReelUIManager.Instance.winAnimationCompleted)
        {
            WildXReelUIManager.Instance.UpdateButtons("Stop");
        }
    }

    private void ShowPaylines()
    {
        SetPaylineAnimator(true);
        WildXReelPaylineController.Instance.StartPaylineLoop();

    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void SetPaylineAnimator(bool flag)
    {
        paylineAnimator.gameObject.SetActive(flag);
        if (paylineAnimator == null) return;

        paylineAnimator.enabled = flag;
        paylineAnimator.SetBool("Play", flag);
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
    public static WildXReelResource? GetResourceById(string id)
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
    public bool isWildXReelSlot(WildXReelSlotType slotType)
    {
        if (slotType == WildXReelSlotType.Wild)
        {
            return true;
        }

        return false;
    }
    #endregion
}