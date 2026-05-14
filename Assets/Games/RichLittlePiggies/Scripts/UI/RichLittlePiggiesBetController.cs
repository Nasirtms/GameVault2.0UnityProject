using UnityEngine;
using TMPro;

public class RichLittlePiggiesBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.10f, 0.50f, 1.00f, 2.00f,
        3.00f, 4.00f, 5.00f, 6.00f,
        7.00f, 8.00f, 9.00f, 10.00f,
        15.00f, 20.00f

    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text JackpotX3;
    [SerializeField] private TMP_Text JackpotX2;
    [SerializeField] private TMP_Text JackpotX1;

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
            RichLittlePiggiesUIManager.Instance.PlaySound("Max");
        }
        else
        {
            RichLittlePiggiesUIManager.Instance.PlaySound("Increase");
        }
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + chipValues.Length) % chipValues.Length;
        if (currentIndex == chipValues.Length - 1)
        {
            RichLittlePiggiesUIManager.Instance.PlaySound("Max");
        }
        else
        {
            RichLittlePiggiesUIManager.Instance.PlaySound("Decrease");
        }
        UpdateBetUI();
    }

    public float GetCurrentBet() => chipValues[currentIndex];

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = chipValues[currentIndex];
        float j3 = bet * 30;
        float j2 = bet * 20;
        float j1 = bet * 10;

        betText.text = bet.ToString("0.00");
        JackpotX3.text = j3.ToString("0.00");
        JackpotX2.text = j2.ToString("0.00");
        JackpotX1.text = j1.ToString("0.00");
    }

    #endregion
}
