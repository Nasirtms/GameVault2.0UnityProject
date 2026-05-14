using Sirenix.OdinInspector;
using UnityEngine;

public class BackendBaseUrlController : MonoBehaviour
{
    public static BackendBaseUrlController instance;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    public string GetBaseUrl()
    {
        Debug.Log($"GetBaseUrl environment : {SceneManagement.buildConfig.GetUrl()}");
        return SceneManagement.buildConfig.GetUrl();
    }

    public string GetSocketBaseUrl()
    {
        Debug.Log($"GetSocketBaseUrl environment : {SceneManagement.buildConfig.GetSocketBaseUrl()}");
        return SceneManagement.buildConfig.GetSocketBaseUrl();
    }
}
