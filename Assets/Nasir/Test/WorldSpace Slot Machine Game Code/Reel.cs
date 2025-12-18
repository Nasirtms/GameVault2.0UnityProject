#if UNITY_EDITOR
#define ENABLE_DEBUG_LOGGING
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;
using Unity.VisualScripting;

/// <summary>
/// Reel component for slot machine games
/// </summary>
public class Reel : MonoBehaviour
{
    [Header("Settings Reference")]
    [SerializeField] private SlotMachineSettings settings;

    [Header("References")]
    [SerializeField] private List<Slot> slots;

    private List<Sprite> symbolSprites;
    private bool isSpinning = false;
    private int targetSymbolIndex = -1;
    private List<Vector3> originalPositions;
    private Vector3 originalReelPosition;

    // Cached settings values
    private float spinSpeed;
    private float slowDownDuration;
    private bool clampToTopPosition;
    private float moveSpeed;
    private float topYPosition;
    private float bottomYPosition;
    private float symbolScaleX;
    private float symbolScaleY;
    private float windUpAmount;
    private float windUpDuration;
    private bool enableDebugLogging;
    
    // Direction control
    private ReelDirection currentSpinDirection = ReelDirection.Down;

    // Properties
    public bool IsSpinning => isSpinning;

    public bool IsInitialized => symbolSprites != null && symbolSprites.Count > 0;

    public void SetSymbolSprites(List<Sprite> sprites)
    {
        if (sprites == null || sprites.Count == 0) return;

        symbolSprites = sprites;
        InitializeSlots();
    }

    private void Start()
    {
        if (settings == null && SlotMachine.Instance?.settings != null)
        {
            settings = SlotMachine.Instance.settings;
        }

        if (settings != null)
        {
            CacheSettings();
            // Apply settings immediately after caching
            ApplyVisualSettings();
        }

        if (slots != null && slots.Count > 0)
        {
            originalReelPosition = transform.localPosition;
            StoreOriginalPositions();
        }
    }

    private void CacheSettings()
    {
        if (settings == null) return;

        spinSpeed = settings.spinSettings.SpinSpeed;
        slowDownDuration = settings.spinSettings.SlowDownDuration;
        clampToTopPosition = settings.spinSettings.ClampToTopPosition;
        moveSpeed = settings.slotSettings.MoveSpeed;
        topYPosition = settings.slotSettings.TopYPosition;
        bottomYPosition = settings.slotSettings.BottomYPosition;
        symbolScaleX = settings.slotSettings.SymbolScaleX;
        symbolScaleY = settings.slotSettings.SymbolScaleY;
        windUpAmount = settings.spinSettings.WindUpAmount;
        windUpDuration = settings.spinSettings.WindUpDuration;
        enableDebugLogging = settings.spinSettings.EnableDebugLogging;

        // Debug: Check what values we're getting from settings
        Debug.Log($"Reel {gameObject.name}: Cached settings - ScaleX: {symbolScaleX}, ScaleY: {symbolScaleY}, SpinSpeed: {spinSpeed}, MoveSpeed: {moveSpeed}");
    }

    private void InitializeSlots()
    {
        if (symbolSprites == null || slots == null) return;

        StoreOriginalPositions();
        InitializeWithRandomSymbols();

        // Ensure settings are cached before applying visual settings
        if (settings != null)
        {
            CacheSettings();
        }

        ApplyVisualSettings();
    }

