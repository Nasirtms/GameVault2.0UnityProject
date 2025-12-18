using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PandaFortuneSlotMachine : BaseSlotMachine
{
    #region Variables

    public static PandaFortuneSlotMachine Instance;
    public List<int> frozenColumns = new List<int>();

    [Header("Machine References")]
    public PandaFortuneGameSettings settings;
    public List<PandaFortuneReelScript> reels;
    [SerializeField] private PandaFortuneBetController betController;
    [SerializeField] private float LastReelSpeed = 40.0f;
    private int reelsCount = 0;
    public int frozenIndexThisSpin = -1;
    private float durationOfReelSwap;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    public bool InSpin = false;
    [HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    public bool scatterOnReel1 = false;
    public bool scatterOnReel3 = false;
    public bool scatterOnReel5 = false;
    public bool wildOnReel1 = false;
    public bool wildOnReel2 = false;
    public bool wildOnReel3 = false;
    public bool wildOnReel4 = false;
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameTwo;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;

    // Free Game 2 extra state
    public bool[] frozenReelsFreeGame2 = new bool[5];
    private bool freeGameTwoActivatedThisSpin = false;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

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
        durationOfReelSwap = PandaFortuneFreeGameTransitionController.Instance.duration + 0.2f;

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

        wildOnReel1 = false;
        wildOnReel2 = false;
        wildOnReel3 = false;
        wildOnReel4 = false;
        scatterOnReel1 = false;
        scatterOnReel3 = false;
        scatterOnReel5 = false;
        freeGameTwoActivatedThisSpin = false;

        if (currentSpinResult.scatterCount >= 3)
            scatterCount = currentSpinResult.scatterCount;
        else
            scatterCount = fakeScatterCount;


        if (currentSpinResult.isFreeSpin || scatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
            if (fakeScatterCount >= 3)
                freeSpinCount = 5;
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
                    if(IsScatter(res.Value.slotType)) scatterOnReel1 = true;
                    if(IsWild(res.Value.slotType)) wildOnReel1 = true;
                }

                if (reelIndex == 1)
                {
                    if (IsWild(res.Value.slotType)) wildOnReel2 = true;
                }

                if (reelIndex == 2)
                {
                    if (IsScatter(res.Value.slotType)) scatterOnReel3 = true;
                    if (IsWild(res.Value.slotType)) wildOnReel3 = true;
                }

                if (reelIndex == 3)
                {
                    if (IsWild(res.Value.slotType)) wildOnReel4 = true;
                }

                if (reelIndex == 4)
                {
                    if (IsScatter(res.Value.slotType)) scatterOnReel5 = true;
                }
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        if (!isFreeGameTwo && isFreeGame && firstFreeSpin)
        {
            if (wildOnReel1 || wildOnReel2 || wildOnReel3 || wildOnReel4)
            {
                isFreeGameTwo = true;
                freeGameTwoActivatedThisSpin = true;
                reelsCount = Mathf.Max(1, reelsCount + 1);
            }
        }

        PandaFortuneUIManager.Instance.SetStopInteractable(true);
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
            PandaFortuneUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        if (isFreeGame)
        {
            if (firstFreeSpin)
            {
                reelsCount = reels.Count;
            }

            frozenIndexThisSpin = Mathf.Clamp(reelsCount - 1, 0, reels.Count - 1);
            frozenColumns.Clear();
            frozenColumns.Add(frozenIndexThisSpin);
        }
        else
        {
            frozenIndexThisSpin = -1;
        }

        //Debug.Log("tina has freegame bool: " + isFreeGame);
        //Debug.Log("tina has freegameready bool: " + isFreeGameReady);
        //Debug.Log("tina has frozen the reel: " + frozenIndexThisSpin);

        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        //Debug.Log("Deepak has a spin again - 2" + isSpinAgain);
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        PandaFortunePaylineController.Instance.ClearPaylineData();
        PandaFortuneUIManager.Instance.winAnimationCompleted = true;
        PandaFortuneUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == PandaFortuneSpinMode.SpinAll)
        {
            if (isFreeGame && !firstFreeSpin && reelsCount > 0 && reelsCount <= 4 && !isFreeGameTwo)
            {
                int a = reelsCount;
                int b = reelsCount - 1;

                if (a >= 0 && a < reels.Count && b >= 0 && b < reels.Count)
                {
                    //Debug.Log("tina is trying to swap the reels");
                    PandaFortuneFreeGameTransitionController.Instance.ReelSwap(reels[a].transform, reels[b].transform);
                    yield return new WaitForSeconds(durationOfReelSwap);

                    var tmp = reels[a];
                    reels[a] = reels[b];
                    reels[b] = tmp;
                }
            }

            if (isFreeGameTwo)
            {
                if (wildOnReel1 || wildOnReel2 || wildOnReel3 || wildOnReel4)
                {
                    FreezeNextAvailableReel();
                }
                yield return new WaitForSeconds(1.8f);
            }


            for (int i = 0; i < reels.Count; i++)
            {
                PandaFortuneReelScript reel = reels[i];
                if (reel == null) continue;

                SetReelDirection(reel);
                isResultReceived = false;

                bool shouldStart = true;

                if (isFreeGame)
                {
                    if (i == frozenIndexThisSpin)
                        shouldStart = false;

                    if (isFreeGameTwo && frozenReelsFreeGame2[i])
                        shouldStart = false;
                }

                if (shouldStart)
                {
                    reel.StartSpin();
                }
                else
                {
                    //Debug.Log("Free spin: skipping StartSpin for frozen reel " + i);
                }
            }

            // Only step reelsCount during free game (controls which reel freezes next)
            if (isFreeGame && !isFreeGameTwo)
            {
                reelsCount = Mathf.Max(1, reelsCount - 1);
            }
        }
        else
        {
            if (isFreeGame && !firstFreeSpin && reelsCount > 0 && reelsCount <= 4 && !isFreeGameTwo)
            {
                int a = reelsCount;
                int b = reelsCount - 1;

                if (a >= 0 && a < reels.Count && b >= 0 && b < reels.Count)
                {
                    PandaFortuneFreeGameTransitionController.Instance.ReelSwap(reels[a].transform, reels[b].transform);
                    yield return new WaitForSeconds(durationOfReelSwap);

                    var tmp = reels[a];
                    reels[a] = reels[b];
                    reels[b] = tmp;
                }
            }

            if (isFreeGameTwo)
            {
                if (wildOnReel1 || wildOnReel2 || wildOnReel3 || wildOnReel4)
                {
                    FreezeNextAvailableReel();
                }
                yield return new WaitForSeconds(1.8f);
            }

            for (int i = 0; i < reels.Count; i++)
            {
                PandaFortuneReelScript reel = reels[i];
                if (reel == null) continue;

                SetReelDirection(reel);
                isResultReceived = false;

                bool shouldStart = true;

                if (isFreeGame)
                {
                    if (i == frozenIndexThisSpin)
                        shouldStart = false;

                    if (isFreeGameTwo && frozenReelsFreeGame2[i])
                        shouldStart = false;
                }

                if (shouldStart)
                {
                    reel.StartSpin();
                }
                else
                {
                    //Debug.Log("Free spin: skipping StartSpin for frozen reel " + i);
                }

                yield return new WaitForSeconds(settings.spinSettings.ReelStartDelay);
            }

            if (isFreeGame && !isFreeGameTwo)
            {
                reelsCount = Mathf.Max(1, reelsCount - 1);
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
                //PandaFortuneFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (PandaFortuneAutoSpinController.isAutoSpinning)
            {
                PandaFortuneUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                PandaFortuneUIManager.Instance.UpdateButtons("Stop");
            }

            isSpinAgain = true;
            Debug.Log("Deepak has a spin again - 4" + isSpinAgain);
            yield break;
        }

        // Optional: small delay for visual pacing
        yield return new WaitForSeconds(0.5f);

        StopWithResult();
        Debug.Log("Deepak has a spin again - 5" + isSpinAgain);
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
            if (!PandaFortuneAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                PandaFortuneUIManager.Instance.UpdateButtons("Stop");
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

        PandaFortuneReelScript lastReel;
        lastReel = null;
        if (settings.spinSettings.endSpin == PandaFortuneSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] == null) continue;

                if (isFreeGame && i == frozenIndexThisSpin) continue;

                if (isFreeGameTwo && frozenReelsFreeGame2[i])
                    continue;

                if (reels[i] == reels[reels.Count - 1] && scatterOnReel1 && scatterOnReel3)
                {
                    lastReel = reels[i];
                    lastReel.spinSpeed = LastReelSpeed;
                    yield return new WaitForSeconds(2.5f);
                    lastReel.ApplyFinalResult(i);
                    lastReel.StopSpin();
                    continue;
                }

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

                if (isFreeGame && i == frozenIndexThisSpin) continue;

                if (isFreeGameTwo && frozenReelsFreeGame2[i])
                    continue;

                if (reels[i] == reels[reels.Count - 1] && scatterOnReel1 && scatterOnReel3)
                {
                    lastReel = reels[i];
                    lastReel.spinSpeed = LastReelSpeed;
                    yield return new WaitForSeconds(1.5f);
                    lastReel.ApplyFinalResult(i);
                    lastReel.StopSpin();
                    continue;
                }

                yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                reels[i].ApplyFinalResult(i);
                reels[i].StopSpin();
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        PandaFortuneUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        ProcessSpinResult();
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            if(isFreeGame && i == frozenIndexThisSpin) continue;

            if (isFreeGameTwo && frozenReelsFreeGame2[i])
                continue;

            reels[i].ApplyFinalResult(i);
            //reels[i].StopSpin(0f);
            reels[i].StopSpin();
        }

        PandaFortuneUIManager.Instance.SetStopInteractable(false);
    }

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

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

        if (isFreeGame && winAmount > 0)
        {
            freeSpinWinAmount += winAmount;
            PandaFortuneUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = PandaFortuneUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 1)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    PandaFortunePaylineResult result = new PandaFortunePaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
                    PandaFortunePaylineController.Instance.AddPaylineData(result);
                }
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        // Spin complete
        isSpinAgain = true;
        InSpin = false;
        Debug.Log("Deepak has a spin again - 7" + isSpinAgain);
        if (!PandaFortuneAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            if (!isFreeGameReady)
            {
                PandaFortuneUIManager.Instance.UpdateButtons("Stop");
            }
        }
        Debug.Log("Deepak has a spin again - 8" + isSpinAgain);
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

                if (isFreeGame && i == frozenIndexThisSpin)
                    continue;

                if (isFreeGameTwo && frozenReelsFreeGame2[i])
                    continue;

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


    private void SetReelDirection(PandaFortuneReelScript reel)
    {
        PandaFortuneSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == PandaFortuneSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? PandaFortuneSpinDirection.Up : PandaFortuneSpinDirection.Down;
        }

        reel.SetSpinDirection(direction);
    }

    public void ForceAllReelsToFinalPosition()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            var reel = reels[i];
            if (reel == null) continue;

            if (isFreeGame && i == frozenIndexThisSpin)
                continue;

            if (isFreeGameTwo && frozenReelsFreeGame2[i])
                continue;

            reel.ForceClampToTop();
        }
    }


    public void ForceAllReelsToTop()
    {
        foreach (PandaFortuneReelScript reel in reels)
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

                if (isFreeGame && i == frozenIndexThisSpin)
                    continue;

                if (isFreeGameTwo && frozenReelsFreeGame2[i])
                    continue;

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
        PandaFortunePaylineController.Instance.StartPayline(scatterCount);
    }

    private void SetSlotAnimationCompleted()
    {
        Debug.Log("Deepak has a spin again - 1" + isSpinAgain);
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

    public static PandaFortuneSlotResource? GetResourceById(string id)
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

    public bool IsScatter(PandaFortuneSlotType slotType)
    {
        if(slotType == PandaFortuneSlotType.Scatter)
        {
            return true;
        }
        else return false;
    }

    public bool IsWild(PandaFortuneSlotType slotType)
    {
        if(slotType == PandaFortuneSlotType.Wild)
        {
            return true;
        }
        else return false;
    }


    public void SetWildOnFrozenReel()
    {
        StartCoroutine(SpinAndSetWild(reels.Count - 1));
    }

    public IEnumerator SpinAndSetWild(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reels.Count)
            yield break;

        int index = 0;
        foreach (PandaFortuneReelScript reel in reels)
        {
            if (index == reelIndex)
            {
                SetReelDirection(reel);
                reel.StartSpin();
                yield return new WaitForSeconds(1.4f);
                reel.SetWildOnReel(reelIndex, 2);
                reel.StopSpin();
                break;
            }

            index++;
        }

        yield return null;
    }

    private void FreezeReelAsWild(int reelIndex)
    {
        if (reelIndex < 0 || reelIndex >= reels.Count)
            return;

        if (frozenReelsFreeGame2[reelIndex])
            return;

        frozenReelsFreeGame2[reelIndex] = true;

        // use the animation method as requested
        StartCoroutine(SpinAndSetWild(reelIndex));
    }

    private void FreezeNextAvailableReel()
    {
        // from right to left: 3 -> 2 -> 1 -> 0
        for (int i = 3; i >= 0; i--)
        {
            if (!frozenReelsFreeGame2[i])
            {
                FreezeReelAsWild(i);
                break;
            }
        }
    }
    #endregion
}
