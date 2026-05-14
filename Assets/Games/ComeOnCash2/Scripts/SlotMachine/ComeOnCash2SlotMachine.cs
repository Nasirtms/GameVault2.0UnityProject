using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SlotMachine2D;

public class ComeOnCash2SlotMachine : BaseSlotMachine
{
    #region Variables

    public static ComeOnCash2SlotMachine Instance;
    public List<int> lockedReels = new List<int>();

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public ComeOnCash2GameSettings settings;
    public List<ComeOnCash2ReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public ComeOnCash2SlotType[,] resultMatrix;

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

    public bool canReel1spin = false;
    public bool canReel2spin = false;
    public bool canReel3spin = false;
    public bool isHighStake = false;

    // Coins Variables
    private float winAmount;
    public Coroutine AnimateToValueCoroutine;

    // Events
    public event Action StopReelProcess;

    // 3 Reels Slot
    public static List<ComeOnCash2SlotResource> CachedRealSymbols { get; private set; }
    //public static CashMachineSlotResource? CachedEmptySymbol { get; private set; }

    #region SlotBools
    public bool zeroOnReel2;
    public bool zeroOnReel3;
    public bool doubleZeroOnReel3;
    public bool diamondOnReel1;
    public bool diamondOnReel2;
    public bool diamondOnReel3;
    private int freeSpinsDone = 0;
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
        ComeOnCash2GameSettings.UpdateLayout += UpdateLayout;
        ComeOnCash2GameSettings.UpdateScale += UpdateScale;
        ComeOnCash2ReelScript.OnSpinComplete += OnReelSpinComplete;
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
        ComeOnCash2GameSettings.UpdateLayout -= UpdateLayout;
        ComeOnCash2GameSettings.UpdateLayout -= UpdateLayout;
        ComeOnCash2ReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != ComeOnCash2SlotType.Zero);
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
        this.resultMatrix = new ComeOnCash2SlotType[reels.Count, 3];
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
        diamondOnReel1 = false;
        diamondOnReel2 = false;
        diamondOnReel3 = false;

        if (currentSpinResult.isFreeSpin)
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

                if (reelIndex == 0)
                {
                    if (IsDiamond(res.Value.type)) diamondOnReel1 = true;
                }

                if (reelIndex == 1)
                {
                    if (IsZero(res.Value.type)) zeroOnReel2 = true;
                    if (IsDiamond(res.Value.type)) diamondOnReel2 = true;
                }

                if (reelIndex == 2)
                {
                    if (IsZero(res.Value.type)) zeroOnReel3 = true;
                    if (IsDoubleZero(res.Value.type)) doubleZeroOnReel3 = true;
                    if (IsDiamond(res.Value.type)) diamondOnReel3 = true;
                }
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        ComeOnCash2UIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        ComeOnCash2PaylineController.Instance.StopPaylineLoop();
        ComeOnCash2PaylineController.Instance.ClearPaylineResults();
        if (!isFreeGame) ComeOnCash2UIManager.Instance.UpdateWinAmount(0f);
        ComeOnCash2UIManager.Instance.SetStopInteractable(false);

        if(!isFreeGame)
        {
            lockedReels.Clear();
        }

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
            ? ComeOnCash2GameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? ComeOnCash2GameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = ComeOnCash2GameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);


        if (settings.spinSettings.startSpin == ComeOnCash2SpinType.All)
        {
            for(int i=0; i< reels.Count; i++)
            {
                if(!canReel1spin && i == 1)
                {
                    continue;
                }
                else if(canReel1spin) ComeOnCash2UIManager.Instance.reel1SpinBg.SetActive(true);

                if(!canReel2spin && i == 1)
                {
                    continue;
                }
                else if(canReel2spin) ComeOnCash2UIManager.Instance.reel2SpinBg.SetActive(true);

                if (!canReel3spin  && i == 2)
                {
                    continue;
                }
                else if(canReel3spin) ComeOnCash2UIManager.Instance.reel3SpinBg.SetActive(true);

                ComeOnCash2UIManager.Instance.reel1SpinBg.SetActive(true);
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
        else if (settings.spinSettings.startSpin == ComeOnCash2SpinType.Single)
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
        if (settings.spinSettings.endSpin == ComeOnCash2SpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == ComeOnCash2SpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !ComeOnCash2AutoSpinController.isAutoSpinning && !isFreeGame)
        {
            ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
            ComeOnCash2UIManager.Instance.StopSpinMusic("Spin");
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
            else if (ComeOnCash2AutoSpinController.isAutoSpinning)
            {
                ComeOnCash2UIManager.Instance.CancelAutoSpin();
            }
            else
            {
                ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
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

        ComeOnCash2UIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
                break;

            if (!canReel1spin && i == 0)
            {
                continue;
            }
            else if(canReel1spin) ComeOnCash2UIManager.Instance.reel1SpinBg.SetActive(false);

            if (!canReel2spin && i == 1)
            {
                continue;
            }
            else if(canReel2spin) ComeOnCash2UIManager.Instance.reel2SpinBg.SetActive(false);

            if (!canReel3spin && i == 2)
            {
                continue;
            }
            else if(canReel3spin) ComeOnCash2UIManager.Instance.reel3SpinBg.SetActive(false);

            ComeOnCash2UIManager.Instance.reel1SpinBg.SetActive(false);

            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
            //reels[i].ApplyFinalResult(i);
            //reels[i].Stop();
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        ProcessSpinResult();
        ComeOnCash2UIManager.Instance.StopSpinMusic("Spin");
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
                float betAmount = ComeOnCash2UIManager.Instance.CurrentBet();

                if (winAmount >= (betAmount * 5000))
                {
                    ComeOnCash2UIManager.Instance.PlayJackpotWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 500))
                {
                    ComeOnCash2UIManager.Instance.PlaySuperWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 100))
                {
                    ComeOnCash2UIManager.Instance.PlayMegaWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 50))
                {
                    ComeOnCash2UIManager.Instance.PlayBigWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 10))
                {
                    ComeOnCash2UIManager.Instance.PlayNiceWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else
                {
                    ComeOnCash2UIManager.Instance.UpdateWinAmount(winAmount);
                }

            }
            else
            {
                ComeOnCash2UIManager.Instance.StartCoroutine(ComeOnCash2UIManager.Instance.AnimateValue(freeSpinWinAmount, (freeSpinWinAmount + winAmount), 0.7f, ComeOnCash2UIManager.Instance.winAmount));
                freeSpinWinAmount += winAmount;
            }
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (winAmount > 0)
        {
            Invoke("ShowPaylines", 0.5f);
        }
        else isPaylineCompleted = true;

        if (isFreeGameReady && !isFreeGame && !decoyFreeSpinBool)
        {
            decoyFreeSpinBool = true;
            ComeOnCash2UIManager.Instance.UpdateButtons("enterfreeSpin");
            ComeOnCash2UIManager.Instance.SetStopInteractable(false);

            if(canReel2spin && canReel3spin)
            {
                if((zeroOnReel2 && (zeroOnReel3 || doubleZeroOnReel3)) || (diamondOnReel2 && diamondOnReel3))
                {
                    canReel2spin = false;
                    canReel3spin = false;
                    ComeOnCash2UIManager.Instance.reel2LockedBg.SetActive(true);
                    ComeOnCash2UIManager.Instance.reel3LockedBg.SetActive(true);
                }
            }
            else if(canReel1spin && canReel3spin)
            {
                if(diamondOnReel1 && diamondOnReel3)
                {
                    canReel1spin = false;
                    canReel3spin = false;
                    ComeOnCash2UIManager.Instance.reel1LockedBg.SetActive(true);
                    ComeOnCash2UIManager.Instance.reel3LockedBg.SetActive(true);
                }
            }
            else if (canReel1spin && canReel2spin)
            {
                if (diamondOnReel1 && diamondOnReel2)
                {
                    canReel1spin = false;
                    canReel2spin = false;
                    ComeOnCash2UIManager.Instance.reel1LockedBg.SetActive(true);
                    ComeOnCash2UIManager.Instance.reel2LockedBg.SetActive(true);
                }
            }
            else if (canReel2spin)
            {
                if (zeroOnReel2)
                {
                    canReel2spin = false;
                    ComeOnCash2UIManager.Instance.reel2LockedBg.SetActive(true);
                }
            }
            else if (canReel3spin)
            {
                if (zeroOnReel3 || doubleZeroOnReel3)
                {
                    canReel3spin = false;
                    ComeOnCash2UIManager.Instance.reel3LockedBg.SetActive(true);
                }
            }

            Invoke("StartFreeSpins", 0.5f);
        }
        //else if (isFreeGame && decoyFreeSpinBool)
        //{
        //}


        InSpin = false;
        isSpinAgain = true;

        if (!ComeOnCash2AutoSpinController.isAutoSpinning && !isFreeGame && ComeOnCash2UIManager.Instance.winAnimationCompleted)
        {
            ComeOnCash2UIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            ComeOnCash2UIManager.Instance.UpdateButtons("enterfreeSpin");
        }
    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private void ShowPaylines()
    {
        ComeOnCash2UIManager.Instance.reel1PaylineBg.SetActive(true);
        if(canReel2spin) ComeOnCash2UIManager.Instance.reel2PaylineBg.SetActive(true);
        if(canReel3spin) ComeOnCash2UIManager.Instance.reel3PaylineBg.SetActive(true);

        ComeOnCash2PaylineController.Instance.PlayPaylineSlots();
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

    public static ComeOnCash2SlotResource? GetResourceById(string id)
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

    public bool IsZero(ComeOnCash2SlotType slotType)
    {
        if (slotType == ComeOnCash2SlotType.Zero)
        {
            return true;
        }
        else return false;
    }

    public bool IsDoubleZero(ComeOnCash2SlotType slotType)
    {
        if (slotType == ComeOnCash2SlotType.DoubleZero)
        {
            return true;
        }
        else return false;
    }

    public bool IsDiamond(ComeOnCash2SlotType slotType)
    {
        if (slotType == ComeOnCash2SlotType.Diamond)
        {
            return true;
        }
        else return false;
    }

    private void StartFreeSpins()
    {
        ComeOnCash2FreeGameTransitionController.Instance.StartFreeSpinTransition();
    }
    #endregion
}