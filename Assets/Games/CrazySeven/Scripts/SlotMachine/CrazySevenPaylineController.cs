using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrazySevenPaylineController : MonoBehaviour
{
    #region Variables

    public static CrazySevenPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<CrazySevenPaylineData> paylines;

    private List<CrazySevenPaylineEntry> activePaylines = new List<CrazySevenPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    [Header("Results")]
    [ShowInInspector][ReadOnly] public List<CrazySevenPaylineResult> spinResult = new List<CrazySevenPaylineResult>();
    private int resultScatterCount;
    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }
    #endregion

    #region Public References

    public void AddPaylineData(CrazySevenPaylineResult result)
    {
        if (spinResult.Contains(result))
        {
            return;
        }
        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        spinResult.Clear();
    }

    public void StopPaylines()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        foreach (var entry in activePaylines)
        {
            if (entry.payline.paylineSprite != null)
            {
                Image img = entry.payline.paylineSprite.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 0;
                    img.color = c;
                }
            }
        }
        foreach (var reel in CrazySevenSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.ToggleSlotBorder(false);
                }
            }
        }
    }

    #endregion

    #region Payline Animations
    private void StartPaylineDisplay(List<CrazySevenPaylineResult> results)
    {
        StopPaylines();

        activePaylines.Clear();
        var seen = new HashSet<int>(); 

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null && seen.Add(paylineData.paylineNumber))
            {
                activePaylines.Add(new CrazySevenPaylineEntry(paylineData));
            }
        }
        PaylineVisible(null);
        if (activePaylines.Count == 0 && resultScatterCount < 2)
        {
            CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
            Debug.LogWarning("No valid paylines to display.");
            return;
        }
        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }
    private void PaylineVisible(int? paylineNumber)
    {
        foreach (var p in paylines)
        {
            if (p?.paylineSprite == null) continue;
            bool show = paylineNumber.HasValue && p.paylineNumber == paylineNumber.Value;
            p.paylineSprite.SetActive(show);
        }
    }
    public void ShowCollectedPaylines(int scatterCount)
    {
        resultScatterCount = scatterCount;
        if ((spinResult == null || spinResult.Count == 0) && scatterCount < 2)
        {
            CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
            return;
        }
        StartPaylineDisplay(spinResult);
    }
    private IEnumerator PlayPaylines()
    {
        try
        {
            if (!CrazySevenAutoSpinController.isAutoSpinning && !CrazySevenSlotMachine.Instance.isFreeGame)
                CrazySevenSlotMachine.Instance.isPaylineCompleted = true;

            if (activePaylines.Count == 0)
            {
                CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }
            if (activePaylines.Count == 1)
            {
                if (CrazySevenAutoSpinController.isAutoSpinning)
                {
                    yield return new WaitForSeconds(0.3f);
                }
                yield return PlaySinglePayline(activePaylines[0]);
                CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }

            if (resultScatterCount > 2)
            {
                yield return ScatterAnimation(resultScatterCount);
            }

            //Multiple Paylines
            if (CrazySevenAutoSpinController.isAutoSpinning || CrazySevenSlotMachine.Instance.isFreeGame)
            {
                // Auto spins, show each winning payline once
                for (int i = 0; i < activePaylines.Count && isShowing; i++)
                {
                    yield return PlaySinglePayline(activePaylines[i]);
                }
                CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
                yield break;
            }
            else
            {
                int idx = 0;
                while (isShowing)
                {
                    var entry = activePaylines[idx];
                    yield return PlaySinglePayline(entry);
                    idx = (idx + 1) % activePaylines.Count;
                    if ((CrazySevenSlotMachine.Instance.isPaylineCompleted && CrazySevenAutoSpinController.isAutoSpinning))
                    {
                        break;
                    }
                }

                yield return new WaitForSeconds(0.5f);
                CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
            }
        }
        finally
        {
            isShowing = false;
            animationLoop = null;
            if(CrazySevenAutoSpinController.isAutoSpinning)
                CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    private bool PaylineMessage(CrazySevenPaylineEntry entry, out string message)
    {
        message = null;

        var sm = CrazySevenSlotMachine.Instance;
        var spin = sm?.currentSpinResult;
        if (spin == null || spin.paylineWins == null || spin.paylineWins.Count == 0)
            return false;

        int number = entry.payline.paylineNumber;

        // Some backends report 0-based or 1-based; keep your existing fallback:
        var winEntry = spin.paylineWins.Find(w => w.paylineIndex == number)
                   ?? spin.paylineWins.Find(w => w.paylineIndex == number - 1);

        if (winEntry == null)
            return false;

        float amount;
        if (!float.TryParse(winEntry.winAmount, out amount))
            amount = 0f;

        message = $"{winEntry.count} <sprite name={winEntry.symbol}> Win {amount:F2}";
        return true;
    }

    private void ClearAllSlotBorders()
    {
        var reels = CrazySevenSlotMachine.Instance.reels;
        foreach (var reel in reels)
        {
            foreach (var slot in reel.slots)
                if (slot != null) slot.ToggleSlotBorder(false);
        }
    }
    private IEnumerator PlaySinglePayline(CrazySevenPaylineEntry entry)
    {
        ClearAllSlotBorders();
        PaylineVisible(entry.payline.paylineNumber);

        var sm = CrazySevenSlotMachine.Instance;

        if (sm != null && sm.textbar != null )
        {
            if (PaylineMessage(entry, out var msg)) sm.textbar.text = msg;
            else sm.textbar.text = "";
        }

        Image img = null;
        Color baseColor = Color.white;
        if (entry.payline.paylineSprite != null)
        {
            img = entry.payline.paylineSprite.GetComponent<Image>();
            if (img != null) baseColor = img.color;
        }

        TriggerPaylineAnimations(entry);
        float showFor = CrazySevenAutoSpinController.isAutoSpinning ? 1.5f : flickerDelay;

        bool infiniteManualSingle = !CrazySevenAutoSpinController.isAutoSpinning && activePaylines != null 
                                    && activePaylines.Count == 1 && !CrazySevenSlotMachine.Instance.isFreeGame; 

        float elapsed = 0f;
        while ((isShowing && CrazySevenAutoSpinController.isAutoSpinning && elapsed < showFor) ||
                  (!CrazySevenAutoSpinController.isAutoSpinning && isShowing && (infiniteManualSingle || elapsed < showFor)))
        {
            elapsed += Time.deltaTime;

            if (img != null)
            {
                float t = Mathf.PingPong(Time.time * 4f, 1f);
                float newAlpha = Mathf.Lerp(0.35f, 1f, t);
                var c = baseColor; c.a = newAlpha;
                img.color = c;
            }

            yield return null;
        }

        if (img != null) img.color = baseColor;
        PaylineVisible(null);
        if (CrazySevenAutoSpinController.isAutoSpinning)
        {
            CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
        }
    }

    List<CrazySevenSlotScript> path = new List<CrazySevenSlotScript>();
    private void TriggerPaylineAnimations(CrazySevenPaylineEntry entry)
    {
        path.Clear();

        var mat = entry.payline.ToMatrix();
        var reels = CrazySevenSlotMachine.Instance.reels;

        for (int x = 0; x < reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = reels[x].slots[y + 1];
                if (slot == null) continue;

                if (mat[x, y] == 1)
                    path.Add(slot);
            }
        }

        if (path.Count == 0) return;;
        CrazySevenSlotType? target = null;

        // target = first non-WILD/SCATTER
        if (target == null)
            target = path.Find(s => s.type != CrazySevenSlotType.Bell && s.type != CrazySevenSlotType.Luck)?.type;

        if (target == null) return;
        int win = 0;
        foreach (var s in path)
        {
            if (s.type == target || s.type == CrazySevenSlotType.Bell) win++;
            else break;
        }

        if (win < 3)
        {
            return;
        }
        for (int i = 0; i < win; i++)
        {
            path[i].ToggleSlotBorder(true);
        }
       
    }
    private IEnumerator ScatterAnimation(int scatterCount)
    {
        int count = 0;
        for (int x = 0; x < CrazySevenSlotMachine.Instance.reels.Count; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var slot = CrazySevenSlotMachine.Instance.reels[x].slots[y + 1];
                if (slot == null) continue;

                if (slot.type == CrazySevenSlotType.Seven)
                {
                    count++;
                    slot.ToggleSlotBorder(true);
                }
            }
        }
        if (scatterCount > 2)
        {
            if (!CrazySevenSlotMachine.Instance.isFreeGame)
            {
                //CrazySevenUIManager.Instance.UpdateButtons("Free Spin");
            }
        }
        if (spinResult.Count < 1)
        {
            CrazySevenSlotMachine.Instance.isPaylineCompleted = true;
        }
        yield return new WaitForSeconds(2f);
    }
    #endregion
}