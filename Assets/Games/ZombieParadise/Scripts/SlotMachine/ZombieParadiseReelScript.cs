using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieParadiseReelScript : MonoBehaviour
{
    #region Variables

    [Header("Settings Reference")]
    private ZombieParadiseGameSettings settings;

    [Header("References")]
    public List<ZombieParadiseSlotScript> slots;

    private bool isSpinning = false;
    private List<Vector3> originalPositions;

    // Cached settings values
    private float spinSpeed;
    private float slowDownDuration;
    private bool clampToTopPosition;
    private float moveSpeed;
    private float windUpAmount;
    private float windUpDuration;

    // Direction control
    private ZombieParadiseSpinDirection currentSpinDirection = ZombieParadiseSpinDirection.Down;

    // Result Data
    private List<SymbolData> finalResultSymbols;
    private bool allowSymbolChanges = true;

    // Properties
    public bool IsSpinning => isSpinning;

    #endregion

    #region Reel Initilization

    public void Initialize()
    {
        settings = ZombieParadiseSlotMachine.Instance.settings;
        UpdateSlotScale(settings.slotSettings.SymbolScaleX, settings.slotSettings.SymbolScaleY);

        if (slots == null || slots.Count != 6)
        {
            slots.Clear();
            slots = new List<ZombieParadiseSlotScript>(GetComponentsInChildren<ZombieParadiseSlotScript>());
        }

        foreach (ZombieParadiseSlotScript slot in slots)
        {
            slot.Initialize();
        }

        StoreOriginalPositions();
        clampToTopPosition = settings.spinSettings .ClampToTopPosition;
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

    public void UpdateSlotScale(float scaleX, float scaleY)
    {
        foreach (ZombieParadiseSlotScript slot in slots)
        {
            slot.UpdateScale(scaleX, scaleY);
        }
    }

    #endregion

    #region Backend Result

    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= ZombieParadiseSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }

        var symbols = ZombieParadiseSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = ZombieParadiseSlotMachine.GetResourceById(symbolData.id);

            if (res.HasValue)
            {
                ZombieParadiseSlotMachine.Instance.isResultReceived = true;

                var slot = slots[rowIndex + 1]; // Make sure slots[1], [2], [3] are the visible ones

                slot.SetType(res.Value);
            }
        }

        EnsureSymbolsInProperPositions();
        ForceClampToTop();
        //StopAllCoroutines();
    }

    #endregion

    #region Reel Spin

    public void StartSpin()
    {
        if (isSpinning) return;

        isSpinning = true;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        spinSpeed = settings.spinSettings.SpinSpeed;
        slowDownDuration = settings.spinSettings.SlowDownDuration;
        moveSpeed = settings.slotSettings.MoveSpeed;
        windUpAmount = settings.spinSettings.WindUpAmount;
        windUpDuration = settings.spinSettings.WindUpDuration;

        StartCoroutine(StartSpinWithWindUp());
    }

    public void SetSpinDirection(ZombieParadiseSpinDirection direction)
    {
        currentSpinDirection = direction;
    }

    private IEnumerator StartSpinWithWindUp()
    {
        yield return StartCoroutine(SmoothWindUp());

        if (currentSpinDirection == ZombieParadiseSpinDirection.Up)
        {
            Vector3 bottomPosition = transform.localPosition;
            bottomPosition.y = settings.slotSettings.BottomYPosition;
            transform.localPosition = bottomPosition;
        }
        else
        {
            Vector3 topPosition = transform.localPosition;
            topPosition.y = settings.slotSettings.TopYPosition;
            transform.localPosition = topPosition;
        }

        StartCoroutine(SpinCoroutine());
    }

    private IEnumerator SmoothWindUp()
    {
        Vector3 startPosition = transform.localPosition;
        float windUpTargetY;

        if (currentSpinDirection == ZombieParadiseSpinDirection.Up)
        {
            windUpTargetY = startPosition.y - windUpAmount;
        }
        else
        {
            windUpTargetY = startPosition.y + windUpAmount;
        }

        bool windUpComplete = false;

        transform.DOLocalMoveY(windUpTargetY, windUpDuration)
            .SetEase(Ease.OutQuad) // Smooth easing
            .OnComplete(() => {
                windUpComplete = true;
            });

        yield return new WaitUntil(() => windUpComplete);

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator SpinCoroutine()
    {
        while (isSpinning && allowSymbolChanges && finalResultSymbols == null)
        {
            if (currentSpinDirection == ZombieParadiseSpinDirection.Up)
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

    private void ShiftSymbolsUp()
    {
        if (slots == null) return;

        //for (var i = 0; i > slots.Count - 1; i++)
        //{
        //    var res = slots[i + 1].currentResource;
        //    slots[i].SetType(res);
        //}

        for (var i = slots.Count - 1; i > 0; i--)
        {
            var res = slots[i].currentResource;
            slots[i - 1].SetType(res);
        }

        slots[slots.Count - 1].GetRandom();

        if (isSpinning)
        {
            float currentY = transform.localPosition.y;
            if (currentY >= -settings.slotSettings.BottomYPosition)
            {
                Vector3 newPosition = transform.localPosition;
                newPosition.y = settings.slotSettings.TopYPosition;
                transform.localPosition = newPosition;
            }
            else
            {
                transform.localPosition += Vector3.up * (moveSpeed * Time.deltaTime);
            }
        }

        EnsureSymbolsInProperPositions();
    }

    private void ShiftSymbolsDown()
    {
        if (slots == null) return;

        for (var i = slots.Count - 1; i > 0; i--)
        {
            var res = slots[i - 1].currentResource;
            slots[i].SetType(res);
        }

        slots[0].GetRandom();

        // Move reel down
        if (isSpinning)
        {
            float currentY = transform.localPosition.y;
            if (currentY <= settings.slotSettings.BottomYPosition)
            {
                Vector3 newPosition = transform.localPosition;
                newPosition.y = settings.slotSettings.TopYPosition;
                transform.localPosition = newPosition;
            }
            else
            {
                transform.localPosition += Vector3.down * (moveSpeed * Time.deltaTime);
            }
        }

        EnsureSymbolsInProperPositions();
    }

    #endregion

    #region Reel Stop

    public void StopSpin()
    {
        if (!isSpinning) return;
        StartCoroutine(StopSpinCoroutine());
    }

    public void ForceStopSpin()
    {
        if (!isSpinning) return;

        if (clampToTopPosition)
        {
            EnsureReelEndsOnTop();
        }

        //StopAllCoroutines();
        isSpinning = false;
        EnsureSymbolsInProperPositions();
    }

    private IEnumerator StopSpinCoroutine()
    {
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

    #endregion

    #region Reel Position

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

    public void ForceClampToTop()
    {
        if (!clampToTopPosition) return;

        Vector3 topPosition = transform.localPosition;
        topPosition.y = settings.slotSettings.TopYPosition;
        transform.localPosition = topPosition;
    }

    private void EnsureReelEndsOnTop()
    {
        if (!clampToTopPosition) return;

        // Force reel to top position when stopping
        Vector3 topPosition = transform.localPosition;
        topPosition.y = settings.slotSettings.TopYPosition;
        transform.localPosition = topPosition;
    }

    public void ForceClampToBottom()
    {
        if (!clampToTopPosition) return;

        // Force reel to bottom position immediately
        Vector3 bottomPosition = transform.localPosition;
        bottomPosition.y = settings.slotSettings.BottomYPosition;
        transform.localPosition = bottomPosition;
    }

    #endregion
}
