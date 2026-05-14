using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InvadersPlanetMoolahBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.25f, 0.50f, 0.75f, 1.00f, 1.25f, 2.50f, 3.75f, 5.00f, 6.25f, 7.50f, 8.75f, 10.00f
    };

    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text lineText;
    [SerializeField] private TMP_Text betText;

    #endregion

    #region Unity Methods

    private void Start()
    {
        UpdateBetUI();
    }

    #endregion

    #region Public References

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

    public float GetCurrentBet() => betValues[currentIndex];

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = betValues[currentIndex];
        float linePlay = bet / 25f;

        lineText.text = linePlay.ToString("0.00");
        betText.text = bet.ToString("0.00");
    }

    #endregion
}