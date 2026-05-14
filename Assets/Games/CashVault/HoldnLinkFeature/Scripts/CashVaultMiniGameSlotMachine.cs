using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CashVaultMiniGameSlotMachine : MonoBehaviour
{
    #region Variables
    public static CashVaultMiniGameSlotMachine Instance;

    [Header("Machine References")]
    public CashVaultMiniGameSettings settings;
    public List<CashVaultMiniGameReelScript> reels;
    [ShowInInspector][ReadOnly] public CashVaultSpinResult currentSpinResult;
    public List<List<SymbolData>> spinSymbolMatrix = new();

    public bool InSpin = false;
    public bool isStopBtnPressed = false;
    public bool isSpinAgain = false;
    public bool isResultReceived;
    private bool isSettingResult;
    public bool firstReSpin;

    // Coroutines
    public Coroutine spinCoroutine;
    public Coroutine stopCoroutine;

    [Header("MiniGame Locking")]
    public bool fakeLockedReels = false;
    public List<int> lockedReelIndexes = new List<int>();
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null) return;
        Instance = this;
    }

    private void Start()
    {
        SpinResultController.Instance.OnSpinResultReceived += OnSpinResultReceived;
        Initialize();
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

    #region Machine Settings
    //private void Initialize()
    //{
    //    foreach (var reel in reels)
    //    {
    //        if (reel != null)
    //        {
    //            reel.Initialize();
    //        }
    //    }
    //}
    private void Initialize()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            if (reels[i] != null)
            {
                reels[i].SetReelIndex(i);
                reels[i].Initialize();
            }
        }
    }
    #endregion

    #region Spin Result Receive
    [Header("Fake Scatter")]
    public int fakeScatterCount;

    public int scatterCount;
    private void OnSpinResultReceived(BaseSpinResult result)
    {
        if (result is CashVaultSpinResult normalSpin)
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

        //bool newLockAdded = false;

        //for (int reelIndex = 0; reelIndex < spinSymbolMatrix.Count; reelIndex++)
        //{
        //    // already locked → skip
        //    if (lockedReelIndexes.Contains(reelIndex))
        //        continue;

        //    var symbols = spinSymbolMatrix[reelIndex];

        //    // check 3 visible rows
        //    for (int row = 0; row < 3; row++)
        //    {
        //        if (symbols[row].id.ToLowerInvariant() == "sphere")
        //        {
        //            lockedReelIndexes.Add(reelIndex);
        //            newLockAdded = true;
        //            break;
        //        }
        //    }
        //}
        CashVaultUIManager.Instance.SetStopInteractable(true);
    }
    #endregion

    #region Spin
    //public void Spin()
    //{
    //    if (spinCoroutine != null)
    //    {
    //        StopCoroutine(spinCoroutine);
    //    }

    //    if (stopCoroutine != null)
    //    {
    //        StopCoroutine(stopCoroutine);
    //    }

    //    spinCoroutine = StartCoroutine(StartSpin());
    //}

    public IEnumerator StartSpin()
    {
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;

        if (!CashVaultSlotMachine.Instance.isMiniGame || firstReSpin)
        {
            CashVaultSlotMachine.Instance.isMiniGameReady = false;
        }
        CashVaultUIManager.Instance.winAnimationCompleted = true;
        //CashVaultUIManager.Instance.PlaySpinMusic("Spin");
        CashVaultUIManager.Instance.SetStopInteractable(false);

        //foreach (CashVaultMiniGameReelScript reel in reels)
        //{
        //    if (reel != null)
        //    {
        //        SetReelDirection(reel);
        //        isResultReceived = false;
        //        reel.StartSpin();
        //    }
        //}
        for (int i = 0; i < reels.Count; i++)
        {
            var reel = reels[i];
            if (reel == null) continue;

            if (fakeLockedReels && lockedReelIndexes.Contains(i))
            {
                reel.ForceStopSpin();
                continue; // DO NOT spin locked reels
            }
                
            SetReelDirection(reel);
            reel.StartSpin();
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
        float timeout = 10f;
        float elapsed = 0f;

        while ((currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentSpinResult == null || currentSpinResult.reels == null || currentSpinResult.reels.Count == 0)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            StopWithResult();
            
            CashVaultUIManager.Instance.UpdateButtons("Stop");
            //CashVaultUIManager.Instance.StopSpinMusic("Spin");
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

            if (!CashVaultSlotMachine.Instance.isMiniGame)
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

        if (settings.spinSettings.endSpin == CashVaultMiniGameSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (fakeLockedReels && lockedReelIndexes.Contains(i))
                    continue;

                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
            }
            //CashVaultUIManager.Instance.PlaySound("Stop");
        }
        else // SpinOneByOne mode
        {
            for (int i = 0; i < reels.Count; i++)
            {
                if (fakeLockedReels && lockedReelIndexes.Contains(i))
                    continue;

                if (reels[i] != null)
                {
                    if (isStopBtnPressed)
                        break;

                    yield return new WaitForSeconds(settings.spinSettings.ReelStopDelay);

                    reels[i].ApplyFinalResult(i);
                    reels[i].StopSpin();
                }
                //CashVaultUIManager.Instance.PlaySound("ReelStop");
            }
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        CashVaultUIManager.Instance.SetStopInteractable(false);
        yield return StartCoroutine(WaitForAllReelsToStop());

        //ForceAllReelsToFinalPosition();

        //CashVaultUIManager.Instance.StopSpinMusic("Spin");
        ProcessSpinResult();
    }

    public void StopButtonPressed()
    {
        for (int i = 0; i < reels.Count; i++)
        {
            if (fakeLockedReels && lockedReelIndexes.Contains(i))
                continue;

            reels[i].ApplyFinalResult(i);
            reels[i].StopSpin();
        }
    }

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            return;
        }

        InSpin = false;
        isSpinAgain = true;

        if (CashVaultSlotMachine.Instance.isMiniGame)
        {
            CashVaultUIManager.Instance.UpdateButtons("Free Spin");
        }
        //CashVaultUIManager.Instance.UpdateButtons("Stop"); 
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
                if (fakeLockedReels && lockedReelIndexes.Contains(i))
                    continue;

            if (reels[i] != null && reels[i].IsSpinning)
                {
                    allStopped = false;
                    break;
                }
            }

            if (!allStopped)
                yield return null;
        }
    }
    //private IEnumerator WaitForAllReelsToStop()
    //{
    //    bool allStopped = false;

    //    while (!allStopped)
    //    {
    //        allStopped = true;

    //        foreach (var reel in reels)
    //        {
    //            if (reel != null && reel.IsSpinning)
    //            {
    //                allStopped = false;
    //                break;
    //            }
    //        }

    //        if (!allStopped)
    //        {
    //            yield return null;
    //        }
    //    }
    //}
    private IEnumerator WaitForAllReelsToBeSpinning()
    {
        bool allSpinning = false;
        while (!allSpinning)
        {
            allSpinning = true;

            for (int i = 0; i < reels.Count; i++)
            {
                if (fakeLockedReels && lockedReelIndexes.Contains(i))
                    continue;

                if (reels[i] != null && !reels[i].IsSpinning)
                {
                    allSpinning = false;
                    break;
                }
            }

            if (!allSpinning)
                yield return new WaitForSeconds(0.05f);
        }
    }
    //private IEnumerator WaitForAllReelsToBeSpinning()
    //{
    //    bool allSpinning = false;
    //    while (!allSpinning)
    //    {
    //        allSpinning = true;
    //        foreach (CashVaultMiniGameReelScript reel in reels)
    //        {
    //            if (reel != null && !reel.IsSpinning)
    //            {
    //                allSpinning = false;
    //                break;
    //            }
    //        }
    //        if (!allSpinning)
    //        {
    //            yield return new WaitForSeconds(0.1f);
    //        }
    //    }
    //}
    private void SetReelDirection(CashVaultMiniGameReelScript reel)
    {
        CashVaultMiniGameSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == CashVaultMiniGameSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? CashVaultMiniGameSpinDirection.Up : CashVaultMiniGameSpinDirection.Down;
        }

        reel.SetSpinDirection(direction);
    }

    //public void ForceAllReelsToFinalPosition()
    //{
    //    foreach (var reel in reels)
    //    {
    //        if (reel != null)
    //        {
    //            // Always clamp to top position regardless of spin direction
    //            reel.ForceClampToTop();
    //        }
    //    }
    //}

    //public void ForceAllReelsToTop()
    //{
    //    foreach (CashVaultMiniGameReelScript reel in reels)
    //    {
    //        if (reel != null)
    //        {
    //            reel.ForceClampToTop();
    //        }
    //    }
    //}
    public static CashVaultMiniGameSlotResource? GetResourceById(string id)
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

    public bool isSphereSlot(CashVaultMiniGameSlotType slotType)
    {
        if (slotType == CashVaultMiniGameSlotType.Sphere)
        {
            return true;
        }

        return false;
    }
    #endregion

    #region Fake Mini Game Lock 
    public bool IsReelLocked(int reelIndex)
    {
        return lockedReelIndexes.Contains(reelIndex);
    }

    public bool AreAllReelsLocked()
    {
        return lockedReelIndexes.Count >= reels.Count;
    }
    #endregion
}