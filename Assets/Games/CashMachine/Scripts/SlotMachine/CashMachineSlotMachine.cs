using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SlotMachine2D;

public class CashMachineSlotMachine : BaseSlotMachine
{
    #region Variables

    public static CashMachineSlotMachine Instance;
    public List<int> LockedReels = new List<int>();

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public CashMachineGameSettings settings;
    public List<CashMachineReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public CashMachineSlotType[,] resultMatrix;

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
    //public bool InSpin;
    //[HideInInspector] public bool isStopBtnPressed = false;
    public bool isSpinAgain = false;
    [HideInInspector] public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    [HideInInspector] public float freeSpinWinAmount = 0f;
    //[HideInInspector] public bool isFreeGame = false;
    [HideInInspector] public bool isFreeGameReady = false;
    [HideInInspector] public bool playingwinanimation;
    [HideInInspector] public int bullseyeCount = 0;
    //public bool HasBullseyeOnMiddleReel { get; private set; }

    public bool canReel2spin = false;
    public bool canReel3spin = false;
    public bool isHighStake = false;

    // Coins Variables
    private float winAmount;
    public Coroutine AnimateToValueCoroutine;

    // Events
    public event Action StopReelProcess;

    // 3 Reels Slot
    public static List<CashMachineSlotResource> CachedRealSymbols { get; private set; }
    //public static CashMachineSlotResource? CachedEmptySymbol { get; private set; }

    #region SlotBools
    public bool zeroOnReel2;
    public bool zeroOnReel3;
    public bool doubleZeroOnReel3;
    public int freeSpinsDone = 0;
    public bool decoyFreeSpinBool = false;
    #endregion

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
        CashMachineGameSettings.UpdateLayout += UpdateLayout;
        CashMachineGameSettings.UpdateScale += UpdateScale;
        CashMachineReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        // Update Settings
        UpdateSettings();

