using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StinkinRichReelScript : MonoBehaviour
{
    #region Variables

    [Header("Settings Reference")]
    private StinkinRichGameSettings settings;

    [Header("References")]
    public List<StinkinRichSlotScript> slots;

    private bool isSpinning = false;
    private List<Vector3> originalPositions;

    // Cached settings values
    public float spinSpeed;
    private float slowDownDuration;
    private bool clampToTopPosition;
    private float moveSpeed;
    private float windUpAmount;
    private float windUpDuration;

    // Direction control
    private StinkinRichSpinDirection currentSpinDirection = StinkinRichSpinDirection.Down;

    // Result Data
    private List<SymbolData> finalResultSymbols;
    private bool allowSymbolChanges = true;

    // Properties
    public bool IsSpinning => isSpinning;

    #endregion

    #region Reel Initilization

    public void Initialize()
    {
        settings = StinkinRichSlotMachine.Instance.settings;
        UpdateSlotScale(settings.slotSettings.SymbolScaleX, settings.slotSettings.SymbolScaleY);

        if (slots == null || slots.Count != 7)
        {
            slots.Clear();
            slots = new List<StinkinRichSlotScript>(GetComponentsInChildren<StinkinRichSlotScript>());
        }

        foreach (StinkinRichSlotScript slot in slots)
        {
            slot.Initialize();
        }

        //DoNotSetWildOnSlotsOutOfMask();
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
        foreach (StinkinRichSlotScript slot in slots)
        {
            slot.UpdateScale(scaleX, scaleY);
        }
    }

    #endregion

    #region Backend Result

    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= StinkinRichSlotMachine.Instance.spinSymbolMatrix.Count)
        {
            return;
        }

        var symbols = StinkinRichSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        for (int rowIndex = 0,rowIndexSlot = 0; rowIndex < 5 && rowIndexSlot < 5; rowIndex++, rowIndexSlot++)
        {
            //foreach(var s in symbols)
            //{
            //   Debug.Log("Symbol ID :" + s.id);
            //}

            if(reelIndex == 2)
            {
                if(rowIndexSlot == 0 || rowIndexSlot == 4)
                {
                    rowIndex--;
                    continue;
                }
            }

            var symbolData = symbols[rowIndex];
            var res = StinkinRichSlotMachine.GetResourceById(symbolData.id);

            if (res.HasValue)
            {
                StinkinRichSlotMachine.Instance.isResultReceived = true;

                var slot = slots[rowIndexSlot + 1]; // Make sure slots[1], [2], [3] are the visible ones

                slot.SetType(res.Value);

                //if(rowIndex + 1 == 1 && slot.slotType == PandaFortuneSlotType.Wild)
                //{
                //    slot.SetWildPosition(rowIndex + 1);
                //}
                
                //if(rowIndex + 1 == 3 && slot.slotType == PandaFortuneSlotType.Wild)
                //{
                //    slot.SetWildPosition(rowIndex + 1);
                //}
            }
        }

        //DoNotSetWildOnSlotsOutOfMask();
        EnsureSymbolsInProperPositions();
        ForceClampToTop();
        //StopAllCoroutines();
    }

    #endregion

    #region Reel Spin

    public void StartSpin()
    {
        if (isSpinning) return;

        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.SetSlotActive(true);
            }
        }

        //DoNotSetWildOnSlotsOutOfMask();
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

    public void SetSpinDirection(StinkinRichSpinDirection direction)
    {
        currentSpinDirection = direction;
    }

    private IEnumerator StartSpinWithWindUp()
    {
        yield return StartCoroutine(SmoothWindUp());

        if (currentSpinDirection == StinkinRichSpinDirection.Up)
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

        if (currentSpinDirection == StinkinRichSpinDirection.Up)
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
            if (currentSpinDirection == StinkinRichSpinDirection.Up)
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
        //DoNotSetWildOnSlotsOutOfMask();
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
        //DoNotSetWildOnSlotsOutOfMask();
        EnsureSymbolsInProperPositions();
    }

    #endregion

    #region Reel Stop

    //public void StopSpin(float delay)
    public void StopSpin()
    {
        if (!isSpinning) return;
        //StartCoroutine(StopSpinCoroutine(delay));
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

    //private IEnumerator StopSpinCoroutine(float delay)
    private IEnumerator StopSpinCoroutine()
    {
        //yield return new WaitForSeconds(delay);

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

    public void EnsureSymbolsInProperPositions()
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

    public void SetWildOnReel(int reelIndex, int SlotNumberInReel)
    {
        if (reelIndex < 0 || reelIndex >= StinkinRichSlotMachine.Instance.spinSymbolMatrix.Count) return;
        if (SlotNumberInReel < 0 || SlotNumberInReel >= slots.Count) return;

        var symbols = StinkinRichSlotMachine.Instance.spinSymbolMatrix[reelIndex];

        finalResultSymbols = symbols;
        allowSymbolChanges = false;

        var symbolData = symbols[SlotNumberInReel - 1];
        var res = StinkinRichSlotMachine.GetResourceById("wild");

        if (res.HasValue)
        {
            var slot = slots[SlotNumberInReel];
            slot.SetType(res.Value);
            //slot.SetWildPosition();
            slot.PlayWildShiftAnimation();

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].SetSlotActive(i == SlotNumberInReel);
            }
        }

        EnsureSymbolsInProperPositions();
        ForceClampToTop();
    }

    private void DoNotSetWildOnSlotsOutOfMask()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            //if (slot.slotType == StinkinRichSlotType.Wild && (i == 0 || i == 4))
            //{
            //    slot.SetSlotActive(false);
            //}
        }
    }

    #endregion
}
