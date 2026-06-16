using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class WildBuffaloPaylineController : MonoBehaviour
{
    #region Variables

    public static WildBuffaloPaylineController Instance;

    [Header("Paylines")]
    [SerializeField]
    private List<WildBuffaloPaylineData> paylines = new()
    {
        new WildBuffaloPaylineData(1,  new[] { 1, 1, 1, 1, 1 }),
        new WildBuffaloPaylineData(2,  new[] { 2, 2, 2, 2, 2 }),
        new WildBuffaloPaylineData(3,  new[] { 0, 0, 0, 0, 0 }),
        new WildBuffaloPaylineData(4,  new[] { 3, 3, 3, 3, 3 }),
        new WildBuffaloPaylineData(5,  new[] { 1, 2, 3, 2, 1 }),
        new WildBuffaloPaylineData(6,  new[] { 2, 1, 0, 1, 2 }),
        new WildBuffaloPaylineData(7,  new[] { 0, 1, 2, 1, 0 }),
        new WildBuffaloPaylineData(8,  new[] { 3, 2, 1, 2, 3 }),
        new WildBuffaloPaylineData(9,  new[] { 2, 3, 2, 3, 2 }),
        new WildBuffaloPaylineData(10, new[] { 1, 0, 1, 0, 1 }),
        new WildBuffaloPaylineData(11, new[] { 1, 1, 2, 3, 3 }),
        new WildBuffaloPaylineData(12, new[] { 2, 2, 1, 0, 0 }),
        new WildBuffaloPaylineData(13, new[] { 3, 2, 2, 2, 3 }),
        new WildBuffaloPaylineData(14, new[] { 0, 1, 1, 1, 0 }),
        new WildBuffaloPaylineData(15, new[] { 1, 2, 1, 0, 1 }),
        new WildBuffaloPaylineData(16, new[] { 2, 1, 2, 3, 2 }),
        new WildBuffaloPaylineData(17, new[] { 1, 0, 0, 1, 2 }),
        new WildBuffaloPaylineData(18, new[] { 2, 3, 3, 2, 1 }),
        new WildBuffaloPaylineData(19, new[] { 1, 2, 2, 2, 1 }),
        new WildBuffaloPaylineData(20, new[] { 2, 1, 1, 1, 2 }),
        new WildBuffaloPaylineData(21, new[] { 2, 2, 3, 2, 1 }),
        new WildBuffaloPaylineData(22, new[] { 1, 1, 0, 1, 2 }),
        new WildBuffaloPaylineData(23, new[] { 0, 1, 0, 1, 0 }),
        new WildBuffaloPaylineData(24, new[] { 3, 2, 3, 2, 3 }),
        new WildBuffaloPaylineData(25, new[] { 0, 0, 1, 0, 0 }),
        new WildBuffaloPaylineData(26, new[] { 3, 3, 2, 3, 3 }),
        new WildBuffaloPaylineData(27, new[] { 1, 1, 2, 1, 1 }),
        new WildBuffaloPaylineData(28, new[] { 2, 2, 1, 2, 2 }),
        new WildBuffaloPaylineData(29, new[] { 0, 0, 1, 2, 2 }),
        new WildBuffaloPaylineData(30, new[] { 3, 3, 2, 1, 1 }),
        new WildBuffaloPaylineData(31, new[] { 1, 2, 1, 2, 1 }),
        new WildBuffaloPaylineData(32, new[] { 2, 1, 2, 1, 2 }),
        new WildBuffaloPaylineData(33, new[] { 2, 3, 2, 1, 2 }),
        new WildBuffaloPaylineData(34, new[] { 1, 0, 1, 2, 1 }),
        new WildBuffaloPaylineData(35, new[] { 1, 0, 0, 0, 1 }),
        new WildBuffaloPaylineData(36, new[] { 2, 3, 3, 3, 2 }),
        new WildBuffaloPaylineData(37, new[] { 1, 1, 1, 2, 3 }),
        new WildBuffaloPaylineData(38, new[] { 2, 2, 2, 1, 0 }),
        new WildBuffaloPaylineData(39, new[] { 0, 1, 2, 3, 2 }),
        new WildBuffaloPaylineData(40, new[] { 3, 2, 1, 0, 1 }),
        new WildBuffaloPaylineData(41, new[] { 1, 2, 3, 3, 3 }),
        new WildBuffaloPaylineData(42, new[] { 2, 1, 0, 0, 0 }),
        new WildBuffaloPaylineData(43, new[] { 0, 0, 0, 1, 2 }),
        new WildBuffaloPaylineData(44, new[] { 3, 3, 3, 2, 1 }),
        new WildBuffaloPaylineData(45, new[] { 3, 2, 2, 1, 0 }),
        new WildBuffaloPaylineData(46, new[] { 0, 1, 1, 2, 3 }),
        new WildBuffaloPaylineData(47, new[] { 1, 2, 2, 3, 3 }),
        new WildBuffaloPaylineData(48, new[] { 2, 1, 1, 0, 0 }),
        new WildBuffaloPaylineData(49, new[] { 0, 1, 0, 1, 2 }),
        new WildBuffaloPaylineData(50, new[] { 3, 2, 3, 2, 1 }),
    };

    public List<WildBuffaloPaylineEntry> activePaylines = new List<WildBuffaloPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2f;
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;
    [SerializeField] private GameObject overlay;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<WildBuffaloPaylineResult> spinResult = new List<WildBuffaloPaylineResult>();
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

    public void AddPaylineData(WildBuffaloPaylineResult result)
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

    private void StartPaylineDisplay(List<WildBuffaloPaylineResult> results)
    {
        StopPaylineDisplay();
        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new WildBuffaloPaylineEntry(
                    paylineData, result.reelLimit, result.winText
                ));
            }
        }

        if (activePaylines.Count == 0 && !resultfreeGameReady)
        {
            WildBuffaloSlotMachine.Instance.isSlotAnimationCompleted = true;
            overlay.SetActive(false);
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
        overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if ((activePaylines == null || activePaylines.Count == 0) && !resultfreeGameReady)
        {
            WildBuffaloSlotMachine.Instance.isSlotAnimationCompleted = true;
            yield break;
        }

        if (activePaylines.Count == 0)
        {
            WildBuffaloSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        overlay.SetActive(true);

        if (resultfreeGameReady)
        {
            if (scatterAnimation != null)
            {
                StopCoroutine(scatterAnimation);
            }

            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if (activePaylines.Count == 1)
        {
            yield return PlaySinglePayline(activePaylines[0]);
        }
        else
        {
            if (WildBuffaloAutoSpinController.isAutoSpinning || WildBuffaloSlotMachine.Instance.isFreeGame)
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

        WildBuffaloSlotMachine.Instance.isSlotAnimationCompleted = true;
    }

    private IEnumerator PlaySinglePayline(WildBuffaloPaylineEntry entry)
    {
        for (int x = 0; x < WildBuffaloSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = WildBuffaloSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }

    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in WildBuffaloSlotMachine.Instance.reels)
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

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < WildBuffaloSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = WildBuffaloSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (WildBuffaloSlotMachine.Instance.freeSpinCount > 0 && slot.slotType == WildBuffaloSlotType.Bonus)
                {
                    slot.PlayAnimation();
                }
            }
        }

        if (WildBuffaloSlotMachine.Instance.freeSpinCount > 0 && !WildBuffaloSlotMachine.Instance.isFreeGame)
        {
            WildBuffaloSlotMachine.Instance.firstFreeSpin = true;
            WildBuffaloUIManager.Instance.UpdateButtons("Transition Start");
            //WildBuffaloFreeGameTransitionController.Instance.StartFreeSpinTransition();
            //WildBuffaloFreeGameTransitionController.Instance.UpdateFreeSpinsCount(WildBuffaloSlotMachine.Instance.freeSpinCount);
        }
        else if (WildBuffaloSlotMachine.Instance.freeSpinCount > 0 && WildBuffaloSlotMachine.Instance.isFreeGame)
        {
            //WildBuffaloFreeGameTransitionController.Instance.UpdateFreeSpinsCount(IrishPotLuckSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            WildBuffaloSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
//public class WildBuffaloPaylineData
//{
//    public int paylineNumber;

//    [Tooltip("Flattened 5x4 matrix (row-major). Index = y * 5 + x")]
//    public List<int> pattern = new List<int>(new int[20]);

//    public int[,] ToMatrix()
//    {
//        int[,] matrix = new int[5, 4];

//        for (int x = 0; x < 5; x++)
//        {
//            for (int y = 0; y < 4; y++)
//            {
//                matrix[x, y] = pattern[y * 5 + x];
//            }
//        }

//        return matrix;
//    }
//}
public class WildBuffaloPaylineData
{
    public int paylineNumber;

    // 5 values only, one row per reel.
    // 0 = top, 1 = second, 2 = third, 3 = bottom
    public List<int> rows = new List<int>(5);

    public WildBuffaloPaylineData(int paylineNumber, int[] rows)
    {
        this.paylineNumber = paylineNumber;
        this.rows = new List<int>(rows);
    }

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 4];

        for (int x = 0; x < 5; x++)
        {
            int y = rows[x];

            if (y >= 0 && y < 4)
                matrix[x, y] = 1;
        }

        return matrix;
    }
}
public class WildBuffaloPaylineEntry
{
    public WildBuffaloPaylineData payline;
    public int reelLimit;
    public string winText;

    public WildBuffaloPaylineEntry(WildBuffaloPaylineData payline, int reelLimit, string winText)
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
public class WildBuffaloPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public WildBuffaloPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion