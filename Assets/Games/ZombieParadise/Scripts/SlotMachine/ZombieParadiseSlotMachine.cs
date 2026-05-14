using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZombieParadiseSlotMachine : BaseSlotMachine
{
    #region Variables

    public static ZombieParadiseSlotMachine Instance;

    [Header("Machine References")]
    public ZombieParadiseGameSettings settings;
    public List<ZombieParadiseReelScript> reels;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public ZombieParadiseSpinResult currentSpinResult;

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
        if (result is ZombieParadiseSpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }
        if (currentSpinResult.scatterCount >= 3)
            scatterCount = currentSpinResult.scatterCount;
        else
            scatterCount = fakeScatterCount;

        if (scatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
            if (fakeScatterCount >= 3)
            {
                freeSpinCount = fakeFreeSpins;
            }
        }
        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
        //string json = JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented);
        //LogLarge("SpinResult (parsed)", json);


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

        ZombieParadiseUIManager.Instance.SetStopInteractable(true);
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
            ZombieParadiseUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        ZombieParadiseUIManager.Instance.SetStopInteractable(false);
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        ZombieParadiseUIManager.Instance.winAnimationCompleted = true;
        ZombieParadisePaylineController.Instance.StopPaylines();
        ZombieParadisePaylineController.Instance.ClearPaylineData();
        ZombieParadiseUIManager.Instance.PlaySpinMusic("Spin");

        if (settings.spinSettings.startSpin == ZombieParadiseSpinMode.SpinAll)
        {
            foreach (ZombieParadiseReelScript reel in reels)
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
            foreach (ZombieParadiseReelScript reel in reels)
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
                ZombieParadiseFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (ZombieParadiseAutoSpinController.isAutoSpinning)
            {
                ZombieParadiseUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                ZombieParadiseUIManager.Instance.UpdateButtons("Spin Stop");
            }

            isSpinAgain = true;
            ZombieParadiseUIManager.Instance.StopSpinMusic("Spin");
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
            if (!ZombieParadiseAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                ZombieParadiseUIManager.Instance.UpdateButtons("Spin Stop");
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
        if (settings.spinSettings.endSpin == ZombieParadiseSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    ZombieParadiseUIManager.Instance.PlaySound("ReelStop");
                    reels[i].StopSpin(); // No delay for simultaneous stop
                    
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
                    ZombieParadiseUIManager.Instance.PlaySound("ReelStop");
                    reels[i].StopSpin();
                    
                }
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        ZombieParadiseUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        // Force all reels to final position based on direction
        ForceAllReelsToFinalPosition();
        ZombieParadiseUIManager.Instance.StopSpinMusic("Spin");

        ProcessSpinResult();

        if (!ZombieParadiseAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            ZombieParadiseUIManager.Instance.UpdateButtons("Spin Stop");
        }
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].ApplyFinalResult(i);
            reels[i].StopSpin();
        }

        ZombieParadiseUIManager.Instance.SetStopInteractable(false);
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
            ZombieParadiseUIManager.Instance.UpdateWinAmount(winAmount, true);

        }
        else if (winAmount > 0)
        {
            float betAmount = ZombieParadiseUIManager.Instance.CurrentBet();
            //Invoke(nameof(UpdateGameCoin), 1f);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    ZombieParadisePaylineResult result = new ZombieParadisePaylineResult(payline.paylineIndex);
                    ZombieParadisePaylineController.Instance.AddPaylineData(result);
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
        if(winAmount > 0)
        {
            ZombieParadiseUIManager.Instance.PlaySound("Win");
        }

        if (!ZombieParadiseAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            ZombieParadiseUIManager.Instance.UpdateButtons("Spin Stop");
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

    private void SetReelDirection(ZombieParadiseReelScript reel)
    {
        ZombieParadiseSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == ZombieParadiseSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? ZombieParadiseSpinDirection.Up : ZombieParadiseSpinDirection.Down;
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
        foreach (ZombieParadiseReelScript reel in reels)
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
            foreach (ZombieParadiseReelScript reel in reels)
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
      ZombieParadisePaylineController.Instance.StartPayline(scatterCount);
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

    public static ZombieParadiseSlotResource? GetResourceById(string id)
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
