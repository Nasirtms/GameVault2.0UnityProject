using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UltimateFireLinkOlveraStreetBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.12f, 0.21f, 0.3f, 0.42f, 0.51f, 0.60f, 0.72f, 0.81f, 0.90f, 1.20f, 2.10f, 3.00f, 4.20f, 5.10f, 6.00f, 7.20f, 8.10f, 9.90f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text totalPlayText;

    public delegate void UltimateFireLinkOlveraStreetBetControllerEvents();
    public static event UltimateFireLinkOlveraStreetBetControllerEvents OnBetValueChanged;

    #endregion

    #region Unity Methods

    private void Start() => UpdateBetUI();

    #endregion

    #region Public References

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % chipValues.Length;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + chipValues.Length) % chipValues.Length;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public void SetMaxBet()
    {
        currentIndex = chipValues.Length - 1;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public float GetCurrentBet() => chipValues[currentIndex];

    #endregion

    #region Bet Control

    private void UpdateBetUI()
    {
        float bet = chipValues[currentIndex];
        totalPlayText.text = bet.ToString("0.00");
    }

    #endregion
}
