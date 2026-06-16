using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class RichLittlePiggiesPaylineController : MonoBehaviour
{
    #region Variables

    public static RichLittlePiggiesPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<RichLittlePiggiesPaylineData> paylines;
    //[SerializeField] private PandaFortuneFreeSpinController freeSpin;
    // Currently running paylines
    public List<RichLittlePiggiesPaylineEntry> activePaylines = new List<RichLittlePiggiesPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true
    private bool hasRevealed = false;

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<RichLittlePiggiesPaylineResult> spinResult = new List<RichLittlePiggiesPaylineResult>();
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

    public void AddPaylineData(RichLittlePiggiesPaylineResult result)
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

    private void StartPaylineDisplay(List<RichLittlePiggiesPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new RichLittlePiggiesPaylineEntry(
                    paylineData,
                    result.reelLimit,
                    result.winText
                ));
            }
        }

        if (activePaylines.Count == 0 && resultScatterCount < 1)
        {
            //overlay.SetActive(false);
            RichLittlePiggiesSlotMachine.Instance.isSlotAnimationCompleted = true;

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
        if (RichLittlePiggiesSlotMachine.Instance.isFreeGame)
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


        if (activePaylines.Count == 0)
        {
            RichLittlePiggiesSlotMachine.Instance.isSlotAnimationCompleted = true;
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
        RichLittlePiggiesSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator PlaySinglePayline(RichLittlePiggiesPaylineEntry entry)
    {
        yield return new WaitUntil(() => hasRevealed);
        yield return new WaitForSeconds(0.5f);
        for (int x = 0; x < RichLittlePiggiesSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = RichLittlePiggiesSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.PlayAnimation();
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        for (int x = 0; x < RichLittlePiggiesSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = RichLittlePiggiesSlotMachine.Instance.reels[x].slots[y + 1];
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

    public void RevealSlots()
    {
        StartCoroutine(RevealSlotsRoutine());
    }

    private IEnumerator RevealSlotsRoutine()
    {
        hasRevealed = false;
        for (int x = 0; x < RichLittlePiggiesSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = RichLittlePiggiesSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (x == RichLittlePiggiesSlotMachine.Instance.x || x == RichLittlePiggiesSlotMachine.Instance.y)
                {
                    slot.RevealSlots();
                }
            }
        }
        hasRevealed = true;
        yield return null;
    }

    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in RichLittlePiggiesSlotMachine.Instance.reels)
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
        yield return new WaitUntil(() => hasRevealed);
        for (int x = 0; x < RichLittlePiggiesSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = RichLittlePiggiesSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == RichLittlePiggiesSlotType.RedCoin || slot.slotType == RichLittlePiggiesSlotType.YellowCoin || slot.slotType == RichLittlePiggiesSlotType.BlueCoin)
                {
                    //slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }


        if (RichLittlePiggiesSlotMachine.Instance.freeSpinCount > 0 && !RichLittlePiggiesSlotMachine.Instance.isFreeGame)
        {
            RichLittlePiggiesSlotMachine.Instance.firstFreeSpin = true;
            ////PandaFortuneUIManager.Instance.UpdateButtons("Transition");
            RichLittlePiggiesGameTransitionController.Instance.StartFreeSpins();
            RichLittlePiggiesFreeSpinController.Instance.UpdateFreeSpins(RichLittlePiggiesSlotMachine.Instance.freeSpinCount);
        }
        else if (RichLittlePiggiesSlotMachine.Instance.freeSpinCount > 0 && RichLittlePiggiesSlotMachine.Instance.isFreeGame)
        {
            RichLittlePiggiesFreeSpinController.Instance.UpdateFreeSpins(RichLittlePiggiesSlotMachine.Instance.freeSpinCount);
        }

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            RichLittlePiggiesSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }
    #endregion
}