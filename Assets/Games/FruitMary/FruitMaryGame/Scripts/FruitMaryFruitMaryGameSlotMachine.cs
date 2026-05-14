using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FruitMaryFruitMaryGameSlotMachine : MonoBehaviour
{
    #region Variables

    public static FruitMaryFruitMaryGameSlotMachine Instance;

    public FruitMaryFruitMaryGameSettings settings;
    public List<FruitMaryFruitMaryGameReelScript> reels;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;

    public bool isSpinAgain = false;
    public bool InSpin;
    private float _delayAmongReel;
    private float _acceleration;
    private float _speed;
    public float winAmount;

    public FruitMaryGame currentSpinResult;
    public List<List<FruitMaryGameSlot>> spinSymbolMatrix = new();

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        FruitMaryFruitMaryGameSpinService.Instance.OnSpinResultReceived += OnSpinResultReceived;
        InSpin = false;
        winAmount = 0;

        Initialize();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (FruitMaryFruitMaryGameSpinService.Instance != null)
            FruitMaryFruitMaryGameSpinService.Instance.OnSpinResultReceived -= OnSpinResultReceived;
    }

    #endregion

    #region Machine Settings

    private void Initialize()
    {
        for (var i = 0; i < reels.Count; i++)
        {
            if (reels[i] != null)
            {
                reels[i].Initialize(i);
            }
        }
    }

    private void UpdateMatrix()
    {
        horizontalLayout.enabled = true;
    }

    #endregion

    #region Spin Result Received

    private void OnSpinResultReceived(FruitMaryGame result)
    {
        currentSpinResult = result;
        Debug.Log("📩 SpinResult (parsed):\n" + JsonConvert.SerializeObject(currentSpinResult, Formatting.Indented));

        // ✅ Clear and fill spin matrix from backend
        spinSymbolMatrix.Clear();

        foreach (var reelList in currentSpinResult.reels)
        {
            List<FruitMaryGameSlot> symbols = new List<FruitMaryGameSlot>();
            foreach (var symbol in reelList)
            {
                symbols.Add(symbol);
            }
            spinSymbolMatrix.Add(symbols);
        }

        FruitMaryUIManager.Instance.SetStopInteractable(true);

        //Debug.Log($"✅ Loaded {spinSymbolMatrix.Count} reels from spin result.");
    }

    #endregion

    #region Spin

    public void Spin()
    {
        if (InSpin) return;
        StopAllCoroutines();

        winAmount = 0;
        currentSpinResult = null;
        isSpinAgain = false;
        InSpin = true;
        horizontalLayout.enabled = false;

        FruitMaryFruitMaryGameManager.Instance.DecreaseFreeSpinCount();

        _acceleration = settings.spinSettings.useSameAcceleration
            ? FruitMaryGameExtension.GetRandomValue(settings.spinSettings.acceleration)
            : 0f;

        _speed = settings.spinSettings.useSameSpeed
            ? FruitMaryGameExtension.GetRandomValue(settings.spinSettings.startSpeed)
            : 0f;

        _delayAmongReel = FruitMaryGameExtension.GetRandomValue(settings.spinSettings.delayAmongReels);

        foreach (var reel in reels)
        {
            reel.Spin(_delayAmongReel, _acceleration, _speed); 
        }

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
            //Debug.Log("StopReel : Stop 1 ");
            InSpin = false;
            foreach (var reel in reels)
            {
                reel.ForceStop();
            }
            UpdateMatrix();
            return;
        }

        StartCoroutine(StopReelsWithResultRoutine());
    }

    #endregion

    #region Result Stop

    private IEnumerator StopReelsWithResultRoutine()
    {
        UpdateMatrix();

        for (int i = 0; i < reels.Count; i++)
        {
            yield return new WaitForSeconds(_delayAmongReel);

            reels[i].canStopReel = true;
        }
        
        ProcessSpinResult();
    }

    private void ProcessSpinResult()
    {
        if (currentSpinResult == null || !currentSpinResult.success)
        {
            //Debug.LogWarning("❌ Spin result is invalid or failed.");
            return;
        }

        InSpin = false;
        isSpinAgain = true;

        FruitMaryFruitMaryGameBoxGame.Instance.StartGame(currentSpinResult.pointerIndex);
    }

    #endregion

   #region Helper Functions

    public static FruitMaryFruitMaryGameSlotResource? GetResourceById(string id)
    {
        if (Instance.settings == null || Instance.settings.slotResources == null)
        {
            //Debug.LogWarning("Settings or resourcesList is null.");
            return null;
        }

        var normalizedId = id.ToLowerInvariant();
        if (normalizedId == "lime") normalizedId = "lemon";

        foreach (var res in Instance.settings.slotResources)
        {
            if (res.slotType.ToString().ToLowerInvariant() == normalizedId)
            {
                return res;
            }
        }
        return null;
    }

    public float GetCurrentWin()
    {
        return currentSpinResult.totalWin;
    }

    #endregion
}
