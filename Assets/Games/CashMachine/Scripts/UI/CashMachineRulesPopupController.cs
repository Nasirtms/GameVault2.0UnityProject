using UnityEngine;
using UnityEngine.UI;

public class CashMachineRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;
    [SerializeField] private GameObject[] pages;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button rightbutton;
    [SerializeField] private Button leftbutton;

    private int currentPageIndex = 0;

    #endregion

    #region Unity Methods

    private void Start()
    {
        currentPageIndex = 0;
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
        leftbutton.onClick.AddListener(OnLeft);
        rightbutton.onClick.AddListener(OnRight);
    }

    #endregion

    #region Popup Control

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        pages[currentPageIndex].SetActive(true);
    }

    public void ClosePopup()
    {
        CashMachineUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
        pages[currentPageIndex].SetActive(false);
    }

    public void OnLeft()
    {
        CashMachineUIManager.Instance.PlaySound("Button");

        pages[currentPageIndex].SetActive(false);
        currentPageIndex = (currentPageIndex - 1 + pages.Length) % pages.Length;
        pages[currentPageIndex].SetActive(true);
    }

    public void OnRight()
    {
        CashMachineUIManager.Instance.PlaySound("Button");

        pages[currentPageIndex].SetActive(false);
        currentPageIndex = (currentPageIndex + 1) % pages.Length;
        pages[currentPageIndex].SetActive(true);
    }

    #endregion
}
