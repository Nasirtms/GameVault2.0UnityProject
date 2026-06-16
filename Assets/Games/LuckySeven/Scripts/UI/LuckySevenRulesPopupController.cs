using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LuckySevenRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;


    [Header("Popup Controls")]
    //[SerializeField] private Button leftButton;
    //[SerializeField] private Button rightButton;
    [SerializeField] private Button closeButton;

    [Header("UI References")]
    [SerializeField] private List<GameObject> pages;

    private int currentPageIndex = 0;
    #endregion

    #region Unity Methods

    private void Start()
    {
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
        //leftButton.onClick.AddListener(PreviousPage);
        //rightButton.onClick.AddListener(NextPage);
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
        LuckySevenUIManager.Instance.PlaySound("Button");
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
        LuckySevenUIManager.Instance.PlaySound("Button");
        ShowPage(currentPageIndex - 1);
    }

    private void NextPage()
    {
        LuckySevenUIManager.Instance.PlaySound("Button");
        ShowPage(currentPageIndex + 1);
    }
    #endregion
}