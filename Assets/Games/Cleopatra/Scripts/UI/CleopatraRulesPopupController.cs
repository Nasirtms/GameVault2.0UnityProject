using UnityEngine;
using UnityEngine.UI;

public class CleopatraRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    public Button closeButton;

    #endregion

    #region Unity Methods

    private void Start()
    {
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
    }

    #endregion

    #region Popup Display

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        CleopatraUIManager.Instance.PlaySound("Rules_Close");
        rulesPopupPanel.SetActive(false);
    }

    #endregion
}
