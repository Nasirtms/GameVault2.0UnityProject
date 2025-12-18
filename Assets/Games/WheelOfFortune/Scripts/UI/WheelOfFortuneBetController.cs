using UnityEngine;
using TMPro;

public class WheelOfFortuneBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.10f, 0.50f, 1.00f, 2.00f,
        3.00f, 4.00f, 5.00f, 6.00f,
        7.00f, 8.00f, 9.00f, 10.00f,

    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text chipText;
    [SerializeField] private TMP_Text betText;

    #endregion

    #region Unity Methods

    private void Start() => UpdateBetUI();

    #endregion

    #region Public References

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % chipValues.Length;
        if (currentIndex == chipValues.Length - 1)
        {
            //CleopatraUIManager.Instance.PlaySound("Max");
        }
        else
        {
            //CleopatraUIManager.Instance.PlaySound("Increase");
        }
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + chipValues.Length) % chipValues.Length;
        if (currentIndex == chipValues.Length - 1)
        {
            //CleopatraUIManager.Instance.PlaySound("Max");
        }
        else
        {
            //CleopatraUIManager.Instance.PlaySound("Decrease");
        }
        UpdateBetUI();
    }

    public float GetCurrentBet() => chipValues[currentIndex];

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = chipValues[currentIndex];
        float chip = bet / 5f;

        chipText.text = chip.ToString("0.00");
        betText.text = bet.ToString("0.00");
    }

    #endregion
}
