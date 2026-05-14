using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CashVaultSlotMachine : BaseSlotMachine
{
    #region Variables
    public static CashVaultSlotMachine Instance;

    [Header("Machine References")]
    public CashVaultGameSettings settings;
    public List<CashVaultReelScript> reels;
    public CashVaultBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public CashVaultSpinResult currentSpinResult;

    // State Variables
    //[HideInInspector] public bool InSpin = false;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    public bool isBlindFeature;
    //public bool isFreeGame;
    public bool isFreeGameReady;
    public int freeSpinCount;
    [HideInInspector] public int wildCount;
    [HideInInspector] public float freeSpinWinAmount;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public CashVaultSlotType[,] resultMatrix;

    public bool hasSymbol = false;
    //[SerializeField] public GameObject reelSymbolEffect;

    public GameObject BaseGameReelsObject;
    public GameObject MiniGameReelsObject;

    public bool isMiniGame;
    public bool isMiniGameReady;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
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
    public int fakeFreeSpins;
    public int fakeWilds;
    [Header("MiniGame Fake Data")]
    public bool miniGame1;
    public List<int> miniGameLockedReels = new List<int>();
    public int scatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is CashVaultSpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        if (scatterCount >= 3)//if (currentSpinResult.isFreeSpin)
        {
            isBlindFeature = true;

            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }

        if(fakeScatterCount > 2)
        {
            isBlindFeature = true;
            if (fakeScatterCount == 3)
            {
                scatterCount = fakeScatterCount;
                if (!isFreeGame)
                    isFreeGameReady = true;

                freeSpinCount = fakeFreeSpins;
                wildCount = fakeWilds;
            }
            if (fakeScatterCount == 6)
            {
                scatterCount = fakeScatterCount;

                if (!isMiniGame)
                    isMiniGameReady = true;
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
                //if (reelIndex < 4)
                //{
                //    var res = GetResourceById(symbol.id);
                //    if (res.HasValue)
                //    {
                //        if (isCCSlot(res.Value.slotType))
                //        {
                //            hasSymbol = true;
                //        }
                //    }
                //}
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        CashVaultUIManager.Instance.SetStopInteractable(true);
    }
    #endregion

    #region Spin
    public override void Spin()
    {
        if (isMiniGame)
        {
            var machine = CashVaultMiniGameSlotMachine.Instance;
            if (machine.spinCoroutine != null)
            {
                StopCoroutine(machine.spinCoroutine);
            }

            if (machine.stopCoroutine != null)
            {
                StopCoroutine(machine.stopCoroutine);
            }

            machine.spinCoroutine = StartCoroutine(machine.StartSpin());
        }
        else
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
    }

    private IEnumerator StartSpin()
    {
        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            CashVaultUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        //miniGameLockedReels.Clear();
        //reelSymbolEffect.SetActive(false);
        freeSpinCount = 0;
        hasSymbol = false;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        isBlindFeature = false;
        CashVaultUIManager.Instance.winAnimationCompleted = true;
        CashVaultUIManager.Instance.PlaySpinMusic("Spin");
        ClearPaylines();
        CashVaultPaylineController.Instance.overlay.SetActive(false);
        CashVaultPaylineController.Instance.StopPaylines();
        CashVaultPaylineController.Instance.ClearPaylineData();
        CashVaultUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == CashVaultSpinMode.SpinAll)
        {
            foreach (CashVaultReelScript reel in reels)
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
            foreach (CashVaultReelScript reel in reels)
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

    #region Stop & Backend Result
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
                CashVaultFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (CashVaultAutoSpinController.isAutoSpinning)
            {
                CashVaultUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                CashVaultUIManager.Instance.UpdateButtons("Stop");
            }
            CashVaultUIManager.Instance.StopSpinMusic("Spin");
            isSpinAgain = true;
            yield break;
        }

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
            if (!CashVaultAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                CashVaultUIManager.Instance.UpdateButtons("Stop");
            }
            return;
        }

        if (isSettingResult)
            return;

        isSettingResult = true;
        stopCoroutine = StartCoroutine(StopReelsWithResultRoutine());
    }
    private IEnumerator StopReelsWithResultRoutine()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }

        //CashVaultReelScript slowReel;
        //slowReel = null;
        if (settings.spinSettings.endSpin == CashVaultSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    //if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    //{
                    //    slowReel = reels[i];
                    //    slowReel.spinSpeed = 8;
                    //    yield return new WaitForSeconds(2);
                    //    slowReel.ApplyFinalResult(i);
                    //    slowReel.StopSpin();
                    //    continue;
                    //}
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            //CashVaultUIManager.Instance.PlaySound("Stop");
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

                    //if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    //{
                    //    PlayReelSymbolEffect();
                    //    slowReel = reels[i];
                    //    slowReel.spinSpeed = 8;
                    //    yield return new WaitForSeconds(2);
                    //    slowReel.ApplyFinalResult(i);
                    //    slowReel.StopSpin();
                    //    continue;
                    //}
                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
                CashVaultUIManager.Instance.PlaySound("ReelStop");
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        CashVaultUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        CashVaultUIManager.Instance.StopSpinMusic("Spin");
        ProcessSpinResult();
    }
    //private void PlayReelSymbolEffect()
    //{
    //    reelSymbolEffect.SetActive(true);
    //    var spriteRenderer = reelSymbolEffect.GetComponent<SpriteRenderer>();

    //    if (spriteRenderer != null)
    //    {
    //        Color c = spriteRenderer.color;
    //        c.a = 0;
    //        spriteRenderer.color = c;

    //        spriteRenderer.DOFade(1, 0.5f)
    //            .OnComplete(() =>
    //            {
    //                DOVirtual.DelayedCall(3.0f, () =>  // stays visible 2s
    //                {
    //                    spriteRenderer.DOFade(0, 0.6f).OnComplete(() =>
    //                    {
    //                        reelSymbolEffect.SetActive(false);
    //                    });
    //                });
    //            });
    //    }
    //}

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].ApplyFinalResult(i);
            reels[i].StopSpin();
        }
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
            return;

        if (forcedWin)
            winAmount = forcedPrize;
        else
            winAmount = currentSpinResult.totalWin;

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            CashVaultUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = CashVaultUIManager.Instance.CurrentBet();
            CashVaultUIManager.Instance.UpdateWinText(winAmount);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                CashVaultPaylineResult result = new CashVaultPaylineResult(payline.paylineIndex);
                CashVaultPaylineController.Instance.AddPaylineData(result);
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady || isBlindFeature) /* || isMiniGameReady */
        {
            CashVaultUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!CashVaultAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            CashVaultUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            CashVaultUIManager.Instance.UpdateButtons("Free Spin");
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

    private void SetReelDirection(CashVaultReelScript reel)
    {
        CashVaultSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == CashVaultSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? CashVaultSpinDirection.Up : CashVaultSpinDirection.Down;
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
        foreach (CashVaultReelScript reel in reels)
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
            foreach (CashVaultReelScript reel in reels)
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
    public static CashVaultSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        foreach (var res in Instance.settings.slotResources)
        {
            if (res.slotType.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }
        return null;
    }
    private void ShowPaylines()
    {
        CashVaultPaylineController.Instance.StartPayline(scatterCount);
    }

    private void SetSlotAnimationCompleted()
    {
        isSpinAgain = true;
        isSlotAnimationCompleted = true;
    }
    public override void ClearPaylines(){ }
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

    public bool isScatterSlot(CashVaultSlotType slotType)
    {
        if (slotType == CashVaultSlotType.Scatter)
        {
            return true;
        }

        return false;
    }
    #endregion
}