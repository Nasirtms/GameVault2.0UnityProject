using Coffee.UIEffects;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TenTimesWinsPaylineController : MonoBehaviour
{
    #region Variables

    public static TenTimesWinsPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<TenTimesWinsPaylineData> paylines;

    // Currently running paylines
    private List<TenTimesWinsPaylineEntry> activePaylines = new List<TenTimesWinsPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.5f;         // Each payline duration
    private Coroutine animationLoop;
    private bool isShowing = false;                             // Paylines will be continue as long as it is true

    [Header("Results")]
    [ShowInInspector][ReadOnly] private List<TenTimesWinsPaylineResult> spinResult = new List<TenTimesWinsPaylineResult>();

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    #endregion

    #region Public References


    public void AddPaylineData(TenTimesWinsPaylineResult result)
    {
        if (spinResult.Contains(result))
            return;

        spinResult.Add(result);
    }

    public void ClearPaylineData()
    {
        isShowing = false;

        foreach (var entry in activePaylines)
        {
            if (entry.payline.paylineSprite != null)
            {
                Image img = entry.payline.paylineSprite.GetComponent<Image>();
                if (img != null)
                {
                    Color c = img.color;
                    c.a = 0.35f;
                    img.color = c;
                }
            }
            entry.payline.paylineSprite.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
            entry.payline.paylineSprite.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);
        }

        activePaylines.Clear();
        spinResult.Clear();
    }


    #endregion

    #region Payline Animations

    private void StartPaylineDisplay(List<TenTimesWinsPaylineResult> results)
    {
        StopPaylineDisplay();

        activePaylines.Clear();

        foreach (var result in results)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
            {
                activePaylines.Add(new TenTimesWinsPaylineEntry(
                    paylineData
                ));
            }
        }

        isShowing = true;

        animationLoop = StartCoroutine(PlayPaylines());
    }
    public void ShowCollectedPaylines()
    {
        // If no wins, immediately mark complete so auto-spin can proceed
        if (spinResult == null || spinResult.Count == 0)
        {
            TenTimesWinsSlotMachine.Instance.isPaylineCompleted = true;
            return;
        }

        StartPaylineDisplay(spinResult); // <-- your existing private method
    }

    public void StopPaylineDisplay()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        foreach (var reel in TenTimesWinsSlotMachine.Instance.reels)
        {
            foreach (var slot in reel.slots)
            {
                if (slot != null)
                {
                    slot.SetSortingLayer(0, false);
                    slot.StopAnimation();

                }
            }
        }
    }

    private IEnumerator PlayPaylines()
    {
        try
        {
            if (!TenTimesWinsAutoSpinController.isAutoSpinning)
            {
                TenTimesWinsSlotMachine.Instance.isPaylineCompleted = true;
            }

            if (activePaylines.Count > 0)
            {
                foreach (var payline in activePaylines)
                {
                    payline.payline.paylineSprite.SetActive(true);
                }

                yield return new WaitForSeconds(1f);
            }
            TenTimesWinsSlotMachine.Instance.isPaylineCompleted = true;

            if (activePaylines.Count > 0)
            {

                while (isShowing)
                {
                    foreach (var entry in activePaylines)
                    {
                        yield return PlaySinglePayline(entry);
                    }

                    TenTimesWinsSlotMachine.Instance.isPaylineCompleted = true;

                    if ((TenTimesWinsSlotMachine.Instance.isPaylineCompleted && TenTimesWinsAutoSpinController.isAutoSpinning))
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            if (TenTimesWinsAutoSpinController.isAutoSpinning)
                TenTimesWinsSlotMachine.Instance.isPaylineCompleted = true;
        }
        
    }

    private IEnumerator PlaySinglePayline(TenTimesWinsPaylineEntry entry)
    {
        if (entry.payline.paylineSprite != null)
        {
            Image img = entry.payline.paylineSprite.GetComponent<Image>();
            Color c = img.color;

            float baseAlpha = c.a;
            float maxAlpha = 1f;

            entry.payline.paylineSprite.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            entry.payline.paylineSprite.transform.GetChild(1).GetChild(1).gameObject.SetActive(true);

            float animationDuration = 1f;
            float elapsed = 0f;

            for (int x = 0; x < TenTimesWinsSlotMachine.Instance.reels.Count; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var slot = TenTimesWinsSlotMachine.Instance.reels[x].slots[y + 1];
                    if (slot == null) continue;

                    if (entry.payline.ToMatrix()[x, y] == 1)
                    {
                        slot.PlayAnimation();
                    }
                    slot.transform.GetChild(0).GetComponent<UIShiny>().width = 0f;
                }
            }
            while (elapsed < animationDuration)
            {
                float t = Mathf.PingPong(Time.time * 4f, 1f);
                float newAlpha = Mathf.Lerp(baseAlpha, maxAlpha, t);

                c.a = newAlpha;
                img.color = c;

                elapsed += Time.deltaTime;
                yield return null;
            }
            c.a = baseAlpha;
            img.color = c;

            entry.payline.paylineSprite.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
            entry.payline.paylineSprite.transform.GetChild(1).GetChild(1).gameObject.SetActive(false);

            yield return new WaitForSeconds(0.7f);
        }
    }

    #endregion
}
