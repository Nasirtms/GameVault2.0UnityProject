using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GoldenDragonSlotMachine : BaseSlotMachine
{
    #region Variables

    public static GoldenDragonSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public GoldenDragonGameSettings settings;
    public List<GoldenDragonReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    //public GoldenDragonAutoSpinController goldenDragonAutoSpinController;
    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public GoldenDragonSlotType[,] resultMatrix;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // Mini Game Variables
    public Button miniGameButton;
    private Tween miniGameButtonTween;

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
    [HideInInspector] public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    public bool isMiniGame;
    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public bool firstFreeSpin;
    [HideInInspector] public int freeSpinCount;

    // Coins Variables
    public float winAmount;
    // Events
    public event Action StopReelProcess;

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

        GoldenDragonGameSettings.UpdateLayout += UpdateLayout;
        GoldenDragonGameSettings.UpdateScale += UpdateScale;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        UpdateSettings();
        winAmount = 0;
        InSpin = false;

        if (miniGameButton != null)
        {
            miniGameButton.transform.localScale = Vector3.one;
            miniGameButton.gameObject.SetActive(false);
            miniGameButton.onClick.AddListener(() =>
            {
                GoldenDragonUIManager.Instance.PlaySound("Maxbet");
                OnMiniGameButtonClicked();
            });
        }
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

        GoldenDragonGameSettings.UpdateLayout -= UpdateLayout;
        GoldenDragonGameSettings.UpdateScale -= UpdateScale;

        if (SpinResultController.Instance != null)
            SpinResultController.Instance.OnSpinResultReceived -= OnSpinResultReceived;

        if (miniGameButton != null)
        {
            miniGameButton.onClick.RemoveListener(OnMiniGameButtonClicked);
        }
        miniGameButtonTween?.Kill();
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
        this.resultMatrix = new GoldenDragonSlotType[reels.Count, 3];
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
                symbols.Add(symbol); var res = GetResourceById(symbol.id);

                if (res.HasValue)
                {
                    if (isCopperCoinSlot(res.Value.type) )
                    {
                        CopperCoin_Count++;
                    }
                }
            }
            spinSymbolMatrix.Add(symbols);
        }

        GoldenDragonUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;
        GoldenDragonUIManager.Instance.StopCurrentSFX();
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            GoldenDragonUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0f;
            winAmount = 0f;
        }

        StopAllCoroutines();
        HideMiniGameButton();
        GoldenDragonPaylineController.Instance.ClearPaylineData();
        GoldenDragonUIManager.Instance.SetStopInteractable(false);

        GoldenDragonUIManager.Instance.PlaySpinMusic("Spin");
        // Reset Variables and Functions State
        freeSpinCount = 0;
        scatterCount = 0;
        CopperCoin_Count = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isPaylineCompleted = false;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        isFreeGameEnded = false;
        GoldenDragonUIManager.Instance.winAnimationCompleted = true;
        ClearPaylines();
        isMiniGame = false;
        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? GoldenDragonGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? GoldenDragonGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = GoldenDragonGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == GoldenDragonSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == GoldenDragonSpinType.Single)
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
                GoldenDragonFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (GoldenDragonAutoSpinController.isAutoSpinning)
            {
                GoldenDragonUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                GoldenDragonUIManager.Instance.UpdateButtons("Stop");
            }
  
            isSpinAgain = true;
            GoldenDragonUIManager.Instance.StopSpinMusic("Spin");
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

        GoldenDragonUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        if (settings.spinSettings.endSpin == GoldenDragonSpinType.All)
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

        GoldenDragonUIManager.Instance.StopSpinMusic("Spin");
        ProcessSpinResult();
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    public int CopperCoin_Count = 0;
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
            GoldenDragonUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0f)
        {
            float betAmount = GoldenDragonUIManager.Instance.CurrentBet();
            //Invoke(nameof(UpdateGameCoin), 1f);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            if (!isFreeGame && !isFreeGameReady)
            {
                ShowMiniGameButton();
            }
        }
        
        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount >= 3 || CopperCoin_Count >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    GoldenDragonPaylineResult result = new GoldenDragonPaylineResult(payline.paylineIndex, payline.count);
                    GoldenDragonPaylineController.Instance.AddPaylineData(result);
                }
            }

            Invoke(nameof(ShowPaylines), 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }

        if (winAmount > 0) 
        {
            GoldenDragonUIManager.Instance.PlaySound("Win");
        }
        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            GoldenDragonUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!GoldenDragonAutoSpinController.isAutoSpinning && !isFreeGame && GoldenDragonUIManager.Instance.winAnimationCompleted)
        {
            GoldenDragonUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            GoldenDragonUIManager.Instance.UpdateButtons("Free Spin");
        }
    }

    private void ShowPaylines()
    {
        GoldenDragonPaylineController.Instance.ShowCollectedPaylines(scatterCount);

    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region MiniGame Helper
    public bool isFreeGameEnded;
    private void OnMiniGameButtonClicked()
    {
        HideMiniGameButton();

        if (isFreeGameEnded)
        {
            GoldenDragonMiniGame.Instance.StartMiniGame(freeSpinWinAmount);
        }
        else
        {
            GoldenDragonMiniGame.Instance.StartMiniGame(GetWinAmount());
        }
    }
    public void ShowMiniGameButton()
    {
        if (miniGameButton == null) return;

        miniGameButton.gameObject.SetActive(true);

        miniGameButtonTween?.Kill();
        miniGameButton.transform.localScale = Vector3.one;

        miniGameButtonTween = miniGameButton.transform
            .DOScale(1.05f, 0.8f)  
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo);
    }
    private void HideMiniGameButton()
    {
        if (miniGameButton == null) return;

        miniGameButtonTween?.Kill();
        miniGameButton.transform.localScale = Vector3.one;
        miniGameButton.gameObject.SetActive(false);
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
    public static GoldenDragonSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
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
    public bool isCopperCoinSlot(GoldenDragonSlotType slotType)
    {
        if (slotType == GoldenDragonSlotType.CopperCoin)
        {
            return true;
        }

        return false;
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