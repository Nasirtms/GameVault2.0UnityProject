using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class FruitSlotPaylineController : MonoBehaviour
{
    #region Variables

    public static FruitSlotPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<FruitSlotPaylineData> paylines;

    // Currently running paylines
    private List<FruitSlotPaylineEntry> activePaylines = new List<FruitSlotPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<FruitSlotPaylineResult> spinResult = new List<FruitSlotPaylineResult>();
    private int resultScatterCount;
    private Coroutine scatterAnimation;
    [Header("Wild Combination Images")]
    [SerializeField] private GameObject wild_1_2;
    [SerializeField] private GameObject wild_2_3;
    [SerializeField] private GameObject wild_3_4;
    [SerializeField] private GameObject wild_1_2_3;
    [SerializeField] private GameObject wild_2_3_4;
    [SerializeField] private GameObject wild_3_4_5;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References
    public void AddPaylineData(FruitSlotPaylineResult result)
    {
        if (spinResult.Contains(result))
        {
            return;
        }
            spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        isShowing = false;

        foreach (var entry in activePaylines)
        {
            for (int x = 0; x < FruitSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = FruitSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        slot.StopAnimation();
                    }
                }
            }
        }
        for (int x = 0; x < FruitSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = FruitSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot != null && slot.type == FruitSlotType.WILD)
                    slot.StopAnimation();
            }
        }
        spinResult.Clear();
        activePaylines.Clear();
    }
    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<FruitSlotPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new FruitSlotPaylineEntry(
                    paylineData, result.reelLimit
                ));
            }
        }
        //if (activePaylines.Count == 0 && resultScatterCount < 6)
        //{
        //    return;
        //}
        isShowing = true;

        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void ShowCollectedPaylines(int scatterCount)
    {
        resultScatterCount = scatterCount;
        // If no wins, immediately mark complete so auto-spin can proceed
        if (spinResult == null || spinResult.Count == 0)
        {
            FruitSlotMachine.Instance.isPaylineCompleted = true;
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
        if (activePaylines == null || activePaylines.Count == 0 && resultScatterCount < 6)
        {
            FruitSlotMachine.Instance.isPaylineCompleted = true;
            yield break;
        }
        if (!FruitSlotAutoSpinController.isAutoSpinning && !FruitSlotMachine.Instance.isFreeGame && resultScatterCount < 6)
        {
            FruitSlotMachine.Instance.isPaylineCompleted = true;
        }
        if (resultScatterCount >= 6)
        {
            yield return ScatterAnimation();
        }
        if (activePaylines.Count == 1)
        {
            yield return PlayAllPaylines();
        }
        else
        {
            if (FruitSlotAutoSpinController.isAutoSpinning)
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

        FruitSlotMachine.Instance.isPaylineCompleted = true;
    }
    private IEnumerator PlayAllPaylines()
    {
        int index = 0;
        int maxIndex = activePaylines.Count;
        while (true)
        {
            if (activePaylines == null || activePaylines.Count == 0)
            {
                yield return null;
                continue;
            }

            foreach (var pl in activePaylines)
            {
                for (int x = 0; x < FruitSlotMachine.Instance.reels.Count; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (pl.payline.ToMatrix()[x, y] == 1 && x < pl.reelLimit)
                        {
                            if (y + 1 >= FruitSlotMachine.Instance.reels[x].slots.Count)
                                continue;

                            var slot = FruitSlotMachine.Instance.reels[x].slots[y + 1];
                            if (slot == null) continue;

                            if (pl.payline.ToMatrix()[x, y] == 1)
                                slot.StopAnimation();
                        }
                    }
                }
            }

            // safe wrap
            if (activePaylines.Count == 0) yield break;
            index = index % activePaylines.Count;

            var currentPayline = activePaylines[index];

            for (int x = 0; x < FruitSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (currentPayline.payline.ToMatrix()[x, y] == 1 && x < currentPayline.reelLimit)
                    {
                        if (y + 1 >= FruitSlotMachine.Instance.reels[x].slots.Count)
                            continue;

                        var slot = FruitSlotMachine.Instance.reels[x].slots[y + 1];
                        if (slot == null) continue;

                        if (currentPayline.payline.ToMatrix()[x, y] == 1)
                            slot.PlayAnimation();
                    }
                }
            }

            yield return new WaitForSeconds(1.8f);
            if (index + 1 == maxIndex && (FruitSlotAutoSpinController.isAutoSpinning || FruitSlotMachine.Instance.isFreeGame))
            {
                FruitSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }
            index++;
        }
    }
    private IEnumerator ScatterAnimation()
    {
        var reels = FruitSlotMachine.Instance.reels;
        bool[] hasWildOnReel = new bool[reels.Count];

        // Step A — detect wilds per reel
        for (int x = 0; x < reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = reels[x].slots[y + 1];
                if (slot == null) continue;
                if (slot.type == FruitSlotType.WILD)
                {
                    hasWildOnReel[x] = true;
                    break;
                }
            }
        }
        //hardcode
        //hasWildOnReel[1] = true;
        //hasWildOnReel[2] = true;
        //hasWildOnReel[3] = true;
        // Step B — check consecutive wilds
        if (hasWildOnReel[1] && hasWildOnReel[2] && !hasWildOnReel[3] && !hasWildOnReel[4]) wild_1_2.SetActive(true);
        if (!hasWildOnReel[1] && hasWildOnReel[2] && hasWildOnReel[3] && !hasWildOnReel[4]) wild_2_3.SetActive(true);
        if (!hasWildOnReel[1] && !hasWildOnReel[2] && hasWildOnReel[3] && hasWildOnReel[4]) wild_3_4.SetActive(true);

        // Step C — check 3-reel combinations
        if (hasWildOnReel[1] && hasWildOnReel[2] && hasWildOnReel[3] && !hasWildOnReel[4]) wild_1_2_3.SetActive(true);
        if (!hasWildOnReel[1] && hasWildOnReel[2] && hasWildOnReel[3] && hasWildOnReel[4]) wild_2_3_4.SetActive(true);
        if (hasWildOnReel[1] && hasWildOnReel[2] && hasWildOnReel[3] && hasWildOnReel[4]) wild_3_4_5.SetActive(true);

        yield return new WaitForSeconds(3f);
        wild_1_2.SetActive(false);
        wild_2_3.SetActive(false);
        wild_3_4.SetActive(false);
        wild_1_2_3.SetActive(false);
        wild_2_3_4.SetActive(false);
        wild_3_4_5.SetActive(false);
        if (FruitSlotMachine.Instance.freeSpinCount > 0 && !FruitSlotMachine.Instance.isFreeGame)
        {
            FruitSlotMachine.Instance.firstFreeSpin = true;
            FruitSlotUIManager.Instance.UpdateButtons("Transition Start");
            FruitSlotGameTransitionController.Instance.StartFreeSpinTransition();
            FruitSlotGameTransitionController.Instance.UpdateFreeSpinsCount(FruitSlotMachine.Instance.freeSpinCount);
        }
        else if (FruitSlotMachine.Instance.freeSpinCount > 0)
        {
            FruitSlotGameTransitionController.Instance.UpdateFreeSpinsCount(FruitSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            FruitSlotMachine.Instance.isPaylineCompleted = true;
        }
}
    #endregion
}
