using UnityEngine;
using UnityEngine.UI;

public class VegasSevenRulesPopupController : MonoBehaviour
{
    #region Variables

    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    [SerializeField] private Button closeButton;

    #endregion

    #region Unity Methods

    private void Start()
    {
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
    }

    #endregion

    #region Popup Control

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        VegasSevenUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
    }

    #endregion
}
