using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackTextPreset : MonoBehaviour
{
    public Button button;
    public TextMeshProUGUI textbox;

    public string GetText()
    {
        if (textbox == null)
            return "";

        return textbox.text;
    }
}
