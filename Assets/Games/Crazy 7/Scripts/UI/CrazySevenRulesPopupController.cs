using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CrazySevenRulesPopupController : MonoBehaviour
{
    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button closePopupButton;

    [Header("Tab Buttons")]
    [SerializeField] private Button rulesTabButton;
    [SerializeField] private Button paylineTabButton;
    [SerializeField] private Button paytableTabButton;
    [SerializeField] private Button bonusTabButton;
    [SerializeField] private Button jackpotTabButton;

    [Header("Tab Panels")]
    [SerializeField] private GameObject rulesTabPanel;
    [SerializeField] private GameObject paylineTabPanel;
    [SerializeField] private GameObject paytableTabPanel;
    [SerializeField] private GameObject bonusTabPanel;
    [SerializeField] private GameObject jackpotTabPanel;

    private void Start()
    {
        SetupButtons();
        rulesPopupPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        closePopupButton?.onClick.RemoveAllListeners();
        rulesTabButton?.onClick.RemoveAllListeners();
        paylineTabButton?.onClick.RemoveAllListeners();
        paytableTabButton?.onClick.RemoveAllListeners();
        bonusTabButton?.onClick.RemoveAllListeners();
        jackpotTabButton?.onClick.RemoveAllListeners();
    }

    private void SetupButtons()
    {
        closePopupButton.onClick.AddListener(ClosePopup);

        rulesTabButton.onClick.AddListener(() => ShowPanel(rulesTabPanel, rulesTabButton));
        paylineTabButton.onClick.AddListener(() => ShowPanel(paylineTabPanel, paylineTabButton));
        paytableTabButton.onClick.AddListener(() => ShowPanel(paytableTabPanel, paytableTabButton));
        bonusTabButton.onClick.AddListener(() => ShowPanel(bonusTabPanel, bonusTabButton));
        jackpotTabButton.onClick.AddListener(() => ShowPanel(jackpotTabPanel, jackpotTabButton));
    }

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        ShowPanel(rulesTabPanel, rulesTabButton);
    }

    private void ClosePopup()
    {
        CrazySevenUIManager.Instance.PlaySound("Crazy_7_Button");

        rulesPopupPanel.SetActive(false);
    }

    private void ShowPanel(GameObject targetPanel, Button targetButton)
    {
        CrazySevenUIManager.Instance.PlaySound("Crazy_7_Button");

        rulesTabPanel.SetActive(false);
        paylineTabPanel.SetActive(false);
        paytableTabPanel.SetActive(false);
        bonusTabPanel.SetActive(false);
        jackpotTabPanel.SetActive(false);

        rulesTabButton.image.enabled = false;
        paylineTabButton.image.enabled = false;
        paytableTabButton.image.enabled = false;
        bonusTabButton.image.enabled = false;
        jackpotTabButton.image.enabled = false;

        targetPanel.SetActive(true);
        targetButton.image.enabled = true;
    }
}