    private void InitializeWithRandomSymbols()
    {
        if (symbolSprites == null || slots == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                Sprite randomSymbol = symbolSprites[Random.Range(0, symbolSprites.Count)];
                slots[i].SetSymbol(randomSymbol, i);
            }
        }
    }

    private void StoreOriginalPositions()
    {
        if (originalPositions == null)
            originalPositions = new List<Vector3>();

        originalPositions.Clear();
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                originalPositions.Add(slot.transform.localPosition);
            }
        }
    }

    private void ApplyVisualSettings()
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.SetSymbolScale(symbolScaleX, symbolScaleY);
            }
        }
    }

    [ContextMenu("Spin")]
    public void StartSpin()
    {
        if (isSpinning || !IsInitialized) return;

        isSpinning = true;
        StartCoroutine(StartSpinWithWindUp());
    }

    /// <summary>
    /// Coroutine to handle the wind-up effect before spinning
    /// </summary>
    private IEnumerator StartSpinWithWindUp()
    {
        // SMOOTH WIND-UP EFFECT: Move reel in opposite direction of spin
        yield return StartCoroutine(SmoothWindUp());

        // Now move to appropriate position for spinning based on direction
        if (currentSpinDirection == ReelDirection.Up)
        {
            // If spinning up, start from bottom position
            Vector3 bottomPosition = transform.localPosition;
            bottomPosition.y = bottomYPosition;
            transform.localPosition = bottomPosition;
        }
        else
        {
            // If spinning down, start from top position (current behavior)
            Vector3 topPosition = transform.localPosition;
            topPosition.y = topYPosition;
            transform.localPosition = topPosition;
        }

        // Start the actual spinning
        StartCoroutine(SpinCoroutine());
    }

    /// <summary>
    /// Smoothly moves the reel in opposite direction of spin for wind-up effect using DOTween
    /// </summary>
    private IEnumerator SmoothWindUp()
    {
        Vector3 startPosition = transform.localPosition;
        float windUpTargetY;

        // Wind-up in opposite direction of spin
        if (currentSpinDirection == ReelDirection.Up)
        {
            // If spinning up, wind-up goes down
            windUpTargetY = startPosition.y - windUpAmount;
        }
        else
        {
            // If spinning down, wind-up goes up (current behavior)
            windUpTargetY = startPosition.y + windUpAmount;
        }

        // Use DOTween for smooth wind-up animation
        bool windUpComplete = false;

        transform.DOLocalMoveY(windUpTargetY, windUpDuration)
            .SetEase(Ease.OutQuad) // Smooth easing
            .OnComplete(() => {
                windUpComplete = true;
            });

        // Wait for wind-up to complete
        yield return new WaitUntil(() => windUpComplete);

        // Small pause to make wind-up effect visible
        yield return new WaitForSeconds(0.1f);
    }

    public void StopSpin(int resultSymbolIndex, float delay)
    {
        if (!isSpinning) return;

        targetSymbolIndex = resultSymbolIndex;
        StartCoroutine(StopSpinCoroutine(delay));
    }

    public void ForceStopSpin()
    {
        if (!isSpinning) return;

        // CLAMP TO TOP POSITION IMMEDIATELY when force stopping
        if (clampToTopPosition)
        {
            EnsureReelEndsOnTop();
        }

        StopAllCoroutines();
        isSpinning = false;
        targetSymbolIndex = -1;
        EnsureSymbolsInProperPositions();
    }

    private IEnumerator SpinCoroutine()
    {
        while (isSpinning)
        {
            // Shift symbols based on current direction
            if (currentSpinDirection == ReelDirection.Up)
            {
                ShiftSymbolsUp();
            }
            else
            {
                ShiftSymbolsDown();
            }
            yield return new WaitForSeconds(1f / spinSpeed);
        }
    }

    private IEnumerator StopSpinCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float originalSpeed = spinSpeed;

        while (elapsed < slowDownDuration)
        {
            spinSpeed = Mathf.Lerp(originalSpeed, 0f, elapsed / slowDownDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        spinSpeed = 0f;

        // FINAL GUARANTEE: Ensure reel is at top position
        if (clampToTopPosition)
        {
            EnsureReelEndsOnTop();
        }

        // Ensure we're completely stopped and ready for next spin
        isSpinning = false;

        // Restore the original spin speed from inspector for next spin
        spinSpeed = originalSpeed;
    }

    private void ShiftSymbolsDown()
    {
        if (slots == null || symbolSprites == null) return;

        // Store current symbols
        Sprite[] tempSymbols = new Sprite[slots.Count];
        int[] tempIndices = new int[slots.Count];

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                tempSymbols[i] = slots[i].CurrentSymbol;
                tempIndices[i] = slots[i].SymbolIndex;
            }
        }

        // Shift symbols down
        for (int i = slots.Count - 1; i > 0; i--)
        {
            if (slots[i] != null)
            {
                slots[i].SetSymbol(tempSymbols[i - 1], tempIndices[i - 1]);
            }
        }

        // Set top slot with new random symbol
        if (slots[0] != null)
        {
            Sprite newSymbol = symbolSprites[Random.Range(0, symbolSprites.Count)];
            slots[0].SetSymbol(newSymbol, 0);
        }

        // Move reel down
        if (isSpinning)
        {
            float currentY = transform.localPosition.y;
            if (currentY <= bottomYPosition)
            {
                Vector3 newPosition = transform.localPosition;
                newPosition.y = topYPosition;
                transform.localPosition = newPosition;
            }
            else
            {
                transform.localPosition += Vector3.down * (moveSpeed * Time.deltaTime);
            }
        }

        EnsureSymbolsInProperPositions();
    }

    private void EnsureSymbolsInProperPositions()
    {
        if (originalPositions == null) return;

        for (int i = 0; i < slots.Count && i < originalPositions.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].transform.localPosition = originalPositions[i];
            }
        }
    }

    /// <summary>
    /// Forces the reel to clamp to top position immediately (public method for external calls)
    /// </summary>
    public void ForceClampToTop()
    {
        if (!clampToTopPosition) return;

        // Force reel to top position immediately
        Vector3 topPosition = transform.localPosition;
        topPosition.y = topYPosition;
        transform.localPosition = topPosition;

        Debug.Log($"Reel {gameObject.name}: FORCE clamped to top position at Y: {topYPosition}");
    }

    /// <summary>
    /// Ensures the reel ends on top position when stopping
    /// </summary>
    private void EnsureReelEndsOnTop()
    {
        if (!clampToTopPosition) return;

        // Force reel to top position when stopping
        Vector3 topPosition = transform.localPosition;
        topPosition.y = topYPosition;
        transform.localPosition = topPosition;

        Debug.Log($"Reel {gameObject.name}: Reel FORCED to top position for stop at Y: {topYPosition}");
    }

    /// <summary>
    /// Sets the spinning direction for this reel
    /// </summary>
    public void SetSpinDirection(ReelDirection direction)
    {
        currentSpinDirection = direction;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Reel {gameObject.name}: Spin direction set to {direction}");
        }
    }

    /// <summary>
    /// Gets the current spinning direction of this reel
    /// </summary>
    public ReelDirection GetCurrentSpinDirection()
    {
        return currentSpinDirection;
    }

    /// <summary>
    /// Forces the reel to clamp to bottom position immediately
    /// </summary>
    public void ForceClampToBottom()
    {
        if (!clampToTopPosition) return;

        // Force reel to bottom position immediately
        Vector3 bottomPosition = transform.localPosition;
        bottomPosition.y = bottomYPosition;
        transform.localPosition = bottomPosition;

        Debug.Log($"Reel {gameObject.name}: FORCE clamped to bottom position at Y: {bottomYPosition}");
    }

    /// <summary>
    /// Shifts symbols upward (for upward spinning)
    /// </summary>
    private void ShiftSymbolsUp()
    {
        if (slots == null || symbolSprites == null) return;

        // Store current symbols
        Sprite[] tempSymbols = new Sprite[slots.Count];
        int[] tempIndices = new int[slots.Count];

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                tempSymbols[i] = slots[i].CurrentSymbol;
                tempIndices[i] = slots[i].SymbolIndex;
            }
        }

        // Shift symbols up
        for (int i = 0; i < slots.Count - 1; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetSymbol(tempSymbols[i + 1], tempIndices[i + 1]);
            }
        }

        // Set bottom slot with new random symbol
        if (slots[slots.Count - 1] != null)
        {
            Sprite newSymbol = symbolSprites[Random.Range(0, symbolSprites.Count)];
            slots[slots.Count - 1].SetSymbol(newSymbol, slots.Count - 1);
        }

        // Move reel up
        if (isSpinning)
        {
            float currentY = transform.localPosition.y;
            if (currentY >= topYPosition)
            {
                Vector3 newPosition = transform.localPosition;
                newPosition.y = bottomYPosition;
                transform.localPosition = newPosition;
            }
            else
            {
                transform.localPosition += Vector3.up * (moveSpeed * Time.deltaTime);
            }
        }

        EnsureSymbolsInProperPositions();
    }

}
