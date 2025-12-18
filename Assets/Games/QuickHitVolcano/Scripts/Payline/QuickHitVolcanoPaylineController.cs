using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickHitVolcanoPaylineController : MonoBehaviour
{
    #region Variables

    public static QuickHitVolcanoPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<QuickHitVolcanoPaylineData> paylines;

    // Currently running paylines
    private List<QuickHitVolcanoPaylineEntry> activePaylines = new List<QuickHitVolcanoPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private Coroutine quickHitAnimation;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<QuickHitVolcanoPaylineResult> spinResult = new List<QuickHitVolcanoPaylineResult>();
    private int resultScatterCount;
    private int resultQuickHitCount;

    [Header("Visuals")]
    [SerializeField] private GameObject overlay;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References

    public void StartPayline(int scatterCount, int quickHitCount)
    {
        resultScatterCount = scatterCount;
        resultQuickHitCount = quickHitCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(QuickHitVolcanoPaylineResult result)
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

    private void StartPaylineDisplay(List<QuickHitVolcanoPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new QuickHitVolcanoPaylineEntry(
                    paylineData,
                    result.reelLimit
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3 && resultQuickHitCount < 3)
        {
            Debug.LogWarning("No valid paylines to display.");
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

        foreach (var reel in QuickHitVolcanoSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSortingLayer(0, false);
                    slot.StopAnimation();
                }
            }
        }

        overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if (QuickHitVolcanoSlotMachine.Instance.isFreeGame)
        {
            flickerDelay = 2f;
        }
        else
        {
            flickerDelay = 3f;
        }

        yield return new WaitForSeconds(0.75f);

        overlay.SetActive(true);

        if (!QuickHitVolcanoAutoSpinController.isAutoSpinning && !QuickHitVolcanoSlotMachine.Instance.isFreeGame && resultScatterCount < 3 && resultQuickHitCount < 3)
        {
            QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (resultQuickHitCount >= 3)
        {
            quickHitAnimation = StartCoroutine(QuickHitAnimation());
        }

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                        yield return new WaitForSeconds(0.25f);
                    }

                    QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted = true;
                }
            }
        }

    }

    private IEnumerator PlaySinglePayline(QuickHitVolcanoPaylineEntry entry)
    {
        float waitTime = flickerDelay;

        for (int x = 0; x < QuickHitVolcanoSlotMachine.Instance.reels.Count; x++)
        {
            //if (x >= entry.reelLimit) break;

            for (int y = 0; y < 3; y++)
            {
                var slot = QuickHitVolcanoSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.SetSortingLayer(1, true);
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (activePaylines.Count > 1)
        {
            // Clear borders & text for cycling mode
            foreach (var reel in QuickHitVolcanoSlotMachine.Instance.reels)
            {
                foreach (var slot in reel.slots)
                {
                    if (slot != null)
                    {
                        slot.SetSortingLayer(0, false);
                        slot.StopAnimation();
                    }
                }
            }
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < QuickHitVolcanoSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = QuickHitVolcanoSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == QuickHitVolcanoSlotType.FreeGame)
                {
                    slot.SetSortingLayer(1, true);
                    slot.PlayAnimation();
                }
            }
        }

        if (QuickHitVolcanoSlotMachine.Instance.freeSpinCount > 0 && !QuickHitVolcanoSlotMachine.Instance.isFreeGame)
        {
            QuickHitVolcanoSlotMachine.Instance.firstFreeSpin = true;
            QuickHitVolcanoGameTransitionController.Instance.StartQuickPickGame(QuickHitVolcanoSlotMachine.Instance.freeSpinCount);
        }
        else if (QuickHitVolcanoSlotMachine.Instance.freeSpinCount > 0)
        {
            QuickHitVolcanoGameTransitionController.Instance.StartQuickPickGame(QuickHitVolcanoSlotMachine.Instance.freeSpinCount);
        }

        if (activePaylines.Count == 0)
        {
            yield return new WaitForSeconds(2f);
            QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    private IEnumerator QuickHitAnimation()
    {
        for (int x = 0; x < QuickHitVolcanoSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = QuickHitVolcanoSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == QuickHitVolcanoSlotType.QuickHit || slot.type == QuickHitVolcanoSlotType.QuickHitWild)
                {
                    slot.SetSortingLayer(1, true);
                    slot.PlayAnimation();
                }
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3)
        {
            yield return new WaitForSeconds(2f);
            QuickHitVolcanoSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class QuickHitVolcanoPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x4 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]); // 5 * 3 = 15

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

public class QuickHitVolcanoPaylineEntry
{
    public QuickHitVolcanoPaylineData payline;
    public int reelLimit;

    public QuickHitVolcanoPaylineEntry(QuickHitVolcanoPaylineData payline, int reelLimit)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
    }
}

[System.Serializable]
public class QuickHitVolcanoPaylineResult
{
    public int paylineNumber;
    public int reelLimit;

    public QuickHitVolcanoPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}

#endregion