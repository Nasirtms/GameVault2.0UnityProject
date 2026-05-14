using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

public class WebGLClipboard : MonoBehaviour
{
    public static WebGLClipboard Instance;

    public UnityAction<string> pasteAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CopyToClipboard(string text);

    [DllImport("__Internal")]
    private static extern void PasteFromClipboard(string gameObjectName, string methodName);
#endif

    public void Copy(string text)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CopyToClipboard(text);
#else
        GUIUtility.systemCopyBuffer = text;
#endif
    }

    public void Paste(UnityAction<string> uAction)
    {
        pasteAction = uAction;

#if UNITY_WEBGL && !UNITY_EDITOR
        PasteFromClipboard(gameObject.name, nameof(OnPasteReceived));
#else
        OnPasteReceived(GUIUtility.systemCopyBuffer);
#endif
    }

    public void OnPasteReceived(string text)
    {
        Debug.Log("Pasted: " + text);

        // Put this into your selected input field here.
        // inputField.text += text;

        pasteAction?.Invoke(text);
        pasteAction = null;
    }
}