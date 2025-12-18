using UnityEngine;
using TMPro;

public class CashMachineBetController : MonoBehaviour
{
    #region Variables

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] betValues = new float[] {
        0.01f, 0.02f, 0.03f, 0.04f,
        0.05f, 0.10f, 0.20f, 0.30f,
        0.40f, 0.50f, 0.60f, 0.70f
    };
    private int currentIndex = 0;
    private bool isHighStake = false;

    [Header("UI References")]
    [SerializeField] private TMP_Text lineBetText;
    [SerializeField] private TMP_Text totalBetText;

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

    public void HighStake()
    {
        CashMachineSlotMachine.Instance.isHighStake = true;
        isHighStake = true;
        UpdateBetUI();
    }

    public void LowStake()
    {
        isHighStake = false;
        CashMachineSlotMachine.Instance.isHighStake = false;
        UpdateBetUI();
    }

    public float GetCurrentBet()
    {
        if (isHighStake)
        {
            return betValues[currentIndex] * 10f;
        }
        else
        {
            return betValues[currentIndex];
        }
    }

    #endregion

    #region Bet Update

    private void UpdateBetUI()
    {
        float bet = betValues[currentIndex];

        if (isHighStake)
        {
            bet *= 10f;
        }
        else
        {
            bet *= 1f;
        }

        lineBetText.text = bet.ToString("N2");
        totalBetText.text = bet.ToString("N2");

        if(currentIndex == 0)
        {
            CashMachineUIManager.Instance.reel2NotSpinBg.SetActive(true);
            CashMachineUIManager.Instance.reel3NotSpinBg.SetActive(true);
            CashMachineSlotMachine.Instance.canReel2spin = false;
            CashMachineSlotMachine.Instance.canReel3spin = false;
        }
        else if (currentIndex == 1)
        {
            CashMachineUIManager.Instance.reel2NotSpinBg.SetActive(false);
            CashMachineUIManager.Instance.reel3NotSpinBg.SetActive(true);
            CashMachineSlotMachine.Instance.canReel2spin = true;
            CashMachineSlotMachine.Instance.canReel3spin = false;
        }
        else if (currentIndex == 2)
        {
            CashMachineUIManager.Instance.reel2NotSpinBg.SetActive(false);
            CashMachineUIManager.Instance.reel3NotSpinBg.SetActive(false);
            CashMachineSlotMachine.Instance.canReel2spin = true;
            CashMachineSlotMachine.Instance.canReel3spin = true;
        }
    }
    public void UpdateBetUi()
    {
        UpdateBetUI();
    }
    #endregion
}
