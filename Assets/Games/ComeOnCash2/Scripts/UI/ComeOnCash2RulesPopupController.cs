using UnityEngine;
using UnityEngine.UI;

public class ComeOnCash2RulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;
    [SerializeField] private GameObject[] pages;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;

    private int currentPageIndex = 0;

    #endregion

    #region Unity Methods

    private void Start()
    {
        currentPageIndex = 0;
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
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
        ComeOnCash2UIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
        pages[currentPageIndex].SetActive(false);
    }
    #endregion
}
