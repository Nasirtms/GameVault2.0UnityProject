using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WildXTrioSlotMachine : BaseSlotMachine
{
    #region Variables
    public static WildXTrioSlotMachine Instance;

    [Header("Machine References")]
    public WildXTrioGameSettings settings;
    public List<WildXTrioReelScript> reels;
    [SerializeField] private WildXTrioBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    [HideInInspector] public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;

    // Win
    private float winAmount = 0f;
    public SpriteRenderer winImage;
    public float winFadeDuration = 0.4f;
    private Tween winImageTween;

    public Animator winAnimator;
    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public WildXTrioSlotType[,] resultMatrix;

    public static List<WildXTrioSlotResource> CachedRealSymbols { get; private set; }
    public static WildXTrioSlotResource? CachedEmptySymbol { get; private set; }
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

        winImageTween?.Kill();
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
        CachedRealSymbols = settings.slotResources.FindAll(r => r.slotType != WildXTrioSlotType.Empty);
        CachedEmptySymbol = settings.slotResources.Find(r => r.slotType == WildXTrioSlotType.Empty);
        
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

        WildXTrioUIManager.Instance.SetStopInteractable(true);
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
        StopWinImageAnimation();
        WildXTrioUIManager.Instance.UpdateWinAmount(0f, false);
        WildXTrioUIManager.Instance.winAnimationCompleted = true;
        winAmount = 0f;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;

        winAnimator.SetBool("Play", false);
        winAnimator.enabled = false;

        WildXTrioUIManager.Instance.PlaySpinMusic("Spin");
        ClearPaylines();

        WildXTrioPaylineController.Instance.StopPaylineLoop();
        WildXTrioPaylineController.Instance.ClearPaylineResults();
        WildXTrioUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == WildXTrioSpinMode.SpinAll)
        {
            foreach (WildXTrioReelScript reel in reels)
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
            foreach (WildXTrioReelScript reel in reels)
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
            StopWithResult(); 

            if (WildXTrioAutoSpinController.isAutoSpinning)
            {
                WildXTrioUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                WildXTrioUIManager.Instance.UpdateButtons("Stop");
            }
            WildXTrioUIManager.Instance.StopSpinMusic("Spin");
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
            if (!WildXTrioAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                WildXTrioUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == WildXTrioSpinMode.SpinAll)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            WildXTrioUIManager.Instance.PlaySound("Stop");
        }
        else // SpinOneByOne mode
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
            //WildXTrioUIManager.Instance.PlaySound("Stop");
        }
        WildXTrioUIManager.Instance.StopSpinMusic("Spin");
        if (isStopBtnPressed)
            StopButtonPressed();

        //WildXTrioUIManager.Instance.SetStopInteractable(false);
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

    [Header("Forced Prize")]
    public bool forcedWin;
    public float forcedPrize;
    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
            return;

        winAmount = forcedWin ? forcedPrize : currentSpinResult.totalWin;

        if (winAmount > 0f)
        {
            PlayWinImageAnimation();

            float betAmount = WildXTrioUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }
        else
        {
            StopWinImageAnimation();
        }

        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0))
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                WildXTrioPaylineResult result = new WildXTrioPaylineResult(payline.paylineIndex, payline.symbol, payline.count);
                WildXTrioPaylineController.Instance.AddPaylineResult(result);
            }

            ShowPaylines();
        }
        else
        {
            isSlotAnimationCompleted = true;
        }

        if (winAmount > 0f)
            WildXTrioUIManager.Instance.PlaySound("Win");

        InSpin = false;
        isSpinAgain = true;

        if (!WildXTrioAutoSpinController.isAutoSpinning && !isFreeGame && WildXTrioUIManager.Instance.winAnimationCompleted)
        {
            WildXTrioUIManager.Instance.UpdateButtons("Stop");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void PlayWinImageAnimation()
    {
        winImage.gameObject.SetActive(true);
        if (winImage == null) return;

        winImageTween?.Kill();

        Color c = winImage.color;
        c.a = 0f;
        winImage.color = c;

        winImageTween = winImage
            .DOFade(1f, winFadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void StopWinImageAnimation()
    {
        winImage.gameObject.SetActive(false);
        winImageTween?.Kill();
        winImageTween = null;

        if (winImage == null) return;

        Color c = winImage.color;
        c.a = 0f;
        winImage.color = c;
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

    private void SetReelDirection(WildXTrioReelScript reel)
    {
        WildXTrioSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == WildXTrioSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? WildXTrioSpinDirection.Up : WildXTrioSpinDirection.Down;
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
        foreach (WildXTrioReelScript reel in reels)
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
            foreach (WildXTrioReelScript reel in reels)
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
    public static WildXTrioSlotResource? GetResourceById(string id)
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
        WildXTrioPaylineController.Instance.StartPaylineLoop();
        winAnimator.enabled = true;
        winAnimator.SetBool("Play", true);  
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
}