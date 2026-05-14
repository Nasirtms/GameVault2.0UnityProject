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

public class StickyPiggySlotMachine : BaseSlotMachine
{
    #region Variables
    public static StickyPiggySlotMachine Instance;

    [Header("Machine References")]
    public StickyPiggyGameSettings settings;
    public List<StickyPiggyReelScript> reels;
    [SerializeField] private StickyPiggyBetController betController;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    //public bool InSpin = false;
    //public bool isStopBtnPressed = false;
    public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    //public bool isFreeGame;
    public bool isFreeGameReady;
    public int freeSpinCount;
    public float freeSpinWinAmount;
    public int freeSpinMultiplier;

    // Win
    private float winAmount = 0f;

    [SerializeField] private TMP_Text winText;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Result")]
    [ShowInInspector][Sirenix.OdinInspector.ReadOnly] public StickyPiggySlotType[,] resultMatrix;
    public bool hasSymbol = false;

    [Header("Wild Prefab")]
    public List<StickyPiggySlotScript> wildSlots;
    public int wildCount;
    public bool isWildCollectionCompleted;
    [SerializeField] private GameObject wildX2_Slot;
    [SerializeField] private GameObject wildX3_Slot;
    public List<WildCollectInstance> activeWilds = new List<WildCollectInstance>();

    public List<int> freeSpinTriggerReelIndexes = new List<int>();
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
    public int scatterCount;
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

