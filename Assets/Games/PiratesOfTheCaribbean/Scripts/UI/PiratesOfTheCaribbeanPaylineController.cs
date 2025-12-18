using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiratesOfTheCaribbeanPaylineController : MonoBehaviour
{
    #region Variables

    public static PiratesOfTheCaribbeanPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<PiratesOfTheCaribbeanPaylineData> paylines;

    // Currently running paylines
    private List<PiratesOfTheCaribbeanPaylineEntry> activePaylines = new List<PiratesOfTheCaribbeanPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<PiratesOfTheCaribbeanPaylineResult> spinResult = new List<PiratesOfTheCaribbeanPaylineResult>();
    private int resultScatterCount;

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

    public void StartPayline(int scatterCount)
    {
        resultScatterCount = scatterCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(PiratesOfTheCaribbeanPaylineResult result)
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

    private void StartPaylineDisplay(List<PiratesOfTheCaribbeanPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        overlay.SetActive(true);

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new PiratesOfTheCaribbeanPaylineEntry(
                    paylineData,
                    //result.reelLimit,
                    result.reelLimit
                    //result.winText
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3)
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

        foreach (var reel in PiratesOfTheCaribbeanSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSpriteToDefault();
                    slot.StopAnimation();
                }
            }
        }

        PiratesOfTheCaribbeanPaylineDrawer.Instance.ClearPaylines();

        overlay.SetActive(false);
    }
    //public int count = 0;
    private IEnumerator PlayPaylines()
    {
        PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Payline");
        if (!PiratesOfTheCaribbeanAutoSpinController.isAutoSpinning && !PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame)
        {
            PiratesOfTheCaribbeanSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                //count++;
                //if (count == 1)
                //{
                //    PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Payline");
                //    count = 0;
                //}

                yield return PlaySinglePayline(activePaylines[0]);

                PiratesOfTheCaribbeanSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                //count++;
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                       //if(count == 1)
                       //{
                       //     PiratesOfTheCaribbeanUIManager.Instance.PlaySound("Payline");
                            
                       //}
                        
                        yield return PlaySinglePayline(entry);
                    }
                    //count = 0;

                    PiratesOfTheCaribbeanSlotMachine.Instance.isSlotAnimationCompleted = true;

                    //if ((PiratesOfTheCaribbeanSlotMachine.Instance.isSlotAnimationCompleted && PiratesOfTheCaribbeanAutoSpinController.isAutoSpinning) || PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame)
                    //{
                    //    break;
                    //}
                }
            }
        }

    }

    private IEnumerator PlaySinglePayline(PiratesOfTheCaribbeanPaylineEntry entry)
    {
        float waitTime = flickerDelay;

        PiratesOfTheCaribbeanPaylineDrawer.Instance.DrawPayline(entry.payline.paylineNumber - 1);

        for (int x = 0; x < PiratesOfTheCaribbeanSlotMachine.Instance.reels.Count; x++)
        {
            //if (x >= entry.reelLimit) break;

            for (int y = 0; y < 4; y++)
            {
                var slot = PiratesOfTheCaribbeanSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (activePaylines.Count > 1)
        {
            // Clear borders & text for cycling mode
            foreach (var reel in PiratesOfTheCaribbeanSlotMachine.Instance.reels)
            {
                foreach (var slot in reel.slots)
                {
                    if (slot != null)
                    {
                        slot.SetSpriteToDefault();
                        slot.StopAnimation();
                    }
                }
            }

            PiratesOfTheCaribbeanPaylineDrawer.Instance.ClearPaylines();
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < PiratesOfTheCaribbeanSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = PiratesOfTheCaribbeanSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == PiratesOfTheCaribbeanSlotType.Scatter)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        if (PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinCount > 0 && !PiratesOfTheCaribbeanSlotMachine.Instance.isFreeGame)
        {
            PiratesOfTheCaribbeanSlotMachine.Instance.firstFreeSpin = true;
            PiratesOfTheCaribbeanUIManager.Instance.UpdateButtons("Transition Start");
            PiratesOfTheCaribbeanFreeGameTransitionController.Instance.StartFreeSpinTransition();
            PiratesOfTheCaribbeanFreeGameTransitionController.Instance.UpdateFreeSpinsCount(PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinCount);
        }
        else if (PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinCount > 0)
        {
            PiratesOfTheCaribbeanFreeGameTransitionController.Instance.UpdateFreeSpinsCount(PiratesOfTheCaribbeanSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            PiratesOfTheCaribbeanSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class PiratesOfTheCaribbeanPaylineData
{
    public int paylineNumber;
    //public Color paylineColor;
    //public GameObject paylineSprite;

    [Tooltip("Flattened 5x4 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[20]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 4]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 4; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class PiratesOfTheCaribbeanPaylineEntry
{
    public PiratesOfTheCaribbeanPaylineData payline;
    public int reelLimit;
    //public string winText;

    //public PiratesOfTheCaribbeanPaylineEntry(CleopatraPaylineData payline, int reelLimit, string winText)
    public PiratesOfTheCaribbeanPaylineEntry(PiratesOfTheCaribbeanPaylineData payline, int reelLimit)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        //this.winText = winText;
    }
}

[System.Serializable]
public class PiratesOfTheCaribbeanPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    //public string winText;

    //public PiratesOfTheCaribbeanPaylineResult(int paylineNumber, int reelLimit, string winText)
    public PiratesOfTheCaribbeanPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        //this.winText = winText;
    }
}

#endregion