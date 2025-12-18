using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FruitMaryBetController : MonoBehaviour
{ 
    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.18f, 0.54f, 0.90f, 1.80f,
        2.70f, 3.60f, 4.50f, 5.40f,
        6.30f, 7.20f, 8.10f, 9.00f
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
