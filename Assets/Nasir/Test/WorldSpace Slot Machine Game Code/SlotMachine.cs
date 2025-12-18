#if UNITY_EDITOR
#define ENABLE_DEBUG_LOGGING
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class SlotMachine : MonoBehaviour
{
    // Singleton instance
    public static SlotMachine Instance { get; private set; }

    [Header("Settings Reference")]
    public SlotMachineSettings settings;

    [Header("Slot Machine Configuration")]
    [SerializeField] private List<Reel> reels;

    private float reelStopDelay;
    private float minSpinDuration;
    private float maxSpinDuration;
    private float reelStartDelay;
    private List<Sprite> symbolSprites;
    private SpinMode startSpinMode; // Cached from settings
    private SpinMode endSpinMode; // Cached from settings
    private ReelDirection reelDirection; // Cached from settings

    [Header("Events")]
    [SerializeField] private UnityEvent onSpinStart;
    [SerializeField] private UnityEvent onSpinComplete;
    [SerializeField] private UnityEvent<int[]> onResultDisplayed;

    // Cached debug logging value for performance
    private bool enableDebugLogging;

    

    private bool isSpinning = false;
    private Coroutine spinCoroutine;

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Validate and cache settings
        if (settings == null)
        {
//#if UNITY_EDITOR
//            Debug.LogError($"SlotMachine {gameObject.name} has no SlotMachineSettings assigned!");
//#endif
            return;
        }

        CacheSettings();
        ValidateSetup();
    }

    /// <summary>
    /// Caches all settings values for performance optimization
    /// </summary>
    private void CacheSettings()
    {
        if (settings == null) return;

        reelStopDelay = settings.spinSettings.ReelStopDelay;
        minSpinDuration = settings.slotSettings.MinSpinDuration;
        maxSpinDuration = settings.slotSettings.MaxSpinDuration;
        reelStartDelay = settings.spinSettings.ReelStartDelay;
        symbolSprites = settings.symbolSprites.ConvertAll(r => r.symbol);
        enableDebugLogging = settings.spinSettings.EnableDebugLogging;
        startSpinMode = settings.spinSettings.startSpin; // Cache start spin mode from settings
        endSpinMode = settings.spinSettings.endSpin; // Cache end spin mode from settings
        reelDirection = settings.spinSettings.reelDirection; // Cache reel direction from settings

//#if UNITY_EDITOR
//        Debug.Log($"SlotMachine {gameObject.name}: Settings cached successfully");
//#endif
    }

    private void ValidateSetup()
    {
        if (reels == null || reels.Count == 0)
        {
//#if UNITY_EDITOR
//            Debug.LogError($"SlotMachine {gameObject.name} has no reels assigned!");
//#endif
            return;
        }

        if (settings.symbolSprites == null || settings.symbolSprites.Count == 0)
        {
//#if UNITY_EDITOR
//            Debug.LogError($"SlotMachine {gameObject.name} has no symbol sprites assigned!");
//#endif
            return;
        }

        // Validate each reel
        for (int i = 0; i < reels.Count; i++)
        {
            if (reels[i] == null)
            {
//#if UNITY_EDITOR
//                Debug.LogError($"SlotMachine {gameObject.name} has null reel at index {i}!");
//#endif
            }
        }

        // Pass symbol sprites to each reel
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.SetSymbolSprites(symbolSprites);
            }
        }
    }

    /// <summary>
    /// Starts spinning the slot machine with the specified result symbols
    /// </summary>
    /// <param name="resultArray">Array of symbol indices for the middle row result</param>
    public void Spin(int[] resultArray)
    {
        try
        {
            // Validate input parameters
            if (resultArray == null)
            {
//#if UNITY_EDITOR
//                Debug.LogError("Slot machine cannot spin with null result array!");
//#endif
                return;
            }

            // Check if already spinning
            if (isSpinning)
            {
                if (enableDebugLogging)
//#if UNITY_EDITOR
//                    Debug.LogWarning("Slot machine is already spinning!");
//#endif
                return;
            }

            // Validate array length
            if (resultArray.Length != reels.Count)
            {
//#if UNITY_EDITOR
//                Debug.LogError($"Result array length ({resultArray.Length}) must match reel count ({reels.Count})!");
//#endif
                return;
            }

            // Validate reel states
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] == null)
                {
//#if UNITY_EDITOR
//                    Debug.LogError($"Reel at index {i} is null! Cannot start spin.");
//#endif
                    return;
                }

                if (!reels[i].IsInitialized)
                {
//#if UNITY_EDITOR
//                    Debug.LogError($"Reel at index {i} is not initialized! Cannot start spin.");
//#endif
                    return;
                }
            }

            // Validate result indices
            for (int i = 0; i < resultArray.Length; i++)
            {
                if (resultArray[i] < 0)
                {
//#if UNITY_EDITOR
//                    Debug.LogError($"Result index {i} cannot be negative: {resultArray[i]}");
//#endif
                    return;
                }

                // Check if result index is within valid range
                if (resultArray[i] >= symbolSprites.Count)
                {
//#if UNITY_EDITOR
//                    Debug.LogError($"Result index {i} ({resultArray[i]}) exceeds maximum symbol index ({symbolSprites.Count - 1})!");
//#endif
                    return;
                }
            }

            // Stop any existing spin coroutine
            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
            }

            // Start the spin
            spinCoroutine = StartCoroutine(SpinCoroutine(resultArray));
        }
        catch (System.Exception ex)
        {
//#if UNITY_EDITOR
//            Debug.LogError($"Exception in Spin method: {ex.Message}");
//#endif
        }
    }

    /// <summary>
    /// Stops the current spin immediately
    /// </summary>
    public void StopSpin()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        isSpinning = false;

        // Stop all reels
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.ForceStopSpin();
            }
        }