        // Initialize Variables
        InSpin = false;
        isHighStake = false;
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
        CashMachineGameSettings.UpdateLayout -= UpdateLayout;
        CashMachineGameSettings.UpdateLayout -= UpdateLayout;
        CashMachineReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != CashMachineSlotType.Zero);
        //CachedEmptySymbol = settings.resourcesList.Find(r => r.type == CashMachineSlotType.Empty);

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
        this.resultMatrix = new CashMachineSlotType[reels.Count, 3];
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

        zeroOnReel2 = false;
        zeroOnReel3 = false;
        doubleZeroOnReel3 = false;

        if(currentSpinResult.isFreeSpin)
        {
            if(!isFreeGame)
            {
                isFreeGameReady = true;
            }
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        int reelIndex = 0;
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                var res = GetResourceById(symbol.id);

                if (reelIndex == 1)
                {
                    if (IsZero(res.Value.type)) zeroOnReel2 = true;
                }

                if (reelIndex == 2)
                {
                    if (IsZero(res.Value.type)) zeroOnReel3 = true;
                    if (IsDoubleZero(res.Value.type)) doubleZeroOnReel3 = true;
                }
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        CashMachineUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        CashMachinePaylineController.Instance.StopPaylineLoop();
        CashMachinePaylineController.Instance.ClearPaylineResults();
        if (!isFreeGame) CashMachineUIManager.Instance.UpdateWinAmount(0f);
        CashMachineUIManager.Instance.SetStopInteractable(false);

        if (!isFreeGame)
        {
            LockedReels.Clear();
        }
        CashMachineUIManager.Instance.winAnimationCompleted = true;
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
        CashMachineUIManager.Instance.PlaySpinMusic("Spin");
        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? CashMachineGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? CashMachineGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = CashMachineGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == CashMachineSpinType.All)
        {
            for(int i=0; i< reels.Count; i++)
            {
                if(!canReel2spin && i == 1)
                {
                    continue;
                }
                else if(canReel2spin) CashMachineUIManager.Instance.reel2SpinBg.SetActive(true);

                if (!canReel3spin  && i == 2)
                {
                    continue;
                }
                else if(canReel3spin) CashMachineUIManager.Instance.reel3SpinBg.SetActive(true);

                CashMachineUIManager.Instance.reel1SpinBg.SetActive(true);
                reels[i].ResetShape();
                if (isFreeGame)
                {
                    reels[i].ReverseDirection(true);
                }
                else
                {
                    reels[i].ReverseDirection(false);
                }
                reels[i].Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == CashMachineSpinType.Single)
        {
            //start spin the first reel
            reels[0].ResetShape();
            if (isFreeGame)
            {
                reels[0].ReverseDirection(true);
            }
            else
            {
                reels[0].ReverseDirection(false);
            }
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
        if (settings.spinSettings.endSpin == CashMachineSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == CashMachineSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !CashMachineAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            CashMachineUIManager.Instance.UpdateButtons("Stop");
            CashMachineUIManager.Instance.StopSpinMusic("Spin");
        }
    }
    public bool errorFreeSpin;
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
                errorFreeSpin = true;
                //DoubleJackpotBullseyeFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (CashMachineAutoSpinController.isAutoSpinning)
            {
                CashMachineUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                CashMachineUIManager.Instance.UpdateButtons("Stop");
            }
            CashMachineUIManager.Instance.StopSpinMusic("Spin");
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

        CashMachineUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
                break;

            if (!canReel2spin && i == 1)
            {
                continue;
            }
            else if(canReel2spin) CashMachineUIManager.Instance.reel2SpinBg.SetActive(false);

            if (!canReel3spin && i == 2)
            {
                continue;
            }
            else if(canReel3spin) CashMachineUIManager.Instance.reel3SpinBg.SetActive(false);

            CashMachineUIManager.Instance.reel1SpinBg.SetActive(false);

            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        ProcessSpinResult();
        CashMachineUIManager.Instance.StopSpinMusic("Spin");
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


        if (winAmount > 0)
        {
            if (!isFreeGame)
            {
                float betAmount = CashMachineUIManager.Instance.CurrentBet();

                if (winAmount >= (betAmount * 5000))
                {
                    CashMachineUIManager.Instance.PlayJackpotWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 500))
                {
                    CashMachineUIManager.Instance.PlaySuperWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 100))
                {
                    CashMachineUIManager.Instance.PlayMegaWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 50))
                {
                    CashMachineUIManager.Instance.PlayBigWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 10))
                {
                    CashMachineUIManager.Instance.PlayNiceWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else
                {
                    CashMachineUIManager.Instance.UpdateWinAmount(winAmount);
                }

            }
            else
            {
                CashMachineUIManager.Instance.StartCoroutine(CashMachineUIManager.Instance.AnimateValue(freeSpinWinAmount, (freeSpinWinAmount + winAmount), 0.7f, CashMachineUIManager.Instance.winAmount));
                freeSpinWinAmount += winAmount;
            }
            Invoke(nameof(UpdateGameCoin), 0.5f);
        }

        if (winAmount > 0)
        {
            Invoke("ShowPaylines", 0.5f);
        }
        else isPaylineCompleted = true;

        if (isFreeGameReady && !isFreeGame && !decoyFreeSpinBool)
        {
            decoyFreeSpinBool = true;
            CashMachineUIManager.Instance.UpdateButtons("enterfreeSpin");
            CashMachineUIManager.Instance.SetStopInteractable(false);
            if (!canReel3spin && canReel2spin)
            {
                canReel2spin = false;
                CashMachineUIManager.Instance.reel2LockedBg.SetActive(true);
            }
            else if (canReel2spin && canReel3spin)
            {
                if (zeroOnReel2 && !doubleZeroOnReel3 && !zeroOnReel3)
                {
                    canReel2spin = false;
                    CashMachineUIManager.Instance.reel2LockedBg.SetActive(true);
                }
                else if ((zeroOnReel2 && doubleZeroOnReel3) || (zeroOnReel2 && zeroOnReel3))
                {
                    canReel3spin = false;
                    canReel2spin = false;
                    CashMachineUIManager.Instance.reel2LockedBg.SetActive(true);
                    CashMachineUIManager.Instance.reel3LockedBg.SetActive(true);
                    freeSpinsDone++;
                }
                else if ((doubleZeroOnReel3 && !zeroOnReel2 && !zeroOnReel3) || (!doubleZeroOnReel3 && !zeroOnReel2 && zeroOnReel3))
                {
                    canReel3spin = false;
                    CashMachineUIManager.Instance.reel3LockedBg.SetActive(true);
                }
            }
            Invoke("StartFreeSpins", 0.5f);
        }
        else if (isFreeGame && decoyFreeSpinBool)
        {
            if (zeroOnReel2 && !doubleZeroOnReel3 && !zeroOnReel3)
            {
                CashMachineFreeSpinController.Instance.UpdateFreeSpins(1);
                decoyFreeSpinBool = false;
            }
            else if ((zeroOnReel3 && doubleZeroOnReel3) || (zeroOnReel2 && zeroOnReel3))
            {
                CashMachineFreeSpinController.Instance.UpdateFreeSpins(1);
                freeSpinsDone++;
                if (freeSpinsDone == 3)
                {
                    decoyFreeSpinBool = false;
                }
            }
            else if ((doubleZeroOnReel3 && !zeroOnReel2 && !zeroOnReel3) || (!doubleZeroOnReel3 && !zeroOnReel2 && zeroOnReel3))
            {
                CashMachineFreeSpinController.Instance.UpdateFreeSpins(1);
                decoyFreeSpinBool = false;
            }
        }


        InSpin = false;
        isSpinAgain = true;

        if (!CashMachineAutoSpinController.isAutoSpinning && !isFreeGame && CashMachineUIManager.Instance.winAnimationCompleted)
        {
            CashMachineUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            CashMachineUIManager.Instance.UpdateButtons("enterfreeSpin");
        }
    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private void ShowPaylines()
    {
        CashMachineUIManager.Instance.reel1PaylineBg.SetActive(true);
        if(canReel2spin) CashMachineUIManager.Instance.reel2PaylineBg.SetActive(true);
        if(canReel3spin) CashMachineUIManager.Instance.reel3PaylineBg.SetActive(true);

        CashMachinePaylineController.Instance.PlayPaylineSlots();
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

    public static CashMachineSlotResource? GetResourceById(string id)
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

    public bool IsZero(CashMachineSlotType slotType)
    {
        if (slotType == CashMachineSlotType.Zero)
        {
            return true;
        }
        else return false;
    }

    public bool IsDoubleZero(CashMachineSlotType slotType)
    {
        if (slotType == CashMachineSlotType.DoubleZero)
        {
            return true;
        }
        else return false;
    }

    private void StartFreeSpins()
    {
        CashMachineFreeGameTransitionController.Instance.StartFreeSpinTransition();
    }
    #endregion
}