using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
public class StinkinRichPaylineController : MonoBehaviour
{
    #region Variables

    public static StinkinRichPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<StinkinRichPaylineData> paylines;
    //[SerializeField] private PandaFortuneFreeSpinController freeSpin;
    // Currently running paylines
    public List<StinkinRichPaylineEntry> activePaylines = new List<StinkinRichPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<StinkinRichPaylineResult> spinResult = new List<StinkinRichPaylineResult>();
    private int resultScatterCount;

    //[SerializeField] public GameObject Target;
    //[SerializeField] public TMP_Text Target_Text;
    //[SerializeField] private GameObject overlay;
    //[SerializeField] public GameObject FreeSpinTarget;


    //[HideInInspector] public List<PandaFortuneSlotScript> freeSpinSlots;
    private int freeSpinTotal;
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

    public void AddPaylineData(StinkinRichPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        ResetAllSlotsToDefault();
        spinResult.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<StinkinRichPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new StinkinRichPaylineEntry(
                    paylineData,
                    result.reelLimit,
                    result.winText,
                    result.symbol
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 1)
        {
            //overlay.SetActive(false);
            StinkinRichSlotMachine.Instance.isSlotAnimationCompleted = true;

            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void StopPaylineDisplay()
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

        //overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        //SaharaRichesUIManager.Instance.PlaySound("Payline");
        //if (!SaharaRichesAutoSpinController.isAutoSpinning && !SaharaRichesSlotMachine.Instance.isFreeGame)
        //{
        //    SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted = true;
        //}
        if (StinkinRichSlotMachine.Instance.isFreeGame)
        {
            flickerDelay = 1.5f;
        }
        else
        {
            flickerDelay = 2.5f;
        }
        //overlay.SetActive(true);

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if((StinkinRichSlotMachine.Instance.isFreeGameReady && !StinkinRichSlotMachine.Instance.isFreeGame) || (!StinkinRichSlotMachine.Instance.isFreeGameReady && StinkinRichSlotMachine.Instance.isFreeGame))
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }


        if (activePaylines.Count == 0)
        {
            StinkinRichSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                Invoke("PaylineAnimationCompleted", flickerDelay);

            }
            else
            {
                int i = 0;
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        i++;
                        yield return null;
                        yield return PlaySinglePayline(entry);
                    }
                    if (activePaylines.Count == i)
                    {
                        Invoke("PaylineAnimationCompleted", flickerDelay);
                    }
                }
            }
        }

    }

    public void PaylineAnimationCompleted()
    {
        isShowing = false;
        StinkinRichSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator PlaySinglePayline(StinkinRichPaylineEntry entry)
    {
        for (int x = 0; x < StinkinRichSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = StinkinRichSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        for (int x = 0; x < StinkinRichSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = StinkinRichSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.StopAnimation();
                }
            }
        }

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }
    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in StinkinRichSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    //slot.SetSpriteToDefault();
                    slot.StopAnimation();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < StinkinRichSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = StinkinRichSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == StinkinRichSlotType.Scatter)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }


        if (StinkinRichSlotMachine.Instance.freeSpinCount > 0 && !StinkinRichSlotMachine.Instance.isFreeGame)
        {
            StinkinRichSlotMachine.Instance.firstFreeSpin = true;
            //PandaFortuneUIManager.Instance.UpdateButtons("Transition");
            StinkinRichFreeGameTransitionController.Instance.StartFreeSpins();
            StinkinRichFreeGameTransitionController.Instance.UpdateFreeSpinsCount(StinkinRichSlotMachine.Instance.freeSpinCount);
        }
        else if (StinkinRichSlotMachine.Instance.freeSpinCount > 0 && StinkinRichSlotMachine.Instance.isFreeGame)
        {
            StinkinRichFreeGameTransitionController.Instance.UpdateFreeSpinsCount(StinkinRichSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            StinkinRichSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }


    public void ConvertSlotsToFreeSpinSlots(bool enable)
    {
        for (int x = 0; x < StinkinRichSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = StinkinRichSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                slot.SetFreeSpinSlots(enable);
            }
        }
    }

    #endregion

    #region TrashSlots
    List<StinkinRichSlotScript> trashSlots = new List<StinkinRichSlotScript>();
    public int countOfNonTrashSlotsAnimated = 0;
    public void PlayTrashSlots()
    {
        trashSlots.Clear();
        for (int x = 0; x < StinkinRichSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = StinkinRichSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == StinkinRichSlotType.TrashForCash)
                {
                    trashSlots.Add(slot);
                    slot.PlayTrashAnimations();
                }
            }
        }
    }
    public void ShowTrashMultipliers(StinkinRichSlotScript slot)
    {
        StinkinRichSlotMachine.Instance.hasShowenTrashAnimation = true;
        StartCoroutine(TrashMultiplier(slot));
    }

    private IEnumerator TrashMultiplier(StinkinRichSlotScript slot)
    {
        trashSlots.RemoveAt(trashSlots.IndexOf(slot));
        trashSlots.Insert(0, slot);

        countOfNonTrashSlotsAnimated = 0;

        for(int i = 0; i < trashSlots.Count; i++)
        {
            if (trashSlots[i] == slot)
            {
                trashSlots[i].showMultiplier();
            }
            else
            {
                if (countOfNonTrashSlotsAnimated < 1)
                {
                    yield return new WaitForSeconds(1.5f);
                }
                else yield return new WaitForSeconds(0.5f);
                countOfNonTrashSlotsAnimated++;
                trashSlots[i].ShowNonMultipliers();
                if(countOfNonTrashSlotsAnimated == trashSlots.Count - 1)
                {
                    StinkinRichSlotMachine.Instance.hasShowenTrashMultipliers = true;
                }
            }
        }
    }

    public void StopTrashAnimations()
    {
        foreach (var s in trashSlots)
        {
            s.StopTrashSlotAnimations();
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class StinkinRichPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[25]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 5]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 5; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class StinkinRichPaylineEntry
{
    public StinkinRichPaylineData payline;
    public int reelLimit;
    public string winText;
    public string symbol;
    public StinkinRichPaylineEntry(StinkinRichPaylineData payline, int reelLimit, string winText, string symbol)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        this.symbol = symbol;
        if (float.TryParse(winText, out float parsedValue))
        {
            this.winText = parsedValue.ToString("F2");
        }
        else
        {
            this.winText = winText;
        }
    }
}

[System.Serializable]
public class StinkinRichPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;
    public string symbol;

    public StinkinRichPaylineResult(int paylineNumber, int reelLimit, string winText, string symbol)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
        this.symbol = symbol;
    }
}

#endregion