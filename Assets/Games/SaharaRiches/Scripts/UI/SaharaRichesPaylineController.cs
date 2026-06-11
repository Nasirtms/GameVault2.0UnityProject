using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
public class SaharaRichesPaylineController : MonoBehaviour
{
    #region Variables

    public static SaharaRichesPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<SaharaRichesPaylineData> paylines;
    [SerializeField] private SaharaRichesFreeSpinController freeSpin;
    // Currently running paylines
    public List<SaharaRichesPaylineEntry> activePaylines = new List<SaharaRichesPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 1.5f;         // Each payline duration
    private Coroutine animationLoop;
    private Coroutine scatterAnimation;
    public bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<SaharaRichesPaylineResult> spinResult = new List<SaharaRichesPaylineResult>();
    private int resultfreespinCount;

    [SerializeField] public GameObject Target;
    [SerializeField] public TMP_Text Target_Text;
    [SerializeField] private GameObject overlay;
    [SerializeField] public GameObject FreeSpinTarget;

    [Header("Free Spin UI")]
    [SerializeField] private TMP_Text freeSpinCollectorText;

    [HideInInspector] public List<SaharaRichesSlotScript> freeSpinSlots;
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
    public void StartPayline(int freeSpinCount)
    {
        resultfreespinCount = freeSpinCount;
        StartPaylineDisplay(spinResult);
    }

    public void StopPaylines()
    {
        StopPaylineDisplay();
    }

    public void AddPaylineData(SaharaRichesPaylineResult result)
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

