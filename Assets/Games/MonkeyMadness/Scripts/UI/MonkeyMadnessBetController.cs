using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class MonkeyMadnessBetController : MonoBehaviour
{

    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] linePlay = new float[] {
        0.01f, 0.02f, 0.03f, 0.04f,
        0.05f, 0.10f, 0.20f, 0.30f,
        0.40f, 0.50f, 0.60f, 0.70f
    };
    private int currentIndex = 0;

    [Header("UI References")]
    [SerializeField] private TMP_Text linePlayText;
    [SerializeField] private TMP_Text totalPlayText;

    // Events
    public delegate void MonkeyMadnessBetControllerEvents();
    public static event MonkeyMadnessBetControllerEvents OnBetValueChanged;

    #endregion

    #region Unity Methods

    private void Start() => UpdateBetUI();

    #endregion

    #region Public References

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % linePlay.Length;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + linePlay.Length) % linePlay.Length;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public float GetCurrentBet() => linePlay[currentIndex] * 9;

    public float GetCurrentLinePlay() => linePlay[currentIndex];

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float linePlay = this.linePlay[currentIndex];
        float totalPlay = linePlay * 9;

        linePlayText.text = linePlay.ToString("N2");
        totalPlayText.text = totalPlay.ToString("N2");
    }

    #endregion
}
