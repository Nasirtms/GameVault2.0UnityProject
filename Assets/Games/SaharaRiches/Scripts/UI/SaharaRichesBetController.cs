using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaharaRichesBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.30f, 0.90f, 1.50f, 3.00f, 6.00f, 9.00f, 12.00f, 15.00f, 21.00f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;

    public delegate void SaharaRichesBetControllerEvents();
    public static event SaharaRichesBetControllerEvents OnBetValueChanged;
    #endregion

    #region Unity Methods

    private void Start() => UpdateBetUI();

    #endregion

    #region Public References

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % betValues.Length;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + betValues.Length) % betValues.Length;
        OnBetValueChanged?.Invoke();
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