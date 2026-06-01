using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkRueRoyaleMiniGameReelScript : MonoBehaviour
{
    #region Variables

    [Header("Settings Reference")]
    private UltimateFireLinkRueRoyaleMiniGameSettings settings;

    [Header("References")]
    public List<UltimateFireLinkRueRoyaleMiniGameSlotScript> slots;

    private bool isSpinning;

    // Cached settings values
    private float slowDownDuration;
    private bool clampToTopPosition;
    private float moveSpeed;
    private float windUpAmount;
    private float windUpDuration;

    private UltimateFireLinkRueRoyaleMiniGameSpinDirection currentSpinDirection = UltimateFireLinkRueRoyaleMiniGameSpinDirection.Down;

    private List<SymbolData> finalResultSymbols;
    private bool allowSymbolChanges = true;
    public bool IsSpinning => isSpinning;

    private Coroutine spinRoutine;
    private Coroutine stopRoutine;
    private float baseMoveSpeed;

    private float topY;
    private float bottomY;

    private float slotStep;

    #endregion

    #region Initialize
    public int ReelIndex { get; private set; } = -1;
    public void SetReelIndex(int index) => ReelIndex = index;
    public void ApplyLockVisual(bool locked)
    {
        var sphereRes = UltimateFireLinkRueRoyaleMiniGameSlotMachine.GetResourceById("sphere");
        var emptyRes = UltimateFireLinkRueRoyaleMiniGameSlotMachine.GetResourceById("empty");

        if (locked)
        {
            if (sphereRes.HasValue)
                slots[1].SetType(sphereRes.Value, false);
        }
        else
        {
            if (emptyRes.HasValue)
                slots[1].SetType(emptyRes.Value, true);
        }
    }
    public void Initialize()
    {
        settings = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.settings;

        if (slots == null) slots = new List<UltimateFireLinkRueRoyaleMiniGameSlotScript>(4);
        if (slots.Count != 4)
        {
            slots.Clear();
            slots.AddRange(GetComponentsInChildren<UltimateFireLinkRueRoyaleMiniGameSlotScript>(true));
        }

        // Cache settings
        clampToTopPosition = settings.spinSettings.ClampToTopPosition;
        slowDownDuration = settings.spinSettings.SlowDownDuration;
        moveSpeed = settings.spinSettings.MoveSpeed;
        windUpAmount = settings.spinSettings.WindUpAmount;
        windUpDuration = settings.spinSettings.WindUpDuration;

        topY = settings.slotSettings.TopYPosition;
        bottomY = settings.slotSettings.BottomYPosition;

        baseMoveSpeed = moveSpeed;
        UpdateSlotScale(settings.slotSettings.SymbolScaleX, settings.slotSettings.SymbolScaleY);

        if (UltimateFireLinkRueRoyaleSlotMachine.Instance.miniGame1)
        {
            var emptyRes = UltimateFireLinkRueRoyaleMiniGameSlotMachine.GetResourceById("empty");
            if (emptyRes.HasValue)
                slots[1].SetType(emptyRes.Value, false);
            //int reelIndex = ReelIndex;
            //Debug.Log("ReelIndex : " + reelIndex);
            //var machine = CashVaultMiniGameSlotMachine.Instance;

            //if (machine.fakeLockedReels && machine.IsReelLocked(reelIndex))
            //{
            //    var sphereRes = CashVaultMiniGameSlotMachine.GetResourceById("sphere");
            //    if (sphereRes.HasValue)
            //    {
            //        //slots[1].UpdateSphereAmount(0f);   // fake amount
            //        slots[1].SetType(sphereRes.Value, false);
            //    }
            //}
        }
        else
        {
            for (int i = 0; i < slots.Count; i++)
                slots[i].GetRandom(false);
        }

        // Compute spacing from initial
        slotStep = ComputeSlotStep();
        if (slotStep <= 0.0001f) slotStep = Mathf.Abs(topY - bottomY) / 3f; // fallback
    }

    //For Distance b\w Slots
    private float ComputeSlotStep()
    {
        List<float> ys = new List<float>(slots.Count);
        for (int i = 0; i < slots.Count; i++)
            ys.Add(slots[i].transform.localPosition.y);

        ys.Sort((a, b) => b.CompareTo(a)); // top -> bottom

        float sum = 0f;
        int count = 0;
        for (int i = 0; i < ys.Count - 1; i++)
        {
            sum += Mathf.Abs(ys[i] - ys[i + 1]);
            count++;
        }
        return (count > 0) ? (sum / count) : 0f;
    }

    public void UpdateSlotScale(float scaleX, float scaleY)
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].UpdateScale(scaleX, scaleY);
    }

    public void SetSpinDirection(UltimateFireLinkRueRoyaleMiniGameSpinDirection direction) => currentSpinDirection = direction;

    #endregion

    #region Spin

    public void StartSpin()
    {
        if (IsLocked) return;

        moveSpeed = baseMoveSpeed;

        StopAllRoutines();
        for (int i = 0; i < slots.Count; i++)
            slots[i].transform.DOKill();

        if (isSpinning) return;

        isSpinning = true;
        allowSymbolChanges = true;
        finalResultSymbols = null;

        spinRoutine = StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        // Wind-up (move ALL slots together a bit)
        yield return SmoothWindUp();

        while (isSpinning && allowSymbolChanges && finalResultSymbols == null)
        {
            StepSlots(settings.spinSettings.ManualDelta);
            yield return null;
        }
    }

    private IEnumerator SmoothWindUp()
    {
        // Kill any previous tweens on slots to avoid tug-of-war
        for (int i = 0; i < slots.Count; i++)
            slots[i].transform.DOKill();

        float sign = (currentSpinDirection == UltimateFireLinkRueRoyaleMiniGameSpinDirection.Up) ? -1f : 1f;
        float offset = sign * windUpAmount;

        bool done = false;
        int completed = 0;

        for (int i = 0; i < slots.Count; i++)
        {
            var tr = slots[i].transform;
            float targetY = tr.localPosition.y + offset;

            tr.DOLocalMoveY(targetY, windUpDuration)
              .SetEase(Ease.OutQuad)
              .OnComplete(() =>
              {
                  completed++;
                  if (completed >= slots.Count) done = true;
              });
        }

        yield return new WaitUntil(() => done);
    }

    // - Move slots continuously
    // - ONLY change symbol when a slot crosses bottom/top and gets recycled
    private void StepSlots(float dt)
    {
        float dir = (currentSpinDirection == UltimateFireLinkRueRoyaleMiniGameSpinDirection.Up) ? 1f : -1f;
        float delta = dir * moveSpeed * dt;

        // Move all slots
        for (int i = 0; i < slots.Count; i++)
        {
            var p = slots[i].transform.localPosition;
            p.y += delta;
            slots[i].transform.localPosition = p;
        }

        // Recycle wrapped slots (and change symbol only here)
        if (dir < 0f)
        {
            // Moving DOWN: when a slot goes below bottom, send it to top+step and change it
            for (int i = 0; i < slots.Count; i++)
            {
                var tr = slots[i].transform;
                if (tr.localPosition.y <= bottomY)
                {
                    float highestY = GetHighestSlotY();
                    tr.localPosition = new Vector3(tr.localPosition.x, highestY + slotStep, tr.localPosition.z);

                    if (allowSymbolChanges && finalResultSymbols == null)
                        slots[i].GetRandom(false);
                }
            }
        }
        else
        {
            // Moving UP: when a slot goes above top, send it to bottom-step and change it
            for (int i = 0; i < slots.Count; i++)
            {
                var tr = slots[i].transform;
                if (tr.localPosition.y >= topY)
                {
                    float lowestY = GetLowestSlotY();
                    tr.localPosition = new Vector3(tr.localPosition.x, lowestY - slotStep, tr.localPosition.z);

                    if (allowSymbolChanges && finalResultSymbols == null)
                        slots[i].GetRandom(false);
                }
            }
        }
    }

    private float GetHighestSlotY()
    {
        float y = float.NegativeInfinity;
        for (int i = 0; i < slots.Count; i++)
            y = Mathf.Max(y, slots[i].transform.localPosition.y);
        return y;
    }

    private float GetLowestSlotY()
    {
        float y = float.PositiveInfinity;
        for (int i = 0; i < slots.Count; i++)
            y = Mathf.Min(y, slots[i].transform.localPosition.y);
        return y;
    }

    #endregion

    #region Stop

    public void StopSpin()
    {
        if (!isSpinning) return;

        if (stopRoutine != null)
            StopCoroutine(stopRoutine);

        stopRoutine = StartCoroutine(StopSpinCoroutine());
    }
    public void ForceStopSpin()
    {
        if (!isSpinning) return;

        isSpinning = false;
        StopAllRoutines();

        moveSpeed = baseMoveSpeed;

        if (clampToTopPosition)
            ForceClampToTop();
    }

    private IEnumerator StopSpinCoroutine()
    {
        float elapsed = 0f;
        float startSpeed = moveSpeed;

        while (elapsed < slowDownDuration)
        {
            float t = elapsed / slowDownDuration;
            moveSpeed = Mathf.Lerp(startSpeed, 0f, t);

            StepSlots(settings.spinSettings.ManualDelta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        moveSpeed = baseMoveSpeed;
        isSpinning = false;

        if (clampToTopPosition)
            ForceClampToTop();

        stopRoutine = null;
    }

    private void StopAllRoutines()
    {
        if (spinRoutine != null) { StopCoroutine(spinRoutine); spinRoutine = null; }
        if (stopRoutine != null) { StopCoroutine(stopRoutine); stopRoutine = null; }
    }

    #endregion

    #region Backend Result

    public void ApplyFinalResult(int reelIndex)
    {
        if (reelIndex >= UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.spinSymbolMatrix.Count)
            return;

        finalResultSymbols = UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.spinSymbolMatrix[reelIndex];
        allowSymbolChanges = false;

        // If you have 3 visible rows, keep your old mapping: slots[1], slots[2], slots[3]
        for (int row = 0; row < 3; row++)
        {
            var symbolData = finalResultSymbols[row];
            var res = UltimateFireLinkRueRoyaleMiniGameSlotMachine.GetResourceById(symbolData.id);
            if (!res.HasValue) continue;

            UltimateFireLinkRueRoyaleMiniGameSlotMachine.Instance.isResultReceived = true;

            var slot = slots[row + 1];

            if (res.Value.slotType == UltimateFireLinkRueRoyaleMiniGameSlotType.Mini)
                slot.UpdateSphereAmount(symbolData.bonusPayout);
            else
                slot.UpdateSphereAmount(0f);

            slot.SetType(res.Value, true);
        }
        //if (clampToTopPosition)
        //    ForceClampToTop();
    }

    #endregion

    #region Clamp

    private void ForceClampToTop()
    {
        float highest = GetHighestSlotY();
        float offset = topY - highest;

        for (int i = 0; i < slots.Count; i++)
        {
            var p = slots[i].transform.localPosition;
            p.y += offset;
            slots[i].transform.localPosition = p;
        }
    }
    public bool IsLocked { get; private set; }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        if (locked && IsSpinning)
            ForceStopSpin();
    }
    #endregion
}