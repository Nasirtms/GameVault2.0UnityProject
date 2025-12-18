using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

public static class JSInputHandler
{
    public static void OnLoginSuccess(string token)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalCall("onLoginStatusChanged", "true");
    // Send token after login
    if (!string.IsNullOrEmpty(token))
    {
        Application.ExternalCall("onTokenReceived", token);
    }
#endif
    }

    public static void OnLoginField()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.ExternalCall("onLoginStatusChanged", "false");
#endif
    }

    public static void OnLogoutSuccess()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        
        // Clear stored token
        Application.ExternalCall("clearStoredToken");
        
        // First hide the fullscreen button
        Application.ExternalCall("onLoginStatusChanged", "false");
        
        // Then exit fullscreen if currently in fullscreen
        Application.ExternalCall("exitFullscreenIfActive");
        
#endif
    }

    public static void exitFullscreenIfActive()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Then exit fullscreen if currently in fullscreen
        Application.ExternalCall("exitFullscreenIfActive");
#endif
    }

    // Send token and baseUrl to JavaScript (encrypted storage)
    public static void SendTokenToJS(string token, string baseUrl)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    if (!string.IsNullOrEmpty(token))
    {
        // Send token immediately
        Application.ExternalCall("onTokenReceived", token);
    }
    else
    {
        Debug.LogWarning("JSInputHandler: Attempted to send empty token");
    }
    
    // Set API base URL
    if (!string.IsNullOrEmpty(baseUrl))
    {
        Application.ExternalCall("setApiBaseUrl", baseUrl);
    }
#endif
    }
}