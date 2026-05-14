using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
public class StickyPiggyPaylineController : MonoBehaviour
{
    #region Variables

    public static StickyPiggyPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<StickyPiggyPaylineData> paylines;
    // Currently running paylines
    public List<StickyPiggyPaylineEntry> activePaylines = new List<StickyPiggyPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 1.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;
    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<StickyPiggyPaylineResult> spinResult = new List<StickyPiggyPaylineResult>();
    private int resultScatterCount;

    public List<StickyPiggySlotScript> bonusSlots;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void StartPayline(int scatterCount)
    {
        resultScatterCount = scatterCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(StickyPiggyPaylineResult result)
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

    private void StartPaylineDisplay(List<StickyPiggyPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new StickyPiggyPaylineEntry(
                    paylineData, result.reelLimit, result.winText
                ));
            }
        }
        if (activePaylines.Count == 0 && resultScatterCount < 2)
        {
            StickyPiggySlotMachine.Instance.isSlotAnimationCompleted = true;
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
        if ((activePaylines == null || activePaylines.Count == 0) && resultScatterCount < 2)
        {
            StickyPiggySlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (activePaylines.Count == 0)
        {
            StickyPiggySlotMachine.Instance.isSlotAnimationCompleted = true;
        }
        
        if (activePaylines.Count == 1)
        {
            yield return PlaySinglePayline(activePaylines[0]);
        }
        else
        {
            if (StickyPiggyAutoSpinController.isAutoSpinning || StickyPiggySlotMachine.Instance.isFreeGame || resultScatterCount > 2)
            {
                foreach (var entry in activePaylines)
                {
                    yield return PlaySinglePayline(entry);
                }
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }
                }
            }
        }
        if (resultScatterCount > 2)
        {
            if (scatterAnimation != null)
            {
                StopCoroutine(scatterAnimation);
            }
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }
        StickyPiggySlotMachine.Instance.isSlotAnimationCompleted = true;
    }

    private IEnumerator PlaySinglePayline(StickyPiggyPaylineEntry entry)
    {
        for (int x = 0; x < StickyPiggySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = StickyPiggySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;
                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();

                    if (StickyPiggySlotMachine.Instance.isFreeGame &&
                        (slot.slotType == StickyPiggySlotType.PiggyWildX2 ||
                         slot.slotType == StickyPiggySlotType.PiggyWildX3))
                    {
                        StickyPiggySlotMachine.Instance.SetWildInstanceAnimation(slot, true);
                    }
                }
                //if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                //{
                //    slot.PlayAnimation();
                //}
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }
    public void ResetAllSlotsToDefault()
    {
        foreach (var reel in StickyPiggySlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.StopAnimation();

                    if (StickyPiggySlotMachine.Instance.isFreeGame &&
                        (slot.slotType == StickyPiggySlotType.PiggyWildX2 ||
                         slot.slotType == StickyPiggySlotType.PiggyWildX3))
                    {
                        StickyPiggySlotMachine.Instance.SetWildInstanceAnimation(slot, false);
                    }
                }
            }
        }

        for (int x = 0; x < StickyPiggySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = StickyPiggySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == StickyPiggySlotType.Bonus)
                {
                    slot.StopBonusAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < StickyPiggySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = StickyPiggySlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (StickyPiggySlotMachine.Instance.freeSpinCount > 0 && slot.slotType == StickyPiggySlotType.Bonus)
                {
                    if (!StickyPiggySlotMachine.Instance.isFreeGame){
                        slot.PlayBonusAnimation();
                    }
                    else
                    {
                        slot.PlayAnimation();   
                    }
                    
                }
            }
        }

        yield return new WaitForSeconds(1.8f);

        if (!StickyPiggySlotMachine.Instance.isFreeGame)
        {
            StickyPiggySlotMachine.Instance.firstFreeSpin = true;
            StickyPiggyUIManager.Instance.UpdateButtons("Transition Start");
            StickyPiggyFreeSpinLocker.Instance.StartLockerTransition();
            StickyPiggyFreeGameTransitionController.Instance.UpdateFreeSpinsCount(StickyPiggySlotMachine.Instance.freeSpinCount);
        }
        else
        {
            StickyPiggyFreeGameTransitionController.Instance.UpdateFreeSpinsCount(StickyPiggySlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            StickyPiggySlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }
    public void bonusSlotPosCollect()
    {
        bonusSlots.Clear();

        for (int x = 0; x < StickyPiggySlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = StickyPiggySlotMachine.Instance.reels[x].slots[y + 1];

                if (StickyPiggySlotMachine.Instance.isBonusSlot(slot.slotType))
                {
                    bonusSlots.Add(slot);
                }
            }
        }
    }
    public IEnumerator BonusCollect()
    {
        bonusSlotPosCollect();
        StickyPiggyFreeSpinLocker.Instance.keyBox.SetActive(true);
        Vector3 targetPos = StickyPiggyFreeSpinLocker.Instance.keyBox.transform.position;
        int completed = 0;
        int total = 0;

        for (int i = 0; i < bonusSlots.Count; i++)
        {
            var slot = bonusSlots[i];
            if (slot == null) continue;

            total++;

            StartCoroutine(slot.MoveKey(targetPos, () =>
            {
                completed++;
            }));
        }

        yield return new WaitUntil(() => completed >= total);

        StickyPiggyFreeSpinLocker.Instance.ShowTriggeredKeys();
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class StickyPiggyPaylineData
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

public class StickyPiggyPaylineEntry
{
    public StickyPiggyPaylineData payline;
    public int reelLimit;
    public string winText;
    public StickyPiggyPaylineEntry(StickyPiggyPaylineData payline, int reelLimit, string winText)
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
public class StickyPiggyPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public StickyPiggyPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion