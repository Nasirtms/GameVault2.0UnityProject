using System.Runtime.InteropServices;
using UnityEngine;

public static class WebGLReload
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ReloadPageJS();
#endif

    public static void Reload()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ReloadPageJS();
#else
        Debug.Log("WebGLReload.Reload called outside WebGL.");
#endif
    }
}