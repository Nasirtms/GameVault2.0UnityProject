using TMPro;
using UnityEngine;

public class CustomPasswordChar : MonoBehaviour
{
    public TMP_InputField passwordInputField;

    void Start()
    {
        // Set content type to Password just to be safe
        passwordInputField.contentType = TMP_InputField.ContentType.Password;

        // Set custom masking character (e.g. '•', '✱', '•')
        passwordInputField.asteriskChar = 'X';

        // Force the input field to re-apply settings
        passwordInputField.ForceLabelUpdate();
    }
}
