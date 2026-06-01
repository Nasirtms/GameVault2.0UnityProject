using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class UltimateFireLinkChinaStreetSlotMachine : BaseSlotMachine
{
    #region Variables

    public static UltimateFireLinkChinaStreetSlotMachine Instance;

    [Header("Machine References")]
    public UltimateFireLinkChinaStreetGameSettings settings;
    public List<UltimateFireLinkChinaStreetReelScript> reels;
    [SerializeField] private UltimateFireLinkChinaStreetBetController betController;
    private int reelsCount = 0;

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
    public int freeSpinWildMultiplier;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public bool isBonusGame;
    [HideInInspector] public bool canShowTrashMultiplier;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;

    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;


    public GameObject BaseGameReelsObject;
    public GameObject MiniGameReelsObject;

    public bool isMiniGame;
    public bool isMiniGameReady;

    public bool miniGame1;
    public List<int> miniGameLockedReels = new List<int>();

    public bool testMode;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        UpdateSlotServicesGameName();
        Initialize();

        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        reelsCount = reels.Count;
        InSpin = false;
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

        if (!testMode)
        {
            scatterCount = currentSpinResult.scatterCount;
            freeSpinWildMultiplier = currentSpinResult.freeSpinMultiplier;
        }
        else if (testMode)
        {
            scatterCount = 3;
            freeSpinWildMultiplier = 5;
        }
        UltimateFireLinkChinaStreetUIManager.Instance.showFreespinMultiplier(freeSpinWildMultiplier);


        if (!testMode)
        {
            if (currentSpinResult.isFreeSpin)
            {
                if (!isFreeGame)
                    isFreeGameReady = true;

                freeSpinCount = currentSpinResult.freeSpinCount;
            }
        }
        else if(testMode)
        {
            if(!isFreeGame)
            {
                isFreeGameReady = true;
                freeSpinCount = 5;
            }
        }

        if (currentSpinResult.isBonusGame)
        {
            isBonusGame = true;
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

        UltimateFireLinkChinaStreetUIManager.Instance.SetStopInteractable(true);
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
        // Reset win only on normal spins or the very first free spin
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            UltimateFireLinkChinaStreetUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        UltimateFireLinkChinaStreetUIManager.Instance.PlaySpinMusic("Spin");

        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        UltimateFireLinkChinaStreetPaylineController.Instance.ClearPaylineData();
        UltimateFireLinkChinaStreetPaylineController.Instance.StopPaylineDisplay();
        UltimateFireLinkChinaStreetUIManager.Instance.winAnimationCompleted = true;
        UltimateFireLinkChinaStreetUIManager.Instance.SetStopInteractable(false);


        if (settings.spinSettings.startSpin == UltimateFireLinkChinaStreetSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                UltimateFireLinkChinaStreetReelScript reel = reels[i];
                if (reel == null) continue;

                SetReelDirection(reel);
                isResultReceived = false;

                reel.StartSpin();
            }
        }
        else
        {
            for (int i = 0; i < reels.Count; i++)
            {
                UltimateFireLinkChinaStreetReelScript reel = reels[i];
                if (reel == null) continue;

                SetReelDirection(reel);
                isResultReceived = false;

                reel.StartSpin();

                yield return new WaitForSeconds(settings.spinSettings.ReelStartDelay);
            }
        }

        yield return StartCoroutine(WaitForAllReelsToBeSpinning());

        yield return new WaitForSeconds(settings.slotSettings.MinSpinDuration);

        StartSpinWithBackendResult();
        //Debug.Log("Deepak has a spin again - 3" + isSpinAgain);
    }


    public void StartSpinWithBackendResult()
    {
        StartCoroutine(WaitUntilResultAndThenStop());
    }

    #endregion

    #region Stop and Backend Result

    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 10f;
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
                //PandaFortuneFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (UltimateFireLinkChinaStreetAutoSpinController.isAutoSpinning)
            {
                UltimateFireLinkChinaStreetUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                UltimateFireLinkChinaStreetUIManager.Instance.UpdateButtons("Idle");
            }
            UltimateFireLinkChinaStreetUIManager.Instance.StopSpinMusic("Spin");
            isSpinAgain = true;
            //Debug.Log("Deepak has a spin again - 4" + isSpinAgain);
            yield break;
        }

        // Optional: small delay for visual pacing
        yield return new WaitForSeconds(0.5f);

        StopWithResult();
        //Debug.Log("Deepak has a spin again - 5" + isSpinAgain);
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
            if (!UltimateFireLinkChinaStreetAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                UltimateFireLinkChinaStreetUIManager.Instance.UpdateButtons("Idle");
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

        if (settings.spinSettings.endSpin == UltimateFireLinkChinaStreetSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] == null) continue;

                reels[i].ApplyFinalResult(i);
                reels[i].StopSpin();
            }
        }
        else
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] == null) continue;
                if (isStopBtnPressed) break;

                yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                reels[i].ApplyFinalResult(i);
                reels[i].StopSpin();
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        UltimateFireLinkChinaStreetUIManager.Instance.StopSpinMusic("Spin");
        UltimateFireLinkChinaStreetUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

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

        UltimateFireLinkChinaStreetUIManager.Instance.SetStopInteractable(false);
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
            freeSpinWinAmount += winAmount;
            UltimateFireLinkChinaStreetUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = UltimateFireLinkChinaStreetUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }


        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    UltimateFireLinkChinaStreetPaylineResult result = new UltimateFireLinkChinaStreetPaylineResult(payline.paylineIndex, payline.count, payline.winAmount, payline.symbol);
                    UltimateFireLinkChinaStreetPaylineController.Instance.AddPaylineData(result);

                }
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }
        if (winAmount > 0)
        {
            UltimateFireLinkChinaStreetUIManager.Instance.PlaySound("Win");
        }
        // Spin complete
        isSpinAgain = true;
        InSpin = false;
        //Debug.Log("Deepak has a spin again - 7" + isSpinAgain);
        if (!UltimateFireLinkChinaStreetAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            if (!isFreeGameReady)
            {
                UltimateFireLinkChinaStreetUIManager.Instance.UpdateButtons("Idle");
            }
        }

        if(isBonusGame)
        {
            UltimateFireLinkChinaStreetUIManager.Instance.spinButton.GetButtonComponent().interactable = false;
            UltimateFireLinkChinaStreetUIManager.Instance.autoButton.GetButtonComponent().interactable = false;
        }
        //Debug.Log("Deepak has a spin again - 8" + isSpinAgain);
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

            for (int i = 0; i < reels.Count; i++)
            {
                var reel = reels[i];
                if (reel == null) continue;

                if (reel.IsSpinning)
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


    private void SetReelDirection(UltimateFireLinkChinaStreetReelScript reel)
    {
        UltimateFireLinkChinaStreetSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == UltimateFireLinkChinaStreetSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? UltimateFireLinkChinaStreetSpinDirection.Up : UltimateFireLinkChinaStreetSpinDirection.Down;
        }

        reel.SetSpinDirection(direction);
    }

    public void ForceAllReelsToFinalPosition()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            var reel = reels[i];
            if (reel == null) continue;

            reel.ForceClampToTop();
        }
    }


    public void ForceAllReelsToTop()
    {
        foreach (UltimateFireLinkChinaStreetReelScript reel in reels)
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

            for (int i = 0; i < reels.Count; i++)
            {
                var reel = reels[i];
                if (reel == null) continue;

                if (!reel.IsSpinning)
                {
                    allSpinning = false;
                    break;
                }
            }

            if (!allSpinning)
            {
                yield return null;
            }
        }
    }


    private void ShowPaylines()
    {
        UltimateFireLinkChinaStreetPaylineController.Instance.StartPayline(scatterCount);
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
        currentSpinResult = null;
        StopWithResult();
    }
    public float CurrentBet()
    {
        return betController.GetCurrentBet();
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

    public static UltimateFireLinkChinaStreetSlotResource? GetResourceById(string id)
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

    #endregion
}
