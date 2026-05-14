using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IrishPotLuckSlotMachine : BaseSlotMachine
{
    #region Variables
    public static IrishPotLuckSlotMachine Instance;

    [Header("Machine References")]
    public IrishPotLuckGameSettings settings;
    public List<IrishPotLuckReelScript> reels;
    [SerializeField] private IrishPotLuckBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    public bool isFreeGameReady;
    public int freeSpinCount;
    public float freeSpinWinAmount;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public IrishPotLuckSlotType[,] resultMatrix;

    [Header("Multiplier Animation")]
    [SerializeField] private Transform multiplierImage;
    [SerializeField] private TMP_Text multiplierText;

    [SerializeField] private float multiplierScaleDuration = 0.5f;

    public int[] mainGameMultipliers = { 1, 2, 3, 5 };
    public int[] freeGameMultipliers = { 2, 4, 7, 15 };

    private Sequence multiplierScaleSequence;
    private Vector3 originalMultiplierScale;

    public int fakeMultiplier;
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
    public bool isFakeFreeGame;
    public int fakeFreeSpinCount;
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

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
        }
        if (isFakeFreeGame)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = fakeFreeSpinCount;
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

        IrishPotLuckUIManager.Instance.SetStopInteractable(true);
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
            IrishPotLuckUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        // Reset Variables
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        IrishPotLuckUIManager.Instance.winAnimationCompleted = true;
        IrishPotLuckUIManager.Instance.StopCurrentSFX();
        //IrishPotLuckUIManager.Instance.PlaySound("Spin");
        StartMultiplierAnimation();
        ClearPaylines();
        IrishPotLuckPaylineController.Instance.StopPaylines();
        IrishPotLuckPaylineController.Instance.ClearPaylineData();
        IrishPotLuckUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == IrishPotLuckSpinMode.SpinAll)
        {
            foreach (IrishPotLuckReelScript reel in reels)
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
            foreach (IrishPotLuckReelScript reel in reels)
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
                IrishPotLuckFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (IrishPotLuckAutoSpinController.isAutoSpinning)
            {
                IrishPotLuckUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                IrishPotLuckUIManager.Instance.UpdateButtons("Stop");
            }
            //IrishPotLuckUIManager.Instance.StopSpinMusic("Spin");
            StopMultiplierAnimation(GetDefaultMultiplier());
            IrishPotLuckUIManager.Instance.StopCurrentSFX();
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
            if (!IrishPotLuckAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                IrishPotLuckUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == IrishPotLuckSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            //IrishPotLuckUIManager.Instance.PlaySound("ReelStop"); 
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
                    reels[i].StopSpin();
                }
                //IrishPotLuckUIManager.Instance.PlaySound("ReelStop");
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        IrishPotLuckUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();

        //IrishPotLuckUIManager.Instance.StopSpinMusic("Spin");
        IrishPotLuckUIManager.Instance.StopCurrentSFX();
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

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
            return;

        winAmount = forcedWin ? forcedPrize : currentSpinResult.totalWin;
        StopMultiplierAnimation(GetFinalMultiplier());

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            IrishPotLuckUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            float betAmount = IrishPotLuckUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }
        bool hasPaylines = currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0;

        if (hasPaylines)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                IrishPotLuckPaylineResult result = new IrishPotLuckPaylineResult(
                    payline.paylineIndex,
                    payline.count,
                    payline.winAmount
                );

                IrishPotLuckPaylineController.Instance.AddPaylineData(result);
            }
        }

        if (hasPaylines || isFreeGameReady)
        {
            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }
        //if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0))
        //{
        //    foreach (var payline in currentSpinResult.paylineWins)
        //    {
        //        IrishPotLuckPaylineResult result = new IrishPotLuckPaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
        //        IrishPotLuckPaylineController.Instance.AddPaylineData(result);
        //    }

        //    ShowPaylines();
        //}
        //else
        //{
        //    SetSlotAnimationCompleted();

        //}
        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            IrishPotLuckUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!IrishPotLuckAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            IrishPotLuckUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            IrishPotLuckUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        Debug.Log("LovKumar 5");
        IrishPotLuckPaylineController.Instance.StartPayline(isFreeGameReady);
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

    private void SetReelDirection(IrishPotLuckReelScript reel)
    {
        IrishPotLuckSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == IrishPotLuckSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? IrishPotLuckSpinDirection.Up : IrishPotLuckSpinDirection.Down;
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
        foreach (IrishPotLuckReelScript reel in reels)
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
            foreach (IrishPotLuckReelScript reel in reels)
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
    public static IrishPotLuckSlotResource? GetResourceById(string id)
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
    #endregion

    #region Multiplier Animation

    private void StartMultiplierAnimation()
    {
        if (multiplierImage == null)
            return;

        if (originalMultiplierScale == Vector3.zero)
            originalMultiplierScale = multiplierImage.localScale;

        StopMultiplierAnimation(GetDefaultMultiplier(), false);

        int[] values = isFreeGame ? freeGameMultipliers : mainGameMultipliers;
        int index = 0;

        multiplierImage.localScale = originalMultiplierScale;

        multiplierScaleSequence = DOTween.Sequence();

        multiplierScaleSequence
            .Append(multiplierImage.DOScaleX(0f, multiplierScaleDuration).SetEase(Ease.InOutSine))
            .AppendCallback(() =>
            {
                if (multiplierText != null && values != null && values.Length > 0)
                {
                    multiplierText.text = "X" + values[index];

                    index++;
                    if (index >= values.Length)
                        index = 0;
                }
            })
            .Append(multiplierImage.DOScaleX(originalMultiplierScale.x, multiplierScaleDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1, LoopType.Restart);
    }

    private void StopMultiplierAnimation(int finalMultiplier, bool setFinalText = true)
    {
        if (multiplierScaleSequence != null)
        {
            multiplierScaleSequence.Kill();
            multiplierScaleSequence = null;
        }

        if (multiplierImage != null && originalMultiplierScale != Vector3.zero)
        {
            multiplierImage.localScale = originalMultiplierScale;
        }

        if (setFinalText && multiplierText != null)
        {
            multiplierText.text = "X" + finalMultiplier;
        }
    }

    private int GetDefaultMultiplier()
    {
        return isFreeGame ? 2 : 1;
    }

    private int GetFinalMultiplier()
    {
        if (fakeMultiplier > 0)
            return fakeMultiplier;

        return GetDefaultMultiplier();
    }

    #endregion
}