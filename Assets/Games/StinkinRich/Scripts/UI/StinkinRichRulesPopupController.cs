using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class StinkinRichRulesPopupController : MonoBehaviour
{
    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    public Button closeButton;
    public Button Forward;
    public Button Back;
    private int currentPageIndex = 0;

    public List<GameObject> rulesPages;

    private void Start()
    {
        if (rulesPopupPanel != null)
            rulesPopupPanel.SetActive(false);

        closeButton?.onClick.AddListener(ClosePopup);
        Forward?.onClick.AddListener(NextPage);
        Back?.onClick.AddListener(PrevPage);
    }

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        currentPageIndex = 0;
        UpdatePage();
    }

    public void ClosePopup()
    {
        StinkinRichUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
        foreach (GameObject page in rulesPages)
        {
            page.SetActive(false);
        }
    }

    public void NextPage()
    {
        StinkinRichUIManager.Instance.PlaySound("Button");

        currentPageIndex = (currentPageIndex + 1) % rulesPages.Count;
        UpdatePage();
    }
    public void PrevPage()
    {
        StinkinRichUIManager.Instance.PlaySound("Button");

        currentPageIndex = (currentPageIndex - 1 + rulesPages.Count) % rulesPages.Count;
        UpdatePage();
    }

    private void UpdatePage()
    {
        for (int i = 0; i < rulesPages.Count; i++)
        {
            rulesPages[i].SetActive(i == currentPageIndex);
        }
    }

}