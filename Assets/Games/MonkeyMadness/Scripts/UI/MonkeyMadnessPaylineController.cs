using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyMadnessPaylineController : MonoBehaviour
{
    public static MonkeyMadnessPaylineController Instance;

    [Header("Paylines")]
    [SerializeField] private List<MonkeyMadnessPaylineData> paylines;

    private List<MonkeyMadnessPaylineEntry> activePaylines = new List<MonkeyMadnessPaylineEntry>();

    [Header("Animation Settings")]
    [SerializeField] private float flickerDelay = 2.0f;
    private Coroutine animationLoop;
    private bool isShowing = false;

    private List<MonkeyMadnessPaylineResult> spinResults = new List<MonkeyMadnessPaylineResult>();

    private void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    public void AddPaylineResult(MonkeyMadnessPaylineResult result)
    {
        if (!spinResults.Contains(result))
            spinResults.Add(result);
    }

    public void ClearPaylineResults()
    {
        spinResults.Clear();
    }

    public void StartPaylineLoop()
    {
        StopPaylineLoop();

        activePaylines.Clear();

        foreach (var result in spinResults)
        {
            var paylineData = paylines.Find(p => p.paylineNumber == result.paylineNumber);
            if (paylineData != null)
                activePaylines.Add(new MonkeyMadnessPaylineEntry(paylineData));
        }

        if (activePaylines.Count == 0)
        {
            Debug.LogWarning("No paylines to display.");
            return;
        }

        isShowing = true;
        animationLoop = StartCoroutine(PlayPaylines());
    }

    public void StopPaylineLoop()
    {
        isShowing = false;

        if (animationLoop != null)
        {
            StopCoroutine(animationLoop);
            animationLoop = null;
        }

        MonkeyMadnessSlotMachine.Instance.StopAllSlotAnimations();

        foreach (var entry in activePaylines)
        {
            if (entry.payline.paylineSprite != null)
                entry.payline.paylineSprite.SetActive(false);
        }
    }

    private IEnumerator PlayPaylines()
    {
        // Step 1: Show all paylines together
        foreach (var entry in activePaylines)
        {
            if (entry.payline.paylineSprite != null)
                entry.payline.paylineSprite.SetActive(true);

            MonkeyMadnessSlotMachine.Instance.AnimateSlotsFromPattern(entry.payline.ToMatrix(), flickerDelay);
        }

        yield return new WaitForSeconds(flickerDelay);
        MonkeyMadnessSlotMachine.Instance.StopAllSlotAnimations();

        foreach (var entry in activePaylines)
        {
            if (entry.payline.paylineSprite != null)
                entry.payline.paylineSprite.SetActive(false);
        }

        // Step 2: Cycle through each payline
        while (isShowing)
        {
            foreach (var entry in activePaylines)
            {
                if (entry.payline.paylineSprite != null)
                    entry.payline.paylineSprite.SetActive(true);

                MonkeyMadnessSlotMachine.Instance.AnimateSlotsFromPattern(entry.payline.ToMatrix(), flickerDelay);

                yield return new WaitForSeconds(flickerDelay);

                MonkeyMadnessSlotMachine.Instance.StopAllSlotAnimations();

                if (entry.payline.paylineSprite != null)
                    entry.payline.paylineSprite.SetActive(false);

                yield return new WaitForSeconds(flickerDelay / 4f);
            }

            MonkeyMadnessSlotMachine.Instance.isPaylineCompleted = true;
        }
    }
}
