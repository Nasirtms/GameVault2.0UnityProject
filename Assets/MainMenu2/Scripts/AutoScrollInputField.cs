using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AutoScrollInputField : MonoBehaviour
{
    public ScrollRect scrollRect;
    private TMP_InputField inputField;

    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
        // Trigger every time the user types a character
        inputField.onValueChanged.AddListener(delegate { SnapToBottom(); });
    }

    public void SnapToBottom()
    {
        // We must wait until the end of the frame so the 
        // LayoutGroup and ContentSizeFitter can finish resizing the content.
        Debug.Log("inputField.caretPosition: " + inputField.caretPosition + " ___ " + inputField.text.Length);
        if (inputField.caretPosition >= inputField.text.Length)
        {
            StartCoroutine(ScrollToBottomCoroutine());
        }
    }

    IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForEndOfFrame();
        // 0 is bottom, 1 is top for verticalNormalizedPosition
        scrollRect.verticalNormalizedPosition = 0f;
    }
}