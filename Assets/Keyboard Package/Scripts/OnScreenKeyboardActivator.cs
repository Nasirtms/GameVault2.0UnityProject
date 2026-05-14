using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OnScreenKeyboardActivator : MonoBehaviour
{
    public TMP_InputField inputField;
    public bool hiddenCharacterField;

    private void OnValidate()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }
    }

    private void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }
    }

    private void OnEnable()
    {
        if (inputField != null)
        {
            inputField.onSelect.AddListener(TextboxSelected);
        }
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(TextboxSelected);
        }
    }

    void TextboxSelected(string str)
    {
        if (OnScreenKeyboardManager.Instance == null)
            return;

        OnScreenKeyboardManager.Instance.ShowKeyboard(this);
    }
}
