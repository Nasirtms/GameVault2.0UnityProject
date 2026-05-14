using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class ImperialDiamondSlotMachine : BaseSlotMachine
{
    #region Variables

    public static ImperialDiamondSlotMachine Instance;

    [Header("Machine References")]
    [OnValueChanged("UpdateSettings")] public ImperialDiamondGameSettings settings;
    public List<ImperialDiamondReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public ImperialDiamondSlotType[,] resultMatrix;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    [Header("Animators")]
    //[SerializeField] private Animator diamondAnimator;
    [SerializeField] public Animator slotMachineLightAnimator;
    public GameObject lastReelEffect;
    private Animator lastReelAnimator;
    public Animator paylinesAnimator;
    public bool firstSpin;
    public Animator borderLights;

    // Spin Variables
    private float _timeCounter;
    private float _delayAmongReel;
    private float _acceleration;
    private float _speed;

    // Machine Variables
    private float _reelsCount;
    private int _reelIndex;

    // State Variables
    //[HideInInspector] public bool InSpin;
    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    public bool isPaylineCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool _isSingleSpin;
    private bool isSettingResult;

    // Free Spin Game
    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public bool firstFreeSpin;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public int scatterCount = 0;

    // Coins Variables
    private float winAmount;
    public Coroutine AnimateToValueCoroutine;
    public event Action StopReelProcess;
    public static List<ImperialDiamondResource> CachedRealSymbols { get; private set; }
    public static ImperialDiamondResource? CachedEmptySymbol { get; private set; }

    public bool hasSymbol = false;

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

        ImperialDiamondGameSettings.UpdateLayout += UpdateLayout;
        ImperialDiamondGameSettings.UpdateScale += UpdateScale;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;

        UpdateSettings();
        InSpin = false;
        firstSpin = true;
    }

    private void Update()
    {
        if (!_isSingleSpin) return;

        if (_timeCounter >= _delayAmongReel)
        {
            if (_reelIndex >= reels.Count)
            {
                _isSingleSpin = false;
                return;
            }
            _timeCounter = 0;
            reels[_reelIndex].ResetShape();
            reels[_reelIndex].Spin(_delayAmongReel, _acceleration, _speed);
            _reelIndex++;
        }
        else
        {
            _timeCounter += Time.deltaTime;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        ImperialDiamondGameSettings.UpdateLayout -= UpdateLayout;
        ImperialDiamondGameSettings.UpdateScale -= UpdateScale;

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

    #region Machine Layout

    private void UpdateSettings()
    {
        CachedRealSymbols = settings.resourcesList.FindAll(r => r.type != ImperialDiamondSlotType.Empty);
        CachedEmptySymbol = settings.resourcesList.Find(r => r.type == ImperialDiamondSlotType.Empty);

        for (var i = 0; i < this.reels.Count; i++)
        {
            this.reels[i].Initialize(i);
        }

        UpdateScale();
        UpdateLayout();
    }

    private void UpdateScale()
    {
        foreach (var reel in reels)
        {
            reel.UpdateSlotScale(settings.slotScale);
        }
    }

    private void UpdateLayout()
    {
        var lastStatus = horizontalLayout.enabled;
        horizontalLayout.enabled = true;
        horizontalLayout.spacing = settings.horizontalLayout;

        foreach (var reel in reels)
        {
            reel.UpdateVerticalLayout(settings.verticalLayout, settings.paddingTop);
        }
        horizontalLayout.enabled = lastStatus;
    }

    private void UpdateMatrix()
    {
        horizontalLayout.enabled = true;
        this.resultMatrix = new ImperialDiamondSlotType[reels.Count, 3];
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < reels.Count; x++)
            {
                this.resultMatrix[x, y] = reels[x].GetSlotType(y);
            }
        }
    }

    #endregion

    #region Spin Result Received
    public bool isFakeScatter;
    [SerializeField] int fakeScatterCount, fakeFreeSpinCount;

    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }

        Debug.Log("SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        spinSymbolMatrix.Clear();

        if (isFakeScatter)
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

        int reelIndex = 0;
        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            int slotIndex = 0;
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                if (slotIndex == 1 && (reelIndex == 0 || reelIndex == 1))
                {
                    var res = GetResourceById(symbol.id);

                    if (res.HasValue && isImperialDiamondSlot(res.Value.type))
                    {
                        hasSymbol = true;
                    }
                }
                slotIndex++;
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        ImperialDiamondUIManager.Instance.SetStopInteractable(true);
    }

    #endregion

    #region Spin

    public override void Spin()
    {
        if (InSpin) return;

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            ImperialDiamondUIManager.Instance.UpdateWinAmount(0f, false);
            freeSpinWinAmount = 0f;
            winAmount = 0f;
        }
        if (firstSpin)
        {
            firstSpin = false;
            borderLights.SetTrigger("Play");
        }
        StopAllCoroutines();

        ImperialDiamondUIManager.Instance.isPaylineVisible = false;
        slotMachineLightAnimator.transform.gameObject.SetActive(true);
        slotMachineLightAnimator.SetBool("On", true);

        paylinesAnimator.SetBool("On", false);
        paylinesAnimator.transform.gameObject.SetActive(false);
        ImperialDiamondPaylineController.Instance.StopPaylineLoop();
        ImperialDiamondPaylineController.Instance.ClearPaylineResults();
        ImperialDiamondUIManager.Instance.SetStopInteractable(false);

        ImperialDiamondUIManager.Instance.PlaySpinMusic("Spin");
        // Reset Variables and Functions State
        freeSpinCount = 0;
        scatterCount = 0;
        currentSpinResult = null;
        InSpin = true;
        hasSymbol = false;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isPaylineCompleted = false;
        horizontalLayout.enabled = false;
        _reelsCount = reels.Count;
        ImperialDiamondUIManager.Instance.winAnimationCompleted = true;
        ClearPaylines();

        // Getting Spin Settings
        _acceleration = settings.spinSettings.useSameAcceleration
            ? ImperialDiamondGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? ImperialDiamondGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = ImperialDiamondGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        if (settings.spinSettings.startSpin == ImperialDiamondSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == ImperialDiamondSpinType.Single)
        {
            //start spin the first reel
            reels[0].ResetShape();
            reels[0].Spin(_delayAmongReel, _acceleration, _speed);

            //init delay variables

            _timeCounter = 0;
            _reelIndex = 1;
            _isSingleSpin = true;
        }

        // Wait for backend and then stop
        StartSpinWithBackendResult();
    }

    public void StartSpinWithBackendResult()
    {
        StartCoroutine(WaitUntilResultAndThenStop());
    }

    #endregion

    #region Stop

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

            if (ImperialDiamondAutoSpinController.isAutoSpinning)
            {
                ImperialDiamondUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                ImperialDiamondUIManager.Instance.UpdateButtons("Stop");
            }

            isSpinAgain = true;
            ImperialDiamondUIManager.Instance.StopSpinMusic("Spin");
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
                reel.ResetShape();
                reel.ForceStop();
            }
            UpdateMatrix();

            return;
        }

        if (isSettingResult)
            return;

        isSettingResult = true;
        StartCoroutine(StopReelsWithResultRoutine());
    }

    #endregion

    #region Result Stop

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            reels[i].canStopReel = true;
        }
        //diamondAnimator.SetBool("On", false);
        ImperialDiamondUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();
        lastReelAnimator = lastReelEffect.GetComponent<Animator>();
        if (settings.spinSettings.endSpin == ImperialDiamondSpinType.All)
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isStopBtnPressed)
                    break;

                if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                {
                    lastReelEffect.SetActive(true);
                    lastReelAnimator.SetBool("LastReel", true);
                    yield return new WaitForSeconds(1.5f);
                    lastReelAnimator.SetBool("LastReel", false);
                    lastReelEffect.SetActive(false);
                }
                reels[i].canStopReel = true;
            }
        }
        else
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (isStopBtnPressed)
                    break;
                if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                {
                    lastReelEffect.SetActive(true);
                    lastReelAnimator.SetBool("LastReel", true);
                    yield return new WaitForSeconds(1.5f);
                    lastReelAnimator.SetBool("LastReel", false);
                    lastReelEffect.SetActive(false);
                }
                yield return new WaitForSeconds(_delayAmongReel);

                reels[i].canStopReel = true;
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        ImperialDiamondUIManager.Instance.StopSpinMusic("Spin");
        slotMachineLightAnimator.SetBool("On", false);
        slotMachineLightAnimator.transform.gameObject.SetActive(false);
        ProcessSpinResult();
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

        if (isFreeGame && firstFreeSpin)
        {
            firstFreeSpin = false;
        }

        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            ImperialDiamondUIManager.Instance.UpdateWinAmount(winAmount, true);

        }
        else if (winAmount > 0f)
        {
            paylinesAnimator.transform.gameObject.SetActive(true);
            paylinesAnimator.SetBool("On", true);
            float betAmount = ImperialDiamondUIManager.Instance.CurrentBet();
            //Invoke(nameof(UpdateGameCoin), 1f);
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                ImperialDiamondPaylineResult result = new ImperialDiamondPaylineResult(payline.paylineIndex, payline.symbol, payline.count);
                ImperialDiamondPaylineController.Instance.AddPaylineResult(result);
            }
            Invoke(nameof(ShowPaylines), 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
        }

        if(winAmount > 0f) 
            ImperialDiamondUIManager.Instance.PlaySound("Win");

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            ImperialDiamondUIManager.Instance.UpdateButtons("Transition");
        }
        else if (!ImperialDiamondAutoSpinController.isAutoSpinning && !isFreeGame && ImperialDiamondUIManager.Instance.winAnimationCompleted)
        {
            ImperialDiamondUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            ImperialDiamondUIManager.Instance.UpdateButtons("enterfreeSpin");
        }
    }

    private void ShowPaylines()
    {
        ImperialDiamondPaylineController.Instance.StartPaylineLoop();

    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region Cleanup

    public override void ClearPaylines() { }

    #endregion

    #region Helper Functions

    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        StopWithResult();
    }
    public static ImperialDiamondResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            //Debug.LogWarning("Settings or resourcesList is null.");
            return null;
        }

        var normalizedId = id.ToLowerInvariant();

        //Manually find match and return nullable
        foreach (var res in Instance.settings.resourcesList)
        {
            if (res.type.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }

        return null;
    }

    public void InvokeStop()
    {
        StopReelProcess?.Invoke();
    }

    public float GetWinAmount()
    {
        return currentSpinResult.totalWin;
    }
    public bool isImperialDiamondSlot(ImperialDiamondSlotType slotType)
    {
        if (slotType == ImperialDiamondSlotType.ImperialDiamond)
        {
            return true;
        }

        return false;
    }
    #endregion
}