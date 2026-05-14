using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SlotMachine2D;

public class ComeOnCashSlotMachine : BaseSlotMachine
{
    #region Variables

    public static ComeOnCashSlotMachine Instance;
    public List<int> LockedReels = new List<int>();
    public List<int> cashValues = new List<int>();
    public List<int> cashIndexes = new List<int>();

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public ComeOnCashGameSettings settings;
    public List<ComeOnCashReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    [SerializeField] private List<int> cv = new List<int> { 2, 2, 4, 5, 6, 8, 4, 2 };
    [SerializeField] private List<int> ci = new List<int> { 2, 4 };

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public ComeOnCashSlotType[,] resultMatrix;

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
    [HideInInspector] public int picksCount = 0;
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
    public static List<ComeOnCashSlotResource> CachedRealSymbols { get; private set; }
    //public static CashMachineSlotResource? CachedEmptySymbol { get; private set; }

    #region SlotBools
    public bool oneOnReel1;
    public bool oneOnReel2;
    public bool twoOnReel1;
    public bool twoOnReel2;
    public bool fiveOnReel1;
    public bool fiveOnReel2;
    public bool tenOnReel1;
    public bool tenOnReel2;
    public bool zeroOnReel2;
    public bool twoXOnReel3;
    public bool threeXOnReel3;
    public bool fiveXOnReel3;
    public bool twoPicksOnReel3;
    public bool threePicksOnReel3;
    public bool fourPicksOnReel3;
    private int freeSpinsDone = 0;
    public bool decoyFreeSpinBool = false;
    public bool isBonusGame = false;
    public bool isTakeOffer = false;
    public bool isBonusGameReady = false;
    public bool testBonusGame = false;
    public bool isBonusGameEnding = false;
    public bool isBonusRetryRequest = false;
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
        ComeOnCashGameSettings.UpdateLayout += UpdateLayout;
        ComeOnCashGameSettings.UpdateScale += UpdateScale;
        ComeOnCashReelScript.OnSpinComplete += OnReelSpinComplete;
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
        ComeOnCashGameSettings.UpdateLayout -= UpdateLayout;
        ComeOnCashGameSettings.UpdateLayout -= UpdateLayout;
        ComeOnCashReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != ComeOnCashSlotType.Zero);
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
        this.resultMatrix = new ComeOnCashSlotType[reels.Count, 3];
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

        oneOnReel1 = false;
        oneOnReel2 = false;
        twoOnReel1 = false;
        twoOnReel2 = false;
        fiveOnReel1 = false;
        fiveOnReel2 = false;
        tenOnReel1 = false;
        tenOnReel2 = false;
        zeroOnReel2 = false;
        twoXOnReel3 = false;
        threeXOnReel3 = false;
        fiveXOnReel3 = false;
        twoPicksOnReel3 = false;
        threePicksOnReel3 = false;
        fourPicksOnReel3 = false;

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
            }
        }

        cashIndexes.Clear();
        cashValues.Clear();
        //foreach (var index in currentSpinResult.cashIndex)
        //{
        //    cashIndexes.Add(index);
        //}
        //foreach (var value in currentSpinResult.cash)
        //{
        //    cashValues.Add(value);
        //}

        if(testBonusGame)
        {
            isBonusGameReady = true;
            foreach (var index in ci)
            {
                cashIndexes.Add(index);
            }
            foreach (var value in cv)
            {
                cashValues.Add(value);
            }
        }

        if (currentSpinResult.bonusTriggered)
        {
            isBonusGameReady = true;
            foreach (var index in currentSpinResult.cashIndex)
            {
                cashIndexes.Add(index);
            }
            foreach (var value in currentSpinResult.cashValue)
            {
                cashValues.Add(value);
            }
        }

        if(isBonusGame)
        {
            picksCount = currentSpinResult.cashIndex.Count;
            foreach (var index in currentSpinResult.cashIndex)
            {
                cashIndexes.Add(index);
            }
            foreach (var value in currentSpinResult.cashValue)
            {
                cashValues.Add(value);
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
                    if (IsOne(res.Value.type)) oneOnReel1 = true;
                    if (IsTwo(res.Value.type)) twoOnReel1 = true;
                    if (IsFive(res.Value.type)) fiveOnReel1 = true;
                    if (IsTen(res.Value.type)) tenOnReel1 = true;
                }

                if (reelIndex == 1)
                {
                    if (IsOne(res.Value.type)) oneOnReel2 = true;
                    if (IsTwo(res.Value.type)) twoOnReel2 = true;
                    if (IsFive(res.Value.type)) fiveOnReel2 = true;
                    if (IsTen(res.Value.type)) tenOnReel2 = true;
                    if (IsZero(res.Value.type)) zeroOnReel2 = true;
                }

                if (reelIndex == 2)
                {
                    if (IsTwoX(res.Value.type)) twoXOnReel3 = true;
                    if (IsThreeX(res.Value.type)) threeXOnReel3 = true;
                    if (IsFiveX(res.Value.type)) fiveXOnReel3 = true;
                    if (IsTwoPicks(res.Value.type)) twoPicksOnReel3 = true;
                    if (IsThreePicks(res.Value.type)) threePicksOnReel3 = true;
                    if (IsFourPicks(res.Value.type)) fourPicksOnReel3 = true;
                }
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        ComeOnCashUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (!isBonusGame)
        {
            if (InSpin) return;

            StopAllCoroutines();
            ComeOnCashPaylineController.Instance.StopPaylineLoop();
            ComeOnCashPaylineController.Instance.ClearPaylineResults();
            if (!isFreeGame) ComeOnCashUIManager.Instance.UpdateWinAmount(0f);
            ComeOnCashUIManager.Instance.SetStopInteractable(false);
            ComeOnCashUIManager.Instance.winAnimationCompleted = true;

            if (!isFreeGame)
            {
                canReel1spin = true;
                LockedReels.Clear();
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
                ? ComeOnCashGameExtension.GetRandomValue(settings.spinSettings.acceleration)
                : 0f;

            _speed = settings.spinSettings.useSameSpeed
                ? ComeOnCashGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
                : 0f;

            _delayAmongReel = ComeOnCashGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);


            if (settings.spinSettings.startSpin == ComeOnCashSpinType.All)
            {
                for (int i = 0; i < reels.Count; i++)
                {
                    if (!canReel1spin && i == 0)
                    {
                        continue;
                    }
                    else if (canReel1spin) ComeOnCashUIManager.Instance.reel1SpinBg.SetActive(true);

                    if (!canReel2spin && i == 1)
                    {
                        continue;
                    }
                    else if (canReel2spin) ComeOnCashUIManager.Instance.reel2SpinBg.SetActive(true);

                    if (!canReel3spin && i == 2)
                    {
                        continue;
                    }
                    else if (canReel3spin) ComeOnCashUIManager.Instance.reel3SpinBg.SetActive(true);

                    ComeOnCashUIManager.Instance.reel1SpinBg.SetActive(true);
                    reels[i].ResetShape();
                    reels[i].Spin(_delayAmongReel, _acceleration, _speed);
                }
            }
            else if (settings.spinSettings.startSpin == ComeOnCashSpinType.Single)
            {
                //start spin the first reel
                reels[0].ResetShape();
                reels[0].Spin(_delayAmongReel, _acceleration, _speed);

                //init delay variables

                _timeCounter = 0;
                _reelIndex = 1;
                _isSingleSpin = true;
            }
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
        if (settings.spinSettings.endSpin == ComeOnCashSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == ComeOnCashSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !ComeOnCashAutoSpinController.isAutoSpinning && !isFreeGame && !isBonusGame)
        {
            ComeOnCashUIManager.Instance.UpdateButtons("Stop");
            ComeOnCashUIManager.Instance.StopSpinMusic("Spin");
        }
    }
    public bool errorFreeSpin;
    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 12f;
        float elapsed = 0f;

        if (!isBonusGame)
        {
            while ((currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
            {
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
                StopWithResult();

                if (isFreeGame) errorFreeSpin = true;
                else if (ComeOnCashAutoSpinController.isAutoSpinning) ComeOnCashUIManager.Instance.CancelAutoSpin();
                else ComeOnCashUIManager.Instance.UpdateButtons("Stop");

                isSpinAgain = true;
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
            StopWithResult();
            yield break;
        }

        // BONUS: retry branch
        if (isBonusGame && !isTakeOffer)
        {
            elapsed = 0f;
            while (!(cashValues.Count == 11 && cashIndexes.Count == picksCount) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!(cashValues.Count == 11 && cashIndexes.Count == picksCount))
            {
                HandleBonusNetworkError(false);
                yield break;
            }

            StopWithResult();
            yield break;
        }

        // BONUS: take offer branch
        if (isBonusGame && isTakeOffer)
        {
            elapsed = 0f;
            while ((currentSpinResult == null || !currentSpinResult.success) && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (currentSpinResult == null || !currentSpinResult.success)
            {
                HandleBonusNetworkError(true);
                yield break;
            }

            isBonusGameEnding = true;
            StopWithResult();

        }
    }

    public void StopWithResult() => Stop();

    public void Stop()
    {
        if (!isBonusGame)
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
        }
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

        ComeOnCashUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        if (!isBonusGame)
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
                else if (canReel1spin) ComeOnCashUIManager.Instance.reel1SpinBg.SetActive(false);

                if (!canReel2spin && i == 1)
                {
                    continue;
                }
                else if (canReel2spin) ComeOnCashUIManager.Instance.reel2SpinBg.SetActive(false);

                if (!canReel3spin && i == 2)
                {
                    continue;
                }
                else if (canReel3spin) ComeOnCashUIManager.Instance.reel3SpinBg.SetActive(false);

                ComeOnCashUIManager.Instance.reel1SpinBg.SetActive(false);

                yield return new WaitForSeconds(_delayAmongReel);

                reels[i].canStopReel = true;
                //reels[i].ApplyFinalResult(i);
                //reels[i].Stop();
            }

            if (isStopBtnPressed)
                StopButtonPressed();
        }

        ProcessSpinResult();
        ComeOnCashUIManager.Instance.StopSpinMusic("Spin");
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
            Debug.Log("is take offer Amount: 2 " + isBonusGame + ", isTakeOffer: " + isTakeOffer + ", winAmount: " + winAmount);

        if ((winAmount > 0 && !isBonusGame) || (isBonusGame && isTakeOffer && winAmount > 0))
        {
            Debug.Log("is take offer Amount: 1 " + isBonusGame + ", isTakeOffer: " + isTakeOffer + ", winAmount: " + winAmount);
            if (!isFreeGame)
            {
                float betAmount = ComeOnCashUIManager.Instance.CurrentBet();

                if (winAmount >= (betAmount * 5000))
                {
                    ComeOnCashUIManager.Instance.PlayJackpotWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 500))
                {
                    ComeOnCashUIManager.Instance.PlaySuperWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 100))
                {
                    ComeOnCashUIManager.Instance.PlayMegaWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 50))
                {
                    ComeOnCashUIManager.Instance.PlayBigWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else if (winAmount >= (betAmount * 10))
                {
                    ComeOnCashUIManager.Instance.PlayNiceWinAnimation(winAmount);
                    playingwinanimation = true;
                }
                else
                {
                    ComeOnCashUIManager.Instance.UpdateWinAmount(winAmount);
                }

            }
            else
            {
                ComeOnCashUIManager.Instance.StartCoroutine(ComeOnCashUIManager.Instance.AnimateValue(freeSpinWinAmount, (freeSpinWinAmount + winAmount), 0.7f, ComeOnCashUIManager.Instance.winAmount));
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
            ComeOnCashUIManager.Instance.UpdateButtons("enterfreeSpin");
            ComeOnCashUIManager.Instance.SetStopInteractable(false);
            if (canReel3spin)
            {
                if(twoXOnReel3 || threeXOnReel3 || fiveXOnReel3)
                {
                    canReel3spin = false;
                    ComeOnCashUIManager.Instance.reel3LockedBg.SetActive(true);
                }
            }

            if (canReel1spin && canReel2spin)
            {
                if ((oneOnReel1 || twoOnReel1 || fiveOnReel1 || tenOnReel1) && (oneOnReel2 || twoOnReel2 || fiveOnReel2 || tenOnReel2))
                {
                    canReel1spin = true;
                    canReel2spin = true;
                }
                else if (oneOnReel1 || twoOnReel1 || fiveOnReel1 || tenOnReel1)
                {
                    if (!zeroOnReel2)
                    {
                        canReel1spin = false;
                        canReel2spin = true;
                        ComeOnCashUIManager.Instance.reel1LockedBg.SetActive(true);
                    }
                    else
                    {
                        canReel1spin = true;
                        canReel2spin = true;
                    }
                }
                else if (oneOnReel2 || twoOnReel2 || fiveOnReel2 || tenOnReel2)
                {
                    canReel2spin = false;
                    canReel1spin = true;
                    ComeOnCashUIManager.Instance.reel2LockedBg.SetActive(true);
                }
            }

            Invoke("StartFreeSpins", 0.5f);
        }

        if (isBonusGameReady)
        {
            if (twoPicksOnReel3 || threePicksOnReel3 || fourPicksOnReel3)
            {
                //Debug.Log("deepak callled processing");
                Invoke("StartFreeSpins", 0.5f);
            }
        }
        else if (isBonusGame && !isTakeOffer)
        {
            if (isBonusRetryRequest)
            {
                ComeOnCashFreeSpinController.Instance.ConsumeOffer();
                isBonusRetryRequest = false;
            }

            ComeOnCashFreeSpinController.Instance.startBonusGame(cashIndexes.ToArray(), 4f);
            InSpin = false;
        }
        else if (isBonusGame && isTakeOffer)
        {
            Debug.Log("deepak callled processing take offer");
            ComeOnCashFreeSpinController.Instance.EndBonus();
        }

        if (!isTakeOffer)
        {
            InSpin = false;
        }

        isSpinAgain = true;

        if (!ComeOnCashAutoSpinController.isAutoSpinning && !isFreeGame && ComeOnCashUIManager.Instance.winAnimationCompleted && !isBonusGame)
        {
            ComeOnCashUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            ComeOnCashUIManager.Instance.UpdateButtons("enterfreeSpin");
        }
    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private void ShowPaylines()
    {
        ComeOnCashUIManager.Instance.reel1PaylineBg.SetActive(true);
        if(canReel2spin) ComeOnCashUIManager.Instance.reel2PaylineBg.SetActive(true);
        if(canReel3spin) ComeOnCashUIManager.Instance.reel3PaylineBg.SetActive(true);

        ComeOnCashPaylineController.Instance.PlayPaylineSlots();
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

    public static ComeOnCashSlotResource? GetResourceById(string id)
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
        if (currentSpinResult != null)
        {
            return currentSpinResult.totalWin;
        }
        else return 0f;
    }

    public bool IsZero(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Zero)
        {
            return true;
        }
        else return false;
    }

    public bool IsOne(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.One)
        {
            return true;
        }
        else return false;
    }
    public bool IsTwo(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Two)
        {
            return true;
        }
        else return false;
    }
    public bool IsFive(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Five)
        {
            return true;
        }
        else return false;
    }

    public bool IsTen(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Ten)
        {
            return true;
        }
        else return false;
    }

    public bool IsTwoX(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.TwoX)
        {
            return true;
        }
        else return false;
    }
    public bool IsThreeX(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.ThreeX)
        {
            return true;
        }
        else return false;
    }
    public bool IsFiveX(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.FiveX)
        {
            return true;
        }
        else return false;
    }
    public bool IsFourPicks(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Bonus4Picks)
        {
            return true;
        }
        else return false;
    }
    public bool IsTwoPicks(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Bonus2Picks)
        {
            return true;
        }
        else return false;
    }
    public bool IsThreePicks(ComeOnCashSlotType slotType)
    {
        if (slotType == ComeOnCashSlotType.Bonus3Picks)
        {
            return true;
        }
        else return false;
    }

    private void StartFreeSpins()
    {
        int[] notes = cashValues.ToArray();
        int[] noteIndexes = cashIndexes.ToArray();
        if (!isBonusGameReady)
        {
            ComeOnCashFreeGameTransitionController.Instance.StartFreeSpinTransition();
        }
        else
        {
            ComeOnCashFreeGameTransitionController.Instance.StartBonusGameTransition(notes, noteIndexes);
        }
    }

    private void HandleBonusNetworkError(bool wasTakeOffer)
    {
        CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");

        InSpin = false;
        isSpinAgain = true;

        isBonusGameReady = false;
        isTakeOffer = false;
        isBonusGameEnding = false;
        isBonusRetryRequest = false;

        ComeOnCashFreeGameTransitionController.Instance.StopGlowOnCash();
        ComeOnCashFreeSpinController.Instance.RestoreBonusButtonsAfterError();
    }
    #endregion
}