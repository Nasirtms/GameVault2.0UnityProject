using DG.Tweening;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaharaRichesSlotMachine : BaseSlotMachine
{
    #region Variables
    public static SaharaRichesSlotMachine Instance;

    [Header("Machine References")]
    public SaharaRichesGameSettings settings;
    public List<SaharaRichesReelScript> reels;
    [SerializeField] private SaharaRichesBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    [HideInInspector] public bool InSpin = false;
    [HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    [HideInInspector] public bool isFreeGame;
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public int freeSpinCount;
    [HideInInspector] public float freeSpinWinAmount;


    [HideInInspector] public List<SaharaRichesSlotScript> cashCollectSlots;
    public bool isDiamondCollectionCompleted = true;

    public int cashCollectCount;
    public bool isFreeSpinCollectionCompleted;
    public bool isCoinCollectionCompleted = true;

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public SaharaRichesSlotType[,] resultMatrix;

    [Header("Cash Collect Prefab Settings")]
    [SerializeField] private GameObject cashCollect_SlotParent;
    [SerializeField] private GameObject cashCollect_Slot;
    private List<CashCollectInstance> activeCashCollects = new List<CashCollectInstance>();

    public bool hasSymbol = false;
    [SerializeField] public GameObject reelSymbolEffect;
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
        isCoinCollectionCompleted = true;
        isDiamondCollectionCompleted = true;
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (SpinResultController.Instance != null)
            SpinResultController.Instance.OnSpinResultReceived -= OnSpinResultReceived;
    }
    public void Reset()
    {
        SaharaRichesPaylineController.Instance.Target.SetActive(false);
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.Reset();
            }
        }

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
        //if (currentSpinResult.scatterCount >= 3)
        //    scatterCount = currentSpinResult.scatterCount;
        //else
        //    scatterCount = fakeScatterCount;


        //if (currentSpinResult.isFreeSpin || scatterCount >= 3)
        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            freeSpinCount = currentSpinResult.freeSpinCount;
            if (fakeScatterCount >= 3)
            {
                freeSpinCount = 3;
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
                if (reelIndex < 4)
                {
                    var res = GetResourceById(symbol.id);
                    //Debug.Log("LovKumar " + res.Value.slotType);
                    if (res.HasValue)
                    {
                        if (isCCSlot(res.Value.slotType) || isDiamondSlot(res.Value.slotType) || isFreeSpinSlot(res.Value.slotType))
                        {
                            hasSymbol = true;
                        }
                    }
                }
                
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }

        SaharaRichesUIManager.Instance.SetStopInteractable(true);
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
        CleanCashCollects();
        foreach (var reel in reels)
        {
            if (reel != null && reel.slots != null && reel.slots.Count > 0)
            {
                var firstSlot = reel.slots[0];
                if (firstSlot != null)
                {
                    var ccText = firstSlot.CC_Text;
                    if (ccText != null)
                        ccText.enabled = false;
                }
            }
        }

        if (!isFreeGame || firstFreeSpin)
        {
            isFreeGameReady = false;
            SaharaRichesUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
            
        }
        if (!isFreeGame)
        {
            ClearCashCollectInstances();
        }

        foreach (var reel in slots_new)
        {
            reel.gameObject.SetActive(true);
        }

        SaharaRichesPaylineController.Instance.Target_Text.text = "0.00";
        CC_Count = 0;
        FreeSpin_Count = 0;
        Diamond_Count = 0;
        cashCollectCount = 0;
        SaharaRichesPaylineController.Instance.Target.SetActive(false);
        reelSymbolEffect.SetActive(false);
        freeSpinCount = 0;
        hasSymbol = false;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        isCoinCollectionCompleted = true;
        isDiamondCollectionCompleted = true;
        isFreeSpinCollectionCompleted = true;
        SaharaRichesJackpotAnimator.Instance.isJackpotCompleted = true;
        SaharaRichesUIManager.Instance.winAnimationCompleted = true;
        cashCollectSlots.Clear();

        ClearPaylines();
        SaharaRichesPaylineController.Instance.StopPaylines();
        SaharaRichesPaylineController.Instance.ClearPaylineData();
        SaharaRichesUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == SaharaRichesSpinMode.SpinAll)
        {
            foreach (SaharaRichesReelScript reel in reels)
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
            foreach (SaharaRichesReelScript reel in reels)
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
                SaharaRichesFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (SaharaRichesAutoSpinController.isAutoSpinning)
            {
                SaharaRichesUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                SaharaRichesUIManager.Instance.UpdateButtons("Stop");
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
            if (!SaharaRichesAutoSpinController.isAutoSpinning && !isFreeGame && !currentSpinResult.isBonusGame)
            {
                SaharaRichesUIManager.Instance.UpdateButtons("Stop");
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
        //if (counterText == null)
        //{
        //    lockedSlots.Clear();
        //}

        SaharaRichesReelScript slowReel;
        slowReel = null;
        if (settings.spinSettings.endSpin == SaharaRichesSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {

                    if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    {
                        slowReel = reels[i];
                        slowReel.spinSpeed = 8;
                        yield return new WaitForSeconds(2);
                        slowReel.ApplyFinalResult(i);
                        slowReel.StopSpin();
                        continue;
                    }
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            //SaharaRichesUIManager.Instance.PlaySound("Stop");
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

                    if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    {
                        PlayReelSymbolEffect();
                        slowReel = reels[i];
                        slowReel.spinSpeed = 8;
                        yield return new WaitForSeconds(2);
                        slowReel.ApplyFinalResult(i);
                        slowReel.StopSpin();
                        continue;
                    }
                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            //SaharaRichesUIManager.Instance.PlaySound("Stop");
        }
        
        if (isStopBtnPressed)
            StopButtonPressed();

        SaharaRichesUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());

        ForceAllReelsToFinalPosition();
        foreach (var reel in slots_new)
        {
            reel.gameObject.SetActive(false);
        }
        ProcessSpinResult();
    }
    private void PlayReelSymbolEffect()
    {
        reelSymbolEffect.SetActive(true);
        var spriteRenderer = reelSymbolEffect.GetComponent<SpriteRenderer>();

        if(spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0;
            spriteRenderer.color = c;

            spriteRenderer.DOFade(1, 0.5f)
                .OnComplete(() =>
                {
                    DOVirtual.DelayedCall(3.0f, () =>  // stays visible 2s
                    {
                        spriteRenderer.DOFade(0, 0.6f).OnComplete(() =>
                        {
                            reelSymbolEffect.SetActive(false);
                        });
                    });
                });
        }
    }

    public int CC_Count = 0;
    public int FreeSpin_Count = 0;
    public int Diamond_Count = 0;
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
    private Coroutine freespinloop;
    public bool jackpotgame;
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

        for (int x = 0; x < reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = reels[x].slots[y + 1];
                if (slot == null) continue;

                if (isCCSlot(slot.slotType))
                {
                    CC_Count++;
                }
                if (isFreeSpinSlot(slot.slotType))
                {
                    FreeSpin_Count++;
                }
                if (isDiamondSlot(slot.slotType))
                {
                    Diamond_Count++;
                }
                if (slot.slotType == SaharaRichesSlotType.CashCollect)
                {
                    cashCollectCount++;
                    slot.reelIndex = x;
                    slot.slotIndex = y;
                    cashCollectSlots.Add(slot);
                    //wildWorldPos.Add(slot.textBox.transform.position);
                }
            }
        }
        HandleCashCollectSymbols(cashCollectSlots, isFreeGame);
        if (cashCollectCount > 0)
        {
            isCoinCollectionCompleted = false;
            isDiamondCollectionCompleted = false;
            isFreeSpinCollectionCompleted = false;
        }


        if (isFreeGame && winAmount > 0)
        {
            firstFreeSpin = false;
            freeSpinWinAmount += winAmount;
            SaharaRichesUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = SaharaRichesUIManager.Instance.CurrentBet();

            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        SaharaRichesUIManager.Instance.StopSpinMusic("Spin");
        SaharaRichesPaylineController.Instance.isJackotGame = currentSpinResult.isBonusGame;
        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || cashCollectCount > 0 )
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                SaharaRichesPaylineResult result = new SaharaRichesPaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
                SaharaRichesPaylineController.Instance.AddPaylineData(result);
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        bool hasJackpot = SaharaRichesPaylineController.Instance.isJackotGame;
        bool hasFreeGame = currentSpinResult.isFreeSpin;

        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady || hasFreeGame)
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!SaharaRichesAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            SaharaRichesUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        Debug.Log("GameBetServices.Instance: " + (GameBetServices.Instance == null));
        Debug.Log("currentSpinResult: " + (currentSpinResult == null));

        if (currentSpinResult != null)
            Debug.Log("newBalance: " + currentSpinResult.newBalance);

        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }


    public void jackpotGamePlay()
    {
        jackpotgame = true;
        SaharaRichesUIManager.Instance.UpdateButtons("Transition Start");
        ShowJackpotGame();
    }
    public void ShowJackpotGame()
    {
        if (SaharaRichesJackpotAnimator.Instance != null)
        {
            SaharaRichesJackpotAnimator.Instance.StartJackpot();
            return;
        }
        else
        {
            Debug.LogError("❌ SaharaRichesJackpotAnimator.Instance is null! Make sure it's active in the scene.");
        }
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

    private void SetReelDirection(SaharaRichesReelScript reel)
    {
        SaharaRichesSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == SaharaRichesSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? SaharaRichesSpinDirection.Up : SaharaRichesSpinDirection.Down;
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
        foreach (SaharaRichesReelScript reel in reels)
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
            foreach (SaharaRichesReelScript reel in reels)
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
    public static SaharaRichesSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            Debug.LogWarning("Settings or resourcesList is null.");
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
        SaharaRichesPaylineController.Instance.StartPayline(freeSpinCount);

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

    public bool isFreeSpinSlot(SaharaRichesSlotType slotType)
    {
        if (slotType == SaharaRichesSlotType.FreeSpin3 ||
            slotType == SaharaRichesSlotType.FreeSpin4 ||
            slotType == SaharaRichesSlotType.FreeSpin5 ||
            slotType == SaharaRichesSlotType.FreeSpin10)

        {
            return true;
        }

        return false;
    }
    public bool isCCSlot(SaharaRichesSlotType slotType)
    {
        if (slotType == SaharaRichesSlotType.CC)
        {
            return true;
        }

        return false;
    }
    public bool isDiamondSlot(SaharaRichesSlotType slotType)
    {
        if (slotType == SaharaRichesSlotType.DaimondJackpot)
        {
            return true;
        }

        return false;
    }
    #endregion

    #region FreeSpin CashCollect

    public GameObject newSlot;
    public List<GameObject> slots_new = new List<GameObject>();
    public List<LockedSlot> lockedSlots = new List<LockedSlot>();
    public TMP_Text counterText;
    public void HandleCashCollectSymbols(List<SaharaRichesSlotScript> cashCollectSlots, bool isFreeGame)
    {
        if (!isFreeGame)
        {
            ClearCashCollectInstances();
            return;
        }

        for (int i = 0; i < cashCollectSlots.Count; i++)
        {
            SaharaRichesSlotScript slot = cashCollectSlots[i];
            if (slot == null) continue;

            // Check if already exists for this exact slot
            CashCollectInstance existing = activeCashCollects.Find(c => c.slot == slot);

            if (existing != null && existing.counter > 0 && existing.instance != null && existing.instance.activeSelf)
                continue;

            if (existing != null)
            {
                if (existing.instance != null)
                    Destroy(existing.instance);
                activeCashCollects.Remove(existing);
            }

            newSlot = Instantiate(cashCollect_Slot);
            slot.isLocked = true;
            //slot.isSlotExist = true;
            int reelIndex = slot.reelIndex;
            int slotIndex = slot.slotIndex;

            lockedSlots.Add(new LockedSlot
            {
                reelIndex = reelIndex,
                slotIndex = slotIndex
            });

            newSlot.transform.parent = slot.transform.parent;
            newSlot.transform.position = slot.transform.position;
            newSlot.transform.parent = null;

            slots_new.Add(slot.gameObject);

            // Initialize counter
            int initialCount = 3;
            counterText = newSlot.GetComponentInChildren<TextMeshPro>();
            
            if (counterText != null)
                counterText.text = initialCount.ToString();

            //// Track it
            activeCashCollects.Add(new CashCollectInstance
            {
                slot = slot,
                instance = newSlot,
                counter = initialCount,
                counterText = counterText
            });
        }

        foreach (var cc in activeCashCollects)
        {
            if (cc.instance == null) continue;

            cc.counter--;

            if (cc.counter <= 0)
            {
                cc.expireNextSpin = true;
                if (cc.counterText != null)
                {
                    cc.counterText.text = "";
                }
                foreach (var i in slots_new)
                {
                    i.gameObject.SetActive(true);
                }
                lockedSlots.Clear();
            }
            else if (cc.counterText != null)
            {
                cc.counterText.text = cc.counter.ToString();
            }
        }
    }

    private void CleanCashCollects()
    { 
        for (int i = activeCashCollects.Count - 1; i >= 0; i--)
        {
            var cc = activeCashCollects[i];
            if (cc == null || cc.instance == null || cc.expireNextSpin)
            {
                if (cc?.instance != null)
                    Destroy(cc.instance);

                activeCashCollects.RemoveAt(i);
            }
        }

        // If you hid base slot GOs when showing CC overlays, re-enable them here.
        foreach (var slot in slots_new)
        {
            if (slot != null) slot.SetActive(true);
        }
        slots_new.Clear();
    }
    public void ClearCashCollectInstances()
    {
        lockedSlots.Clear();
        
        foreach (var cc in activeCashCollects)
        {
            if (cc.instance != null)
                Destroy(cc.instance);
        }
        foreach (var cc in slots_new)
        {
            cc.gameObject.SetActive(true);
        }
        slots_new.Clear();
        activeCashCollects.Clear();
    }
    #endregion
}
[System.Serializable]
public class CashCollectInstance
{
    public SaharaRichesSlotScript slot;
    public GameObject instance;
    public int counter;
    public TMP_Text counterText;
    public bool expireNextSpin;

}

[System.Serializable]
public class LockedSlot
{
    public int reelIndex;
    public int slotIndex;
}