    private void StartPaylineDisplay(List<SaharaRichesPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new SaharaRichesPaylineEntry(
                    paylineData,
                    result.reelLimit,
                    result.winText
                ));
            }
        }
        var total_count = SaharaRichesSlotMachine.Instance.CC_Count + SaharaRichesSlotMachine.Instance.Diamond_Count + SaharaRichesSlotMachine.Instance.FreeSpin_Count;
            
        if (activePaylines.Count == 0 && SaharaRichesSlotMachine.Instance.cashCollectCount == 0 && total_count == 0)
        {
            Debug.LogWarning("No valid paylines to display.");
            overlay.SetActive(false);
            SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted = true;

            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    public bool isJackotGame;
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

        ResetAllSlotsToDefault();

        overlay.SetActive(false);
    }

    private IEnumerator PlayPaylines()
    {
        if (SaharaRichesSlotMachine.Instance.isFreeGame)
        {
            flickerDelay = 1f;
        }
        else
        {
            flickerDelay = 2.5f;
        }
        overlay.SetActive(true);


        if (SaharaRichesSlotMachine.Instance.cashCollectCount > 0)
        {
            if (SaharaRichesSlotMachine.Instance.Diamond_Count > 0)
            {
                yield return DiamondCollect();
            }
            else
            {
                SaharaRichesSlotMachine.Instance.isDiamondCollectionCompleted = true;
            }
            yield return new WaitForSeconds(1f);

            if (SaharaRichesSlotMachine.Instance.CC_Count > 0)
            {
                yield return CoinCollect();
            }
            else
            {
                SaharaRichesSlotMachine.Instance.isCoinCollectionCompleted = true;
            }

            yield return new WaitForSeconds(1f);

            if (SaharaRichesSlotMachine.Instance.FreeSpin_Count > 0)
            {
                yield return FreeSpinCollect();
            }
            else
            {
                SaharaRichesSlotMachine.Instance.isFreeSpinCollectionCompleted = true;
            }


            if (activePaylines.Count == 0)
            {
                SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted = true;
            }
        }
        if (activePaylines.Count > 0)
        {
            if (activePaylines.Count == 1)
            {
                yield return PlaySinglePayline(activePaylines[0]);
                
                Invoke("PaylineAnimationCompleted", 2f);

            }
            else
            {
                int i = 0;
                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        i++;
                        ResetAllSlotsToDefault();
                        yield return null;
                        yield return PlaySinglePayline(entry);
                    }
                    if (activePaylines.Count == i)
                    {
                        Invoke("PaylineAnimationCompleted", 2f);
                    }
                }
            }
        }
    }

    public void PaylineAnimationCompleted()
    {
        isShowing = false;
        SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted = true;
    }
    private IEnumerator PlaySinglePayline(SaharaRichesPaylineEntry entry)
    {
        for (int x = 0; x < SaharaRichesSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = SaharaRichesSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (entry.payline.ToMatrix()[x, y] == 1 && x < entry.reelLimit)
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                    if (x == entry.reelLimit - 1)
                    {
                        slot.SetTextGroupVisible(true);
                        slot.SetWinText(entry.winText);
                    }
                }
            }
        }

        yield return new WaitForSeconds(flickerDelay);

        if (activePaylines.Count > 1)
        {
            ResetAllSlotsToDefault();
        }
    }
    private void ResetAllSlotsToDefault()
    {
        foreach (var reel in SaharaRichesSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSpriteToDefault();
                    slot.StopAnimation();
                    slot.HideAllVisualOverlays();
                }
            }
        }
    }

    public IEnumerator ScatterAnimation()
    {
        for (int x = 0; x < SaharaRichesSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = SaharaRichesSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.slotType == SaharaRichesSlotType.CashCollect && (slot.slotType == SaharaRichesSlotType.FreeSpin3  || slot.slotType == SaharaRichesSlotType.FreeSpin4
                    || slot.slotType == SaharaRichesSlotType.FreeSpin5 || slot.slotType == SaharaRichesSlotType.FreeSpin10))
                {
                    slot.SetSpriteToPayline();
                    slot.PlayAnimation();
                }
            }
        }

        if (SaharaRichesSlotMachine.Instance.freeSpinCount > 0 && !SaharaRichesSlotMachine.Instance.isFreeGame)
        {
            SaharaRichesSlotMachine.Instance.firstFreeSpin = true;
            SaharaRichesUIManager.Instance.UpdateButtons("Transition Start");
            SaharaRichesFreeGameTransitionController.Instance.StartFreeSpinTransition();
            SaharaRichesFreeGameTransitionController.Instance.UpdateFreeSpinsCount(SaharaRichesSlotMachine.Instance.freeSpinCount);
        }
        //else if (SaharaRichesSlotMachine.Instance.freeSpinCount > 0 && SaharaRichesSlotMachine.Instance.isFreeGame)
        //{
        //    SaharaRichesFreeGameTransitionController.Instance.UpdateFreeSpinsCount(SaharaRichesSlotMachine.Instance.freeSpinCount);
        //}

        yield return new WaitForSeconds(1f);

        if (activePaylines.Count == 0)
        {
            SaharaRichesSlotMachine.Instance.isSlotAnimationCompleted = true;
        }
    }

    #endregion

    #region Cash Collect

    public IEnumerator CoinCollect()
    {
        for (int i = 0; i < SaharaRichesSlotMachine.Instance.cashCollectSlots.Count; i++)
        {
            SaharaRichesSlotMachine.Instance.cashCollectSlots[i].PlayAnimation();

            for (int x = 0; x < SaharaRichesSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = SaharaRichesSlotMachine.Instance.reels[x].slots[y + 1];

                    if (SaharaRichesSlotMachine.Instance.isCCSlot(slot.slotType))
                    {
                        slot.PlayAnimation();
                        Target.SetActive(true);
                        slot.MoveCCParticles(Target_Text.transform.position);
                        yield return new WaitForSeconds(0.75f);

                        SaharaRichesSlotMachine.Instance.cashCollectSlots[i].UpdateBox(slot.GetCCAmount(), slot.textBox);

                        yield return new WaitForSeconds(0.5f);
                        slot.StopAnimation();
                        //Target.SetActive(false);
                    }
                }
            }

            SaharaRichesSlotMachine.Instance.cashCollectSlots[i].StopAnimation();
        }
        SaharaRichesSlotMachine.Instance.isCoinCollectionCompleted = true;
    }
    public IEnumerator DiamondCollect()
    {
        if (SaharaRichesSlotMachine.Instance.cashCollectCount > 0)
        {
            SaharaRichesSlotMachine.Instance.jackpotGamePlay();
            yield return new WaitUntil(() => SaharaRichesJackpotAnimator.Instance.isJackpotCompleted);
        }
        
        for (int i = 0; i < SaharaRichesSlotMachine.Instance.cashCollectSlots.Count; i++)
        {
            SaharaRichesSlotMachine.Instance.cashCollectSlots[i].PlayAnimation();

            for (int x = 0; x < SaharaRichesSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = SaharaRichesSlotMachine.Instance.reels[x].slots[y + 1];

                    if (SaharaRichesSlotMachine.Instance.isDiamondSlot(slot.slotType))
                    {
                        slot.PlayAnimation();
                        yield return new WaitForSeconds(1.5f);
                        Target.SetActive(true);
                        
                        slot.MoveDiamondParticles(Target.transform.position);
                        yield return new WaitForSeconds(0.75f);
                        SaharaRichesSlotMachine.Instance.cashCollectSlots[i].UpdateBox(slot.GetDiamondAmount(), slot.textBox);
                        yield return new WaitForSeconds(0.5f);
                        slot.StopAnimation();
                        //Target.SetActive(false);
                    }
                }
            }
            SaharaRichesSlotMachine.Instance.cashCollectSlots[i].StopAnimation();
        }
        SaharaRichesSlotMachine.Instance.isDiamondCollectionCompleted = true;
    }

    private void UpdateCollectorNumber(int total)
    {
        if (!freeSpinCollectorText) return;
        freeSpinCollectorText.text = ToSpriteDigits(total);
        freeSpinCollectorText.transform.DOKill();
        freeSpinCollectorText.transform.DOPunchScale(Vector3.one * 0.12f, 0.28f, 5, 0.9f);
    }
    private string ToSpriteDigits(int value)
    {
        var s = value.ToString();
        var sb = new StringBuilder();

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (ch >= '0' && ch <= '9')
                sb.Append($"<sprite index={(ch - '0')}>");
            else
                sb.Append(ch);
        }
        Debug.Log("sb.ToString() " + sb.ToString());
        return sb.ToString();
    }
    public void FreeSpinPosCollect()
    {
        freeSpinSlots.Clear();

        //for (int i = 0; i < SaharaRichesSlotMachine.Instance.cashCollectSlots.Count; i++)
        //{
            for (int x = 0; x < SaharaRichesSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = SaharaRichesSlotMachine.Instance.reels[x].slots[y + 1];

                    if (SaharaRichesSlotMachine.Instance.isFreeSpinSlot(slot.slotType))
                    {
                        //Debug.Log("FreeSpinPosCollect");
                        freeSpinSlots.Add(slot);
                    }
                }
            }
        //}
    }
    public IEnumerator FreeSpinCollect()
    {
        FreeSpinPosCollect();
        yield return new WaitForSeconds(0.5f);

        int total = 0;
        if (freeSpinCollectorText)
        {
            //Debug.Log("freeSpinCollectorText" + freeSpinCollectorText);
            freeSpinCollectorText.text = ToSpriteDigits(0);
        }
        for (int i = 0; i < SaharaRichesSlotMachine.Instance.cashCollectSlots.Count; i++)
        {
            var collectorSlot = SaharaRichesSlotMachine.Instance.cashCollectSlots[i];
            collectorSlot?.PlayAnimation();

            // Play anims on all free-spin slots first
            foreach (var fs in freeSpinSlots)
                fs.PlayAnimation();

            // small delay if you want a beat before all fly together
            yield return new WaitForSeconds(0.5f);

            int pending = 0;

            // local helper to launch one move and update total upon completion
            IEnumerator MoveSlots(SaharaRichesSlotScript slot)
            {
                // compute once per slot
                int value = slot.GetFreeSpinValue();
                int iconIndex =
                    value == 3 ? 0 :
                    value == 4 ? 1 :
                    value == 5 ? 2 :
                    value == 10 ? 3 : -1;

                if (iconIndex >= 0)
                {
                    pending++;
                    // inside LaunchOneMove
                    yield return slot.MoveFreeSpinIcon(iconIndex, FreeSpinTarget.transform.position);


                    FreeSpinTarget.SetActive(true);
                    total += value;
                    UpdateCollectorNumber(total);
                }

                slot.StopAnimation();
                pending--;
            }

            // start all flights in parallel
            foreach (var fs in freeSpinSlots)
                StartCoroutine(MoveSlots(fs));

            // wait until all parallel flights complete
            yield return new WaitUntil(() => pending == 0);

            collectorSlot?.StopAnimation();
        }

        var targetfreespin = SaharaRichesFreeGameTransitionController.Instance.freeSpinsCountText;

        var rt = freeSpinCollectorText.rectTransform;

        // cache original state
        Vector3 originalPos = rt.position;
        Vector3 originalScale = rt.localScale;
        string originalText = freeSpinCollectorText.text;

        // set the value we want to fly
        freeSpinCollectorText.text = ToSpriteDigits(total);
        // wait before starting the move
        yield return new WaitForSeconds(0.5f);

        // slower move + gentle scale pulse
        Sequence s = DOTween.Sequence();

        var move = rt.DOMove(targetfreespin.transform.position, 1f)
                        .SetEase(Ease.InOutQuad);

        s.Append(move);
        s.Join(
                rt.DOScale(originalScale * 0.85f, 0.3f)
                    .SetLoops(2, LoopType.Yoyo) // shrink then return to original
        );
        s.Insert(0.6f, // time within the sequence (a bit before 1s move ends)
        DOVirtual.DelayedCall(0f, () =>
        {
            FreeSpinTarget.SetActive(false);
            SaharaRichesFreeGameTransitionController.Instance.UpdateFreeSpinsCount(
                SaharaRichesSlotMachine.Instance.freeSpinCount
            );
            // Or simply deactivate if you don’t use CanvasGroup
            targetfreespin.SetActive(true);
            
        })
        );

        move.OnComplete(() =>
        {
            
            SaharaRichesSlotMachine.Instance.isFreeSpinCollectionCompleted = true;
        });

        yield return s.WaitForCompletion();

        rt.position = originalPos;
            rt.localScale = originalScale;
            freeSpinCollectorText.text = originalText;

        StartCoroutine(ScatterAnimation());
    }
    #endregion
}

#region Support Classes

[System.Serializable]
public class SaharaRichesPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 5x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]); // 5 * 4 = 20

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3]; // [columns, rows]
        for (int x = 0; x < 5; x++)     // columns
        {
            for (int y = 0; y < 3; y++) // rows
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}

public class SaharaRichesPaylineEntry
{
    public SaharaRichesPaylineData payline;
    public int reelLimit;
    public string winText;
    public SaharaRichesPaylineEntry(SaharaRichesPaylineData payline, int reelLimit, string winText)
    {
        this.payline = payline;
        this.reelLimit = reelLimit;
        if (float.TryParse(winText, out float parsedValue))
        {
            float floored = Mathf.Floor(parsedValue * 100f) / 100f;
            this.winText = floored.ToString("F2");
        }
        else
        {
            this.winText = winText;
        }
    }
}

[System.Serializable]
public class SaharaRichesPaylineResult
{
    public int paylineNumber;
    public int reelLimit;
    public string winText;

    public SaharaRichesPaylineResult(int paylineNumber, int reelLimit, string winText)
    {
        this.paylineNumber = paylineNumber;
        this.reelLimit = reelLimit;
        this.winText = winText;
    }
}

#endregion