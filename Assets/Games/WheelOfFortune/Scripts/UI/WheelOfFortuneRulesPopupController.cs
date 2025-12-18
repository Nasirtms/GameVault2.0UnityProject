using UnityEngine.UI;
using UnityEngine;

public class WheelOfFortuneRulesPopupController : MonoBehaviour
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

    #region Popup Display

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        WheelOfFortuneUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
    }

    #endregion
}
