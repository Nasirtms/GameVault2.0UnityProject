using UnityEngine;
using TMPro;

public class TenTimesWinsBetController : MonoBehaviour
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
    private readonly int[] multiplierValues = new int[] { 1, 2, 3 };
    private int currentMultiplierIndex = 0; 

    [Header("UI References")]
    [SerializeField] private TMP_Text betText;
    [SerializeField] private TMP_Text multiplierText;

    [Header("Multiplier Indicator")]
    [SerializeField] private RectTransform[] multiplierImage;
    #endregion

    #region Unity Methods

    private void Start()
    {
        multiplierImage[0].gameObject.SetActive(true);
        UpdateBetUI();
    }
    private void Update()
    {
        UpdateBetUI();
    }
    #endregion

    #region Public References

    public void IncreaseChipValue()
    {
        currentIndex = (currentIndex + 1) % chipValues.Length;
        if (currentIndex == chipValues.Length - 1)
        {
            TenTimesWinsUIManager.Instance.PlaySound("Max");
        }
        else
        {
            TenTimesWinsUIManager.Instance.PlaySound("Increase");
        }
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        currentIndex = (currentIndex - 1 + chipValues.Length) % chipValues.Length;
        if (currentIndex == chipValues.Length - 1)
        {
            TenTimesWinsUIManager.Instance.PlaySound("Max");
        }
        else
        {
            TenTimesWinsUIManager.Instance.PlaySound("Decrease");
        }
        UpdateBetUI();
    }

    public float GetCurrentBet() => chipValues[currentIndex] * GetCurrentMultiplier();

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float chip = chipValues[currentIndex];
        float bet = chip * GetCurrentMultiplier();

        betText.text = bet.ToString("0.00");
        multiplierText.text = GetCurrentMultiplier() + "X";
    }

    #endregion
    #region Multiplier Methods

    public void IncreaseMultiplier()
    {
        currentMultiplierIndex = (currentMultiplierIndex + 1) % multiplierValues.Length;
        UpdateBetUI();
        UpdateMultiplierImage();
    }

    public void DecreaseMultiplier()
    {
        currentMultiplierIndex = (currentMultiplierIndex - 1 + multiplierValues.Length) % multiplierValues.Length;
        UpdateBetUI();
        UpdateMultiplierImage();
    }

    public int GetCurrentMultiplier() => multiplierValues[currentMultiplierIndex];

    private void UpdateMultiplierImage()
    {
        int mult = GetCurrentMultiplier();

        if (mult == 1)
        {
            multiplierImage[0].gameObject.SetActive(true);
            multiplierImage[1].gameObject.SetActive(false);
            multiplierImage[2].gameObject.SetActive(false);
        }
        else if (mult == 2)
        {
            multiplierImage[1].gameObject.SetActive(true);
            multiplierImage[0].gameObject.SetActive(false);
            multiplierImage[2].gameObject.SetActive(false);
        }
        else if (mult == 3)
        {
            multiplierImage[2].gameObject.SetActive(true);
            multiplierImage[1].gameObject.SetActive(false);
            multiplierImage[0].gameObject.SetActive(false);
        }
    }

    #endregion

}
