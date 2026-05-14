using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TheGreenMachineDeluxeSlotMachine : BaseSlotMachine
{
    #region Variables

    public static TheGreenMachineDeluxeSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public TheGreenMachineDeluxeGameSettings settings;
    public List<TheGreenMachineDeluxeReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Slot Probability")]
    public float emptyWeight = 50f;
    public float cashWeight = 30f;
    public float freeSpinWeight = 15f;
    public float jackpotWeight = 5f;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public TheGreenMachineDeluxeSlotType[,] resultMatrix;

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
    //[HideInInspector] public bool InSpin;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool _firstAutoSpin = true;
    private bool isSettingResult;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;

    // Coins Variables
    private float winAmount;

    // Cached Slot Types
    [HideInInspector] public List<TheGreenMachineDeluxeSlotResource> emptyList = new();
    [HideInInspector] public List<TheGreenMachineDeluxeSlotResource> cashList = new();
    [HideInInspector] public List<TheGreenMachineDeluxeSlotResource> freeSpinList = new();
    [HideInInspector] public List<TheGreenMachineDeluxeSlotResource> jackpotList = new();

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
        TheGreenMachineDeluxeGameSettings.UpdateLayout += UpdateLayout;
        TheGreenMachineDeluxeGameSettings.UpdateScale += UpdateScale;
        TheGreenMachineDeluxeReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        // Update Settings
        UpdateSettings();
        InSpin = false;

        InitializeResourceProbabilities();
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
        TheGreenMachineDeluxeGameSettings.UpdateLayout -= UpdateLayout;
        TheGreenMachineDeluxeGameSettings.UpdateScale -= UpdateScale;
        TheGreenMachineDeluxeReelScript.OnSpinComplete -= OnReelSpinComplete;

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
        this.resultMatrix = new TheGreenMachineDeluxeSlotType[reels.Count, 3];
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
    public int fakefreespins;
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

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        if (fakefreespins > 0)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = fakefreespins;
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

        TheGreenMachineDeluxeUIManager.Instance.SetStopInteractable(true);
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
            TheGreenMachineDeluxeUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
        }

        if (TheGreenMachineDeluxeUIManager.Instance.winCoroutine != null)
        {
            StopCoroutine(TheGreenMachineDeluxeUIManager.Instance.winCoroutine);
        }
        TheGreenMachineDeluxeUIManager.Instance.PlaySpinMusic("ReelSpin");

        // Reset Variables and Functions State
        winAmount = 0;
        freeSpinCount = 0;
        isSettingResult = false;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isSlotAnimationCompleted = false;
        isSpinAgain = false;
        InSpin = true;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        ClearPaylines();
        TheGreenMachineDeluxeUIManager.Instance.winAnimationCompleted = true;
        TheGreenMachineDeluxeUIManager.Instance.SetStopInteractable(false);

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? TheGreenMachineDeluxeGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? TheGreenMachineDeluxeGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = TheGreenMachineDeluxeGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == TheGreenMachineDeluxeSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == TheGreenMachineDeluxeSpinType.Single)
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
        if (settings.spinSettings.endSpin == TheGreenMachineDeluxeSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
        }
        else if (settings.spinSettings.endSpin == TheGreenMachineDeluxeSpinType.All)
        {
            InSpin = false;
        }

        if (index == reels.Count - 1 && !TheGreenMachineDeluxeAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Single Stop");
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
            if (isFreeGame)
            {
                TheGreenMachineDeluxeFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (TheGreenMachineDeluxeAutoSpinController.isAutoSpinning)
            {
                TheGreenMachineDeluxeUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Single Stop");
            }
            TheGreenMachineDeluxeUIManager.Instance.StopSpinMusic("ReelSpin");
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

        TheGreenMachineDeluxeUIManager.Instance.SetStopInteractable(false);
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

        TheGreenMachineDeluxeUIManager.Instance.SetStopInteractable(false);
        TheGreenMachineDeluxeUIManager.Instance.StopSpinMusic("ReelSpin");
        ProcessSpinResult();

    }
    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    //public bool forceddslot;
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
            TheGreenMachineDeluxeUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = TheGreenMachineDeluxeUIManager.Instance.CurrentBet();
            TheGreenMachineDeluxeUIManager.Instance.UpdateWinAmount(winAmount, false);
            UpdateGameCoin();
        }

        if (winAmount > 0)
        {
            Invoke("PlaySlotAnimations", 0.5f);
        }
        else
        {

            SetSlotAnimationCompleted();
        }

        InSpin = false;

        if (!TheGreenMachineDeluxeAutoSpinController.isAutoSpinning && !isFreeGame && freeSpinCount == 0)
        {
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Single Stop");
        }

        if (isFreeGameReady)
        {
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Free Game Transition");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region Slot Animation
    public bool forcedAnimation;
    public int forcedWinIndex;
    private void PlaySlotAnimations()
    {
        if (reels == null || reels.Count == 0)
        {
            Invoke(nameof(SetSlotAnimationCompleted), isSlotAnimationCompletedDelay);
            return;
        }

        var ui = TheGreenMachineDeluxeUIManager.Instance;
        int reelCount = reels.Count;

        for (int i = 0; i < reelCount; i++)
        {
            var reel = reels[i];
            if (reel?.slots == null) continue;

            for (int j = 0; j < 3; j++)
            {
                int idx = j + 1;
                if (idx < 0 || idx >= reel.slots.Count) continue;

                var slot = reel.slots[idx];
                if (slot == null) continue;

                slot.StartAnimation();

                if (forcedAnimation)
                {
                    slot.category = TheGreenMachineDeluxeSlotCategory.Jackpot;
                    if (forcedWinIndex == 1) slot.type = TheGreenMachineDeluxeSlotType.MINI;
                    else if (forcedWinIndex == 2) slot.type = TheGreenMachineDeluxeSlotType.MINOR;
                    else if (forcedWinIndex == 3) slot.type = TheGreenMachineDeluxeSlotType.MAJOR;
                    else if (forcedWinIndex == 4) slot.type = TheGreenMachineDeluxeSlotType.MEGA;
                    else if (forcedWinIndex == 5) slot.type = TheGreenMachineDeluxeSlotType.GRAND;
                }

                Debug.Log("LovKumar slot.type :  " + slot.type);

                if (slot.category == TheGreenMachineDeluxeSlotCategory.Jackpot) 
                {
                    Debug.Log("LovKumar slot.category : " + slot.category);
                    switch (slot.type)
                    {
                        case TheGreenMachineDeluxeSlotType.MINI:
                            Debug.Log("LovKumar case slot.type :  " + slot.type);
                            ui?.PlayNiceWinAnimation(winAmount);
                            break;

                        case TheGreenMachineDeluxeSlotType.MINOR:
                            Debug.Log("LovKumar case slot.type :  " + slot.type);
                            ui?.PlayBigWinAnimation(winAmount);
                            break;

                        case TheGreenMachineDeluxeSlotType.MAJOR:
                            Debug.Log("LovKumar case slot.type :  " + slot.type);
                            ui?.PlayMegaWinAnimation(winAmount);
                            break;

                        case TheGreenMachineDeluxeSlotType.MEGA:
                            Debug.Log("LovKumar case slot.type :  " + slot.type);
                            ui?.PlaySuperWinAnimation(winAmount);
                            break;

                        case TheGreenMachineDeluxeSlotType.GRAND:
                            Debug.Log("LovKumar case slot.type :  " + slot.type);
                            ui?.PlayJackpotWinAnimation(winAmount);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        if (TheGreenMachineDeluxeAutoSpinController.isAutoSpinning || isFreeGame)
        {
            Invoke(nameof(SetSlotAnimationCompleted), isSlotAnimationCompletedDelay * 3);
        }
        else
        {
            Invoke(nameof(SetSlotAnimationCompleted), isSlotAnimationCompletedDelay);
    }

    }

    public float isSlotAnimationCompletedDelay;

    private void StopSlotAnimations()
    {
        foreach (var reel in reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.StopAnimation();
                }
            }
        }
    }

    private void SetSlotAnimationCompleted()
    {
        isSpinAgain = true;
        isSlotAnimationCompleted = true;

        if ((freeSpinCount > 0 || fakefreespins > 0) && !isFreeGame)
        {
            firstFreeSpin = true;
            TheGreenMachineDeluxeUIManager.Instance.UpdateButtons("Free Game Transition");
            TheGreenMachineDeluxeFreeGameTransitionController.Instance.StartFreeSpinTransition();
            TheGreenMachineDeluxeFreeGameTransitionController.Instance.UpdateFreeSpinsCount(freeSpinCount);
        }
        else if (freeSpinCount > 0)
        {
            TheGreenMachineDeluxeFreeGameTransitionController.Instance.UpdateFreeSpinsCount(freeSpinCount);
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

        foreach (var res in settings.resourcesList)
        {
            switch (res.category)
            {
                case TheGreenMachineDeluxeSlotCategory.Empty:
                    emptyList.Add(res);
                    break;
                case TheGreenMachineDeluxeSlotCategory.Cash:
                    cashList.Add(res);
                    break;
                case TheGreenMachineDeluxeSlotCategory.FreeSpin:
                    freeSpinList.Add(res);
                    break;
                case TheGreenMachineDeluxeSlotCategory.Jackpot:
                    jackpotList.Add(res);
                    break;
            }
        }
    }

    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();
    }

    public static TheGreenMachineDeluxeSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
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

    #endregion
}
