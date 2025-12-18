using Sirenix.OdinInspector;
using UnityEngine;

public class BackendBaseUrlController : MonoBehaviour
{
    [Header("Select Backend Environment")]
    public BackendEnvironment environment;

    private const string LocalUrl = "http://localhost:5036";
    private const string ProductionUrl = "http://3.231.201.150:5036";

    public string GetBaseUrl()
    {
        Debug.Log($"GetBaseUrl environment : {environment.ToString()}");

        return environment switch
        {
            BackendEnvironment.Local_ngrok => LocalUrl,
            BackendEnvironment.Production_AWS_Server => ProductionUrl,
            _ => ProductionUrl
        };
    }
}

[Searchable]
public enum BackendEnvironment
{
    Local_ngrok,
    Production_AWS_Server
}
