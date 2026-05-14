using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class InternetWatchdog : MonoBehaviour
{
    public static InternetWatchdog Instance;

    public bool isWatchdogActive = false;

    [Header("UI")]
    [SerializeField] private GameObject offlinePanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button reloadButton;

    [Header("Checks")]
    [Tooltip("A lightweight URL that should respond quickly (200/204). Ideally your own domain.")]
    [SerializeField] private string healthUrlEndpoint = "/api/health";

    [Tooltip("Seconds between checks while online.")]
    [SerializeField] private float checkIntervalOnline = 5f;

    [Tooltip("Seconds between checks while offline (more frequent).")]
    [SerializeField] private float checkIntervalOffline = 2f;

    [Tooltip("Request timeout in seconds.")]
    [SerializeField] private int timeoutSeconds = 4;

    [Tooltip("How many consecutive failed health checks before showing the offline popup.")]
    [SerializeField] private int consecutiveFailsBeforeShow = 3;

    private string HealthUrl => BackendBaseUrlController.instance.GetBaseUrl() + healthUrlEndpoint;

    private bool isOfflineShown;
    private int consecutiveFailCount = 0;

    private void Awake()
    {
        //if (Instance == null)
        //{
        //    Instance = this;
        //    DontDestroyOnLoad(gameObject);
        //}
        //else
        //{
        //    DestroyImmediate(gameObject);
        //}

        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (offlinePanel != null)
            offlinePanel.SetActive(false);

        if (reloadButton != null)
        {
            reloadButton.onClick.RemoveAllListeners();
            reloadButton.onClick.AddListener(ReloadPage);
        }

        StartCoroutine(ConnectivityLoop());
    }

    private IEnumerator ConnectivityLoop()
    {
        while (true)
        {
            if (isWatchdogActive)
            {
                yield return CheckOnce();
            }
            yield return new WaitForSeconds(isOfflineShown ? checkIntervalOffline : checkIntervalOnline);
        }
    }

    private IEnumerator CheckOnce()
    {
        bool success = false;
        string failureReason = "Unknown connection error.";

        // Quick cheap hint
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            success = false;
            failureReason = "No internet connection detected.";
        }
        else
        {
            // Real check: make a tiny request with cache-buster
            string url = HealthUrl;
            if (!url.Contains("?")) url += "?";
            else url += "&";
            url += "t=" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                success = (req.result == UnityWebRequest.Result.Success) &&
                          (req.responseCode >= 200 && req.responseCode < 400);
#else
                success = (!req.isNetworkError && !req.isHttpError) &&
                          (req.responseCode >= 200 && req.responseCode < 400);
#endif

                Debug.Log($"Api Response for {healthUrlEndpoint}: {req.result.ToString()}");

                if (!success)
                {
                    failureReason = string.IsNullOrEmpty(req.error)
                        ? $"Request failed with response code {req.responseCode}."
                        : req.error;
                }
            }
        }

        if (success)
        {
            if (consecutiveFailCount > 0)
            {
                Debug.Log($"InternetWatchdog: Health check succeeded. Resetting fail counter from {consecutiveFailCount} to 0.");
            }

            consecutiveFailCount = 0;
            //HideOffline();
        }
        else
        {
            consecutiveFailCount++;

            Debug.Log(
                $"InternetWatchdog: Health check failed ({consecutiveFailCount}/{consecutiveFailsBeforeShow}). Reason: {failureReason}"
            );

            if (consecutiveFailCount >= consecutiveFailsBeforeShow)
            {
                ShowOffline(
                    "Connection problem detected.\n" +
                    "Please reload the page."
                );
            }
        }
    }

    public void ForceShowOfflinePopup()
    {
        ShowOffline(
                    "Connection problem detected.\n" +
                    "Please reload the page."
                );
    }

    private void ShowOffline(string msg)
    {
        isOfflineShown = true;

        if (offlinePanel != null && !offlinePanel.activeSelf)
            offlinePanel.SetActive(true);

        if (messageText != null)
            messageText.text = msg;
    }

    private void HideOffline()
    {
        isOfflineShown = false;

        if (offlinePanel != null && offlinePanel.activeSelf)
            offlinePanel.SetActive(false);
    }

    public void ReloadPage()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLReload.Reload();
#else
        Debug.Log("Reload requested (non-WebGL). Restarting current scene.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
#endif
    }
}