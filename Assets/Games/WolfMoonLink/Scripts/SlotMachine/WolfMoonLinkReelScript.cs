using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfMoonLinkReelScript : MonoBehaviour
{
    #region Variables

    [Header("Settings Reference")]
    private WolfMoonLinkGameSettings settings;

    [Header("References")]
    public List<WolfMoonLinkSlotScript> slots;

    private bool isSpinning = false;
    private List<Vector3> originalPositions;

    public float spinSpeed;
    public float slowDownDuration;
    private bool clampToTopPosition;
    public float moveSpeed;
    public float windUpAmount;
    public float windUpDuration;

    private WolfMoonLinkSpinDirection currentSpinDirection = WolfMoonLinkSpinDirection.Down;

    private List<SymbolData> finalResultSymbols;
    private bool allowSymbolChanges = true;
    private bool nextIsRealSymbol = true;

    public bool IsSpinning => isSpinning;

    #endregion

    #region Reel Initializations

    public void Initialize()
    {
        settings = WolfMoonLinkSlotMachine.Instance.settings;
        UpdateSlotScale(settings.slotSettings.SymbolScaleX, settings.slotSettings.SymbolScaleY);

        if (slots == null || slots.Count != 6)
        {
            slots.Clear();
            slots = new List<WolfMoonLinkSlotScript>(GetComponentsInChildren<WolfMoonLinkSlotScript>());
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].slotIndex = i;
            slots[i].Initialize();
        }

        StoreOriginalPositions();
        clampToTopPosition = settings.spinSettings.ClampToTopPosition;
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
        foreach (WolfMoonLinkSlotScript slot in slots)
        {
            slot.UpdateScale(scaleX, scaleY);
        }
    }

    #endregion

    #region Backend Result

    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= WolfMoonLinkSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }

        var symbols = WolfMoonLinkSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0; rowIndex < 3; rowIndex++)
        {
            var symbolData = symbols[rowIndex];
            var res = WolfMoonLinkSlotMachine.GetResourceById(symbolData.id);

            if (res.HasValue)
            {
                WolfMoonLinkSlotMachine.Instance.isResultReceived = true;

                var slot = slots[rowIndex + 2];
                slot.SetType(res.Value);
            }
        }

        var extraSlot = WolfMoonLinkSlotMachine.GetResourceById("Empty");

        if (extraSlot.HasValue)
        {
            bool centerIsEmpty = slots[3].slotType == WolfMoonLinkSlotType.Empty;

            if (!centerIsEmpty)
            {
                slots[0].SetType(
                    WolfMoonLinkSlotMachine.CachedRealSymbols[
                        Random.Range(0, WolfMoonLinkSlotMachine.CachedRealSymbols.Count)]);

                slots[1].SetType(
                    WolfMoonLinkSlotMachine.CachedRealSymbols[
                        Random.Range(0, WolfMoonLinkSlotMachine.CachedRealSymbols.Count)]);

                slots[5].SetType(
                    WolfMoonLinkSlotMachine.CachedRealSymbols[
                        Random.Range(0, WolfMoonLinkSlotMachine.CachedRealSymbols.Count)]);
            }
            else
            {
                slots[0].SetType(extraSlot.Value);
                slots[1].SetType(extraSlot.Value);
                slots[5].SetType(extraSlot.Value);
            }
        }

        EnsureSymbolsInProperPositions();
        ForceClampToTop();
    }

    #endregion

    #region Reel Spin

    public void StartSpin()
    {
        if (isSpinning) return;

        isSpinning = true;
        allowSymbolChanges = true;
        finalResultSymbols = null;
        _nextIsRealSymbol = true;

        spinSpeed = settings.spinSettings.SpinSpeed;
        slowDownDuration = settings.spinSettings.SlowDownDuration;
        moveSpeed = settings.slotSettings.MoveSpeed;
        windUpAmount = settings.spinSettings.WindUpAmount;
        windUpDuration = settings.spinSettings.WindUpDuration;

        StartCoroutine(StartSpinWithWindUp());
    }

    public void SetSpinDirection(WolfMoonLinkSpinDirection direction)
    {
        currentSpinDirection = direction;
    }

    private IEnumerator StartSpinWithWindUp()
    {
        yield return StartCoroutine(SmoothWindUp());

        if (currentSpinDirection == WolfMoonLinkSpinDirection.Up)
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

        if (currentSpinDirection == WolfMoonLinkSpinDirection.Up)
        {
            windUpTargetY = startPosition.y - windUpAmount;
        }
        else
        {
            windUpTargetY = startPosition.y + windUpAmount;
        }

        bool windUpComplete = false;

        transform.DOLocalMoveY(windUpTargetY, windUpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                windUpComplete = true;
            });

        yield return new WaitUntil(() => windUpComplete);

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator SpinCoroutine()
    {
        while (isSpinning && allowSymbolChanges && finalResultSymbols == null)
        {
            if (currentSpinDirection == WolfMoonLinkSpinDirection.Up)
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

        for (var i = slots.Count - 1; i > 0; i--)
        {
            var res = slots[i].currentResource;
            slots[i - 1].SetType(res);
        }

        slots[slots.Count - 1].SetType(GetNextGeneratedSymbol());

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

        slots[0].SetType(GetNextGeneratedSymbol());

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

        if (clampToTopPosition)
        {
            EnsureReelEndsOnTop();
        }

        isSpinning = false;
        spinSpeed = originalSpeed;
    }

    #endregion

    #region Reel Position

    private bool _nextIsRealSymbol = true;

    private WolfMoonLinkSlotResource GetNextGeneratedSymbol()
    {
        if (_nextIsRealSymbol)
        {
            _nextIsRealSymbol = false;

            var real = WolfMoonLinkSlotMachine.CachedRealSymbols;
            return real[Random.Range(0, real.Count)];
        }
        else
        {
            _nextIsRealSymbol = true;
            return WolfMoonLinkSlotMachine.CachedEmptySymbol.Value;
        }
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

        Vector3 topPosition = transform.localPosition;
        topPosition.y = settings.slotSettings.TopYPosition;
        transform.localPosition = topPosition;
    }

    public void ForceClampToBottom()
    {
        if (!clampToTopPosition) return;

        Vector3 bottomPosition = transform.localPosition;
        bottomPosition.y = settings.slotSettings.BottomYPosition;
        transform.localPosition = bottomPosition;
    }

    #endregion
}