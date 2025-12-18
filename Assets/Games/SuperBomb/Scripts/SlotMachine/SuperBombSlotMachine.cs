using Newtonsoft.Json;
using Sirenix.OdinInspector;
//using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SuperBombSlotScript;

public class SuperBombSlotMachine : BaseSlotMachine
{
    #region Variables

    public static SuperBombSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public SuperBombGameSettings settings;
    public List<SuperBombReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [HideInInspector] public bool hasFreeSpinTriggerViaWild;
    [HideInInspector] public int pendingFreeSpinCount;

    public bool[] lockedReels;
    public bool[] triggerReelsMask;
    private bool needMidFeatureHighlight = false;
    private int newlyAddedReelForHighlight = -1;
    private bool[] wasLockedLastSpin;
    [SerializeField] public bool testMode = false;
    [SerializeField] public bool testMode2 = false;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public SuperBombSlotType[,] resultMatrix;

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
    [HideInInspector] public bool InSpin;
    [HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isPaylineCompleted;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool _firstAutoSpin = true;
    private bool isSettingResult;

    // Free Spin Game
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool makeFreeGameReady = false;
    [HideInInspector] public bool isFreeSpinWhenNoPayline = false;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;

    // Coins Variables
    private float winAmount;

    // Cached Slot Types
    [HideInInspector] public List<SuperBombSlotResource> emptyList = new();
    [HideInInspector] public List<SuperBombSlotResource> cashList = new();
    [HideInInspector] public List<SuperBombSlotResource> freeSpinList = new();
    [HideInInspector] public List<SuperBombSlotResource> jackpotList = new();

    // Events
    public event Action StopReelProcess;

    //[SerializeField] public bool testMode = false;


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
        SuperBombGameSettings.UpdateLayout += UpdateLayout;
        SuperBombGameSettings.UpdateScale += UpdateScale;
        SuperBombReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        lockedReels = new bool[reels.Count];
        triggerReelsMask = new bool[reels.Count];
        wasLockedLastSpin = new bool[reels.Count];

        // Update Settings
        UpdateSettings();

        // Initialize Variables
        InSpin = false;
        //isFreeGameReady = false;
        //isFreeGame = false;

        InitializeResourceProbabilities();
    }

