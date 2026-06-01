using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoScrollTextBox : MonoBehaviour
{
    public ScrollRect scrollRect;
    private TextMeshProUGUI textField;

    public bool scrollVertical = true;
    public bool scrollHorizontal = true;

    private string lastText;

    void Start()
    {
        textField = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (textField != null)
        {
            if (textField.text != lastText)
            {
                lastText = textField.text;
                OnTextChanged(lastText);
            }
        }
    }

    private void OnTextChanged(string newText)
    {
        SnapToBottom();
    }

    public void SnapToBottom()
    {
        StartCoroutine(ScrollToBottomCoroutine());
    }

    IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();
        // 0 is bottom, 1 is top for verticalNormalizedPosition
        if (scrollVertical) scrollRect.verticalNormalizedPosition = 0f;
        if (scrollHorizontal) scrollRect.horizontalNormalizedPosition = 1f;
    }
}
