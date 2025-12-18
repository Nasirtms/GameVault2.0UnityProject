using UnityEngine;
using TMPro;

public class VegasSevenBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.01f, 0.02f, 0.03f, 0.04f,
        0.05f, 0.10f, 0.20f, 0.30f,
        0.40f, 0.50f, 0.60f, 0.70f
    };
    [SerializeField] int numberOfPlayLines = 5;
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text linePlayText;

    #endregion

    #region Unity Methods

    private void Start() => UpdateBetUI();

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
        float line = betValues[currentIndex] / numberOfPlayLines;
        betText.text = bet.ToString("N2");
        linePlayText.text = line.ToString("0.00");
    }

    #endregion
}
