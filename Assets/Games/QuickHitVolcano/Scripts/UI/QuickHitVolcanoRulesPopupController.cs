using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuickHitVolcanoRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI paginationText;
    [SerializeField] private List<GameObject> pages;

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

    #region Public References

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        ShowPage(0);
    }

    public void ClosePopup()
    {
        QuickHitVolcanoUIManager.Instance.PlaySound("Button");

        rulesPopupPanel.SetActive(false);
    }

    #endregion

    #region Popup Control

    private void ShowPage(int index)
    {
        if (pages == null || pages.Count == 0) return;

        index = Mathf.Clamp(index, 0, pages.Count - 1);
        currentPageIndex = index;

        for (int i = 0; i < pages.Count; i++)
        {
            pages[i].SetActive(i == currentPageIndex);
        }

        paginationText.text = $"{currentPageIndex + 1}/{pages.Count}";
    }

    private void PreviousPage()
    {
        QuickHitVolcanoUIManager.Instance.PlaySound("Button");

        int newIndex = (currentPageIndex - 1 + pages.Count) % pages.Count;
        ShowPage(newIndex);
    }

    private void NextPage()
    {
        QuickHitVolcanoUIManager.Instance.PlaySound("Button");

        int newIndex = (currentPageIndex + 1) % pages.Count;
        ShowPage(newIndex);
    }

    #endregion
}
