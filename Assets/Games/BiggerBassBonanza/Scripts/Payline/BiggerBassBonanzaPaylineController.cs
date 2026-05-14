using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class BiggerBassBonanzaPaylineController : MonoBehaviour
{
    #region Variables

    public static BiggerBassBonanzaPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<BiggerBassBonanzaPaylineData> paylines;

    // Currently running paylines
    private List<BiggerBassBonanzaPaylineEntry> activePaylines = new List<BiggerBassBonanzaPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<BiggerBassBonanzaPaylineResult> spinResult = new List<BiggerBassBonanzaPaylineResult>();
    private int resultScatterCount;

    [Header("Wild Header")]
    [SerializeField] private List<GameObject> headerWilds;

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

    public void AddPaylineData(BiggerBassBonanzaPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        spinResult.Clear();
    }

    public void ResetHeader()
    {
        for (int i = 0; i < headerWilds.Count; i++)
        {
            headerWilds[i].SetActive(false);
        }
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<BiggerBassBonanzaPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new BiggerBassBonanzaPaylineEntry(
                    paylineData,
                    result.reelLimit
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 3 && BiggerBassBonanzaSlotMachine.Instance.wildWorldPos.Count == 0)
        {
            //Debug.LogWarning("No valid paylines to display.");
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

        ResetAllSlotsAnimation();
    }

    private IEnumerator PlayPaylines()
    {
        if (!BiggerBassBonanzaAutoSpinController.isAutoSpinning && !BiggerBassBonanzaSlotMachine.Instance.isFreeGame && resultScatterCount < 3)
        {
            BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (BiggerBassBonanzaSlotMachine.Instance.wildWorldPos.Count > 0 && BiggerBassBonanzaSlotMachine.Instance.isFreeGame)
        {
            yield return FishCollect();

            if (activePaylines.Count == 0)
            {
                BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
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

                BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted = true;
                }
            }
        }
    }

    private IEnumerator PlaySinglePayline(BiggerBassBonanzaPaylineEntry entry)
    {
        float waitTime = flickerDelay;

        for (int x = 0; x < BiggerBassBonanzaSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = BiggerBassBonanzaSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (activePaylines.Count > 1)
        {
            // Clear borders & text for cycling mode
            ResetAllSlotsAnimation();
            yield return new WaitForSeconds(0.5f);
        }

    }

    private void ResetAllSlotsAnimation()
    {
        foreach (var reel in BiggerBassBonanzaSlotMachine.Instance.reels)
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
    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < BiggerBassBonanzaSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = BiggerBassBonanzaSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == BiggerBassBonanzaSlotType.Scatter)
                {
                    slot.PlayAnimation();
                }
            }
        }

        if (BiggerBassBonanzaSlotMachine.Instance.freeSpinCount > 0 && !BiggerBassBonanzaSlotMachine.Instance.isFreeGame)
        {
            BiggerBassBonanzaSlotMachine.Instance.firstFreeSpin = true;
            BiggerBassBonanzaUIManager.Instance.UpdateButtons("Game Transition");
            BiggerBassBonanzaFreeGameTransitionController.Instance.StartFreeSpinTransition();
            BiggerBassBonanzaFreeGameTransitionController.Instance.UpdateFreeSpinsCount(BiggerBassBonanzaSlotMachine.Instance.freeSpinCount);
        }

        if (activePaylines.Count == 0)
        {
            yield return new WaitForSeconds(2f);
            BiggerBassBonanzaSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion

    #region Cash Collect

    private IEnumerator FishCollect()
    {
        for (int i = 0; i < BiggerBassBonanzaSlotMachine.Instance.wildSlots.Count; i++)
        {
            BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].PlayAnimation();
            
            for (int x = 0; x < BiggerBassBonanzaSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    var slot = BiggerBassBonanzaSlotMachine.Instance.reels[x].slots[y + 1];

                    if (BiggerBassBonanzaSlotMachine.Instance.isFishSlot(slot.slotType))
                    {
                        slot.PlayAnimation();
                        slot.MoveBox(BiggerBassBonanzaSlotMachine.Instance.wildWorldPos[i]);

                        yield return new WaitForSeconds(0.75f);

                        if (BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].textBox.activeSelf == false)
                        {
                            BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].ShowBox();
                        }

                        BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].UpdateBox(slot.GetFishAmount(), slot.textBox);

                        yield return new WaitForSeconds(0.25f);

                        slot.StopAnimation();
                    }
                }
            }

            if (BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount] > 1)
            {
                yield return new WaitForSeconds(0.5f);

                BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].MoveWild(
                    BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].wildMultipliersAnimated[BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].activeMultiplierIndex]
                );

                yield return new WaitForSeconds(0.5f);

                BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].UpdateBox(
                    BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].wildCollectAmount * (BiggerBassBonanzaSlotMachine.Instance.wildMultipliers[BiggerBassBonanzaSlotMachine.Instance.retriggerCount] - 1),
                    BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].wildMultipliersAnimated[BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].activeMultiplierIndex]
                );
            }

            yield return new WaitForSeconds(1f);

            if (BiggerBassBonanzaSlotMachine.Instance.wildCount < 12)
            {
                BiggerBassBonanzaSlotMachine.Instance.wildCount++;

                BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].MoveParticles(headerWilds[BiggerBassBonanzaSlotMachine.Instance.wildCount - 1].transform.position);

                yield return new WaitForSeconds(0.5f);

                BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].HideBox();

                yield return new WaitForSeconds(0.75f);

                headerWilds[BiggerBassBonanzaSlotMachine.Instance.wildCount - 1].SetActive(true);
            }
            else
            {
                BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].HideBox();
            }

            BiggerBassBonanzaSlotMachine.Instance.wildSlots[i].StopAnimation();
        }

        BiggerBassBonanzaSlotMachine.Instance.isFishCollectionCompleted = true;
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class BiggerBassBonanzaPaylineData
{
    public int paylineNumber;

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

public class BiggerBassBonanzaPaylineEntry
{
    public BiggerBassBonanzaPaylineData payline;
    public int reelLimit;

    public BiggerBassBonanzaPaylineEntry(BiggerBassBonanzaPaylineData payline, int reelLimit)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
    }
}

[System.Serializable]
public class BiggerBassBonanzaPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    
    public BiggerBassBonanzaPaylineResult(int paylineNumber, int reelLimit)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
    }
}

#endregion