using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PiratesOfTheCaribbeanSlotMachine : BaseSlotMachine
{
    #region Variables

    public static PiratesOfTheCaribbeanSlotMachine Instance { get; private set; }

    [Header("Machine References")]
    public PiratesOfTheCaribbeanGameSettings settings;
    public List<PiratesOfTheCaribbeanReelScript> reels;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    //public bool InSpin = false;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;

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
    [Header("Fake Scatter")]
    public int fakeScatterCount;
    public void SetSpinResult(SpinResult spinResult)
    {
        currentSpinResult = spinResult;
    }
    public int scatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }
        if (currentSpinResult.scatterCount >= 3)
            scatterCount = currentSpinResult.scatterCount;

        if (scatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        else if(fakeScatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = 3;
        }


            //Debug.Log("Scatter Count: " + currentSpinResult.scatterCount + "\nPaylines Count: " + currentSpinResult.paylineWins.Count);
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

        PiratesOfTheCaribbeanUIManager.Instance.SetStopInteractable(true);
    }

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
            PiratesOfTheCaribbeanUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;

        PiratesOfTheCaribbeanUIManager.Instance.autoSpinPopupPanel.transform.localScale = new Vector3(1, 0, 1);
        PiratesOfTheCaribbeanUIManager.Instance.autoSpinPopupPanel.SetActive(false);
        PiratesOfTheCaribbeanUIManager.Instance.winAnimationCompleted = true;
        PiratesOfTheCaribbeanPaylineController.Instance.StopPaylines();
        PiratesOfTheCaribbeanPaylineController.Instance.ClearPaylineData();
        PiratesOfTheCaribbeanUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == PiratesOfTheCaribbeanSpinMode.SpinAll)
        {
            foreach (PiratesOfTheCaribbeanReelScript reel in reels)
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
            foreach (PiratesOfTheCaribbeanReelScript reel in reels)
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
                PiratesOfTheCaribbeanFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (PiratesOfTheCaribbeanAutoSpinController.isAutoSpinning)
            {
                PiratesOfTheCaribbeanUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Single Stop");
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
                isResultReceived = false;
                reel.ForceStopSpin();
            }
            if (!PiratesOfTheCaribbeanAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Single Stop");
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
        if (settings.spinSettings.endSpin == PiratesOfTheCaribbeanSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    //reels[i].StopSpin(0f); // No delay for simultaneous stop
                    reels[i].StopSpin(); // No delay for simultaneous stop
                }
            }
            PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Stop");
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

                    //float delay = i * settings.spinSettings.ReelStopDelay;
                    //reels[i].StopSpin(delay);
                    reels[i].StopSpin();
                }
            }
            PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Stop");
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        PiratesOfTheCaribbeanUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        // Force all reels to final position based on direction
        ForceAllReelsToFinalPosition();

        ProcessSpinResult();
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].ApplyFinalResult(i);
            //reels[i].StopSpin(0f);
            reels[i].StopSpin();
        }

        PiratesOfTheCaribbeanUIManager.Instance.SetStopInteractable(false);
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            //Debug.LogWarning("❌ Spin result is invalid or failed.");
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
            PiratesOfTheCaribbeanUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = PiratesOfTheCaribbeanUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    PiratesOfTheCaribbeanPaylineResult result = new PiratesOfTheCaribbeanPaylineResult(payline.paylineIndex, payline.count);
                    PiratesOfTheCaribbeanPaylineController.Instance.AddPaylineData(result);
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

        if (!PiratesOfTheCaribbeanAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Single Stop");
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

    private void SetReelDirection(PiratesOfTheCaribbeanReelScript reel)
    {
        PiratesOfTheCaribbeanSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == PiratesOfTheCaribbeanSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? PiratesOfTheCaribbeanSpinDirection.Up : PiratesOfTheCaribbeanSpinDirection.Down;
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
        foreach (PiratesOfTheCaribbeanReelScript reel in reels)
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
            foreach (PiratesOfTheCaribbeanReelScript reel in reels)
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
        PiratesOfTheCaribbeanPaylineController.Instance.StartPayline(scatterCount);
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
        if (forcedWin)
        {
            return forcedPrize;
        }
        else
        {
            return currentSpinResult.totalWin;
        }
    }

    public static PiratesOfTheCaribbeanSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            //Debug.LogWarning("Settings or resourcesList is null.");
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
