using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MonkeyMadnessPayboardController : MonoBehaviour
{
    [SerializeField] private TMP_Text wildText;
    [SerializeField] private TMP_Text drumsText;
    [SerializeField] private TMP_Text toucanText;
    [SerializeField] private TMP_Text bananaText;
    [SerializeField] private TMP_Text pineappleText;
    [SerializeField] private TMP_Text coconutText;
    [SerializeField] private TMP_Text anyText;

    private MonkeyMadnessBetController monkeyMadnessBetController;

    private void Start()
    {
        monkeyMadnessBetController = GetComponent<MonkeyMadnessBetController>();
        MonkeyMadnessBetController.OnBetValueChanged += HandleBetChanged;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        float betValue = monkeyMadnessBetController.GetCurrentLinePlay();

        float wildPay = betValue * 1000;
        float drumsPay = betValue * 50;
        float toucanPay = betValue * 30;
        float bananaPay = betValue * 20;
        float pineapplePay = betValue * 12;
        float coconutPay = betValue * 8;
        float anyPay = betValue * 4;

        wildText.text = wildPay.ToString("N2");
        drumsText.text = drumsPay.ToString("N2");
        toucanText.text = toucanPay.ToString("N2");
        bananaText.text = bananaPay.ToString("N2");
        pineappleText.text = pineapplePay.ToString("N2");
        coconutText.text = coconutPay.ToString("N2");
        anyText.text = anyPay.ToString("N2");
    }

    private void HandleBetChanged()
    {
        UpdateDisplay();
    }
}
