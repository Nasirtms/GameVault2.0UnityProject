using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BiggerBassBonanzaSlotMachine : BaseSlotMachine
{
    #region Variables

    public static BiggerBassBonanzaSlotMachine Instance;

    [Header("Machine References")]
    public BiggerBassBonanzaGameSettings settings;
    public List<BiggerBassBonanzaReelScript> reels;
    [SerializeField] private BiggerBassBonanzaBetController betController;
    [HideInInspector] public new List<List<BiggerBassBonanzaSymbolData>> spinSymbolMatrix = new();

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public BiggerBassBonanzaSpinResult currentSpinResult;

    // State Variables
    [HideInInspector] public bool inSpin;
    [HideInInspector] public bool isStopBtnPressed;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    [HideInInspector] public bool firstFreeSpin;
    private bool applyingResult;

    // Free Spin Game
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    private int scatterCount;

    // Wild
    public List<int> wildMultipliers;
    [HideInInspector] public List<BiggerBassBonanzaSlotScript> wildSlots;
    [HideInInspector] public List<Vector3> wildWorldPos;
    [HideInInspector] public int wildCount;
    [HideInInspector] public int retriggerCount;
    [HideInInspector] public bool isFishCollectionCompleted;

    // Win
    [HideInInspector] public float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    #region Editor Triggers

    [Header("Fake Free Spins")]
    [SerializeField] private bool fakeScatter;
    [SerializeField] private int fakeScatterCount;
    [SerializeField] private int fakeFreeSpinCount;

    [Header("Forced Prize")]
    [SerializeField] public bool forcedWin;
    [SerializeField] public float forcedPrize;

    #endregion

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        inSpin = false;
        isStopBtnPressed = false;

        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        UpdateSlotServicesGameName();
        Initialize();
    }

    private void OnDestroy()
    {
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

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is BiggerBassBonanzaSpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        if (fakeScatter)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            scatterCount = fakeScatterCount;
            freeSpinCount = fakeFreeSpinCount;
        }
        else if (currentSpinResult.scatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            scatterCount = currentSpinResult.scatterCount;
            freeSpinCount = currentSpinResult.freeSpinCount;
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<BiggerBassBonanzaSymbolData> symbols = new List<BiggerBassBonanzaSymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
            }
            spinSymbolMatrix.Add(symbols);
        }

        BiggerBassBonanzaUIManager.Instance.SetStopInteractable(true);
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

        spinCoroutine = StartCoroutine(StartSpin());
    }

    private IEnumerator StartSpin()
    {
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            freeSpinWinAmount = 0;
            winAmount = 0f;
            wildCount = 0;
            retriggerCount = 0;
            BiggerBassBonanzaUIManager.Instance.UpdateWinAmount(0f);
        }

        scatterCount = 0;
        freeSpinCount = 0;
        currentSpinResult = null;
        inSpin = true;
        applyingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        isFishCollectionCompleted = true;
        BiggerBassBonanzaUIManager.Instance.winAnimationCompleted = true;

        wildSlots.Clear();
        wildWorldPos.Clear();

        for (int i = 0; i < reels.Count; i++)
        {
            for (int j = 0; j < reels[i].slots.Count; j++)
            {
                reels[i].slots[j].HideBox();
                reels[i].slots[j].HideMultiplier();
            }
        }

        if (BiggerBassBonanzaPaylineController.Instance != null)
        {
            BiggerBassBonanzaPaylineController.Instance.StopPaylines();
            BiggerBassBonanzaPaylineController.Instance.ClearPaylineData();
        }

        BiggerBassBonanzaUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == BiggerBassBonanzaSpinMode.SpinAll)
        {
            foreach (BiggerBassBonanzaReelScript reel in reels)
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
            foreach (BiggerBassBonanzaReelScript reel in reels)
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

            if (isFreeGame)
            {
                BiggerBassBonanzaFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (BiggerBassBonanzaAutoSpinController.isAutoSpinning)
            {
                BiggerBassBonanzaUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
            }

            yield break;
        }

        // Optional: small delay for visual pacing
        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }

    public void StopWithResult() => Stop();

    public void Stop()
    {
        if (inSpin == false) { return; }
        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            inSpin = false;
            foreach (var reel in reels)
            {
                isResultReceived = false;
                reel.ForceStopSpin();
            }

            return;
        }

        if (applyingResult)
        {
            return;
        }

        applyingResult = true;

        stopCoroutine = StartCoroutine(StopReelsWithResultRoutine());
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }

        // Stop reels based on end spin mode
        if (settings.spinSettings.endSpin == BiggerBassBonanzaSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    BiggerBassBonanzaUIManager.Instance.PlaySound("ReelStop");
                    reels[i].StopSpin(); // No delay for simultaneous stop
                }
            }
        }
        else 
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    if (isStopBtnPressed)
                        break;

                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);

                    reels[i].StopSpin();
                }
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        ProcessSpinResult();
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].ApplyFinalResult(i);
            reels[i].StopSpin();
        }
    }

    private void ProcessSpinResult()
    {
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

        if (isFreeGame)
        {
            for (int x = 0; x < reels.Count; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var slot = reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (slot.slotType == BiggerBassBonanzaSlotType.Wild)
                    {
                        wildSlots.Add(slot);
                        wildWorldPos.Add(slot.textBox.transform.position);

                        isFishCollectionCompleted = false;
                    }
                }
            }
        }

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            BiggerBassBonanzaUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = BiggerBassBonanzaUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3 || wildWorldPos.Count > 0)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    BiggerBassBonanzaPaylineResult result = new BiggerBassBonanzaPaylineResult(payline.paylineIndex, payline.count);
                    if (BiggerBassBonanzaPaylineController.Instance != null)
                    {
                        BiggerBassBonanzaPaylineController.Instance.AddPaylineData(result);
                    }
                }
            }

            ShowPaylines();
        }
        else
        {
            isSlotAnimationCompleted = true;
        }

        inSpin = false;
        isStopBtnPressed = false;

        if (!BiggerBassBonanzaAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            BiggerBassBonanzaUIManager.Instance.UpdateButtons("Default");
        }
    }

    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }

    #endregion

    #region Reel Helpers

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

    private void SetReelDirection(BiggerBassBonanzaReelScript reel)
    {
        BiggerBassBonanzaSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == BiggerBassBonanzaSpinDirection.Random)
        {
            direction = UnityEngine.Random.value > 0.5f ? BiggerBassBonanzaSpinDirection.Up : BiggerBassBonanzaSpinDirection.Down;
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

    private IEnumerator WaitForAllReelsToBeSpinning()
    {
        bool allSpinning = false;
        while (!allSpinning)
        {
            allSpinning = true;
            foreach (BiggerBassBonanzaReelScript reel in reels)
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

    #endregion

    #region Base Slot Machine

    public override void ClearPaylines()
    {

    }

    public override void StopSpinGettingError()
    {

    }

    #endregion

    #region Helper Functions

    private void ShowPaylines()
    {
        if (BiggerBassBonanzaPaylineController.Instance != null)
        {
            BiggerBassBonanzaPaylineController.Instance.StartPayline(scatterCount);
        }
    }

    public float GetWinAmount()
    {
        return currentSpinResult.totalWin;
    }

    public float CurrentBet()
    {
        return betController.GetCurrentBet();
    }

    public static BiggerBassBonanzaSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            Debug.LogWarning("Settings or resourcesList is null.");
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

    public bool isFishSlot(BiggerBassBonanzaSlotType slotType)
    {
        if (slotType == BiggerBassBonanzaSlotType.GoldenFish ||
            slotType == BiggerBassBonanzaSlotType.BigFish ||
            slotType == BiggerBassBonanzaSlotType.MediumFish ||
            slotType == BiggerBassBonanzaSlotType.SmallFish ||
            slotType == BiggerBassBonanzaSlotType.VerySmallFish)
        {
            return true;
        }
        
        return false;
    }

    #endregion
}
