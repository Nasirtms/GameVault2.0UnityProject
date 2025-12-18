using UnityEngine;
using UnityEngine.UI;

public class TenTimesWinsRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;
    [SerializeField] private GameObject[] rulePages;

    [Header("Popup Controls")]
    public Button closeButton;
    public Button forward;
    public Button backward;
    private int currentPage; 

    #endregion

    #region Unity Methods

    private void Start()
    {
        rulesPopupPanel.SetActive(false);
        currentPage = 0;
        closeButton.onClick.AddListener(ClosePopup);
        forward.onClick.AddListener(OnForward);
        backward.onClick.AddListener(OnBackward);
    }

    #endregion

    #region Popup Display

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
        currentPage = 0;
        rulePages[currentPage].SetActive(true);
    }

    public void ClosePopup()
    {
        rulePages[currentPage].SetActive(false);
        TenTimesWinsUIManager.Instance.PlaySound("Rules_Close");
        rulesPopupPanel.SetActive(false);
    }

    public void OnForward()
    {
        TenTimesWinsUIManager.Instance.PlaySound("Increase");
        if (currentPage <= 2)
        {
            rulePages[currentPage].SetActive(false);
            rulePages[currentPage + 1].SetActive(true);
            currentPage+=1;
        }
        else
        {
            rulePages[currentPage].SetActive(false);
            currentPage = 0;
            rulePages[currentPage].SetActive(true);
        }
    }
    public void OnBackward()
    {
        TenTimesWinsUIManager.Instance.PlaySound("Decrease");
        if (currentPage >= 1)
        {
            rulePages[currentPage].SetActive(false);
            rulePages[currentPage - 1].SetActive(true);
            currentPage -= 1;
        }
        else
        {
            rulePages[currentPage].SetActive(false);
            currentPage = 3;
            rulePages[currentPage].SetActive(true);
        }
    }
    #endregion
}
