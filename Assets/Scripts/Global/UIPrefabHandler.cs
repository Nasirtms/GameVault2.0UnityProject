using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIPrefabHandler : MonoBehaviour
{
    public Prefab_type prefab_Type = Prefab_type.None;
    [SerializeField] private Transform panelContent;
    [SerializeField] private Transform Content;
    [SerializeField] private List<Button> buttons = new List<Button>();
    [SerializeField] private List<InputField> _inputFieldList = new List<InputField>();

    //Events
    public event Action<TMP_InputField, TMP_InputField, TMP_InputField> OnChangePasswordRequest;
    public event Action<TMP_InputField, TMP_InputField, TMP_InputField> OnChangePasswordCancle;





    [SerializeField] private TMP_InputField oldPasswordField;
    [SerializeField] private TMP_InputField newPasswordField;
    [SerializeField] private TMP_InputField confirmPasswordField;
    private void Start()
    {

        CachePanel();
        CacheButtons();
        RegisterButtonEvents();
    }

    void CachePanel()
    {
        if (transform.childCount > 0)
            panelContent = transform.GetChild(0);
        else
            Debug.LogError($"{name}: No panel child detected!");
    }

    void CacheButtons()
    {
        if (!panelContent) return;

        buttons.Clear();
        buttons.AddRange(panelContent.GetComponentsInChildren<Button>(true));
    }

    void RegisterButtonEvents()
    {
        switch (prefab_Type)
        {
            case Prefab_type.changePassword_Panel:
                ChangePasswordPanelButtonRef();
                break;

            case Prefab_type.None:
            default:
                // Optional: do nothing or log
                break;
        }

    }


    void ChangePasswordPanelButtonRef()
    {
        Transform content = panelContent.transform.Find("Content");

        oldPasswordField = content.Find("OldPassword_Input").GetComponent<TMP_InputField>();
        newPasswordField = content.Find("NewPassword_Input").GetComponent<TMP_InputField>();
        confirmPasswordField = content.Find("ConfirmPassword_Input").GetComponent<TMP_InputField>();


        foreach (var btn in buttons)
        {
            btn.onClick.RemoveAllListeners();

            string btnName = btn.name.ToLower().Trim();
            Debug.Log("nasir_log Btn Name : " + btnName);
            switch (btnName)
            {
                case var name when name.Contains("close"):
                    btn.onClick.AddListener(ClosePanel);
                    break;
                case var name when name.Contains("changepassword_btn"):
                    OnChangePasswordRequest?.Invoke(oldPasswordField, newPasswordField, confirmPasswordField);
                    break;
                default:
                    Debug.Log($"No assigned action for button: {btn.name}");
                    break;
            }
        }
    }

    // === Actions ===

    void ClosePanel()
    {
        if (panelContent != null)
            panelContent.localScale = Vector3.zero;

        if(prefab_Type == Prefab_type.changePassword_Panel)
        {

        }

        gameObject.SetActive(false);
    }
}


public enum Prefab_type
{
    None,
    changePassword_Panel

}
