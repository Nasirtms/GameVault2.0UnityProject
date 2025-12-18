using DG.Tweening;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DayOfDeadSlotMachine : BaseSlotMachine
{
    #region Variables

    public static DayOfDeadSlotMachine Instance;

    [Header("Machine References")]
    public DayOfDeadGameSettings settings;
    public List<DayOfDeadReelScript> reels;

    [Header("Spin Result - Parsed JSON")]
    [ShowInInspector][ReadOnly] public SpinResult currentSpinResult;

    // State Variables
    public bool InSpin = false;
    [HideInInspector] public bool isStopBtnPressed = false;
    [HideInInspector] public bool isSpinAgain = false;
    public bool isSlotAnimationCompleted;
    [HideInInspector] public bool isResultReceived;
    private bool isSettingResult;
    public bool firstFreeSpin;

    // Free Spin Game
    [HideInInspector] public bool isFreeGameReady;
    [HideInInspector] public float freeSpinWinAmount;

    public List<int> freeSpinWildReel = new List<int>();

    // Win
    private float winAmount = 0f;

    // Coroutines
    private Coroutine spinCoroutine;
    private Coroutine stopCoroutine;

    [Header("Expanding Wild")]
    public GameObject expandingWildPrefab;

    // Expanding wild detection
    public List<DayOfDeadSlotScript> expandingWildSlots = new List<DayOfDeadSlotScript>();
    public int expandingWildCount = 0;

    private int expandingWildReelIndex = -1;
    private int expandingWildRowIndex = -1;

    public bool isRespinActive = false;
    public bool isReSpin = false;  
    public int remainingRespins = 0;
    [SerializeField] private bool showLog;
    private Coroutine respinCoroutine;
    public float ReSpinWinAmount;
    public bool isWildCompleted;
    public bool firstReSpin;

    //FreeGame Wild
    public int freeSpinWildCount = 0;
    public List<DayOfDeadSlotScript> freeSpinWildSlots = new List<DayOfDeadSlotScript>();

    public List<FreeSpinWalkingWild> activeWilds = new List<FreeSpinWalkingWild>();
    public int currentFreeSpinMultiplier = 0;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        // Creating Instance
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
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
        // Clearing Instance    
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
        if (currentSpinResult.scatterCount >= 3)
            scatterCount = currentSpinResult.scatterCount;
        else
            scatterCount = fakeScatterCount;

        if (currentSpinResult.isFreeSpin || scatterCount >= 3)
        {
            if (!isFreeGame)
                isFreeGameReady = true;

            //freeSpinCount = currentSpinResult.freeSpinCount;
            //freeSpinCount = 3;
        }

        string response = JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented);
        Debug.Log("Scatter Count: " + currentSpinResult.scatterCount + "\nPaylines Count: " + currentSpinResult.paylineWins.Count);
        Debug.Log("SpinResult (parsed):\n" + response);

       

        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<SymbolData> symbols = new List<SymbolData>();
            
            foreach (var symbol in reelList)
            {
                if (symbol.isBonus)
                {
                    Debug.Log("Nasir_ Get isBonusTrue");
                }
                symbols.Add(symbol);
            }
            spinSymbolMatrix.Add(symbols);
        }
        DayOfDeadUIManager.Instance.SetStopInteractable(true);
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
        addExpendingWildIndexIntoList();
    }

    private IEnumerator StartSpin()
    {
        if (!isReSpin && !isRespinActive )
        {
            ReSpinWinAmount = 0;
            ClearExpandingWilds();
        }
        if ((!isFreeGame && !isRespinActive) || firstFreeSpin )
        {
            isFreeGameReady = false;
            DayOfDeadUIManager.Instance.UpdateWinAmount(0f);
            freeSpinWinAmount = 0;
            winAmount = 0f;
        }

        //freeSpinCount = 0;
        currentSpinResult = null;
        InSpin = true;
        isSpinAgain = false;
        isSettingResult = false;
        isStopBtnPressed = false;
        isSlotAnimationCompleted = false;

        freeSpinWildSlots.Clear();
        spinSymbolMatrix.Clear();
        DayOfDeadUIManager.Instance.winAnimationCompleted = true;
        DayOfDeadPaylineController.Instance.StopPaylines();
        DayOfDeadPaylineController.Instance.ClearPaylineData();
        DayOfDeadUIManager.Instance.SetStopInteractable(false);

        if (settings.spinSettings.startSpin == DayOfDeadSpinMode.SpinAll)
        {
            foreach (DayOfDeadReelScript reel in reels)
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
            foreach (DayOfDeadReelScript reel in reels)
            {
                if (reel != null)
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
                DayOfDeadFreeGameTransitionController.Instance.NetworkErrorFreeSpin();
            }
            else if (DayOfDeadAutoSpinController.isAutoSpinning)
            {
                DayOfDeadUIManager.Instance.CancelAutoSpin();
            }
            else
            {
                DayOfDeadUIManager.Instance.UpdateButtons("Single Stop");
            }

            isSpinAgain = true;
            yield break;
        }

        yield return new WaitForSeconds(0.5f);

        StopWithResult();
    }
    void addExpendingWildIndexIntoList()
    {
        freeSpinWildReel.Clear();
        foreach (var ew in activeExpandingWilds)
        {
            freeSpinWildReel.Add(ew.reelIndex);
        }
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
            if (!DayOfDeadAutoSpinController.isAutoSpinning && !isFreeGame && !isRespinActive)
            {
                DayOfDeadUIManager.Instance.UpdateButtons("Single Stop");
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

        // Stop reels based on end spin mode
        if (settings.spinSettings.endSpin == DayOfDeadSpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].ApplyFinalResult(i);
                    //reels[i].StopSpin(0f); // No delay for simultaneous stop
                    reels[i].StopSpin(); // No delay for simultaneous stop
                }
            }
            //DayOfDeadUIManager.Instance.PlaySound("Stop");
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
            }
            //DayOfDeadUIManager.Instance.PlaySound("Stop");
        }

        if (isStopBtnPressed)
            StopButtonPressed();

        DayOfDeadUIManager.Instance.SetStopInteractable(false);

        yield return StartCoroutine(WaitForAllReelsToStop());

        // Force all reels to final position based on direction
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

        DayOfDeadUIManager.Instance.SetStopInteractable(false);
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

        if (isFreeGame && !isRespinActive && winAmount > 0)
        {
            firstFreeSpin = false;
            winAmount = winAmount * currentFreeSpinMultiplier;
            freeSpinWinAmount += winAmount;
            DayOfDeadUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (isReSpin && winAmount > 0)
        {
            ReSpinWinAmount += winAmount;
            DayOfDeadUIManager.Instance.UpdateWinAmount(winAmount, true);
            //Invoke(nameof(UpdateGameCoin), 1f);
        }
        else if (winAmount > 0)
        {
            float betAmount = DayOfDeadUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, winAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);
        }
        if (isFreeGame)
        {
            freeSpinWildCount = 0;

            for (int x = 0; x < reels.Count; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var slot = reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (slot.slotType == DayOfDeadSlotType.FreeGameWild)
                    {
                        freeSpinWildCount++;

                        slot.reelIndex = x;
                        slot.slotIndex = y + 1;

                        freeSpinWildSlots.Add(slot);
                        DayOfDeadFreeSpinController.Instance.HandleFreeSpinWild(slot);
                    }
                }
            }
        }
        if ((currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0) || scatterCount >= 3)
        {
            if (currentSpinResult.paylineWins != null && currentSpinResult.paylineWins.Count > 0)
            {
                foreach (var payline in currentSpinResult.paylineWins)
                {
                    DayOfDeadPaylineResult result = new DayOfDeadPaylineResult(payline.paylineIndex, payline.count, payline.winAmount);
                    DayOfDeadPaylineController.Instance.AddPaylineData(result);
                }
            }

            ShowPaylines();
        }
        else
        {
            SetSlotAnimationCompleted();
        }

        if (!isReSpin && !isFreeGameReady && !isFreeGame)
        {
            FindExpandingWildSlots();
            StartCoroutine(HandleExpandingWild());
        }
        // Spin complete
        InSpin = false;
        isSpinAgain = true;
        if(DayOfDeadAutoSpinController.isAutoSpinning && isReSpin)
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Auto Respin");
        }
        if (!DayOfDeadAutoSpinController.isAutoSpinning && !isFreeGame && !isRespinActive)
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Single Stop");
        }
        if (!DayOfDeadAutoSpinController.isAutoSpinning && isRespinActive)
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Free Spin");
        }
    }
    public void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResult.newBalance);
    }
    #endregion

    #region Expanding Wild 

    public List<ExpandingWildInstance> activeExpandingWilds = new List<ExpandingWildInstance>();
    public List<ExpandingWildLockedSlot> lockedSlots = new List<ExpandingWildLockedSlot>();
    public GameObject newSlot;
    public Animator newSlotAnimator;
    public HashSet<int> alreadyExistReel = new HashSet<int>();
    public bool isSlotAnimationDone;
    private void FindExpandingWildSlots()
    {
        expandingWildSlots.Clear();
        expandingWildCount = 0;

        for (int x = 0; x < reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == DayOfDeadSlotType.ExpandingWild)
                {
                    //slot.PlayAnimation();
                    expandingWildCount++;

                    slot.reelIndex = x;
                    slot.slotIndex = y + 1;

                    expandingWildSlots.Add(slot);
                    //slot.StopAnimation();
                }
            }
        }
    }
    private IEnumerator HandleExpandingWild()
    {
        yield return new WaitUntil(() => DayOfDeadUIManager.Instance.winAnimationCompleted);
        if (isFreeGameReady || isFreeGame)
            yield break;

        if (expandingWildSlots.Count < 0)
        {
            if (!DayOfDeadAutoSpinController.isAutoSpinning && !isFreeGame)
            {
                DayOfDeadUIManager.Instance.UpdateButtons("Single Stop");
            }
            yield break;
        }
        if (isRespinActive)
        {
            AddNewExpandingWildsDuringRespins();
            yield break;
        }

        alreadyExistReel.Clear();
        activeExpandingWilds.Clear();
        lockedSlots.Clear();

        int maxReelIndex = -1;

        foreach (var slot in expandingWildSlots)
        {
            if (slot == null) continue;

            slot.isLocked = true;

            int reelIndex = slot.reelIndex;
            int slotIndex = slot.slotIndex;

            if (alreadyExistReel.Contains(reelIndex))
                continue;

            isRespinActive = true;
            alreadyExistReel.Add(reelIndex);
            slot.isLocked = true;
            GameObject newSlot = Instantiate(expandingWildPrefab);
            GameObject newSlotPrefab = newSlot.transform.GetChild(11).gameObject;
            newSlotPrefab.SetActive(true);

            newSlot.transform.parent = slot.transform.parent;
            newSlot.transform.position = slot.transform.position;
            newSlot.transform.localScale = slot.transform.localScale;

            newSlot.transform.parent = null;

            DayOfDeadAnimationController animCtrl = newSlot.GetComponent<DayOfDeadAnimationController>();

            ExpandingWildInstance instance = new ExpandingWildInstance
            {
                slot = slot,
                instance = newSlot,
                reelIndex = reelIndex,
                slotIndex = slotIndex,
                remainingRespins = 0,
                animController = animCtrl
            };
            lockedSlots.Add(new ExpandingWildLockedSlot
            {
                reelIndex = reelIndex,
                slotIndex = slotIndex,
                wildInstance = instance
            });
            Sequence wildSeq = DOTween.Sequence();

            wildSeq.AppendInterval(0.5f).AppendCallback(() =>
                {
                    instance.animController.PlaySmallToBigOnce();
                })

                .AppendCallback(() =>
                {
                    MoveWildToRow3(instance);
                })

                .AppendInterval(1f)

                .AppendCallback(() =>
                {
                    var child11 = newSlot.transform.GetChild(11).gameObject;
                    var child14 = newSlot.transform.GetChild(14).gameObject;
                    child11.SetActive(false);
                    child14.SetActive(true);
                });
            //instance.animController.PlaySmallToBigOnce();
            //MoveWildToRow3(instance);

            //DG.Tweening.DOVirtual.DelayedCall(0.7f, () =>
            //{
            //    var child11 = newSlot.transform.GetChild(11).gameObject;
            //    var child14 = newSlot.transform.GetChild(14).gameObject;

            //    child11.SetActive(false);
            //    child14.SetActive(true);
            //});

            activeExpandingWilds.Add(instance);
            addExpendingWildIndexIntoList();
            if (reelIndex > maxReelIndex)
                maxReelIndex = reelIndex;
        }

        remainingRespins = Mathf.Max(0, maxReelIndex);

        yield return new WaitForSeconds(1f);
        if (remainingRespins > 0 && activeExpandingWilds.Count > 0)
        {
            if (DayOfDeadAutoSpinController.isAutoSpinning)
            {
                DayOfDeadUIManager.Instance.UpdateButtons("Auto Respin");
            }
            else
            {
                DayOfDeadUIManager.Instance.UpdateButtons("Free Spin");
            }
            
            isWildCompleted = false;
            StartRespinLoop();
        }
        else
        {
            isRespinActive = false;
        }
    }
    private int AddNewExpandingWildsDuringRespins()
    {
        int newMaxReelIndex = -1;

        HashSet<int> reelsWithWild = new HashSet<int>();
        foreach (var i in activeExpandingWilds)
            reelsWithWild.Add(i.reelIndex);
        isSlotAnimationDone = false;
        foreach (var slot in expandingWildSlots)
        {
            if (slot == null) continue;

            int reelIndex = slot.reelIndex;
            int slotIndex = slot.slotIndex;
            if (reelsWithWild.Contains(reelIndex))
                continue;

            bool alreadyTracked = activeExpandingWilds.Exists(e => e.slot == slot);
            if (alreadyTracked)
                continue;

            slot.isLocked = true;

            GameObject newSlot = Instantiate(expandingWildPrefab);
            GameObject newSlotPrefab = newSlot.transform.GetChild(11).gameObject;
            newSlotPrefab.SetActive(true);

            newSlot.transform.parent = slot.transform.parent;
            newSlot.transform.position = slot.transform.position;
            newSlot.transform.localScale = slot.transform.localScale;

            newSlot.transform.parent = null;

            DayOfDeadAnimationController animCtrl = newSlot.GetComponent<DayOfDeadAnimationController>();
            ExpandingWildInstance instance = new ExpandingWildInstance
            {
                slot = slot,
                instance = newSlot,
                reelIndex = reelIndex,
                slotIndex = slotIndex,
                remainingRespins = 0,
                animController = animCtrl
            };
            lockedSlots.Add(new ExpandingWildLockedSlot
            {
                reelIndex = reelIndex,
                slotIndex = slotIndex,
                wildInstance = instance
            });
            Sequence wildSeq = DOTween.Sequence();

            wildSeq.AppendInterval(0.5f).AppendCallback(() =>
                {
                    instance.animController.PlaySmallToBigOnce();
                })

                .AppendCallback(() =>
                {
                    MoveWildToRow3(instance);
                })

                .AppendInterval(1f)

                .AppendCallback(() =>
                {
                    var child11 = newSlot.transform.GetChild(11).gameObject;
                    var child14 = newSlot.transform.GetChild(14).gameObject;
                    child11.SetActive(false);
                    child14.SetActive(true);
                });

            activeExpandingWilds.Add(instance);

            reelsWithWild.Add(reelIndex);
            addExpendingWildIndexIntoList();
            if (reelIndex > newMaxReelIndex)
                newMaxReelIndex = reelIndex;

        }
        isSlotAnimationDone = true;
        return newMaxReelIndex;
    }
    private void MoveWildToRow3(ExpandingWildInstance instance)
    {
        if (instance == null || instance.instance == null)
            return;

        int reelIndex = instance.reelIndex;
        if (reelIndex < 0 || reelIndex >= reels.Count)
            return;

        int targetSlotIndex = 4;

        var reel = reels[reelIndex];
        if (targetSlotIndex < 0 || targetSlotIndex >= reel.slots.Count)
            return;

        DayOfDeadSlotScript targetSlot = reel.slots[targetSlotIndex];

        Transform inst = instance.instance.transform;
        Vector3 startPos = inst.position;
        Vector3 targetPos = targetSlot.transform.position;
        Vector3 finalPos = new Vector3(startPos.x, -2.39f, startPos.z);

        inst.DOMoveY(finalPos.y, 0.6f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                instance.slotIndex = targetSlotIndex;
                instance.slot = targetSlot;
            });
    }

    private void StartRespinLoop()
    {
        if (isFreeGame || isFreeGameReady)
            return;

        if (respinCoroutine != null)
        {
            StopCoroutine(respinCoroutine);
        }
        firstReSpin = true;
        isFreeGame = true;
        isReSpin = true;
        respinCoroutine = StartCoroutine(RespinLoop());
    }
    private IEnumerator RespinLoop()
    {
        yield return new WaitUntil(() => isSlotAnimationCompleted);
        yield return new WaitForSeconds(1f);

        while (isRespinActive && remainingRespins > 0 && activeExpandingWilds.Count > 0)
        {
            if (firstReSpin)
            {
                firstReSpin = false;
            }
            else
            {
                yield return new WaitForSeconds(1f); 
            }
            int baseRemaining = Mathf.Max(remainingRespins - 1, 0);

            FindExpandingWildSlots();
            int newMaxReelIndex = AddNewExpandingWildsDuringRespins();

            int nextRemainingRespins;
            int highestReelIndex = -1;
            foreach (var w in activeExpandingWilds)
            {
                if (w.reelIndex > highestReelIndex)
                    highestReelIndex = w.reelIndex;
            }

            nextRemainingRespins = Mathf.Max(baseRemaining, highestReelIndex);

            if (nextRemainingRespins == 0)
            {
                int highestActive = -1;
                foreach (var w in activeExpandingWilds)
                {
                    if (w.reelIndex > highestActive)
                        highestActive = w.reelIndex;
                }

                if (highestActive == 0)
                {
                    remainingRespins = 0;
                    isRespinActive = false;
                    isReSpin = false;
                    break;
                }
            }

            foreach (var wild in activeExpandingWilds)
            {
                if (nextRemainingRespins == 0 && wild.reelIndex == 0)
                    continue;

                wild.reelIndex--;
            }

            for (int i = activeExpandingWilds.Count - 1; i >= 0; i--)
            {
                var activeSlot = activeExpandingWilds[i];

                if (activeSlot.reelIndex < 0)
                {
                    if (activeSlot.animController != null)
                    {
                        activeSlot.animController.ResetAll();
                    }

                    if (activeSlot.instance != null)
                    {
                        Destroy(activeSlot.instance);
                    }
                    lockedSlots.RemoveAll(ls => ls.wildInstance == activeSlot);
                    activeExpandingWilds.RemoveAt(i);
                }
            }

            if (activeExpandingWilds.Count == 0)
            {
                remainingRespins = 0;

                isRespinActive = false;
                break;
            }
;
            float betAmount = DayOfDeadUIManager.Instance.CurrentBet();

            isSpinAgain = false;
            isSlotAnimationCompleted = false;

            yield return new WaitUntil(() => isSlotAnimationDone);

            SlotSpinService.Instance.Spin(betAmount);

            yield return new WaitUntil(() => isSpinAgain);

            if (currentSpinResult != null && GetWinAmount() > 0)
            {
                yield return new WaitUntil(() => isSlotAnimationCompleted);
            }

            remainingRespins = nextRemainingRespins;

            if (remainingRespins <= 0)
            {
                isRespinActive = false;
                break;
            }
            yield return new WaitForSeconds(3f);
        }

        isWildCompleted = true;
        isReSpin= false;
        isFreeGame = false;
        if (ReSpinWinAmount > 0)
        {
            float betAmount = DayOfDeadUIManager.Instance.CurrentBet();
            GameBetServices.Instance.PlayWinAnimation(betAmount, ReSpinWinAmount, currentSpinResult.newBalance);
            Invoke(nameof(UpdateGameCoin), 1f);

        }
        DayOfDeadPaylineController.Instance.StopPaylines();
        DayOfDeadPaylineController.Instance.ClearPaylineData();
        if (DayOfDeadAutoSpinController.isAutoSpinning)
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Auto Respin End");
        }
        else
        {
            DayOfDeadUIManager.Instance.UpdateButtons("Free Spin End");
        }
            
    }

    public void UpdateWildVisual(ExpandingWildInstance expandingWildInstance)
    {
        if (expandingWildInstance == null)
            return;

        int reelIndex = expandingWildInstance.reelIndex;
        int slotIndex = expandingWildInstance.slotIndex;

        if (reelIndex < 0 || reelIndex >= reels.Count)
            return;
        if (slotIndex < 0 || slotIndex >= reels[reelIndex].slots.Count)
            return;

        DayOfDeadSlotScript slot = reels[reelIndex].slots[slotIndex];
        GameObject inst = expandingWildInstance.instance;

        if (slot == null || inst == null)
            return;
        if (expandingWildInstance.animController != null)
        {
            expandingWildInstance.animController.PlaySlotShift();
        }
        DG.Tweening.DOVirtual.DelayedCall(0.5f, () =>
        {
            inst.transform.parent = slot.transform.parent;
            Vector3 currentPos = inst.transform.position;
            Vector3 targetPos = slot.transform.position;
            Vector3 newPos = new Vector3(targetPos.x, currentPos.y, currentPos.z);
            inst.transform.position = newPos;
            inst.transform.localScale = slot.transform.localScale;
            inst.transform.parent = null;
        });
    }

    public void ClearExpandingWilds()
    {
        for (int i = activeExpandingWilds.Count - 1; i >= 0; i--)
        {
            var wild = activeExpandingWilds[i];
            if (wild.animController != null)
            {
                wild.animController.ResetAll();
            }

            if (wild.instance != null)
            {
                Destroy(wild.instance);
            }

            if (wild.slot != null)
            {
                wild.slot.isLocked = false;
            }
        }

        activeExpandingWilds.Clear();
        lockedSlots.Clear();
        expandingWildSlots.Clear();
        alreadyExistReel.Clear();

        isRespinActive = false;
        remainingRespins = 0;
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

    private void SetReelDirection(DayOfDeadReelScript reel)
    {
        DayOfDeadSpinDirection direction = settings.spinSettings.spinDirection;

        // If random direction, choose randomly for each reel
        if (direction == DayOfDeadSpinDirection.Random)
        {
            direction = Random.value > 0.5f ? DayOfDeadSpinDirection.Up : DayOfDeadSpinDirection.Down;
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
        foreach (DayOfDeadReelScript reel in reels)
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
            foreach (DayOfDeadReelScript reel in reels)
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

    private void ShowPaylines()
    {
        DayOfDeadPaylineController.Instance.StartPayline(scatterCount);
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

    public static DayOfDeadSlotResource? GetResourceById(string id)
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

    #endregion

    #region FreeSpin Wild Helper Functions
    public void MoveFreeSpinWildToRow3(FreeSpinWalkingWild instance)
    {
        if (instance == null || instance.instance == null)
            return;

        int reelIndex = instance.reelIndex;
        if (reelIndex < 0 || reelIndex >= reels.Count)
            return;

        int targetSlotIndex = 4;

        var reel = reels[reelIndex];
        if (targetSlotIndex < 0 || targetSlotIndex >= reel.slots.Count)
            return;

        DayOfDeadSlotScript targetSlot = reel.slots[targetSlotIndex];

        Transform inst = instance.instance.transform;
        Vector3 startPos = inst.position;
        Vector3 targetPos = targetSlot.transform.position;
        Vector3 finalPos = new Vector3(startPos.x, -2.39f, startPos.z);

        //targetPos.y

        inst.DOMoveY(finalPos.y, 1f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                instance.slotIndex = targetSlotIndex;
                instance.slot = targetSlot;
            });
    }

    public DayOfDeadSlotScript GetRandomWalkingWildSlot()
    {
        if (reels == null || reels.Count == 0)
            return null;

        if (currentSpinResult == null || spinSymbolMatrix == null)
            return null;

        const int visibleOffset = 1;   // your reel layout offset

        for (int reelIndex = 0; reelIndex < spinSymbolMatrix.Count; reelIndex++)
        {
            var symbolRows = spinSymbolMatrix[reelIndex];
            var reelObj = reels[reelIndex];

            if (symbolRows == null || reelObj == null)
                continue;

            for (int row = 0; row < symbolRows.Count; row++)
            {
                SymbolData data = symbolRows[row];
                if (data == null || !data.isBonus)
                    continue;

                // Convert symbol-row -> actual slot index
                int slotIndex = row + visibleOffset;

                if (slotIndex < 0 || slotIndex >= reelObj.slots.Count)
                {
                    Debug.LogWarning($"[GetWalkingWild] Invalid slotIndex={slotIndex} for reel={reelIndex}");
                    return null;
                }

                DayOfDeadSlotScript slot = reelObj.slots[slotIndex];

                if (slot == null)
                    return null;

                slot.reelIndex = reelIndex;
                slot.slotIndex = slotIndex;

                Debug.Log("slot " + slot);
                Debug.Log("slot.reelIndex " + slot.reelIndex);
                Debug.Log("slot.slotlIndex " + slot.slotIndex);
                //spinSymbolMatrix.Clear();
                return slot;  
            }
        }
        return null;
    }


    public void UpdateFreeSpinWildVisual(FreeSpinWalkingWild freeSpinWildInstance)
    {
        if (freeSpinWildInstance == null)
            return;

        int reelIndex = freeSpinWildInstance.reelIndex;
        int slotIndex = freeSpinWildInstance.slotIndex;

        if (reelIndex < 0 || reelIndex >= reels.Count)
            return;
        if (slotIndex < 0 || slotIndex >= reels[reelIndex].slots.Count)
            return;

        DayOfDeadSlotScript slot = reels[reelIndex].slots[slotIndex];
        GameObject inst = freeSpinWildInstance.instance;

        if (slot == null || inst == null)
            return;
        if (freeSpinWildInstance.animController != null)
        {
            freeSpinWildInstance.animController.PlaySlotShift();
        }
        DG.Tweening.DOVirtual.DelayedCall(0.5f, () =>
        {
            inst.transform.parent = slot.transform.parent;
            Vector3 currentPos = inst.transform.position;
            Vector3 targetPos = slot.transform.position;
            Vector3 newPos = new Vector3(targetPos.x, currentPos.y, currentPos.z);
            inst.transform.position = newPos;
            inst.transform.localScale = slot.transform.localScale;
            inst.transform.parent = null;
        });
    }
    #endregion
}

[System.Serializable]
public class ExpandingWildInstance
{
    public DayOfDeadSlotScript slot;   // original slot where it first appeared
    public GameObject instance;        // the locked prefab
    public int reelIndex;             
    public int slotIndex;            
    public int remainingRespins;

    public DayOfDeadAnimationController animController;
}

[System.Serializable]
public class ExpandingWildLockedSlot
{
    public int reelIndex;
    public int slotIndex;
    public ExpandingWildInstance wildInstance;
}