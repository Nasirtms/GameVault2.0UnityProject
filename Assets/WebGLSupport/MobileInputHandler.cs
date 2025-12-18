using UnityEngine;
using TMPro;

public class MobileInputHandler : MonoBehaviour
{
    public TMP_InputField myTMPInputField;

    public void OnMobileInput(string value) // <-- called by JS
    {
        myTMPInputField.text = value;
    }
}
