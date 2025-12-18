using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MonkeyMadnessRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Page")]
    [SerializeField] private List<GameObject> pages;
    [SerializeField] private TextMeshProUGUI paginationText;

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

    #region Popup Display

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        MonkeyMadnessUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
    }

    #endregion

    #region Popup Controls

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
        MonkeyMadnessUIManager.Instance.PlaySound("Button");
        int newIndex = (currentPageIndex - 1 + pages.Count) % pages.Count;
        ShowPage(newIndex);
    }

    private void NextPage()
    {
        MonkeyMadnessUIManager.Instance.PlaySound("Button");
        int newIndex = (currentPageIndex + 1) % pages.Count;
        ShowPage(newIndex);
    }

    #endregion
}
