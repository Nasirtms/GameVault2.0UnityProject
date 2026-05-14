using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class RichLittlePiggiesSlotMachine : BaseSlotMachine
{
    #region Variables

    public static RichLittlePiggiesSlotMachine Instance;

    [Header("Machine References")]
    public RichLittlePiggiesGameSettings settings;
    public List<RichLittlePiggiesReelScript> reels;


    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    //public bool InSpin = false;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    private bool _isSingleSpin;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    public bool firstFreeSpin;

    [ShowInInspector][ReadOnly] public string freeGameType;
    public bool testmode = false;
    public string testmodeCardName;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

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
        UpdateSlotServicesGameName();
        Initialize();

        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        InSpin = false;
    }

    private void OnDestroy()
    {
        // Clearing Instance    
        if (Instance == this)
            Instance = null;

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

    #region Machine Settings

    private void Initialize()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.Initialize();
            }
        }
    }

    #endregion

    #region Spin Result Receive

    [Header("Fake Free Spin")]
    public int fakeScatterCount;
    public int fakeFreeSpins;
    public int scatterCount;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        scatterCount = currentSpinResult.scatterCount;


        if (!testmode)
        {
            if (currentSpinResult.isFreeSpin)
            {
                if (!isFreeGame)
                {
                    isFreeGameReady = true;
                    freeSpinCount = currentSpinResult.freeSpinCount;
                    freeGameType = currentSpinResult.cardName.ToLower();
                }
            }
        }
        else
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
                freeSpinCount = 3;
                freeGameType = testmodeCardName.ToLower();
                scatterCount = fakeScatterCount;
            }
        }

        //if (fakeScatterCount >= 3)
        //{
        //    freeSpinCount = fakeFreeSpins;
        //}


        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
        //string json = JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented);
        //LogLarge("SpinResult (parsed)", json);.

        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                var res = GetResourceById(symbol.id);
            }
            spinSymbolMatrix.Add(symbols);
        }

        RichLittlePiggiesUIManager.Instance.SetStopInteractable(true);
    }



    //void LogLarge(string prefix, string text, int chunkSize = 8000)
    //{
    //    if (string.IsNullOrEmpty(text))
    //    {
    //        return;
    //    }

    //    for (int i = 0; i < text.Length; i += chunkSize)
    //    {
    //        string chunk = text.Substring(i, Mathf.Min(chunkSize, text.Length - i));
    //    }
    //}

    #endregion

    #region Spin

    public override void Spin()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }

        if (stopCoroutine != null)
        {
            StopCoroutine(stopCoroutine);
        }

        // Start the spin
        spinCoroutine = StartCoroutine(StartSpin());
    }

    private IEnumerator StartSpin()
    {
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            RichLittlePiggiesUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        RichLittlePiggiesUIManager.Instance.SetStopInteractable(false);
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        RichLittlePiggiesUIManager.Instance.winAnimationCompleted = true;
        RichLittlePiggiesPaylineController.Instance.StopPaylines();
        RichLittlePiggiesPaylineController.Instance.ClearPaylineData();
        RichLittlePiggiesUIManager.Instance.PlaySpinMusic("Spin");

        if (settings.spinSettings.startSpin == RichLittlePiggiesSpinMode.SpinAll)
        {
            foreach (RichLittlePiggiesReelScript reel in reels)
            {
                if (reel != null)
                {
                    SetReelDirection(reel);
                    isResultReceived = false;
                    reel.StartSpin();
                }
            }
        }
        else
        {
            foreach (RichLittlePiggiesReelScript reel in reels)
            {
                if (reels != null)
                {
                    SetReelDirection(reel);
                    isResultReceived = false;
                    reel.StartSpin();

                    yield return new WaitForSeconds(settings.spinSettings.ReelStartDelay);
                }
            }
        }

        yield return StartCoroutine(WaitForAllReelsToBeSpinning());

        yield return new WaitForSeconds(settings.slotSettings.MinSpinDuration);

        StartSpinWithBackendResult();
    }

    public void StartSpinWithBackendResult()
    {
        StartCoroutine(WaitUntilResultAndThenStop());
    }

    #endregion

    #region Stop and Backend Result

    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 12f;
        float elapsed = 0f;

        // Wait until result is received
        while (currentSpinResult == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult(); // fallback

            if (isFreeGame)
            {
                //RichLittlePiggiesFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (RichLittlePiggiesAutoSpinController.isAutoSpinning)
            {
                RichLittlePiggiesUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                RichLittlePiggiesUIManager.Instance.UpdateButtons("Spin Stop");
            }

            isSpinAgain = true;
            RichLittlePiggiesUIManager.Instance.StopSpinMusic("Spin");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);
        StopWithResult();
    }

    public void StopWithResult() => Stop();

    public void Stop()
    {
        if (InSpin == false) { return; }
        if (currentSpinResult == null)
        {
            InSpin = false;
            foreach (var reel in reels)
            {
                isResultReceived = false;
                reel.ForceStopSpin();
            }
            if (!RichLittlePiggiesAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                RichLittlePiggiesUIManager.Instance.UpdateButtons("Spin Stop");
            }

            return;
        }

        if (isSettingResult)
        {
            return;
        }

        isSettingResult = true;

        stopCoroutine = StartCoroutine(StopReelsWithResultRoutine());
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }

        // Stop reels based on end spin mode
        if (settings.spinSettings.endSpin == RichLittlePiggiesSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    RichLittlePiggiesUIManager.Instance.PlaySound("ReelStop");
                    if (i == 1 || i == 2)
                    {
                        reels[i].StopSpin(true);
                    }
                    else
                    {
                        reels[i].StopSpin(false);
                    } // No delay for simultaneous stop

                }
            }
        }
        else // SpinOneByOne mode
        {
            // Stop reels one by one with delays
            for (int i = 0; i < reels.Count; i++)
            {

                if (reels[i] != null)
                {
                    if (isStopBtnPressed)
                        break;

                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);
                    RichLittlePiggiesUIManager.Instance.PlaySound("ReelStop");
                    if (i == 1 || i == 2)
                    {
                        reels[i].StopSpin(true);
                    }
                    else
                    {
                        reels[i].StopSpin(false);
                    }

                }
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        RichLittlePiggiesUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        // Force all reels to final position based on direction
        ForceAllReelsToFinalPosition();
        RichLittlePiggiesUIManager.Instance.StopSpinMusic("Spin");

        ProcessSpinResult();

        if (!RichLittlePiggiesAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            RichLittlePiggiesUIManager.Instance.UpdateButtons("Single Stop");
        }
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].ApplyFinalResult(i);
            if (i == 1 || i == 2)
            {
                reels[i].StopSpin(true);
            }
            else
            {
                reels[i].StopSpin(false);
            }
        }

        RichLittlePiggiesUIManager.Instance.SetStopInteractable(false);
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    private void ProcessSpinResult()
    {
        RichLittlePiggiesPaylineController.Instance.RevealSlots();
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
        Debug.Log("winAmount : " + winAmount);
        Debug.Log("currentSpinResult.totalWin : " + currentSpinResult.totalWin);
        Debug.Log("newBalance : " + currentSpinResult.newBalance);
        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            RichLittlePiggiesUIManager.Instance.UpdateWinAmount(winAmount);

        }
        else if (winAmount > 0)
        {
            float betAmount = RichLittlePiggiesUIManager.Instance.CurrentBet();
            //Invoke(nameof(UpdateGameCoin), 1f);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || (scatterCount >= 3))
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    RichLittlePiggiesPaylineResult result = new RichLittlePiggiesPaylineResult(payline.paylineIndex, payline.count, payline.count.ToString());
                    RichLittlePiggiesPaylineController.Instance.AddPaylineData(result);
                }
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        // Spin complete
        InSpin = false;
        isSpinAgain = true;
        if (winAmount > 0)
        {
            RichLittlePiggiesUIManager.Instance.PlaySound("Win");
        }

        if (!RichLittlePiggiesAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            RichLittlePiggiesUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region Helper Functions

    private IEnumerator WaitForAllReelsToStop()
    {
        bool allStopped = false;

        while (!allStopped)
        {
            allStopped = true;

            foreach (var reel in reels)
            {
                if (reel != null && reel.IsSpinning)
                {
                    allStopped = false;
                    break;
                }
            }

            if (!allStopped)
            {
                yield return null;
            }
        }
    }

    private void SetReelDirection(RichLittlePiggiesReelScript reel)
    {
        RichLittlePiggiesSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == RichLittlePiggiesSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? RichLittlePiggiesSpinDirection.Up : RichLittlePiggiesSpinDirection.Down;
        }

        reel.SetSpinDirection(direction);
    }

    public void ForceAllReelsToFinalPosition()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                // Always clamp to top position regardless of spin direction
                reel.ForceClampToTop();
            }
        }
    }

    public void ForceAllReelsToTop()
    {
        foreach (RichLittlePiggiesReelScript reel in reels)
        {
            if (reel != null)
            {
                reel.ForceClampToTop();
            }
        }
    }

    private IEnumerator WaitForAllReelsToBeSpinning()
    {
        bool allSpinning = false;
        while (!allSpinning)
        {
            allSpinning = true;
            foreach (RichLittlePiggiesReelScript reel in reels)
            {
                if (reel != null && !reel.IsSpinning)
                {
                    allSpinning = false;
                    break;
                }
            }
            if (!allSpinning)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void ShowPaylines()
    {
        RichLittlePiggiesPaylineController.Instance.StartPayline(scatterCount);
    }

    private void SetSlotAnimationCompleted()
    {
        isSpinAgain = true;
        isSlotAnimationCompleted = true;
    }

    public override void ClearPaylines()
    {

    }

    public override void StopSpinGettingError()
    {

    }

    public float GetWinAmount()
    {
        return currentSpinResult.totalWin;
    }

    public static RichLittlePiggiesSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        //Manually find match and return nullable
        foreach (var res in Instance.settings.slotResources)
        {
            if (res.slotType.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }

        return null;
    }
    #endregion
}
