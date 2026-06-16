using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class LifeOfLuxuryPaylineController : MonoBehaviour
{
    #region Variables

    public static LifeOfLuxuryPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<LifeOfLuxuryPaylineData> paylines;
    // Currently running paylines
    public List<LifeOfLuxuryPaylineEntry> activePaylines = new List<LifeOfLuxuryPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 1.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<LifeOfLuxuryPaylineResult> spinResult = new List<LifeOfLuxuryPaylineResult>();
    private bool resultfreeGameReady;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void StartPayline(bool freeSpinReady)
    {
        resultfreeGameReady = freeSpinReady;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(LifeOfLuxuryPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        spinResult.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<LifeOfLuxuryPaylineResult> results)
    {
        StopPaylineDisplay();
        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new LifeOfLuxuryPaylineEntry(
                    paylineData, result.reelLimit, result.winText
                ));
            }
        }
        if (activePaylines.Count == 0 && !resultfreeGameReady && !LifeOfLuxurySlotMachine.Instance.isFreeGame)
        {
            LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }
    private void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        if (scatterAnimation != null)
        {
            StopCoroutine(scatterAnimation);
            scatterAnimation = null;
        }

        ResetAllSlotsToDefault();
    }

    private IEnumerator PlayPaylines()
    {
        if ((activePaylines == null || activePaylines.Count == 0) && !resultfreeGameReady && !LifeOfLuxurySlotMachine.Instance.isFreeGame)
        {
            LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (resultfreeGameReady)
        {
            if (scatterAnimation != null)
            {
                StopCoroutine(scatterAnimation);
            }
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if (activePaylines.Count == 0)
        {
            AnimateWildSlotsInFreeSpin();

            yield return new WaitForSeconds(flickerDelay);

            LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (activePaylines.Count == 1)
        {
            yield return PlaySinglePayline(activePaylines[0]);
            LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
        }
        else
        {
            if (LifeOfLuxuryAutoSpinController.isAutoSpinning || LifeOfLuxurySlotMachine.Instance.isFreeGame)
            {
                foreach (var entry in activePaylines)
                {
                    yield return PlaySinglePayline(entry);
                }
                LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                bool completedFirstCycle = false;

                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    if (!completedFirstCycle)
                    {
                        completedFirstCycle = true;
                        LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
                    }
                }
            }
            //else
            //{
            //    while (isShowing)
            //    {
            //        foreach (var entry in activePaylines)
            //        {
            //            yield return PlaySinglePayline(entry);
            //        }
            //    }
            //}
        }

    }

    private IEnumerator PlaySinglePayline(LifeOfLuxuryPaylineEntry entry)
    {
        for (int x = 0; x < LifeOfLuxurySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = LifeOfLuxurySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        // Free spin wild animation, even if wild is not in winning payline
        AnimateWildSlotsInFreeSpin();

        yield return new WaitForSeconds(flickerDelay);

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }
    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in LifeOfLuxurySlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.StopAnimation();
                }
            }
        }
    }
    private void AnimateWildSlotsInFreeSpin()
    {
        if (!LifeOfLuxurySlotMachine.Instance.isFreeGame)
            return;

        if (LifeOfLuxuryFreeSpinController.Instance != null &&
            LifeOfLuxuryFreeSpinController.Instance.IsLastFreeSpin())
            return;

        for (int x = 0; x < LifeOfLuxurySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = LifeOfLuxurySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (LifeOfLuxurySlotMachine.Instance.isWildSlot(slot.slotType))
                {
                    Debug.Log("FreeSpin Wild Slot");
                    slot.PlayAnimation();
                }
            }
        }
    }
    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < LifeOfLuxurySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = LifeOfLuxurySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (LifeOfLuxurySlotMachine.Instance.freeSpinCount > 0 && slot.slotType == LifeOfLuxurySlotType.GoldCoin)
                {
                    slot.PlayAnimation();
                }
            }
        }
        if (LifeOfLuxurySlotMachine.Instance.freeSpinCount > 0 && !LifeOfLuxurySlotMachine.Instance.isFreeGame)
        {
            LifeOfLuxurySlotMachine.Instance.firstFreeSpin = true;
            LifeOfLuxuryUIManager.Instance.UpdateButtons("Transition Start");
            LifeOfLuxuryFreeGameTransitionController.Instance.StartFreeSpinTransition();
            LifeOfLuxuryFreeGameTransitionController.Instance.UpdateFreeSpinsCount(LifeOfLuxurySlotMachine.Instance.freeSpinCount);
        }
        else if (LifeOfLuxurySlotMachine.Instance.freeSpinCount > 0 && LifeOfLuxurySlotMachine.Instance.isFreeGame)
        {
            LifeOfLuxuryFreeGameTransitionController.Instance.UpdateFreeSpinsCount(LifeOfLuxurySlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            LifeOfLuxurySlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class LifeOfLuxuryPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]);

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 3; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class LifeOfLuxuryPaylineEntry
{
    public LifeOfLuxuryPaylineData payline;
    public int reelLimit;
    public string winText;
    public LifeOfLuxuryPaylineEntry(LifeOfLuxuryPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        if (float.TryParse(winText, out float parsedValue))
        {
            float floored = Mathf.Floor(parsedValue * 100f) / 100f;
            this.winText = floored.ToString("F2");
        }
        else
        {
            this.winText = winText;
        }
    }
}

[System.Serializable]
public class LifeOfLuxuryPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public LifeOfLuxuryPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion
