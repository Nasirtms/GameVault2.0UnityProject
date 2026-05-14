using UnityEngine;
using UnityEngine.UI;

public class RichLittlePiggiesRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;
    [SerializeField] private GameObject[] pages;

    [Header("Popup Controls")]
    public Button closeButton;
    public Button left;
    public Button right;

    private int currentPageIndex = 0;

    #endregion

    #region Unity Methods

    private void Start()
    {
        currentPageIndex = 0;
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
        left.onClick.AddListener(onBackward);
        right.onClick.AddListener(onForward);
    }

    #endregion

    #region Popup Display

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        pages[currentPageIndex].SetActive(true);
    }

    public void onForward()
    {
        RichLittlePiggiesUIManager.Instance.PlaySound("Rules_Close");
        pages[currentPageIndex].SetActive(false);
        currentPageIndex = (currentPageIndex + 1) % pages.Length;
        pages[currentPageIndex].SetActive(true);
    }

    public void onBackward()
    {
        RichLittlePiggiesUIManager.Instance.PlaySound("Rules_Close");
        pages[currentPageIndex].SetActive(false);
        currentPageIndex = (currentPageIndex - 1 + pages.Length) % pages.Length;
        pages[currentPageIndex].SetActive(true);
    }

    public void ClosePopup()
    {
        RichLittlePiggiesUIManager.Instance.PlaySound("Rules_Close");
        rulesPopupPanel.SetActive(false);
        pages[currentPageIndex].SetActive(false);
    }

    #endregion
}
