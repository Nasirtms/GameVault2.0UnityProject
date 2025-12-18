using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GoldenDragonRulesPopupController : MonoBehaviour
{
    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Image RulePanelBg;

    [Header("UI References")]
    [SerializeField] private List<GameObject> pages;

    private int currentPageIndex = 0;

    private void Start()
    {
        rulesPopupPanel.SetActive(false);
        RulePanelBg.gameObject.SetActive(false);
        closeButton.onClick.AddListener(ClosePopup);
        leftButton.onClick.AddListener(PreviousPage);
        rightButton.onClick.AddListener(NextPage);
    }

    public void OpenPopup()
    {
        RulePanelBg.gameObject.SetActive(true);
        rulesPopupPanel.SetActive(true);
        ShowPage(0);
    }

    public void ClosePopup()
    {
        //GoldenDragonUIManager.Instance.PlaySound("FruitParadise_Button");
        RulePanelBg.gameObject.SetActive(false);
        rulesPopupPanel.SetActive(false);
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Count == 0) return;

        currentPageIndex = (index + pages.Count) % pages.Count;

        for (int i = 0; i < pages.Count; i++)
            pages[i].SetActive(i == currentPageIndex);
    }

    private void PreviousPage()
    {
        //GoldenDragonUIManager.Instance.PlaySound("FruitParadise_Button");
        ShowPage(currentPageIndex - 1);
    }

    private void NextPage()
    {
        //GoldenDragonUIManager.Instance.PlaySound("FruitParadise_Button");
        ShowPage(currentPageIndex + 1);
    }
}