using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SecretsOfChristmasRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button closeButton;

    [Header("UI References")]
    [SerializeField] private List<GameObject> pages;

    [Header("Page Indicators")]
    [SerializeField] private Image[] pageDots;
    [SerializeField] private Sprite activeDotSprite;
    [SerializeField] private Sprite inactiveDotSprite;

    private int currentPageIndex = 0;
    #endregion

    #region Unity Methods

    private void Start()
    {
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
        leftButton.onClick.AddListener(PreviousPage);
        rightButton.onClick.AddListener(NextPage);
    }

    #endregion

    #region Popup Control

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        ShowPage(currentPageIndex);
    }

    public void ClosePopup()
    {
        //SecretsOfChristmasUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Count == 0) return;

        currentPageIndex = (index + pages.Count) % pages.Count;

        for (int i = 0; i < pages.Count; i++)
            pages[i].SetActive(i == currentPageIndex);

        UpdatePageIndicators();
    }

    private void PreviousPage()
    {
        //SecretsOfChristmasUIManager.Instance.PlaySound("Button");
        ShowPage(currentPageIndex - 1);
    }

    private void NextPage()
    {
        //SecretsOfChristmasUIManager.Instance.PlaySound("Button");
        ShowPage(currentPageIndex + 1);
    }

    private void UpdatePageIndicators()
    {
        if (pageDots == null || pageDots.Length == 0)
            return;

        for (int i = 0; i < pageDots.Length; i++)
        {
            if (pageDots[i] == null)
                continue;

            pageDots[i].sprite = (i == currentPageIndex) ? activeDotSprite : inactiveDotSprite;
        }
    }

    #endregion
}