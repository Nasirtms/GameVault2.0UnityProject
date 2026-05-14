using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class StinkinRichSlotMachine : BaseSlotMachine
{
    #region Variables

    public static StinkinRichSlotMachine Instance;

    [Header("Machine References")]
    public StinkinRichGameSettings settings;
    public List<StinkinRichReelScript> reels;
    [SerializeField] private StinkinRichBetController betController;
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

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public bool isBonusGame;
    [HideInInspector] public bool canShowTrashMultiplier;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public List<StinkinRichSlotScript> trashSlots;
    public List<int> keysToRichesPaylineIndexes;

    [SerializeField] public bool testMode;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    private bool trash1;
    private bool trash2;
    private bool trash3;
    public int trashMultiplier;
    public int trashNotMultiplier1;
    public int trashNotMultiplier2;

    private bool hasShowenSkunkWinAnimation;
    public bool hasShowenTrashAnimation;
    public bool canClickTrashSlots;
    public bool hasShowenTrashMultipliers;


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

        trash1 = false;
        trash2 = false;
        trash3 = false;


        if (currentSpinResult.scatterCount >= 3)
            scatterCount = currentSpinResult.scatterCount;
        else
            scatterCount = fakeScatterCount;


        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }

        if(testMode)
        {
            isFreeGameReady = true;
            scatterCount = 3;
            freeSpinCount = 20;
        }

        if (currentSpinResult.isBonusGame)
        {
            isBonusGame = true;
            canClickTrashSlots = false;
            StinkinRichUIManager.Instance.waitForTrashForCashEnd = true;
            trashMultiplier = currentSpinResult.StickinRichMultiplier;
            trashNotMultiplier1 = currentSpinResult.StickinRichcurrencyValue1;
            trashNotMultiplier2 = currentSpinResult.StickinRichcurrencyValue2;
        }


        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        int reel = 0;
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                var res = GetResourceById(symbol.id);

                if (reel == 2)
                {
                    if (IsTrash(res.Value.slotType)) trash1 = true; 
                }
                if (reel == 3)
                {
                    if (IsTrash(res.Value.slotType)) trash2 = true;
                }
                if (reel == 4)
                {
                    if (IsTrash(res.Value.slotType)) trash3 = true;
                }
            }
            reel = reel + 1;
            spinSymbolMatrix.Add(symbols);
        }
        reel = 0;

        StinkinRichUIManager.Instance.SetStopInteractable(true);
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
            StinkinRichUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        StinkinRichUIManager.Instance.PlaySpinMusic("Spin");

        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        StinkinRichPaylineController.Instance.ClearPaylineData();
        StinkinRichPaylineController.Instance.StopPaylineDisplay();
        StinkinRichUIManager.Instance.winAnimationCompleted = true;
        StinkinRichUIManager.Instance.SetStopInteractable(false);
        if (hasShowenSkunkWinAnimation)
        {
            StinkinRichUIManager.Instance.PlaySkunkIdleAnimations();
            hasShowenSkunkWinAnimation = false;
        }

        if(hasShowenTrashAnimation)
        {
            StinkinRichPaylineController.Instance.StopTrashAnimations();
            hasShowenTrashAnimation = false;
        }

        if (settings.spinSettings.startSpin == StinkinRichSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                StinkinRichReelScript reel = reels[i];
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
                StinkinRichReelScript reel = reels[i];
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
            else if (StinkinRichAutoSpinController.isAutoSpinning)
            {
                StinkinRichUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                StinkinRichUIManager.Instance.UpdateButtons("Idle");
            }
            StinkinRichUIManager.Instance.StopSpinMusic("Spin");
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
            if (!StinkinRichAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                StinkinRichUIManager.Instance.UpdateButtons("Idle");
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

        if (settings.spinSettings.endSpin == StinkinRichSpinMode.SpinAll)
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

        StinkinRichUIManager.Instance.StopSpinMusic("Spin");
        StinkinRichUIManager.Instance.SetStopInteractable(false);

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

        StinkinRichUIManager.Instance.SetStopInteractable(false);
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
            Debug.Log("Deepak 1: " + winAmount);
        }


        Debug.Log("Deepak 2: " + winAmount);
        if (isFreeGame && winAmount > 0)
        {
            freeSpinWinAmount += winAmount;
            StinkinRichUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            Debug.Log("Deepak 3: " + winAmount);
            if (isBonusGame)
            {
                Debug.Log("Deepak 4: " + winAmount);
                ShowTrashMultiplier();
            }
            else
            {
                Debug.Log("Deepak 5: " + winAmount);
                float betAmount = StinkinRichUIManager.Instance.CurrentBet();
                GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
                //Invoke(nameof(UpdateGameCoin), 1f);
            }
        }

        if (isBonusGame && isFreeGame)
        {
            Debug.Log("Deepak 6: " + winAmount);
            ShowTrashMultiplier();
            Debug.Log("Deepak 7: " + winAmount);
        }


        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 1)
        {
            keysToRichesPaylineIndexes.Clear();
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    StinkinRichPaylineResult result = new StinkinRichPaylineResult(payline.paylineIndex, payline.count, payline.winAmount, payline.symbol);
                    StinkinRichPaylineController.Instance.AddPaylineData(result);

                    Debug.Log("Deepak Is Here 1 " + isFreeGameReady);
                    if (isFreeGameReady)
                    {
                        Debug.Log("Deepak Is Here 2 " + result.symbol);
                        if (result.symbol == "KeysToRiches")
                        {
                            Debug.Log("Deepak Is Here 3 " + result.symbol);
                            keysToRichesPaylineIndexes.Add(result.paylineNumber);
                            Debug.Log("Deepak Is Here 4" + result.paylineNumber);
                        }
                        Debug.Log("Deepak Is Here 5");
                    }
                    Debug.Log("Deepak Is Here 6");

                }
            }
            //if (testMode)
            //{
            //    keysToRichesPaylineIndexes = new List<int>() { 5, 1, 2, 4 };
            //}

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }
        if (winAmount > 0)
        {
            StinkinRichUIManager.Instance.PlaySound("Win");
        }
        // Spin complete
        isSpinAgain = true;
        InSpin = false;
        //Debug.Log("Deepak has a spin again - 7" + isSpinAgain);
        if (!StinkinRichAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            if (!isFreeGameReady)
            {
                StinkinRichUIManager.Instance.UpdateButtons("Idle");
            }
        }

        if(isBonusGame)
        {
            StinkinRichUIManager.Instance.spinButton.GetButtonComponent().interactable = false;
            StinkinRichUIManager.Instance.autoButton.GetButtonComponent().interactable = false;
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


    private void SetReelDirection(StinkinRichReelScript reel)
    {
        StinkinRichSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == StinkinRichSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? StinkinRichSpinDirection.Up : StinkinRichSpinDirection.Down;
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
        foreach (StinkinRichReelScript reel in reels)
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
        StinkinRichUIManager.Instance.PlaySkunkWinAnimations();
        hasShowenSkunkWinAnimation = true;
        StinkinRichPaylineController.Instance.StartPayline(scatterCount);
    }

    private void SetSlotAnimationCompleted()
    {
        isSpinAgain = true;
        isSlotAnimationCompleted = true;

        if(trash1 && trash2 && trash3)
        {
            canShowTrashMultiplier = true;
        }
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

    public static StinkinRichSlotResource? GetResourceById(string id)
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

    public bool IsTrash(StinkinRichSlotType slotType)
    {
        if (slotType == StinkinRichSlotType.TrashForCash)
        {
            return true;
        }
        else return false;
    }
    public bool IsKeys(StinkinRichSlotType slotType)
    {
        if (slotType == StinkinRichSlotType.KeysToRiches)
        {
            return true;
        }
        else return false;
    }

    public void ShowTrashMultiplier()
    {
        if (isBonusGame)
        {
            hasShowenTrashMultipliers = false;
            if (isFreeGame)
            {
                StinkinRichUIManager.Instance.stopButton.GetButtonComponent().interactable = false;
            }
            else
            {
                StinkinRichUIManager.Instance.spinButton.GetButtonComponent().interactable = false;
                StinkinRichUIManager.Instance.autoButton.GetButtonComponent().interactable = false;
            }
            StinkinRichUIManager.Instance.PlayTrashForCashStart();
        }
    }

    public void ShowWinAfterTrash()
    {
        StartCoroutine(WinAfterTrashAnimationRoutine());
    }

    private IEnumerator WinAfterTrashAnimationRoutine()
    {
        yield return new WaitUntil(() => hasShowenTrashMultipliers);
        yield return new WaitForSeconds(2f);
        if (!isFreeGame)
        {
            float betAmount = StinkinRichUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }
        StinkinRichUIManager.Instance.PlayTrashForCashEnd();
    }

    #endregion
}
