using TMPro;
using UnityEngine;

public class LifeOfLuxuryBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[]
    {
        0.20f, 0.40f, 0.60f, 0.80f, 1.00f,
        2.00f, 3.00f, 4.00f, 5.00f, 6.00f,
        7.00f, 8.00f, 9.00f, 10.00f
    };

    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;

    [Header("Line Bet Texts")]
    [SerializeField] private TMP_Text[] lineBetTexts;

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
        if (currentIndex == betValues.Length - 1)
        {
            LifeOfLuxuryUIManager.Instance.PlaySound("Maxbet");
        }
        else
        {
            LifeOfLuxuryUIManager.Instance.PlaySound("Button");
        }
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + betValues.Length) % betValues.Length;
        if (currentIndex == betValues.Length - 1)
        {
            LifeOfLuxuryUIManager.Instance.PlaySound("Maxbet");
        }
        else
        {
            LifeOfLuxuryUIManager.Instance.PlaySound("Button");
        }
        UpdateBetUI();
    }

    public float GetCurrentBet()
    {
        return betValues[currentIndex];
    }

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = GetCurrentBet();
        float lineBet = bet / 20f;

        if (betText != null)
        {
            betText.text = bet.ToString("0.00");
        }

        UpdateLineBetTexts(lineBet);
    }

    private void UpdateLineBetTexts(float lineBet)
    {
        if (lineBetTexts == null)
            return;

        string lineBetValue = lineBet.ToString("0.00");

        for (int i = 0; i < lineBetTexts.Length; i++)
        {
            if (lineBetTexts[i] != null)
            {
                lineBetTexts[i].text = lineBetValue;
            }
        }
    }

    #endregion
}