using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SecretsOfChristmasBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.25f, 0.50f, 0.75f, 1.00f, 1.50f, 2.00f, 3.00f, 4.00f, 5.00f, 6.00f, 7.00f, 8.00f, 9.00f, 10.00f, 15.00f
    };

    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;

    #endregion

    private void Start()
    {
        UpdateBetUI();
    }

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % betValues.Length;
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + betValues.Length) % betValues.Length;
        UpdateBetUI();
    }
    public void SetMaxBet()
    {
        int maxIndex = betValues.Length - 1;
        currentIndex = maxIndex;
        UpdateBetUI();
    }
    public float GetCurrentBet() => betValues[currentIndex];

    private void UpdateBetUI()
    {
        float bet = betValues[currentIndex];
        betText.text = bet.ToString("0.00");
    }
}