using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IrishPotLuckBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.20f, 0.60f, 1.00f, 2.00f, 3.00f, 5.00f, 10.00f, 15.00f, 20.00f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text mega_Jackpot;
    [SerializeField] private TMP_Text midi_Jackpot;
    [SerializeField] private TMP_Text mini_Jackpot;

    #endregion

    #region Unity Methods

    private void Start()
    {
        UpdateDisplay();
        UpdateBetUI();
    }

    #endregion

    #region Public References

    public void IncreaseChipValue()
    {
        Debug.Log("LovKumar Iecrease");
        currentIndex = (currentIndex + 1) % betValues.Length;
        UpdateBetUI();
        UpdateDisplay();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + betValues.Length) % betValues.Length;
        UpdateBetUI();
        UpdateDisplay();
    }

    public float GetCurrentBet() => betValues[currentIndex];

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = betValues[currentIndex];
        Debug.Log("LovKumar Iecrease UI");
        betText.text = bet.ToString("0.00");
    }
    public void UpdateDisplay()
    {
        float betValue = GetCurrentBet();

        float jackpotMajor = betValue * 5000;
        float jackpotMinor = betValue * 100;
        float jackpotMini = betValue * 20;

        mega_Jackpot.text = jackpotMajor.ToString("N2");
        midi_Jackpot.text = jackpotMinor.ToString("N2");
        mini_Jackpot.text = jackpotMini.ToString("N2");
    }
    #endregion
}