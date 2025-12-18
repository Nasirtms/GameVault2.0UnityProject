using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SaharaRichesJackpotPanelController : MonoBehaviour
{

    [SerializeField] private TMP_Text jackpotTextGrand;
    [SerializeField] private TMP_Text jackpotTextMajor;
    [SerializeField] private TMP_Text jackpotTextMinor;
    [SerializeField] private TMP_Text jackpotTextMini;

    private SaharaRichesBetController saharaRichesBetController;

    private void Start()
    {
        saharaRichesBetController = GetComponent<SaharaRichesBetController>();
        SaharaRichesBetController.OnBetValueChanged += HandleBetChanged;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        float betValue = saharaRichesBetController.GetCurrentBet();

        float jackpotGrand = betValue * 200;
        float jackpotMajor = betValue * 100;
        float jackpotMinor = betValue * 20;
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
