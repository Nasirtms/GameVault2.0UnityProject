using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BiggerBassBonanzaBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.24f, 0.72f, 1.20f, 2.40f,
        3.60f, 4.80f, 6.00f, 7.20f,
        8.40f, 9.60f, 10.80f, 12.00f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;

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

    public void SetMaxBet()
    {
        currentIndex = betValues.Length - 1;
        UpdateBetUI();
    }

    public float GetCurrentBet() => betValues[currentIndex];

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = betValues[currentIndex];

        betText.text = bet.ToString("0.00");
    }

    #endregion
}
