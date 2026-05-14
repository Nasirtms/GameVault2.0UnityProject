// MobileKeyboardManager: opens keyboard on mobile.
// - Native iOS/Android: uses TouchScreenKeyboard.
// - WebGL (mobile browser): TouchScreenKeyboard does NOT work; uses HTML input via JavaScript.
//
// Setup:
// 1. Create a GameObject named exactly "MobileKeyboardManager" in your scene (or change the name in index.html SendMessage target).
// 2. Add this script to it.
// 3. For WebGL: copy Plugins/WebGL/MobileKeyboard.jslib into your Unity project's Assets/Plugins/WebGL/.
// 4. Your WebGL build's index.html must define showMobileKeypad() and send input to Unity (already in your template).

using UnityEngine;

public class MobileKeyboardManager : MonoBehaviour
{
    private TouchScreenKeyboard mobileKeyboard;
    public static string keyboardText = "";

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void ShowMobileKeypad();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SetMobileKeyboardPlaceholder(string placeholder);
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SetMobileKeyboardInitialValue(string value);
#endif

    public void OpenKeyboard()
    {
        OpenKeyboard("Placeholder Text", keyboardText);
    }

    public void OpenKeyboard(string placeholder, string initialValue)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SetMobileKeyboardPlaceholder(placeholder ?? "");
        SetMobileKeyboardInitialValue(initialValue ?? keyboardText);
        ShowMobileKeypad();
#else
        mobileKeyboard = TouchScreenKeyboard.Open(keyboardText, TouchScreenKeyboardType.Default, false, false, false, false, placeholder ?? "Placeholder Text");
#endif
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (mobileKeyboard != null)
        {
            keyboardText = mobileKeyboard.text;

            if (mobileKeyboard.done)
            {
                Debug.Log("Input finished: " + keyboardText);
                OnInputFinished(keyboardText);
                mobileKeyboard = null;
            }
        }
#endif
    }

    // Called from WebGL index.html via SendMessage('MobileKeyboardManager', 'OnKeyboardInput', text)
    public void OnKeyboardInput(string text)
    {
        keyboardText = text ?? "";
        Debug.Log("Input finished (WebGL): " + keyboardText);
        OnInputFinished(keyboardText);
    }

    protected virtual void OnInputFinished(string text)
    {
        // Override in subclass or hook from inspector to react when user finishes typing.
    }
}
