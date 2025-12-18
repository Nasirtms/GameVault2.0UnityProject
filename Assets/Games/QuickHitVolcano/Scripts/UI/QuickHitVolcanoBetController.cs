using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuickHitVolcanoBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.01f, 0.02f, 0.03f, 0.04f,
        0.05f, 0.10f, 0.20f, 0.30f,
        0.40f, 0.50f, 0.60f, 0.70f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text chipText;
    [SerializeField] private TMP_Text betText;

    public delegate void QuickHitVolcanoBetControllerEvents();
    public static event QuickHitVolcanoBetControllerEvents OnBetValueChanged;

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

    public float GetCurrentBet() => chipValues[currentIndex] * 30f;

    #endregion

    #region Bet Control

    private void UpdateBetUI()
    {
        float chip = chipValues[currentIndex];
        float bet = chip * 30f;

        chipText.text = chip.ToString("0.00");
        betText.text = bet.ToString("0.00");
    }

    #endregion
}