    private void Update()
    {
        if (!_isSingleSpin) return;

        if (_isSingleSpin && _timeCounter >= _delayAmongReel)
        {
            while (_reelIndex < reels.Count && isFreeGame && lockedReels[_reelIndex]) _reelIndex++;

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
        SuperBombGameSettings.UpdateLayout -= UpdateLayout;
        SuperBombGameSettings.UpdateScale -= UpdateScale;
        SuperBombReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        this.resultMatrix = new SuperBombSlotType[reels.Count, 3];
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

        bool[] hasWildOnReel = new bool[currentSpinResult.reels.Count];

        for (int x = 0; x < currentSpinResult.reels.Count; x++)
        {
            for (int y = 0; y < 3 && y < currentSpinResult.reels[x].Count && x < reels.Count; y++)
            {
                var sym = currentSpinResult.reels[x][y];

                if (string.Equals(sym.id, "Wild", StringComparison.OrdinalIgnoreCase))
                {
                    hasWildOnReel[x] = true;
                    break;

                }
            }
        }

        for (int i = 0; i < hasWildOnReel.Length; i++)
        {
            Debug.Log($"Deepak Reel {i + 1} has Wild: {hasWildOnReel[i]}");
        }

        Debug.Log("Deepak Chawla 2 has free spins: " + freeSpinCount);
        if (testMode)
        {
            freeSpinCount = 2;
        }
        else if (testMode2)
        {
            freeSpinCount = 3;
        }
        else
        {
            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        //freeSpinCount = currentSpinResult.freeSpinCount;
        Debug.Log("Deepak Chawla 3 has free spins: " + freeSpinCount);

        if (testMode)
        {
            if (hasWildOnReel[1])
            {
                hasWildOnReel[2] = true;
            }
            else if (hasWildOnReel[2])
            {
                hasWildOnReel[3] = true;
            }
            else if (hasWildOnReel[3])
            {
                hasWildOnReel[1] = true;
            }
        }

        if (testMode2)
        {
            if (hasWildOnReel[1])
            {
                hasWildOnReel[2] = true;
                hasWildOnReel[3] = true;
            }
            else if (hasWildOnReel[2])
            {
                hasWildOnReel[1] = true;
                hasWildOnReel[3] = true;
            }
            else if (hasWildOnReel[3])
            {
                hasWildOnReel[1] = true;
                hasWildOnReel[2] = true;
            }
        }

        bool trigger =
        (reels.Count > 1 && hasWildOnReel[1]) ||
        (reels.Count > 2 && hasWildOnReel[2]) ||
        (reels.Count > 3 && hasWildOnReel[3]);

        Debug.Log($"Deepak Free Spin Trigger via Wild: {trigger}");

        if (!isFreeGame)
        {
            if (trigger)
            {
                hasFreeSpinTriggerViaWild = true;
                for (int i = 0; i < triggerReelsMask.Length; i++)
                {
                    Debug.Log($"Deepak has trigger reel mask on reel {triggerReelsMask[i]}");
                }
                Array.Clear(triggerReelsMask, 0, triggerReelsMask.Length);
                for (int i = 0; i < triggerReelsMask.Length; i++)
                {
                    Debug.Log($"Deepak 1 has trigger reel mask on reel {triggerReelsMask[i]}");
                }
                if (reels.Count > 1 && hasWildOnReel[1]) { triggerReelsMask[1] = true; lockedReels[1] = true; }
                if (reels.Count > 2 && hasWildOnReel[2]) { triggerReelsMask[2] = true; lockedReels[2] = true; }
                if (reels.Count > 3 && hasWildOnReel[3]) { triggerReelsMask[3] = true; lockedReels[3] = true; }
                //isFreeGameReady = true;
                makeFreeGameReady = true;
                for (int i = 0; i < triggerReelsMask.Length; i++)
                {
                    Debug.Log($"Deepak 2 has trigger reel mask on reel {triggerReelsMask[i]}");
                }
            }
            else
            {
                hasFreeSpinTriggerViaWild = false;
                isFreeGameReady = false;
                Array.Clear(triggerReelsMask, 0, triggerReelsMask.Length);
                Array.Clear(lockedReels, 0, lockedReels.Length);
            }
        }
        else
        {
            bool addedAny = false;
            if (reels.Count > 1 && hasWildOnReel[1] && !lockedReels[1]) { triggerReelsMask[1] = true; lockedReels[1] = true; addedAny = true; }
            if (reels.Count > 2 && hasWildOnReel[2] && !lockedReels[2]) { triggerReelsMask[2] = true; lockedReels[2] = true; addedAny = true; }
            if (reels.Count > 3 && hasWildOnReel[3] && !lockedReels[3]) { triggerReelsMask[3] = true; lockedReels[3] = true; addedAny = true; }

            if (addedAny)
            {
                newlyAddedReelForHighlight =
                    lockedReels[1] && !wasLockedLastSpin[1] ? 2 :
                    lockedReels[2] && !wasLockedLastSpin[2] ? 3 :
                    4;

                needMidFeatureHighlight = true;
            }

            Array.Copy(lockedReels, wasLockedLastSpin, lockedReels.Length);
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
        if(makeFreeGameReady)
        {
            SuperBombUIManager.Instance.increaseBetButton.interactable = false;
            SuperBombUIManager.Instance.decreaseBetButton.interactable = false;
            SuperBombUIManager.Instance.maxBetButton.interactable = false;
        }

        SuperBombUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    private bool firstFreeSpin;

    public override void Spin()
    {
        if (InSpin) return;

        StopAllCoroutines();
        StopSlotAnimations();
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            SuperBombUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        SuperBombUIManager.Instance.comboEffect.SetActive(false);
        SuperBombUIManager.Instance.superComboEffect.SetActive(false);
        SuperBombPaylineController.Instance.dontShowComboVfxInSpin = true;
        SuperBombPaylineController.Instance.ClearPaylineData();
        winAmount = 0;
        freeSpinCount = 0;
        Debug.Log("Deepak Chawla 1 has free spins: " + freeSpinCount);
        isSettingResult = false;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isPaylineCompleted = false;
        isSlotAnimationCompleted = false;
        isSpinAgain = false;
        InSpin = true;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        ClearPaylines();
        SuperBombUIManager.Instance.winAnimationCompleted = true;
        SuperBombUIManager.Instance.SetStopInteractable(false);

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? SuperBombGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? SuperBombGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = SuperBombGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);
        SuperBombUIManager.Instance.PlaySpinMusic("ReelSpin");
        if (settings.spinSettings.startSpin == SuperBombSpinType.All)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isFreeGame && i < lockedReels.Length && lockedReels[i]) continue;
                reels[i].ResetShape();
                reels[i].Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == SuperBombSpinType.Single)
        {
            int startIdx = 0;
            while (startIdx < reels.Count && isFreeGame && lockedReels[startIdx]) startIdx++;
            if (startIdx < reels.Count)
            {
                reels[startIdx].ResetShape();
                reels[startIdx].Spin(_delayAmongReel, _acceleration, _speed);
            }
            _timeCounter = 0;
            _reelIndex = startIdx + 1;
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
        if (settings.spinSettings.endSpin == SuperBombSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == SuperBombSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !SuperBombAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            if (!makeFreeGameReady || !isFreeGameReady)
            {
                SuperBombUIManager.Instance.UpdateButtons("Single Stop");
            }
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

        SuperBombUIManager.Instance.SetStopInteractable(false);
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

        //StarBurstSlotsUIManager.Instance.SetStopInteractable(false);

        ProcessSpinResult();

        //InSpin = false;

        SuperBombUIManager.Instance.StopSpinMusic("ReelSpin");
    }
    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    //public bool forceddslot;
    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            Debug.LogWarning("❌ Spin result is invalid or failed.");
            return;
        }

        SuperBombUIManager.Instance.hasShowenComboVfx = false;
        SuperBombPaylineController.Instance.dontShowComboVfxInSpin = false;

        int c = 0;
        if(makeFreeGameReady)
        {
            isFreeGameReady = true;
            for (int i = 0; i < triggerReelsMask.Length; i++)
            {
                if (triggerReelsMask[i])
                {
                    c++;
                }
            }
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
            SuperBombUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = SuperBombUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if(currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
        {
            foreach(var payline in currentSpinResult.paylineWins)
            {
                SuperBombPaylineResult result = new SuperBombPaylineResult(payline.paylineIndex, payline.count, float.Parse(payline.winAmount), payline.IsLeft);
                SuperBombPaylineController.Instance.AddPaylineData(result);
            }

            if (isFreeGameReady)
            {
                StartCoroutine(SuperBombFreeGameTransitionController.Instance.ConvertRellsToWild(c));
                c = 0;
                Invoke(nameof(ShowPaylinesWrapper), 2.9f);
            }
            else
            {
                Invoke(nameof(ShowPaylinesWrapper), 0.5f);
            }
        }
        else
        {
            isPaylineCompleted = true;

            Debug.Log("Deepak Chawla 4 has free spins: " + freeSpinCount);


            if (isPaylineCompleted && freeSpinCount > 0 && isFreeGameReady)
            {
                if (SuperBombAutoSpinController.isAutoSpinning)
                {
                    isFreeSpinWhenNoPayline = true;
                }
                StartCoroutine(SuperBombFreeGameTransitionController.Instance.ConvertRellsToWild(c));
                c = 0;
                Invoke(nameof(SetSlotAnimationCompleted), 3f);
            }
        }

        InSpin = false;
        isSpinAgain = true;

        //if (!StarBurstSlotsAutoSpinController.isAutoSpinning && !isFreeGame && freeSpinCount == 0)
        //{
        //    StarBurstSlotsUIManager.Instance.UpdateButtons("Single Stop");
        //}

        if (freeSpinCount > 0 && !isFreeGame)
        {
            SuperBombUIManager.Instance.UpdateButtons("Free Game Transition");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    private void ShowPaylinesWrapper()
    {
        var plc = SuperBombPaylineController.Instance;
        if (plc != null) plc.ShowCollectedPaylines();
        else isPaylineCompleted = true;
    }
    #endregion

    #region Slot Animation

    public float isSlotAnimationCompletedDelay;

    private void StopSlotAnimations()
    {
        foreach (var reel in reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.HidePaylineWin();
                }
            }
        }
    }

    public void SetSlotAnimationCompleted()
    {
        isSlotAnimationCompleted = true;
        makeFreeGameReady = false;

        if (hasFreeSpinTriggerViaWild && freeSpinCount > 0 && !isFreeGame)
        {
            Debug.Log("Deepak Chawla 5 has free spins: " + freeSpinCount);

            SuperBombUIManager.Instance.UpdateButtons("Free Game Transition");
            SuperBombFreeGameTransitionController.Instance.UpdateFreeSpinsCount(freeSpinCount);
            SuperBombFreeGameTransitionController.Instance.StartFreeSpinTransition();
            return;
        }
        else if (freeSpinCount > 0)
        {
            Debug.Log("Deepak Chawla 0 has free spins: " + freeSpinCount);
            SuperBombFreeGameTransitionController.Instance.UpdateFreeSpinsCount(freeSpinCount);
        }

        if (isFreeGame && needMidFeatureHighlight && newlyAddedReelForHighlight != -1 && freeSpinCount > 0)
        {
            SuperBombFreeGameTransitionController.Instance.HighlightNewWildReel(newlyAddedReelForHighlight);
            Debug.Log("Deepak Chawla 6 has free spins: " + freeSpinCount);
            SuperBombFreeGameTransitionController.Instance.UpdateFreeSpinsCount(freeSpinCount);

            needMidFeatureHighlight = false;
            newlyAddedReelForHighlight = -1;
            return;
        }
    }

    #endregion

    #region Cleanup

    public override void ClearPaylines() { }

    #endregion

    #region Helper Functions

    private void InitializeResourceProbabilities()
    {
        emptyList.Clear();
        cashList.Clear();
        freeSpinList.Clear();
        jackpotList.Clear();
    }

    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();
    }

    public static SuperBombSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            Debug.LogWarning("Settings or resourcesList is null.");
            return null;
        }

        switch (id)
        {
            case "$1":
                id = "Dollar1";
                break;
            case "$2":
                id = "Dollar2";
                break;
            case "$5":
                id = "Dollar5";
                break;
            case "$10":
                id = "Dollar10";
                break;
            case "$20":
                id = "Dollar20";
                break;
            case "$50":
                id = "Dollar50";
                break;
            case "$100":
                id = "Dollar100";
                break;
            case "$500":
                id = "Dollar500";
                break;
            case "$1000":
                id = "Dollar1000";
                break;
            default:
                break;
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
        return winAmount;
    }

    public void ResetFreeGameLocks()
    {
        for (int i = 0; i < lockedReels.Length; i++) lockedReels[i] = false;
        for (int i = 0; i < triggerReelsMask.Length; i++) triggerReelsMask[i] = false;
        hasFreeSpinTriggerViaWild = false;
        pendingFreeSpinCount = 0;
    }
    #endregion
}
/* all that a human carry is eitehr hatred or affection */
