using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuickHitVolcanoQuickPickGameManager : MonoBehaviour
{
    public static QuickHitVolcanoQuickPickGameManager Instance { get; private set; }

    [Header("Free Spins")]
    [SerializeField] private int freeSpinCount;

    [Header("Item Prefab and Rows")]
    [SerializeField] private GameObject quickPickItemPrefab;
    [SerializeField] private Transform row1Parent; // 7 items
    [SerializeField] private Transform row2Parent; // 6 items
    [SerializeField] private Transform row3Parent; // 7 items

    [Header("Sprites")]
    [SerializeField] private Sprite spriteFreeSpin7;
    [SerializeField] private Sprite spriteFreeSpin8;
    [SerializeField] private Sprite spriteFreeSpin10;
    [SerializeField] private Sprite spriteFreeSpin12;
    [SerializeField] private Sprite spriteFreeSpin15;
    [SerializeField] private Sprite spriteWild;

    [Header("Win Screen")]
    [SerializeField] private GameObject winPopup;
    [SerializeField] private TMP_Text winText;

    [Header("Predefined Reveal List")]
    [SerializeField] private List<QuickHitVolcanoQuickPickSymbolType> predefinedPickOrder = new();
    private readonly List<QuickHitVolcanoQuickPickSymbolType> revealedSymbols = new();

    private readonly List<QuickHitVolcanoQuickPickItem> allItems = new();
    private readonly Dictionary<QuickHitVolcanoQuickPickSymbolType, int> symbolCount = new();
    private int currentPickIndex = 0;
    private int wildsPicked = 0;
    private bool gameEnded = false;

    private QuickHitVolcanoQuickPickListGenerator listGenerator;
    private QuickHitVolcanoQuickPickLightManager lightManager;

    public bool IsGameEnded => gameEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        listGenerator = GetComponent<QuickHitVolcanoQuickPickListGenerator>();   
        lightManager = GetComponent<QuickHitVolcanoQuickPickLightManager>();   
    }

    public void StartQuickPickGame(int freeSpins)
    {
        freeSpinCount = freeSpins;
        predefinedPickOrder = listGenerator.GenerateList(freeSpinCount);
        SpawnItems();
    }

    private void SpawnItems()
    {
        ResetGame();
        SpawnBoard();
    }

    private void ResetGame()
    {
        // Cleanup old items
        foreach (var item in allItems)
        {
            Destroy(item.gameObject);
        }

        allItems.Clear();
        symbolCount.Clear();
        wildsPicked = 0;
        currentPickIndex = 0;
        gameEnded = false;

        revealedSymbols.Clear();

        lightManager.ResetLights();
    }

    private void SpawnBoard()
    {
        int totalItems = 20; // Always spawn full set (15 spins + 2 wilds)
        for (int i = 0; i < totalItems; i++)
        {
            GameObject go = Instantiate(quickPickItemPrefab);
            QuickHitVolcanoQuickPickItem item = go.GetComponent<QuickHitVolcanoQuickPickItem>();
            item.ResetItem();
            allItems.Add(item);

            if (i < 7)
                go.transform.SetParent(row1Parent, false);
            else if (i < 13)
                go.transform.SetParent(row2Parent, false);
            else
                go.transform.SetParent(row3Parent, false);
        }
    }

    public void OnItemClicked(QuickHitVolcanoQuickPickItem item)
    {
        if (gameEnded || item.IsRevealed) return;

        if (currentPickIndex >= predefinedPickOrder.Count)
        {
            Debug.LogWarning("No more symbols in predefinedPickOrder.");
            return;
        }

        var type = predefinedPickOrder[currentPickIndex++];
        var sprite = GetSpriteForType(type);

        item.ForceReveal(type, sprite);
    }

    public void OnItemForceRevealed(QuickHitVolcanoQuickPickItem item, QuickHitVolcanoQuickPickSymbolType type)
    {
        if (type == QuickHitVolcanoQuickPickSymbolType.Wild)
        {
            wildsPicked++;
            lightManager.FillLightForAll();
        }
        else
        {
            if (!symbolCount.ContainsKey(type))
                symbolCount[type] = 0;

            symbolCount[type]++;
            lightManager.FillLight(type);
        }

        revealedSymbols.Add(type);
        TryEndGame(type);
    }

    private void TryEndGame(QuickHitVolcanoQuickPickSymbolType lastType)
    {
        foreach (var kvp in symbolCount)
        {
            if (kvp.Value + wildsPicked >= 3)
            {
                StartCoroutine(EndGame(kvp.Key));
                return;
            }
        }
    }

    private IEnumerator EndGame(QuickHitVolcanoQuickPickSymbolType matchedType)
    {
        gameEnded = true;

        int baseSpins = GetFreeSpinValue(matchedType);
        int totalSpins = baseSpins + (wildsPicked * 5);

        Debug.Log($"🎉 You won {totalSpins} Free Spins! ({baseSpins} + {wildsPicked} × 5)");

        yield return new WaitForSeconds(0.5f);

        RevealRemainingItems();
        SetWinText(freeSpinCount);

        yield return new WaitForSeconds(1f);

        PopupAnimation(winPopup, 1, 0.75f, true);

        yield return new WaitForSeconds(3f);

        PopupAnimation(winPopup, 0, 0.75f, false);

        QuickHitVolcanoGameTransitionController.Instance.StartFreeSlotGame();
    }

    private void RevealRemainingItems()
    {
        List<QuickHitVolcanoQuickPickSymbolType> used = new(revealedSymbols); // ✅ actual revealed types

        List<QuickHitVolcanoQuickPickSymbolType> fullPool = new()
    {
        QuickHitVolcanoQuickPickSymbolType.FreeSpin7, QuickHitVolcanoQuickPickSymbolType.FreeSpin7, QuickHitVolcanoQuickPickSymbolType.FreeSpin7,
        QuickHitVolcanoQuickPickSymbolType.FreeSpin8, QuickHitVolcanoQuickPickSymbolType.FreeSpin8, QuickHitVolcanoQuickPickSymbolType.FreeSpin8,
        QuickHitVolcanoQuickPickSymbolType.FreeSpin10, QuickHitVolcanoQuickPickSymbolType.FreeSpin10, QuickHitVolcanoQuickPickSymbolType.FreeSpin10,
        QuickHitVolcanoQuickPickSymbolType.FreeSpin12, QuickHitVolcanoQuickPickSymbolType.FreeSpin12, QuickHitVolcanoQuickPickSymbolType.FreeSpin12,
        QuickHitVolcanoQuickPickSymbolType.FreeSpin15, QuickHitVolcanoQuickPickSymbolType.FreeSpin15, QuickHitVolcanoQuickPickSymbolType.FreeSpin15,
        QuickHitVolcanoQuickPickSymbolType.Wild, QuickHitVolcanoQuickPickSymbolType.Wild, QuickHitVolcanoQuickPickSymbolType.Wild, QuickHitVolcanoQuickPickSymbolType.Wild, QuickHitVolcanoQuickPickSymbolType.Wild
    };

        foreach (var symbol in used)
        {
            int indexToRemove = fullPool.FindIndex(s => s == symbol);
            if (indexToRemove != -1)
                fullPool.RemoveAt(indexToRemove);
        }

        Shuffle(fullPool);

        int poolIndex = 0;
        foreach (var item in allItems)
        {
            if (!item.IsRevealed && poolIndex < fullPool.Count)
            {
                var type = fullPool[poolIndex++];
                var sprite = GetSpriteForType(type);
                item.ForceReveal(type, sprite, triggerLogic: false); // ✅ silent reveal
            }
        }
    }

    private Sprite GetSpriteForType(QuickHitVolcanoQuickPickSymbolType type)
    {
        return type switch
        {
            QuickHitVolcanoQuickPickSymbolType.FreeSpin7 => spriteFreeSpin7,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin8 => spriteFreeSpin8,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin10 => spriteFreeSpin10,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin12 => spriteFreeSpin12,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin15 => spriteFreeSpin15,
            QuickHitVolcanoQuickPickSymbolType.Wild => spriteWild,
            _ => null
        };
    }

    private int GetFreeSpinValue(QuickHitVolcanoQuickPickSymbolType type)
    {
        return type switch
        {
            QuickHitVolcanoQuickPickSymbolType.FreeSpin7 => 7,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin8 => 8,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin10 => 10,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin12 => 12,
            QuickHitVolcanoQuickPickSymbolType.FreeSpin15 => 15,
            _ => 0
        };
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }

    private void PopupAnimation(GameObject obj, float scale, float duration, bool state)
    {
        obj.transform.parent.gameObject.SetActive(state);

        obj.transform.localScale = Vector3.one * 0.5f;

        obj.transform.DOScale(scale, duration * 1.2f)
            .SetEase(Ease.OutBack);
    }

    private void SetWinText(int value)
    {
        if (value < 10)
        {
            winText.text = $"<sprite={0}><sprite={value}>"; ;
        }
        else if (value < 100)
        {
            int tens = value / 10;
            int ones = value % 10;
            winText.text = $"<sprite={tens}><sprite={ones}>";
        }
        else
        {
            Debug.LogWarning("Input number is 100 or greater — only two-digit values are expected.");
        }
    }

}
