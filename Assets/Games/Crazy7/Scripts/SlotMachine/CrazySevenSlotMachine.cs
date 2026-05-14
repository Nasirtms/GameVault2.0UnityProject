using Coffee.UIEffects;
using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class CrazySevenSlotMachine : BaseSlotMachine
{
    #region Variables
    public static CrazySevenSlotMachine Instance;

    [Header("Scripts References")]
    public CoinManager coinManager;
    public CrazySevenAutoSpinController crazySevenAutoSpinController;
    public CrazySevenGameTransitionController gameTransitionController;
    public bool isResultReceived;
    public uint goldAmount = 0;
    public event Action StopReelProcess;

    [Header("Win Banner")]
    [SerializeField] public TMP_Text textbar;
    [SerializeField] private GameObject winBanner;
    [SerializeField] private float bannerRiseDistance = 120f;
    [SerializeField] private float bannerRiseDuration = 1.5f;
    [SerializeField] private float bannerHoldDuration = 1.5f;
    [SerializeField] private float bannerFadeOutDuration = 0.2f;

    private RectTransform winBannerRT;
    public CanvasGroup winBannerCG;
    private Vector2 winBannerHomePos;
    private Tween winBannerTween;

    public CrazySevenGameSettings settings;
    public List<CrazySevenReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    private string coinVfxName;
    [SerializeField] private RectTransform vfxHolder;
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public CrazySevenSlotType[,] resultMatrix;

    [Header("Star Animators")]
    [SerializeField] private Animator spinAnimator;
    [SerializeField] private Animator spinAnimator1;

    [HideInInspector] public Button spinBtn;
    public UIShiny uIShiny;
    public Coroutine CounterRoutine;

    //[HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    [HideInInspector] public bool isPaylineCompleted;
    //[HideInInspector] public bool InSpin;
    private float _delayAmongReel;
    private float _acceleration;
    private float _speed;
    public float winAmount;
    private bool _isSingleSpin;
    private float _timeCounter;
    private int _reelsCount;
    private int _reelIndex;
    [HideInInspector] public List<CrazySevenSlotScript> BorderSlots = new List<CrazySevenSlotScript>();
    private bool firstAutoSpin = true;
    public SpinResult currentSpinResult;

    //[HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;
    [HideInInspector] public int scatterCount;
    [HideInInspector] public bool lastFreeSpin = false;
    public bool firstFreeSpin;
    #endregion

    #region Unity Methods
    public void InvokeStop()
    {
        StopReelProcess?.Invoke();
    }
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
    public Tween CurrentWinBannerTween { get; private set; }
    private void Start()
    {
        UpdateSlorServicesGameName();
        CrazySevenGameSettings.UpdateLayout += UpdateLayout;
        CrazySevenGameSettings.UpdateScale += UpdateScale;
        CrazySevenReelScript.OnSpinComplete += OnReelSpinComplete;
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;
        InSpin = false;
        isFreeGameReady = false;
        //isFreeGame = false;

        UpdateSettings();
        spinBtn = CrazySevenUIManager.Instance.spinButton.button;
        uIShiny = spinBtn.gameObject.GetComponent<UIShiny>();
        winAmount = 0;

        if (winBanner != null)
        {
            winBannerRT = winBanner.GetComponent<RectTransform>();
            winBannerCG = winBanner.GetComponent<CanvasGroup>();
            if (winBannerCG == null) winBannerCG = winBanner.AddComponent<CanvasGroup>();

            winBannerHomePos = winBannerRT.anchoredPosition;
            winBanner.SetActive(false);
        }
        if (textbar != null)
        {
            textbar.text = "WELCOME TO CRAZY 7!";
        }
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

        CrazySevenGameSettings.UpdateLayout -= UpdateLayout;
        CrazySevenGameSettings.UpdateScale -= UpdateScale;
        CrazySevenReelScript.OnSpinComplete -= OnReelSpinComplete;

        if (SpinResultController.Instance != null)
            SpinResultController.Instance.OnSpinResultReceived -= OnSpinResultReceived;
    }
    #endregion

    #region Machine Layout
    private void UpdateSettings()
    {
        for (var i = 0; i < reels.Count; i++)
            reels[i].Initialize(i);

        UpdateScale();
        UpdateLayout();
    }

    private void UpdateScale()
    {
        foreach (var reel in reels)
            reel.UpdateSlotScale(settings.slotScale);
    }

    private void UpdateLayout()
    {
        var lastStatus = horizontalLayout.enabled;
        horizontalLayout.enabled = true;
        horizontalLayout.spacing = settings.horizontalLayout;

        foreach (var reel in reels)
            reel.UpdateVerticalLayout(settings.verticalLayout, settings.paddingTop);

        horizontalLayout.enabled = lastStatus;
    }
    private void UpdateMatrix()
    {
        horizontalLayout.enabled = true;
        resultMatrix = new CrazySevenSlotType[reels.Count, 3];
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < reels.Count; x++)
            {
                resultMatrix[x, y] = reels[x].GetSlotType(y);
            }
        }
    }
    void UpdateSlorServicesGameName()
    {
        string sceneName = GameSlotRegistry.TrimSceneName(SceneManager.GetActiveScene().name);
        Debug.Log("Scene Name : " + sceneName);
        GameSlotRegistry.Register(sceneName, this);
        SceneManagement.UpdateCurrentSceneName(sceneName);
    }
    #endregion

    #region Spin Result Received
    [Header("Fake Scatter")]
    public int fakeScatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is SpinResult normalSpin)
        {
            currentSpinResult = normalSpin;
        }
        
        if(result.scatterCount > 2)
            scatterCount = result.scatterCount;
        else
                scatterCount = fakeScatterCount;

        //if (scatterCount > 2)


        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
            }
            CrazySevenUIManager.Instance.freeGameSpinCount += currentSpinResult.freeSpinCount;

            if (textbar != null)
            {
                StartCoroutine(ShowFreeSpinTextbar());
            }
        }
        else  if(fakeScatterCount > 2)
        {
            if (!isFreeGame)
            {
                isFreeGameReady = true;
            }
            CrazySevenUIManager.Instance.freeGameSpinCount += 3;

            if (textbar != null)
            {
                StartCoroutine(ShowFreeSpinTextbar());
            }
        }

        Debug.Log("📩 SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));
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
        if (CrazySevenAutoSpinController.isAutoSpinning)
        {
            CrazySevenUIManager.Instance.SetAutoStopInteractable(true);
        }
        else
        {
            CrazySevenUIManager.Instance.SetStopInteractable(true);
        }
    }
    private IEnumerator ShowFreeSpinTextbar()
    {
        yield return new WaitForSeconds(2f);
        textbar.text = $"YOU WON 3 FREE SPINS...";
    }
    #endregion

    #region Spin
    public override void Spin()
    {
        ToggleAnimator(true);
        if (InSpin) return;
        StopAllCoroutines();
        CrazySevenPaylineController.Instance.StopPaylines();
        CrazySevenPaylineController.Instance.ClearPaylineData();
        if (coinManager != null)
            coinManager.StopBurstCoins();

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            CrazySevenUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }
        if (isFreeGameReady)
        {

        }
        else
        {
            winAmount = 0f;
            CrazySevenUIManager.Instance.UpdateWinAmount(0f);
        }

        CrazySevenUIManager.Instance.winAnimationCompleted = true;
        crazySevenAutoSpinController.cancelRequested = false;
        if (!isFreeGame) freeSpinCount = 0;
        isStopBtnPressed = false;
        currentSpinResult = null;
        isPaylineCompleted = false;
        isSpinAgain = false;
        _reelsCount = reels.Count;
        ClearPaylines();
        BorderSlots.Clear();
        winningIndices.Clear();

        InSpin = true;
        horizontalLayout.enabled = false;
        CrazySevenUIManager.Instance.PlayMusic("Crazy_7_Slot_Machine");
        if (CrazySevenAutoSpinController.isAutoSpinning)
        {
            CrazySevenUIManager.Instance.SetAutoStopInteractable(false);
        }
        else
        {
            CrazySevenUIManager.Instance.SetStopInteractable(false);
        }

        _acceleration = settings.spinSettings.useSameAcceleration
            ? CrazySevenGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? CrazySevenGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = CrazySevenGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);
        if (settings.spinSettings.startSpin == CrazySevenSpinType.All)
        {
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.Spin(_delayAmongReel, _acceleration, _speed);
            }
        }
        else if (settings.spinSettings.startSpin == CrazySevenSpinType.Single)
        {
            reels[0].ResetShape();
            reels[0].Spin(_delayAmongReel, _acceleration, _speed);

            _timeCounter = 0;
            _reelIndex = 1;
            _isSingleSpin = true;
        }

        StartSpinWithBackendResult();

        if (InSpin)
        {
            textbar.text = "GOOD LUCK!";
        }
    }
    public void StartSpinWithBackendResult()
    {
        CounterRoutine = StartCoroutine(WaitUntilResultAndThenStop());
    }
    public void ToggleAnimator(bool flag)
    {
        spinAnimator?.SetBool("Start", flag);
        spinAnimator1?.SetBool("Start", flag);
    }
    #endregion

    #region Stop
    private void OnReelSpinComplete(int index)
    {
        if (settings.spinSettings.endSpin == CrazySevenSpinType.Single && index == reels.Count - 1)
        {
            InSpin = false;
            UpdateMatrix();
        }
        else if (settings.spinSettings.endSpin == CrazySevenSpinType.All)
        {
            InSpin = false;
            UpdateMatrix();
        }

        if (index == reels.Count - 1 && !CrazySevenAutoSpinController.isAutoSpinning)
        {
            firstAutoSpin = true;
            CrazySevenSoundManager.Instance.StopMusic("Crazy_7_Slot_Machine");
            if (isFreeGame)
            {
                return;
            }
            else
            {
                CrazySevenUIManager.Instance.UpdateButtons("Spin Stop");
            }

        }
    }
    //public float timeout;
    private IEnumerator WaitUntilResultAndThenStop()
    {
        float timeout = 12f;
        float elapsed = 0f;

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
                CrazySevenGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (CrazySevenAutoSpinController.isAutoSpinning)
            {
                CrazySevenUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                CrazySevenUIManager.Instance.UpdateButtons("Spin Stop");
            }
            ToggleAnimator(false);
            isSpinAgain = true;
            CrazySevenUIManager.Instance.StopMusic("Crazy_7_Slot_Machine");
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }
    public void StopWithResult() => Stop();
    public void Stop()
    {
        if (!InSpin) return;

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            InSpin = false;
            ClearPaylines();
            foreach (var reel in reels)
            {
                reel.ResetShape();
                reel.ForceStop();
            }
            UpdateMatrix();
            return;

        }
        if (uIShiny != null)
        {
            uIShiny.enabled = true;
        }
        if (currentSpinResult.isFreeSpin)
        {
            CrazySevenUIManager.Instance.spinButton.GetButtonComponent().interactable = false;
        }
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
        ToggleAnimator(false);
        CrazySevenUIManager.Instance.SetStopInteractable(false);
    }

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            if (isStopBtnPressed)
                break;

            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        CrazySevenUIManager.Instance.SetStopInteractable(false);

        ProcessSpinResult();
        InSpin = false;
        isSpinAgain = true;
        ToggleAnimator(false);
        if (CrazySevenAutoSpinController.isAutoSpinning)
        {
            CrazySevenUIManager.Instance.SetAutoStopInteractable(false);
        }
        else
        {
            CrazySevenUIManager.Instance.SetStopInteractable(false);
        }
        CrazySevenUIManager.Instance.StopMusic("Crazy_7_Slot_Machine");
        
    }

    public List<int> winningIndices = new();
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
        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            CrazySevenUIManager.Instance.PlayFreeGameWinAnimation(winAmount);
        }
        else if (winAmount > 0)
        {
            float betAmount = CrazySevenUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }

        if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0 || scatterCount > 2)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    CrazySevenPaylineResult result = new CrazySevenPaylineResult(payline.paylineIndex);
                    CrazySevenPaylineController.Instance.AddPaylineData(result);
                }
            }
            Invoke(nameof(ShowPaylinesWrapper), 0.5f);
        }
        else
        {
            isPaylineCompleted = true;
            if (currentSpinResult.isFreeSpin)
                //if (isFreeGameReady)
            {
                if (isFreeGame)
                {
                    //gameTransitionController.UpdateFreeSpins(3);
                    gameTransitionController.UpdateFreeSpins(currentSpinResult.freeSpinCount);
                }
                else
                {
                    FreeSpinPopupShow();
                }

            }
        }
        InSpin = false;
        isSpinAgain = true;
    }

    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    public void FreeSpinPopupShow()
    {
        gameTransitionController.StartFreeSpinPop();
        CrazySevenPaylineController.Instance.StopPaylines();
        CrazySevenPaylineController.Instance.ClearPaylineData();
        CrazySevenUIManager.Instance.UpdateButtons("Free Spin");
    }
    #endregion

    #region Public Entry
    public override void ClearPaylines()
    {

    }
    private Coroutine _waitCoinsRoutine;

    private void ShowPaylinesWrapper()
    {
        var plc = CrazySevenPaylineController.Instance;
        if (plc != null)
        {
            isPaylineCompleted = false;
            plc.ShowCollectedPaylines(scatterCount);

            if (_waitCoinsRoutine != null)
                StopCoroutine(_waitCoinsRoutine);

            if (currentSpinResult.isFreeSpin)
                //if (isFreeGameReady)
            {
                if (isFreeGame)
                {
                    //gameTransitionController.UpdateFreeSpins(3);
                    gameTransitionController.UpdateFreeSpins(currentSpinResult.freeSpinCount);
                }
                else
                {
                    FreeSpinPopupShow();
                }
            }
            _waitCoinsRoutine = StartCoroutine(WaitForPaylinesThenBurst());
        }
        else
        {
            isPaylineCompleted = true;
        }
    }
    public void TryBurst(float amount, string fallbackMsg = null)
    {
        if (amount <= 0f || isFreeGame || lastFreeSpin) return;

        if (coinManager != null) coinManager.BurstCoins();

        var ui = CrazySevenUIManager.Instance;
        if (ui != null)
        {
            ui.StopCoinCounter = false;
            ui.PlayCoinTextAnimation(amount);
        }

        string msg = (textbar != null)
                       ? textbar.text
                       : (fallbackMsg ?? $"WIN {amount:0.00}");
        PlayWinBannerOnImage(msg);
    }
    private IEnumerator WaitForPaylinesThenBurst()
    {
        yield return new WaitUntil(() => isPaylineCompleted);
        yield return null;                    // let flags/UI settle a frame
        while (InSpin) yield return null;     // avoid racing the spin flag

        _waitCoinsRoutine = null;
        if (isFreeGame) yield break;
        TryBurst(winAmount);
    }
    #endregion
    public void PlayWinBannerOnImage(string message)
    {
        // never during free spins
        if (isFreeGame) return;

        if (winBanner == null || winBannerRT == null) return;

        if (winBannerCG == null) winBannerCG = winBanner.GetComponent<CanvasGroup>();
        if (winBannerCG == null) return;

        var tmp = winBanner.GetComponentInChildren<TMP_Text>(true);
        if (tmp) tmp.text = message;

        winBanner.SetActive(true);
        winBannerCG.alpha = 0f;
        winBannerRT.anchoredPosition = winBannerHomePos + new Vector2(0f, -bannerRiseDistance);

        winBannerRT.DOKill();
        winBannerCG.DOKill();
        CurrentWinBannerTween?.Kill();

        CurrentWinBannerTween = DOTween.Sequence()
            .Append(winBannerRT.DOAnchorPosY(winBannerHomePos.y, bannerRiseDuration).SetEase(Ease.OutCubic))
            .Join(winBannerCG.DOFade(1f, 0.6f))
            .AppendInterval(bannerHoldDuration)
            .Append(winBannerCG.DOFade(0f, bannerFadeOutDuration).SetEase(Ease.Linear))
            .OnComplete(() =>
            {
                winBanner.SetActive(false);
                CurrentWinBannerTween = null;
            });
    }

    public void HideWinBanner()
    {
        if (winBanner == null) return;

        if (winBannerCG == null)
            winBannerCG = winBanner.GetComponent<CanvasGroup>();
        if (winBannerCG == null) { winBanner.SetActive(false); return; }

        if (CurrentWinBannerTween != null && CurrentWinBannerTween.IsActive())
        {
            CurrentWinBannerTween.Kill();
            CurrentWinBannerTween = null;
        }

        winBannerCG.DOKill();
        winBannerCG.DOFade(0f, 0.15f).SetEase(Ease.Linear).OnComplete(() => { winBanner.SetActive(false); });
    }


    #region Slot Result Data
    public override void StopSpinGettingError()
    {
        currentSpinResult = null;
        CrazySevenUIManager.Instance.ToggleSpinButton();
        StopWithResult();
    }

    public static CrazySevenSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.resourcesList == null)
        {
            return null;
        }

        var normalizedId = id.ToLowerInvariant();
        if (normalizedId == "lime") normalizedId = "lemon";

        foreach (var res in Instance.settings.resourcesList)
        {
            if (res.type.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }
        return null;
    }
    #endregion

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
}