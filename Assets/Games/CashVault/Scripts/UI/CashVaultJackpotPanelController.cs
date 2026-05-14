using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class CashVaultJackpotPanelController : MonoBehaviour
{

    [SerializeField] private TMP_Text jackpotTextGrand;
    [SerializeField] private TMP_Text jackpotTextMajor;
    [SerializeField] private TMP_Text jackpotTextMinor;
    [SerializeField] private TMP_Text jackpotTextMini;

    private CashVaultBetController cashVaultBetController;

    private void Start()
    {
        cashVaultBetController = GetComponent<CashVaultBetController>();
        CashVaultBetController.OnBetValueChanged += HandleBetChanged;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        float betValue = cashVaultBetController.GetCurrentBet();

        float jackpotGrand = betValue * 1000;
        float jackpotMajor = betValue * 250;
        float jackpotMinor = betValue * 50;
        float jackpotMini = betValue * 10;

        jackpotTextGrand.text = jackpotGrand.ToString("N2");
        jackpotTextMajor.text = jackpotMajor.ToString("N2");
        jackpotTextMinor.text = jackpotMinor.ToString("N2");
        jackpotTextMini.text = jackpotMini.ToString("N2");
    }

    private void HandleBetChanged()
    {
        UpdateDisplay();
    }
}