using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class PandaFortuneRulesPopupController : MonoBehaviour
{
    [Header("Rules Popup")]
    [SerializeField] private GameObject rulesPopupPanel;

    [Header("Popup Controls")]
    public Button closeButton;

    private void Start()
    {
        rulesPopupPanel.SetActive(false);

        closeButton.onClick.AddListener(ClosePopup);
    }

    public void OpenPopup()
    {
        rulesPopupPanel.SetActive(true);
    }

    public void ClosePopup()
    {
        //PandaFortuneUIManager.Instance.PlaySound("Button");
        rulesPopupPanel.SetActive(false);
    }

}