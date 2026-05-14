using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldGobblersPaylineController : MonoBehaviour
{
    #region Variables

    public static GoldGobblersPaylineController Instance;

    // Currently running paylines
    private List<GoldGobblersPaylineResult> activePaylines = new List<GoldGobblersPaylineResult>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    private Coroutine tunnelAnimationRoutine;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<GoldGobblersPaylineResult> spinResult = new List<GoldGobblersPaylineResult>();
    private int resultScatterCount;
    private bool resultTunnelSlot;
    private int topHidden = 2;
    private int bottomHidden = 1;

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

    public void StartPayline(int scatterCount, bool tunnelslot)
    {
        resultScatterCount = scatterCount;
        resultTunnelSlot = tunnelslot;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(GoldGobblersPaylineResult result)
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

    private void  StartPaylineDisplay(List<GoldGobblersPaylineResult> results)
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

        foreach (var reel in GoldGobblersSlotMachine.Instance.reels)
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
        if (!GoldGobblersAutoSpinController.isAutoSpinning && !GoldGobblersSlotMachine.Instance.isFreeGame)
        {
            GoldGobblersSlotMachine.Instance.isSlotAnimationCompleted = true;
        }

        if (resultScatterCount >= 3)
        {
            scatterAnimation = StartCoroutine(ScatterAnimation());
        }

        if(resultTunnelSlot)
        {
            tunnelAnimationRoutine = StartCoroutine(tunnelAnimation());
        }

        if(GoldGobblersSlotMachine.Instance.isFreeGameReady ||(!GoldGobblersSlotMachine.Instance.isFreeGameReady && GoldGobblersSlotMachine.Instance.isFreeGame))
        {
            FreeGame();
        }

        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);

                GoldGobblersSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
            else
            {
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    GoldGobblersSlotMachine.Instance.isSlotAnimationCompleted = true;

                    //if ((ZombieParadiseSlotMachine.Instance.isSlotAnimationCompleted && ZombieParadiseAutoSpinController.isAutoSpinning) || ZombieParadiseSlotMachine.Instance.isFreeGame)
                    //{
                    //    break;
                    //}
                }
            }
        }

    }

    private IEnumerator PlaySinglePayline(GoldGobblersPaylineResult entry)
    {
        float waitTime = flickerDelay;
        int paylineLimit = entry.slots.Count;

        for (int x = 0; x < GoldGobblersSlotMachine.Instance.reels.Count; x++)
        {
            if (x >= paylineLimit) break;
            var reel = GoldGobblersSlotMachine.Instance.reels[x];
            int totalSlots = reel.slots.Count;

            int visibleSlots = totalSlots - topHidden - bottomHidden;
            int visibleStartSlot = topHidden;

            for (int y = 0; y < visibleSlots; y++)
            {
                var slot = reel.slots[visibleStartSlot + y];
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
            foreach (var reel in GoldGobblersSlotMachine.Instance.reels)
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
            yield return new WaitForSeconds(0.5f);
            //ZombieParadisePaylineDrawer.Instance.ClearPaylines();
        }
    }

    private IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < GoldGobblersSlotMachine.Instance.reels.Count; x++)
        {
            var reel = GoldGobblersSlotMachine.Instance.reels[x];
            int totalSlots = reel.slots.Count;

            int visibleSlots = totalSlots - topHidden - bottomHidden;
            int visibleStartSlot = topHidden;
            for (int y = 0; y < visibleSlots; y++)
            {
                var slot = reel.slots[visibleStartSlot + y];
                if (slot == null) continue;

                if (slot.slotType == GoldGobblersSlotType.RedGem)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(2f);

        if (activePaylines.Count == 0)
        {
            GoldGobblersSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }
    private IEnumerator tunnelAnimation()
    {
        int i = 0;
        for (int x = 0; x < GoldGobblersSlotMachine.Instance.reels.Count; x++)
        {
            var reel = GoldGobblersSlotMachine.Instance.reels[x];
            int totalSlots = reel.slots.Count;

            int visibleSlots = totalSlots - topHidden - bottomHidden;
            int visibleStartSlot = topHidden;

            for (int y = 0; y < visibleSlots; y++)
            {
                var slot = reel.slots[visibleStartSlot + y];
                if (slot == null) continue;

                if (slot.slotType == GoldGobblersSlotType.MineEntrance)
                {
                    slot.SetSpriteToPayline();
                    if (i < GoldGobblersSlotMachine.Instance.tunnelSlotFreeSpinCount.Count)
                    {
                        slot.tunnelSlotText.text = tunnelSlotText(GoldGobblersSlotMachine.Instance.tunnelSlotFreeSpinCount[i]);
                        i++;
                    }
                    slot.PlayAnimation();
                }
            }
        }
        yield return new WaitForSeconds(3f);
        GoldGobblersSlotMachine.Instance.showingTunnelSlotAnimation = false;
    }

    public void FreeGame()
    {
        if (GoldGobblersSlotMachine.Instance.freeSpinCount > 0 && !GoldGobblersSlotMachine.Instance.isFreeGame)
        {
            GoldGobblersSlotMachine.Instance.firstFreeSpin = true;
            GoldGobblersUIManager.Instance.UpdateButtons("Transition Start");
            GoldGobblersFreeGameTransitionController.Instance.StartFreeSpinTransition();
            GoldGobblersFreeGameTransitionController.Instance.UpdateFreeSpinsCount(GoldGobblersSlotMachine.Instance.freeSpinCount);
        }
        else if (GoldGobblersSlotMachine.Instance.freeSpinCount > 0 && GoldGobblersSlotMachine.Instance.isFreeGame)
        {
            GoldGobblersFreeGameTransitionController.Instance.UpdateFreeSpinsCount(GoldGobblersSlotMachine.Instance.freeSpinCount);
            
            if(GoldGobblersSlotMachine.Instance.hasNewFreeGameTriggeredInBetween)
            {
                GoldGobblersFreeGameTransitionController.Instance.MiddleGroundFreeGameTransition();
            }
        }
    }

    public void ChangeSlotScale(bool isFreegame)
    {
        foreach (var reel in GoldGobblersSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.ChangeScaleYOfSlots(isFreegame);
                }
            }
        }
    }

    private string tunnelSlotText(int freespinCount)
    {
        // Convert number to string so we can iterate digits
        string number = freespinCount.ToString();

        // Start with opening sprite
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append("<sprite index=10>");

        // Loop through each digit
        foreach (char digit in number)
        {
            result.Append($"<sprite index={digit}>");
        }

        // Add closing sprite
        result.Append("<sprite index=11>");

        return result.ToString();
    }

    #endregion
}

#region Support Classes

[System.Serializable]
public class GoldGobblersPaylineResult
{
    public List<int> slots;

    public GoldGobblersPaylineResult(List<int> slotNumbers)
    {
        this.slots = slotNumbers;
    }
}

#endregion