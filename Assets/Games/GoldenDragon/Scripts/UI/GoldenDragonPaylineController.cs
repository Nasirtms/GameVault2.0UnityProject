using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldenDragonPaylineController : MonoBehaviour
{
    #region Variables

    public static GoldenDragonPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<GoldenDragonPaylineData> paylines;

    // Currently running paylines
    private List<GoldenDragonPaylineEntry> activePaylines = new List<GoldenDragonPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<GoldenDragonPaylineResult> spinResult = new List<GoldenDragonPaylineResult>();
    private int resultScatterCount;
    public int CopperCoin_Count;
    private Coroutine scatterAnimation;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void AddPaylineData(GoldenDragonPaylineResult result)
    {
        if (spinResult.Contains(result))
        {
            return;
        }
        spinResult.Add(result);
    }
    public void ClearPaylineData()
    {
        StopPaylineDisplay(); 

        foreach (var entry in activePaylines)
        {
            for (int x = 0; x < GoldenDragonSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = GoldenDragonSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        slot.StopAnimation();
                    }
                }
            }
        }
        for (int x = 0; x < GoldenDragonSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = GoldenDragonSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == GoldenDragonSlotType.GoldenPhoenix || slot.type == GoldenDragonSlotType.CopperCoin)
                {
                    slot.StopAnimation();
                }
            }
        }

        spinResult.Clear();
        activePaylines.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<GoldenDragonPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new GoldenDragonPaylineEntry(
                    paylineData, result.reelLimit
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3 && CopperCoin_Count < 3)
        {
            Debug.LogWarning("No valid paylines to display.");
            return;
        }

        isShowing = true;

        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void ShowCollectedPaylines(int scatterCount)
    {
        resultScatterCount = scatterCount;
        CopperCoin_Count = GoldenDragonSlotMachine.Instance.CopperCoin_Count;

        if (spinResult == null && spinResult.Count == 0 && resultScatterCount < 3)
        {
            GoldenDragonSlotMachine.Instance.isPaylineCompleted = true;
            return;
        }

        StartPaylineDisplay(spinResult); 
    }
    private void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }
        foreach (var reel in GoldenDragonSlotMachine.Instance.reels)
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
    private IEnumerator PlayPaylines()
    {
        if (activePaylines == null && activePaylines.Count == 0 && resultScatterCount < 3)
        {
            GoldenDragonSlotMachine.Instance.isPaylineCompleted = true;
            yield break;
        }

        if (!GoldenDragonAutoSpinController.isAutoSpinning && !GoldenDragonSlotMachine.Instance.isFreeGame && resultScatterCount < 3)
        {
            GoldenDragonSlotMachine.Instance.isPaylineCompleted = true;
        }

        if (resultScatterCount >= 3 || CopperCoin_Count >= 3)
        {
            yield return ScatterAnimation();
        }

        if (activePaylines.Count == 1)
        {
            yield return PlayAllPaylines();
        }
        else
        {
            if (GoldenDragonAutoSpinController.isAutoSpinning)
            {
                yield return PlayAllPaylines();
            }
            else
            {
                while (isShowing)
                {
                    yield return PlayAllPaylines();
                }
            }
        }

        GoldenDragonSlotMachine.Instance.isPaylineCompleted = true;
    }
    private IEnumerator PlayAllPaylines()
    {
        int index = 0;
        int maxIndex = activePaylines.Count;
        while (true)
        {
            if (activePaylines == null && activePaylines.Count == 0)
            {
                yield return null;
                continue;
            }

            foreach (var pl in activePaylines)
            {
                for (int x = 0; x < GoldenDragonSlotMachine.Instance.reels.Count; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (pl.payline.ToMatrix()[x, y] == 1 && x < pl.reelLimit)
                        {
                            if (y + 1 >= GoldenDragonSlotMachine.Instance.reels[x].slots.Count)
                                continue;

                            var slot = GoldenDragonSlotMachine.Instance.reels[x].slots[y + 1];
                            if (slot == null) continue;

                            if (pl.payline.ToMatrix()[x, y] == 1)
                            {
                                slot.StopAnimation();
                            }
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.3f);
            // safe wrap
            if (activePaylines.Count == 0) yield break;
            index = index % activePaylines.Count;


            var currentPayline = activePaylines[index];

            for (int x = 0; x < GoldenDragonSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (currentPayline.payline.ToMatrix()[x, y] == 1 && x < currentPayline.reelLimit)
                    {
                        if (y + 1 >= GoldenDragonSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = GoldenDragonSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot == null) continue;

                        if (currentPayline.payline.ToMatrix()[x, y] == 1)
                        {
                            slot.PlayAnimation();
                        }
                    }
                }
            }

            yield return new WaitForSeconds(1.8f);
            if (index + 1 == maxIndex && (GoldenDragonAutoSpinController.isAutoSpinning || GoldenDragonSlotMachine.Instance.isFreeGame))
            {
                GoldenDragonSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }
            index++;
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < GoldenDragonSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = GoldenDragonSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if ((CopperCoin_Count >= 3 && slot.type == GoldenDragonSlotType.CopperCoin) ||  (
                    resultScatterCount >= 3 && slot.type == GoldenDragonSlotType.GoldenPhoenix))
                {
                    slot.PlayAnimation();
                }
            }
        }

        if (GoldenDragonSlotMachine.Instance.freeSpinCount > 0 && !GoldenDragonSlotMachine.Instance.isFreeGame)
        {
            GoldenDragonSlotMachine.Instance.firstFreeSpin = true;
            GoldenDragonUIManager.Instance.UpdateButtons("Transition Start");
            GoldenDragonFreeGameTransitionController.Instance.StartFreeSpinTransition();
            GoldenDragonFreeGameTransitionController.Instance.UpdateFreeSpinsCount(GoldenDragonSlotMachine.Instance.freeSpinCount);
        }
        else if (GoldenDragonSlotMachine.Instance.freeSpinCount > 0)
        {
            GoldenDragonFreeGameTransitionController.Instance.UpdateFreeSpinsCount(GoldenDragonSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            GoldenDragonSlotMachine.Instance.isPaylineCompleted = true;
        }

        for (int x = 0; x < GoldenDragonSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = GoldenDragonSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == GoldenDragonSlotType.GoldenPhoenix || slot.type == GoldenDragonSlotType.CopperCoin)
                {
                    slot.StopAnimation();
                }
            }
        }
    }
    #endregion
}
[System.Serializable]
public class GoldenDragonPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 3x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]);

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}
public class GoldenDragonPaylineEntry
{
    public GoldenDragonPaylineData payline;
    public int reelLimit;

    public GoldenDragonPaylineEntry(GoldenDragonPaylineData payline, int reelLimit)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
    }
}
[System.Serializable]
public class GoldenDragonPaylineResult
{
    public int paylineNumber;
    public int reelLimit;

    public GoldenDragonPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}
