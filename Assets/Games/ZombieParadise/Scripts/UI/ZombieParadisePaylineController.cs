using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieParadisePaylineController : MonoBehaviour
{
    #region Variables

    public static ZombieParadisePaylineController Instance;

    // Currently running paylines
    private List<ZombieParadisePaylineResult> activePaylines = new List<ZombieParadisePaylineResult>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<ZombieParadisePaylineResult> spinResult = new List<ZombieParadisePaylineResult>();
    private int resultScatterCount;

    //[SerializeField] private GameObject overlay;

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

    public void AddPaylineData(ZombieParadisePaylineResult result)
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

    private void StartPaylineDisplay(List<ZombieParadisePaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();
        //overlay.SetActive(true);

        foreach (var result in results)
        {
            activePaylines.Add(result);
        }

        if (activePaylines.Count == 0 && resultScatterCount > 3)
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

        foreach (var reel in ZombieParadiseSlotMachine.Instance.reels)
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

        //ZombieParadisePaylineDrawer.Instance.ClearPaylines();

        //overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if (!ZombieParadiseAutoSpinController.isAutoSpinning && !ZombieParadiseSlotMachine.Instance.isFreeGame)
        {
            ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted = true;
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

                ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted = true;

                    //if ((ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted && ZombieParadiseAutoSpinController.isAutoSpinning) || ZombieParadiseSlotMachine.Instance.isFreeGame)
                    //{
                    //    break;
                    //}
                }
            }
        }

    }

    private IEnumerator PlaySinglePayline(ZombieParadisePaylineResult entry)
    {
        float waitTime = flickerDelay;
        int paylineLimit = entry.slots.Count;

        for (int x = 0; x < ZombieParadiseSlotMachine.Instance.reels.Count; x++)
        {
            if (x >= paylineLimit) break;

            for (int y = 0; y < 4; y++)
            {
                var slot = ZombieParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.slots[x] == y)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(waitTime);

        if (activePaylines.Count > 1)
        {
            // Clear borders & text for cycling mode
            foreach (var reel in ZombieParadiseSlotMachine.Instance.reels)
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

            //ZombieParadisePaylineDrawer.Instance.ClearPaylines();
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < ZombieParadiseSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                var slot = ZombieParadiseSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == ZombieParadiseSlotType.Scatter)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        if (ZombieParadiseSlotMachine.Instance.freeSpinCount > 0 && !ZombieParadiseSlotMachine.Instance.isFreeGame)
        {
            ZombieParadiseSlotMachine.Instance.firstFreeSpin = true;
            ZombieParadiseUIManager.Instance.UpdateButtons("Transition Start");
            ZombieParadiseFreeGameTransitionController.Instance.StartFreeSpinTransition();
            ZombieParadiseFreeGameTransitionController.Instance.UpdateFreeSpinsCount(ZombieParadiseSlotMachine.Instance.freeSpinCount);
        }
        else if (ZombieParadiseSlotMachine.Instance.freeSpinCount > 0)
        {
            ZombieParadiseFreeGameTransitionController.Instance.UpdateFreeSpinsCount(ZombieParadiseSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class ZombieParadisePaylineResult
{
    public List<int> slots;

    public ZombieParadisePaylineResult(List<int> slotNumbers)
    {
        this.slots = slotNumbers;
    }
}

#endregion