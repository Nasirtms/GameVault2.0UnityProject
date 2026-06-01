using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
public class UltimateFireLinkOlveraStreetPaylineController : MonoBehaviour
{
    #region Variables

    public static UltimateFireLinkOlveraStreetPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<UltimateFireLinkOlveraStreetPaylineData> paylines;
    //[SerializeField] private PandaFortuneFreeSpinController freeSpin;
    // Currently running paylines
    public List<UltimateFireLinkOlveraStreetPaylineEntry> activePaylines = new List<UltimateFireLinkOlveraStreetPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<UltimateFireLinkOlveraStreetPaylineResult> spinResult = new List<UltimateFireLinkOlveraStreetPaylineResult>();
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

    public void AddPaylineData(UltimateFireLinkOlveraStreetPaylineResult result)
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

    private void StartPaylineDisplay(List<UltimateFireLinkOlveraStreetPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new UltimateFireLinkOlveraStreetPaylineEntry(
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
            UltimateFireLinkOlveraStreetSlotMachine.Instance.isSlotAnimationCompleted = true;

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
        if (UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGame)
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

        if((UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGameReady && !UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGame) || (!UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGameReady && UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGame))
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }


        if (activePaylines.Count == 0)
        {
            UltimateFireLinkOlveraStreetSlotMachine.Instance.isSlotAnimationCompleted = true;
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
        UltimateFireLinkOlveraStreetSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator PlaySinglePayline(UltimateFireLinkOlveraStreetPaylineEntry entry)
    {
        for (int x = 0; x < UltimateFireLinkOlveraStreetSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = UltimateFireLinkOlveraStreetSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        for (int x = 0; x < UltimateFireLinkOlveraStreetSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = UltimateFireLinkOlveraStreetSlotMachine.Instance.reels[x].slots[y + 1];
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
        foreach (var reel in UltimateFireLinkOlveraStreetSlotMachine.Instance.reels)
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
        for (int x = 0; x < UltimateFireLinkOlveraStreetSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                var slot = UltimateFireLinkOlveraStreetSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == UltimateFireLinkOlveraStreetSlotType.Wild)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }


        if (UltimateFireLinkOlveraStreetSlotMachine.Instance.freeSpinCount > 0 && !UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGame)
        {
            UltimateFireLinkOlveraStreetSlotMachine.Instance.firstFreeSpin = true;
            //PandaFortuneUIManager.Instance.UpdateButtons("Transition");
            UltimateFireLinkOlveraStreetFreeGameTransitionController.Instance.StartFreeSpins();
            UltimateFireLinkOlveraStreetFreeGameTransitionController.Instance.UpdateFreeSpinsCount(UltimateFireLinkOlveraStreetSlotMachine.Instance.freeSpinCount);
        }
        else if (UltimateFireLinkOlveraStreetSlotMachine.Instance.freeSpinCount > 0 && UltimateFireLinkOlveraStreetSlotMachine.Instance.isFreeGame)
        {
            UltimateFireLinkOlveraStreetFreeGameTransitionController.Instance.UpdateFreeSpinsCount(UltimateFireLinkOlveraStreetSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            UltimateFireLinkOlveraStreetSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class UltimateFireLinkOlveraStreetPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[20]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 5]; // [columns, rows]
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

public class UltimateFireLinkOlveraStreetPaylineEntry
{
    public UltimateFireLinkOlveraStreetPaylineData payline;
    public int reelLimit;
    public string winText;
    public string symbol;
    public UltimateFireLinkOlveraStreetPaylineEntry(UltimateFireLinkOlveraStreetPaylineData payline, int reelLimit, string winText, string symbol)
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
public class UltimateFireLinkOlveraStreetPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;
    public string symbol;

    public UltimateFireLinkOlveraStreetPaylineResult(int paylineNumber, int reelLimit, string winText, string symbol)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
        this.symbol = symbol;
    }
}

#endregion