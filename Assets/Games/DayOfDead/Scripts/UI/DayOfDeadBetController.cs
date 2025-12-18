using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class DayOfDeadBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.4f, 2.00f, 4.00f, 8.00f,
        12.00f, 16.00f, 20.00f, 24.00f,
        28.00f, 32.00f, 36.00f, 40.00f
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