//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log("Spin stopped manually");
//#endif
    }


    #region Context Menu Wrapper Methods
    /// <summary>
    /// Context menu wrapper for Spin - starts the slot machine with default result
    /// </summary>
    [ContextMenu("Spin")]
    public void SpinFromContextMenu()
    {
        // Create a default result array with all zeros (first symbol on each reel)
        int[] defaultResult = new int[reels.Count];
        for (int i = 0; i < reels.Count; i++)
        {
            defaultResult[i] = 0; // Show first symbol on each reel
        }
        Spin(defaultResult);
    }

    /// <summary>
    /// Context menu wrapper for StopSpin - stops the current spin
    /// </summary>
    [ContextMenu("Stop")]
    public void StopFromContextMenu()
    {
        StopSpin();
    }
    #endregion

    private IEnumerator SpinCoroutine(int[] resultArray)
    {
        isSpinning = true;

//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log($"Starting spin with result: [{string.Join(", ", resultArray)}] - Start Mode: {startSpinMode}, End Mode: {endSpinMode}");
//#endif

        // Trigger spin start event
        onSpinStart?.Invoke();

        // Start reels based on start spin mode
        if (startSpinMode == SpinMode.SpinAll)
        {
            // Start all reels spinning simultaneously
            foreach (var reel in reels)
            {
                if (reel != null)
                {
                    // Set direction for each reel based on settings
                    SetReelDirection(reel);
                    reel.StartSpin();
                }
            }

//            if (enableDebugLogging)
//#if UNITY_EDITOR
//                Debug.Log("All reels started spinning simultaneously");
//#endif
        }
        else // SpinOneByOne mode
        {
            // Start reels one by one with delays
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    // Set direction for each reel based on settings
                    SetReelDirection(reels[i]);
                    reels[i].StartSpin();

//                    if (enableDebugLogging)
//#if UNITY_EDITOR
//                        Debug.Log($"Started reel {i} with {reelStartDelay}s delay");
//#endif

                    // Wait for delay before starting next reel (except for the last one)
                    if (i < reels.Count - 1)
                    {
                        yield return new WaitForSeconds(reelStartDelay);
                    }
                }
            }

//            if (enableDebugLogging)
//#if UNITY_EDITOR
//                Debug.Log("All reels started spinning one by one");
//#endif
        }

        // Wait for minimum spin duration
        yield return new WaitForSeconds(minSpinDuration);

        // Wait for all reels to be spinning
        yield return StartCoroutine(WaitForAllReelsToBeSpinning());

        // Stop reels based on end spin mode
        if (endSpinMode == SpinMode.SpinAll)
        {
            // Stop all reels simultaneously
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].StopSpin(resultArray[i], 0f); // No delay for simultaneous stop

//                    if (enableDebugLogging)
//#if UNITY_EDITOR
//                        Debug.Log($"Stopping reel {i} with symbol {resultArray[i]} simultaneously");
//#endif
                }
            }
        }
        else // SpinOneByOne mode
        {
            // Stop reels one by one with delays
            for (int i = 0; i < reels.Count; i++)
            {
                if (reels[i] != null)
                {
                    float delay = i * reelStopDelay;
                    reels[i].StopSpin(resultArray[i], delay);

//                    if (enableDebugLogging)
//#if UNITY_EDITOR
//                        Debug.Log($"Stopping reel {i} with symbol {resultArray[i]} after {delay}s delay");
//#endif
                }
            }
        }

        // Wait for all reels to stop
        yield return StartCoroutine(WaitForAllReelsToStop());

        // Force all reels to final position based on direction
        ForceAllReelsToFinalPosition();

        // Spin complete
        isSpinning = false;
        spinCoroutine = null;

//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log("Spin complete!");
//#endif

        // Trigger events
        onResultDisplayed?.Invoke(resultArray);
        onSpinComplete?.Invoke();
    }

    /// <summary>
    /// Waits for all reels to stop
    /// </summary>
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

    /// <summary>
    /// Sets the spinning direction for a reel based on the direction setting
    /// </summary>
    private void SetReelDirection(Reel reel)
    {
        ReelDirection direction = reelDirection;
        
        // If random direction, choose randomly for each reel
        if (reelDirection == ReelDirection.Random)
        {
            direction = Random.value > 0.5f ? ReelDirection.Up : ReelDirection.Down;
        }
        
        // Set the direction on the reel (you'll need to implement this in Reel.cs)
        reel.SetSpinDirection(direction);
        
//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log($"Reel {reel.name} set to spin {direction}");
//#endif
    }

    /// <summary>
    /// Forces ALL reels to clamp to top position regardless of direction
    /// </summary>
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

//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log("All reels forced to top position regardless of direction");
//#endif
    }

    /// <summary>
    /// Forces ALL reels to clamp to top position immediately (legacy method)
    /// </summary>
    public void ForceAllReelsToTop()
    {
        foreach (var reel in reels)
        {
            if (reel != null)
            {
                reel.ForceClampToTop();
            }
        }

//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log("All reels forced to top position");
//#endif
    }

    /// <summary>
    /// Waits for all reels to start spinning
    /// </summary>
    private IEnumerator WaitForAllReelsToBeSpinning()
    {
        // Check if all reels are spinning
        bool allSpinning = false;
        while (!allSpinning)
        {
            allSpinning = true;
            foreach (var reel in reels)
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

//        if (enableDebugLogging)
//#if UNITY_EDITOR
//            Debug.Log("All reels are spinning");
//#endif
    }


}
