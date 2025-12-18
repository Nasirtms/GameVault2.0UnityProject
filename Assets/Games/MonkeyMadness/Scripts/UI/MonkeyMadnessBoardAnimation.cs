using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonkeyMadnessBoardAnimation : MonoBehaviour
{
    [Tooltip("Top-level row GameObjects like Wild, Drums, Toucan, etc.")]
    public List<GameObject> allRows; // These are the rows: Wild, Drums, etc.

    [Tooltip("Blink delay between steps")]
    public float blinkInterval = 0.5f;

    private Coroutine blinkCoroutine;
    private List<int> activeRowIndices = new List<int>();
    private Dictionary<int, List<GameObject>> rowGlowMap = new Dictionary<int, List<GameObject>>();

    [ContextMenu("Wild")]
    public void PlayWild()
    {
        StartGlowPattern(new List<int> { 1 });
    }

    [ContextMenu("Toucan and Any")]
    public void PlayToucanAndAny()
    {
        StartGlowPattern(new List<int> { 3 , 7 });
    }

    public void StartGlowPattern(List<int> rowIndices)
    {
        StopBlinking();

        activeRowIndices = rowIndices;
        BuildRowGlowMap(rowIndices);

        blinkCoroutine = StartCoroutine(GlowPatternLoop());
    }

    [ContextMenu("Stop")]
    public void StopBlinking()
    {
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        SetAllGlow(false);
    }

    private void BuildRowGlowMap(List<int> rowIndices)
    {
        rowGlowMap.Clear();

        for (int i = 0; i < allRows.Count; i++)
        {
            int rowIndex = i + 1; // Make it 1-based
            if (!rowIndices.Contains(rowIndex)) continue;

            var rowObject = allRows[i];
            var glowObjects = new List<GameObject>();

            foreach (Transform column in rowObject.transform) // 1, 2, 3
            {
                var glow = column.Find("Glow");
                if (glow != null)
                    glowObjects.Add(glow.gameObject);
            }

            rowGlowMap[rowIndex] = glowObjects;
        }
    }

    private IEnumerator GlowPatternLoop()
    {
        while (true)
        {
            // Step 1: Glow all active rows together
            foreach (var row in activeRowIndices)
                SetGlowForRow(row, true);

            yield return new WaitForSeconds(blinkInterval);

            SetAllGlow(false);
            yield return new WaitForSeconds(blinkInterval);

            // Step 2: Glow each row one by one
            foreach (var row in activeRowIndices)
            {
                SetGlowForRow(row, true);
                yield return new WaitForSeconds(blinkInterval);
                SetGlowForRow(row, false);
                yield return new WaitForSeconds(blinkInterval);
            }
        }
    }

    private void SetGlowForRow(int rowIndex, bool state)
    {
        if (!rowGlowMap.ContainsKey(rowIndex)) return;

        foreach (var glow in rowGlowMap[rowIndex])
        {
            if (glow != null)
                glow.SetActive(state);
        }
    }

    private void SetAllGlow(bool state)
    {
        foreach (var row in activeRowIndices)
        {
            SetGlowForRow(row, state);
        }
    }
}
