using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetManager_RPS : MonoBehaviour
{
    private readonly List<float> betOptions = new List<float>
        { 0.5f, 1f, 2f, 3f, 5f, 10f, 15f, 20f };

    private int currentIndex = 0;
    [SerializeField] private TMP_Text betAmountText;
    public float CurrentBet => betOptions[currentIndex];

    private void Start()
    {
        UpdateBetText();
    }
    public void IncreaseBet()
    {
        currentIndex = (currentIndex + 1) % betOptions.Count;
        UpdateBetText();
    }
    public void DecreaseBet()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = betOptions.Count - 1;
        UpdateBetText();
    }
    private void UpdateBetText()
    {
        if (betAmountText != null)
            betAmountText.text = CurrentBet.ToString("F2");
    }
}
