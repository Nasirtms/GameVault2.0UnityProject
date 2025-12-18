using System;
using System.Collections.Generic;
using UnityEngine;

public class QuickHitVolcanoQuickPickListGenerator : MonoBehaviour
{
    //[Header("Generator Settings")]
    //public int targetValue = 8;

    [Header("Generated Output (Read Only)")]
    public List<QuickHitVolcanoQuickPickSymbolType> generatedList = new();

    private static readonly List<int> ValidSpinValues = new() { 7, 8, 10, 12, 15 };

    private static readonly Dictionary<int, QuickHitVolcanoQuickPickSymbolType> SpinValueToSymbol = new()
    {
        { 7, QuickHitVolcanoQuickPickSymbolType.FreeSpin7 },
        { 8, QuickHitVolcanoQuickPickSymbolType.FreeSpin8 },
        { 10, QuickHitVolcanoQuickPickSymbolType.FreeSpin10 },
        { 12, QuickHitVolcanoQuickPickSymbolType.FreeSpin12 },
        { 15, QuickHitVolcanoQuickPickSymbolType.FreeSpin15 }
    };

    [ContextMenu("Generate Pick List")]
    public List<QuickHitVolcanoQuickPickSymbolType> GenerateList(int targetValue)
    {
        generatedList.Clear();
        HashSet<int> usedFillers = new();

        bool isDirectMatch = ValidSpinValues.Contains(targetValue);
        int fillerMax = isDirectMatch ? 2 : 1;

        QuickHitVolcanoQuickPickSymbolType matchSymbol;
        int baseMatchValue = 0;
        bool useWild = false;

        if (isDirectMatch)
        {
            baseMatchValue = targetValue;
            matchSymbol = SpinValueToSymbol[targetValue];
        }
        else
        {
            // Try to find a base value that can be used with a wild to reach targetValue
            foreach (int baseValue in ValidSpinValues)
            {
                if (baseValue + 5 == targetValue)
                {
                    baseMatchValue = baseValue;
                    matchSymbol = SpinValueToSymbol[baseValue];
                    useWild = true;
                    goto matched;
                }
            }

            Debug.LogError($"❌ Cannot generate match for target value {targetValue}. Must be one of {string.Join(", ", ValidSpinValues)} or solvable with 2x base + wild = target.");
            return generatedList;
        }

    matched:

        // Step 1: Add 3 match symbols (or 2 + 1 wild)
        if (useWild)
        {
            generatedList.Add(matchSymbol);
            generatedList.Add(matchSymbol);
            generatedList.Add(QuickHitVolcanoQuickPickSymbolType.Wild);
        }
        else
        {
            generatedList.Add(matchSymbol);
            generatedList.Add(matchSymbol);
            generatedList.Add(matchSymbol);
        }

        // Step 2: Add fillers
        List<int> fillerOptions = new(ValidSpinValues);
        fillerOptions.Remove(baseMatchValue); // avoid repeating the match symbol

        Shuffle(fillerOptions);

        foreach (int filler in fillerOptions)
        {
            if (generatedList.Count >= 6) break;

            if (!usedFillers.Contains(filler))
            {
                var fillerSymbol = SpinValueToSymbol[filler];
                generatedList.Add(fillerSymbol);
                usedFillers.Add(filler);

                if (fillerMax == 2 && UnityEngine.Random.value < 0.5f && generatedList.Count < 6)
                {
                    generatedList.Add(fillerSymbol);
                }
            }
        }

        // Step 3: Shuffle result
        Shuffle(generatedList);

        Debug.Log($"✅ Generated Pick List ({targetValue}): {string.Join(", ", generatedList)}");

        return generatedList;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
}
