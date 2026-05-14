using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RedHotTrippleSlotMachine : BaseSlotMachine
{
    #region Variables

    public static RedHotTrippleSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public RedHotTrippleGameSettings settings;
    public List<RedHotTrippleReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public RedHotTrippleSlotType[,] resultMatrix;

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
    //[HideInInspector] public bool InSpin;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    // Free Spin Game
    //public bool isFreeGame;
    public bool isFreeGameReady;
    public int freeSpinCount;
    public float freeSpinWinAmount;
    [HideInInspector] public bool firstFreeSpin;

    // Coins Variables
    public float winAmount;
    // Events
    public event Action StopReelProcess;

    // 3 Reels Slot
    public static List<RedHotTrippleSlotResource> CachedRealSymbols { get; private set; }
    public static RedHotTrippleSlotResource? CachedEmptySymbol { get; private set; }

    //searching spincash slot
    public int fakesymbolCount = 0;
    public bool fakehasSymbol = false;
    public int symbolCount = 0;
    public bool hasSymbol = false;
    //public GameObject lastReelEffect;
    //private Animator lastReelAnimator;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        UpdateSlotServicesGameName();

        RedHotTrippleGameSettings.UpdateLayout += UpdateLayout;
        RedHotTrippleGameSettings.UpdateScale += UpdateScale;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        UpdateSettings();
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

        RedHotTrippleGameSettings.UpdateLayout -= UpdateLayout;
        RedHotTrippleGameSettings.UpdateLayout -= UpdateLayout;

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
    public void UpdateCachedSymbols()
    {
        CachedRealSymbols = settings.resourcesList.FindAll(r =>
            r.type != RedHotTrippleSlotType.Empty &&
            (isFreeGame || !IsFreeSpinSymbol(r.type)));

        CachedEmptySymbol = settings.resourcesList.Find(r => r.type == RedHotTrippleSlotType.Empty);
    }
    public void UpdateSettings()
    {
        UpdateCachedSymbols();

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
        this.resultMatrix = new RedHotTrippleSlotType[reels.Count, 3];
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
    [Header("Fake Free Spin")]
    public bool fakeFreeSpin;
    public int fakeScatterCount;
    public int fakeFreeSpins;
    public int scatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
        spinSymbolMatrix.Clear();

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;


            scatterCount = currentSpinResult.scatterCount;
            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        else if (fakeFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            scatterCount = fakeScatterCount;
            freeSpinCount = fakeFreeSpins;
        }

        int reelIndex = 0;
        symbolCount = 0;
        bool[] reelHasSymbol = new bool[2];
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                if (reelIndex < 2)
                {
                    var res = GetResourceById(symbol.id);
                    if (res.HasValue && isSymbolSlot(res.Value.type))
                    {
                        reelHasSymbol[reelIndex] = true;
                    }

                }
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }
        if (reelHasSymbol[0] && reelHasSymbol[1])
        {
            hasSymbol = true;
            symbolCount = 2;
        }
        RedHotTrippleUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            RedHotTrippleUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0;
            winAmount = 0f;

        }
        StopAllCoroutines();
        RedHotTripplePaylineController.Instance.StopPaylineLoop();
        RedHotTripplePaylineController.Instance.ClearPaylineResults();
        RedHotTrippleUIManager.Instance.SetStopInteractable(false);

        // Reset Variables and Functions State

        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isPaylineCompleted = false;
        horizontalLayout.enabled = false;
        hasSymbol = false;
        symbolCount = 0;
        _reelsCount = reels.Count;
        RedHotTrippleUIManager.Instance.PlaySpinMusic("Spin");
        RedHotTrippleUIManager.Instance.StopCurrentSFX();
        isFreeGameReady = false;
        RedHotTrippleUIManager.Instance.winAnimationCompleted = true;
        ClearPaylines();

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? RedHotTrippleGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? RedHotTrippleGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = RedHotTrippleGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == RedHotTrippleSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == RedHotTrippleSpinType.Single)
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
                RedHotTrippleFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (RedHotTrippleAutoSpinController.isAutoSpinning)
            {
                RedHotTrippleUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                RedHotTrippleUIManager.Instance.UpdateButtons("Stop");
            }
            isSpinAgain = true;
            RedHotTrippleUIManager.Instance.StopSpinMusic("Spin");
            RedHotTrippleUIManager.Instance.StopCurrentSFX();
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }

    public void StopWithResult() => Stop();

    public void Stop()
    {
        if (!InSpin) return;

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
        //if (lastReelAnimator != null)
        //{
        //    lastReelAnimator.SetBool("LastReel", false);
        //    lastReelEffect.SetActive(false);
        //}
        RedHotTrippleUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();
        //lastReelAnimator = lastReelEffect.GetComponent<Animator>();
        if (settings.spinSettings.endSpin == RedHotTrippleSpinType.All)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isStopBtnPressed)
                    break;

                //if (reels[i] == reels[reels.Count - 1] && hasSymbol && symbolCount == 2)
                //{
                //    lastReelEffect.SetActive(true);
                //    lastReelAnimator.SetBool("LastReel", true);
                //    if (isStopBtnPressed)
                //        break;
                //    yield return new WaitForSeconds(1.1f);
                //    lastReelAnimator.SetBool("LastReel", false);
                //    lastReelEffect.SetActive(false);
                //}
                reels[i].canStopReel = true;
            }
        }
        else
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isStopBtnPressed)
                    break;

                //if (reels[i] == reels[reels.Count - 1] && hasSymbol && symbolCount == 2)
                //{
                //    lastReelEffect.SetActive(true);
                //    lastReelAnimator.SetBool("LastReel", true);
                //    if (isStopBtnPressed)
                //        break;
                //    yield return new WaitForSeconds(1.1f);
                //    lastReelAnimator.SetBool("LastReel", false);
                //    lastReelEffect.SetActive(false);
                //}
                yield return new WaitForSeconds(_delayAmongReel);

                reels[i].canStopReel = true;
            }
        }
        RedHotTrippleUIManager.Instance.PlaySound("ReelStop");
        if (isStopBtnPressed)
            StopButtonPressed();

        RedHotTrippleUIManager.Instance.StopSpinMusic("Spin");
        ProcessSpinResult();
        InSpin = false;
        isSpinAgain = true;
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

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            RedHotTrippleUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0f)
        {
            float betAmount = RedHotTrippleUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount >= 3)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                RedHotTripplePaylineResult result = new RedHotTripplePaylineResult(payline.paylineIndex, payline.symbol, payline.count);
                RedHotTripplePaylineController.Instance.AddPaylineResult(result);
            }

            Invoke("ShowPaylines", 1f);
        }
        else
        {
            isPaylineCompleted = true;
        }
        
        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            RedHotTrippleUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!RedHotTrippleAutoSpinController.isAutoSpinning && !isFreeGame && RedHotTrippleUIManager.Instance.winAnimationCompleted)
        {
            RedHotTrippleUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            RedHotTrippleUIManager.Instance.UpdateButtons("FreeSpin");
        }
    }
   
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        RedHotTripplePaylineController.Instance.StartPaylineLoop(scatterCount);
        if (winAmount > 0f)
        {
            RedHotTrippleUIManager.Instance.PlaySound("Win");
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

    public static RedHotTrippleSlotResource? GetResourceById(string id)
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
    public bool isSymbolSlot(RedHotTrippleSlotType slotType)
    {
        if (slotType == RedHotTrippleSlotType.BonusSymbol)
        {
            return true;
        }

        return false;
    }
    public bool IsFreeSpinSymbol(RedHotTrippleSlotType type)
    {
        return type == RedHotTrippleSlotType.Two ||
               type == RedHotTrippleSlotType.Three ||
               type == RedHotTrippleSlotType.Four ||
               type == RedHotTrippleSlotType.Five ||
               type == RedHotTrippleSlotType.Six ||
               type == RedHotTrippleSlotType.Seven;
    }
    #endregion
}