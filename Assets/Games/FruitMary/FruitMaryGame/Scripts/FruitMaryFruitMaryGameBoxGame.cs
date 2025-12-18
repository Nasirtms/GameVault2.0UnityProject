using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class FruitMaryFruitMaryGameBoxGame : MonoBehaviour
{
    #region Variables

    public static FruitMaryFruitMaryGameBoxGame Instance;

    [SerializeField] private GameObject[] boxes;
    [SerializeField] private GameObject highlight;
    [SerializeField] private float switchDelay;
    [SerializeField] private int currentIndex;
    [SerializeField] private int slowGap;
    [SerializeField] private int gap;

    private Coroutine fruitMaryGameCoroutine;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    #endregion

    #region Box Game

    public void StartGame(int winIndex)
    {
        fruitMaryGameCoroutine = StartCoroutine(FruitMaryGame(winIndex));
    }

    private IEnumerator FruitMaryGame(int winIndex)
    {
        gap = 0;
        int count = 0;

        while (!(count >= 24 && gap == slowGap))
        {
            count++;

            currentIndex = currentIndex % boxes.Length;

            HighlightBox(boxes[currentIndex]);
            yield return new WaitForSeconds(switchDelay);

            currentIndex++;

            // Calculate modular distance
            int distance = (winIndex - currentIndex + boxes.Length) % boxes.Length;

            // Slow down when we're slowGap steps away
            gap = distance == slowGap ? slowGap : 0;
        }

        // Slow phase
        for (int i = 1; i <= slowGap; i++)
        {
            currentIndex = currentIndex % boxes.Length;
            HighlightBox(boxes[currentIndex]);
            yield return new WaitForSeconds(switchDelay * i);
            currentIndex++;
        }

        // Final highlight
        HighlightBox(boxes[(winIndex + boxes.Length) % boxes.Length]);

        yield return new WaitForSeconds(0.5f);

        if (FruitMaryFruitMaryGameSlotMachine.Instance.GetCurrentWin() > 0)
        {
            FruitMaryFruitMaryGameManager.Instance.UpdateWinAmount(FruitMaryFruitMaryGameSlotMachine.Instance.GetCurrentWin());
            Invoke(nameof(UpdateGameCoin), 1f);
        }

        yield return new WaitForSeconds(0.5f);

        if (winIndex == 3 || winIndex == 9 || winIndex == 15 || winIndex == 21)
        {
            FruitMaryFruitMaryGameManager.Instance.EndFruitMaryGame();
        }
        else if (FruitMaryFruitMaryGameManager.Instance.GetFreeSpinCount() <= 0)
        {
            FruitMaryFruitMaryGameManager.Instance.EndFruitMaryGame();
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            FruitMaryFruitMaryGameSpinService.Instance.Spin(FruitMaryFruitMaryGameManager.Instance.GetBetAmount());
        }
    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(FruitMaryFruitMaryGameSlotMachine.Instance.GetCurrentWin());
    }
    private void HighlightBox(GameObject parent)
    {
        RectTransform target = parent.transform as RectTransform;
        highlight.transform.SetParent(target);
        highlight.transform.SetSiblingIndex(0);
        highlight.transform.localPosition = Vector3.zero;
    }

    #endregion
}