        if (currentSpinResult.isFreeSpin)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            scatterCount = currentSpinResult.scatterCount;
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
        int reelIndex = 0;
        //bool hasFreeSpinOnReel0 = false;
        //bool hasFreeSpinOnReel2 = false;

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            bool bonusFoundOnReel = false;
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
                var res = GetResourceById(symbol.id);
                if (res.HasValue && isBonusSlot(res.Value.slotType))
                {
                    bonusFoundOnReel = true;

                    //if (reelIndex == 0)
                    //    hasFreeSpinOnReel0 = true;

                    //if (reelIndex == 2)
                    //    hasFreeSpinOnReel2 = true;
                }
            }
            if (bonusFoundOnReel)
            {
                freeSpinTriggerReelIndexes.Add(reelIndex);
            }
            spinSymbolMatrix.Add(symbols);
            reelIndex++;
        }
        //hasSymbol = hasFreeSpinOnReel0 && hasFreeSpinOnReel2;

        StickyPiggyUIManager.Instance.SetStopInteractable(true);
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
            StickyPiggyUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        // Reset Variables
        if (!isFreeGame)
        {
            wildCount = 0;
            wildSlots.Clear();
        }
        hasSymbol = false;
        scatterCount = 0;
        freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;
        StickyPiggyUIManager.Instance.winAnimationCompleted = true;
        StickyPiggyUIManager.Instance.StopCurrentSFX();
        StickyPiggyUIManager.Instance.PlaySpinMusic("Spin");
        freeSpinTriggerReelIndexes.Clear();
        ClearPaylines();
        StickyPiggyPaylineController.Instance.StopPaylines();
        StickyPiggyPaylineController.Instance.ClearPaylineData();
        StickyPiggyUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == StickyPiggySpinMode.SpinAll)
        {
            foreach (StickyPiggyReelScript reel in reels)
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
            foreach (StickyPiggyReelScript reel in reels)
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
                StickyPiggyFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (StickyPiggyAutoSpinController.isAutoSpinning)
            {
                StickyPiggyUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                StickyPiggyUIManager.Instance.UpdateButtons("Stop");
            }
            StickyPiggyUIManager.Instance.StopSpinMusic("Spin");
            StickyPiggyUIManager.Instance.StopCurrentSFX();
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
            if (!StickyPiggyAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                StickyPiggyUIManager.Instance.UpdateButtons("Stop");
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

        if (settings.spinSettings.endSpin == StickyPiggySpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    //if (reels[i] == reels[reels.Count - 1] && hasSymbol)
                    //{
                    //    yield return new WaitForSeconds(1.5f);
                    //    reels[i].ApplyFinalResult(i);
                    //    reels[i].StopSpin();
                    //    continue;
                    //}

                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            StickyPiggyUIManager.Instance.PlaySound("ReelStop");
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
                    //    yield return new WaitForSeconds(1.5f);
                    //    reels[i].ApplyFinalResult(i);
                    //    reels[i].StopSpin();
                    //    continue;
                    //}
                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
                StickyPiggyUIManager.Instance.PlaySound("ReelStop");
            }
        }
        StickyPiggyUIManager.Instance.StopSpinMusic("Spin");
        if (isStopBtnPressed)
            StopButtonPressed();

        //StickyPiggyUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());
        ForceAllReelsToFinalPosition();
        //StickyPiggyUIManager.Instance.StopCurrentSFX();
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
            StickyPiggyUIManager.Instance.UpdateWinAmount(winAmount, true);
        }
        else if (winAmount > 0)
        {
            StickyPiggyUIManager.Instance.PlaySound("Win");
            float betAmount = StickyPiggyUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
        }
        if(winAmount > 0)
            StickyPiggyUIManager.Instance.PlaySound("Win");

        for (int x = 0; x < reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == StickyPiggySlotType.PiggyWildX2 || slot.slotType == StickyPiggySlotType.PiggyWildX3)
                {
                    wildCount++;
                    slot.reelIndex = x;
                    slot.slotIndex = y;
                    wildSlots.Add(slot);
                }
            }
        }
        HandleWildSymbols(wildSlots, isFreeGame);
        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount > 2)
        {
            foreach (var payline in currentSpinResult.paylineWins)
            {
                StickyPiggyPaylineResult result = new StickyPiggyPaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
                StickyPiggyPaylineController.Instance.AddPaylineData(result);
            }
            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();

        }
        InSpin = false;
        isSpinAgain = true;

        if (isFreeGameReady)
        {
            StickyPiggyUIManager.Instance.UpdateButtons("Transition Start");
        }
        else if (!StickyPiggyAutoSpinController.isAutoSpinning && !isFreeGame)
        {
            StickyPiggyUIManager.Instance.UpdateButtons("Stop");
        }
        else if (isFreeGame)
        {
            StickyPiggyUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    private void ShowPaylines()
    {
        StickyPiggyPaylineController.Instance.StartPayline(scatterCount);
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

    private void SetReelDirection(StickyPiggyReelScript reel)
    {
        StickyPiggySpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == StickyPiggySpinDirection.Random)
        {
            direction = Random.value > 0.5f ? StickyPiggySpinDirection.Up : StickyPiggySpinDirection.Down;
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
        foreach (StickyPiggyReelScript reel in reels)
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
            foreach (StickyPiggyReelScript reel in reels)
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
    public static StickyPiggySlotResource? GetResourceById(string id)
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
    public bool isBonusSlot(StickyPiggySlotType slotType)
    {
        if (slotType == StickyPiggySlotType.Bonus)
        {
            return true;
        }

        return false;
    }
    #endregion

    #region Wild Collection in FreeSpin

    public GameObject newSlot;
    public List<GameObject> slots_new = new List<GameObject>();
    public List<LockedWildSlot> lockedSlots = new List<LockedWildSlot>(); 
    public void HandleWildSymbols(List<StickyPiggySlotScript> wildSlots, bool isFreeGame)
    {
        if (!isFreeGame)
        {
            ClearWildInstances();
            return;
        }

        for (int i = 0; i < wildSlots.Count; i++)
        {
            StickyPiggySlotScript slot = wildSlots[i];
            if (slot == null) continue;

            WildCollectInstance existing = activeWilds.Find(c => c.slot == slot);

            if (existing != null && existing.instance != null && existing.instance.activeSelf)
                continue;

            if (existing != null)
            {
                if (existing.instance != null)
                    Destroy(existing.instance);

                activeWilds.Remove(existing);
            }

            GameObject spawnedSlot = null;
            string animBool = "";

            if (slot.slotType == StickyPiggySlotType.PiggyWildX2)
            {
                spawnedSlot = Instantiate(wildX2_Slot);
                animBool = "PiggyX2";
            }
            else if (slot.slotType == StickyPiggySlotType.PiggyWildX3)
            {
                spawnedSlot = Instantiate(wildX3_Slot);
                animBool = "PiggyX3";
            }

            if (spawnedSlot == null)
                continue;

            slot.isLocked = true;

            lockedSlots.Add(new LockedWildSlot
            {
                reelIndex = slot.reelIndex,
                slotIndex = slot.slotIndex
            });

            spawnedSlot.transform.SetParent(slot.transform.parent);

            Vector3 targetPos = slot.transform.position;
            targetPos.y += 0.1f;

            spawnedSlot.transform.position = targetPos;
            spawnedSlot.transform.SetParent(null);

            slots_new.Add(slot.gameObject);

            Animator spawnedAnimator = spawnedSlot.GetComponent<Animator>();

            activeWilds.Add(new WildCollectInstance
            {
                slot = slot,
                instance = spawnedSlot,
                multiplier = slot.slotType == StickyPiggySlotType.PiggyWildX3 ? 3 : 2,
                animator = spawnedAnimator,
                animationBool = animBool
            });
        }
    }
    public void SetWildInstanceAnimation(StickyPiggySlotScript slot, bool play)
    {
        if (!isFreeGame)
            return;

        WildCollectInstance wild = activeWilds.Find(x => x.slot == slot);

        if (wild == null || wild.animator == null || string.IsNullOrEmpty(wild.animationBool))
            return;

        wild.animator.enabled = true;
        wild.animator.SetBool(wild.animationBool, play);
    }
    public void ClearWildInstances()
    {
        lockedSlots.Clear();

        foreach (var cc in activeWilds)
        {
            if (cc.instance != null)
                Destroy(cc.instance);
        }
        foreach (var cc in slots_new)
        {
            cc.gameObject.SetActive(true);
        }
        slots_new.Clear();
        activeWilds.Clear();
    }
    #endregion
}

#region Wild Prefabs

[System.Serializable]
public class WildCollectInstance
{
    public StickyPiggySlotScript slot;
    public GameObject instance;
    public int multiplier;

    public Animator animator;
    public string animationBool;
}

[System.Serializable]
public class LockedWildSlot
{
    public int reelIndex;
    public int slotIndex;
}
#endregion