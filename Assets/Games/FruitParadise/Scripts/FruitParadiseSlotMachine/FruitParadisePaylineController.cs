using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class FruitParadisePaylineController : MonoBehaviour
{
    #region Variables

    public static FruitParadisePaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<FruitParadisePaylineData> paylines;

    // Currently running paylines
    private List<FruitParadisePaylineEntry> activePaylines = new List<FruitParadisePaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<FruitParadisePaylineResult> spinResult = new List<FruitParadisePaylineResult>();
    private int resultScatterCount;
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
    public void AddPaylineData(FruitParadisePaylineResult result)
    {
        if (spinResult.Contains(result))
        {
            return;
        }
        spinResult.Add(result);
    }
    public void ClearPaylineData()
    {
        StopPaylineDisplay(); // <- make sure the loop ends

        foreach (var entry in activePaylines)
        {
            for (int x = 0; x < FruitParadiseSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = FruitParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        slot.StopAnimation();
                    }
                }
            }
        }
        for (int x = 0; x < FruitParadiseSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot != null && slot.type == FruitParadiseSlotType.Scatter)
                    slot.StopAnimation();
            }
        }

        spinResult.Clear();
        activePaylines.Clear();
    }

    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<FruitParadisePaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new FruitParadisePaylineEntry(
                    paylineData, result.reelLimit
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

    public void ShowCollectedPaylines(int scatterCount)
    {
        resultScatterCount = scatterCount;
        // If no wins, immediately mark complete so auto-spin can proceed
        if (spinResult == null && spinResult.Count == 0 && resultScatterCount < 3)
        {
            FruitParadiseSlotMachine.Instance.isPaylineCompleted = true;
            return;
        }

        StartPaylineDisplay(spinResult); // <-- your existing private method
    }
    private void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }
    }


    private IEnumerator PlayPaylines()
    {
        if (activePaylines == null && activePaylines.Count == 0 && resultScatterCount < 3)
        {
            FruitParadiseSlotMachine.Instance.isPaylineCompleted = true;
            yield break;
        }

        if (!FruitParadiseAutoSpinController.isAutoSpinning && !FruitParadiseSlotMachine.Instance.isFreeGame && resultScatterCount < 3)
        {
            FruitParadiseSlotMachine.Instance.isPaylineCompleted = true;
        }

        if (resultScatterCount >= 3)
        {
            yield return ScatterAnimation();
        }

        if (activePaylines.Count == 1)
        {
            yield return PlayAllPaylines();
        }
        else
        {
            if (FruitParadiseAutoSpinController.isAutoSpinning)
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

        FruitParadiseSlotMachine.Instance.isPaylineCompleted = true;
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
                for (int x = 0; x < FruitParadiseSlotMachine.Instance.reels.Count; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (pl.payline.ToMatrix()[x, y] == 1 && x < pl.reelLimit)
                        {
                            if (y + 1 >= FruitParadiseSlotMachine.Instance.reels[x].slots.Count)
                                continue;

                            var slot = FruitParadiseSlotMachine.Instance.reels[x].slots[y + 1];
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

            for (int x = 0; x < FruitParadiseSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (currentPayline.payline.ToMatrix()[x, y] == 1 && x < currentPayline.reelLimit)
                    {
                        if (y + 1 >= FruitParadiseSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = FruitParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot == null) continue;

                        if (currentPayline.payline.ToMatrix()[x, y] == 1)
                        {
                            slot.PlayAnimation();
                        }
                    }
                }
            }

            yield return new WaitForSeconds(1.8f);
            if (index + 1 == maxIndex && (FruitParadiseAutoSpinController.isAutoSpinning || FruitParadiseSlotMachine.Instance.isFreeGame))
            {
                FruitParadiseSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }
            index++;
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < FruitParadiseSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == FruitParadiseSlotType.Scatter || slot.type == FruitParadiseSlotType.Bonus)
                {
                    slot.PlayAnimation();
                }
            }
        }

        if (FruitParadiseSlotMachine.Instance.freeSpinCount > 0 && !FruitParadiseSlotMachine.Instance.isFreeGame)
        {
            FruitParadiseSlotMachine.Instance.firstFreeSpin = true;
            FruitParadiseUIManager.Instance.UpdateButtons("Transition Start");
            FruitParadiseFreeGameTransitionController.Instance.StartFreeSpinTransition();
            FruitParadiseFreeGameTransitionController.Instance.UpdateFreeSpinsCount(FruitParadiseSlotMachine.Instance.freeSpinCount);
        }
        else if (FruitParadiseSlotMachine.Instance.freeSpinCount > 0)
        {
            FruitParadiseFreeGameTransitionController.Instance.UpdateFreeSpinsCount(FruitParadiseSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            FruitParadiseSlotMachine.Instance.isPaylineCompleted = true;
        }

        for (int x = 0; x < FruitParadiseSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == FruitParadiseSlotType.Scatter || slot.type == FruitParadiseSlotType.Bonus)
                {
                    slot.StopAnimation();
                }
            }
        }
    }
    #endregion
}
