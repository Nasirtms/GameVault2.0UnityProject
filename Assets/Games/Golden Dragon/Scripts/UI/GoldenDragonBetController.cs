using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoldenDragonBetController : MonoBehaviour
{
    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.10f, 0.20f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.00f, 2.00f, 3.00f, 4.00f, 5.00f, 6.00f, 7.00f, 8.00f, 9.00f, 10.00f
    };

    private int currentIndex = 0;
    [Header("UI References")]
    [SerializeField] private TMP_Text chipText;
    private void Start()
    {
        UpdateBetUI();
    }
    public void IncreaseChipValue()
    {
        int maxIndex = chipValues.Length - 1;
        currentIndex = (currentIndex < maxIndex) ? currentIndex + 1 : 0;
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        int maxIndex = chipValues.Length - 1;
        currentIndex = (currentIndex > 0) ? currentIndex - 1 : maxIndex;
        UpdateBetUI();
    }

    public void SetMaxBet()
    {
        int maxIndex = chipValues.Length - 1;
        currentIndex = maxIndex;
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        float chip = chipValues[currentIndex];
        chipText.text = chip.ToString("0.00");
    }

    public float GetCurrentBet() => chipValues[currentIndex];
}
