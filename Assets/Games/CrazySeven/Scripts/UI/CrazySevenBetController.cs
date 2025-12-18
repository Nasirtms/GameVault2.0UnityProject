using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrazySevenBetController : MonoBehaviour
{
    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.01f, 0.02f, 0.03f, 0.04f,
        0.05f, 0.06f, 0.07f, 0.08f,
        0.09f, 0.10f, 0.20f, 0.30f,
        0.40f, 0.50f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text chipText;
    [SerializeField] private TMP_Text betText;

    private void Start() => UpdateBetUI();

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % chipValues.Length;
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + chipValues.Length) % chipValues.Length;
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        float chip = chipValues[currentIndex];
        float bet = chip * 30f;

        chipText.text = chip.ToString("0.00");
        betText.text = bet.ToString("0.00");
    }

    public float GetCurrentBet() => chipValues[currentIndex] * 30f;
}
