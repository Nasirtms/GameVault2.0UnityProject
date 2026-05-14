using Newtonsoft.Json;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UnitySessionManager : MonoBehaviour
{
    public static UnitySessionManager Instance;

    [SerializeField] private const float PollInterval = 20f;
    [SerializeField] GameObject SessionLogoutPanelPrefab;
    [SerializeField] Button confirmBtm = null;

    [SerializeField] private GameObject sessionEndPanel = null;
    string _massage;

    public static Action OnForcedLogout;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartSessionWatch();
    }

    private void AddListenee()
    {
        confirmBtm.onClick.AddListener(LoadLoginScene);
    }

    public void StartSessionWatch()
    {
        if (UserManager.Instance == null || string.IsNullOrEmpty(UserManager.Instance.UserId))
        {
            Debug.LogWarning("⚠️ Cannot start session polling: userId not found.");
            return;
        }

        Debug.Log($"🔐 Starting session polling every {PollInterval} seconds...");
        CancelInvoke(nameof(CheckSessionStatus)); // Avoid duplicates

        UserManager.Instance.sendCountToGetWinDataList = true;
        CheckSessionStatus(); // Run immediately
        InvokeRepeating(nameof(CheckSessionStatus), PollInterval, PollInterval);
    }

    public void StopSessionWatch()
    {
        CancelInvoke(nameof(CheckSessionStatus));
    }

    private void CheckSessionStatus()
    {
        StartCoroutine(CheckSessionStatusCoroutine());

        if (UserManager.Instance != null)
        {
            if (UserManager.Instance.useCanGetCoinFromDB)
            {
                UserManager.Instance.GetUserCurrentCoin();
            }
        }
    }

    private IEnumerator CheckSessionStatusCoroutine()
    {

        string userId = UserManager.Instance.UserId;
        string session = UserManager.Instance.sessionId;
        string url = ApiEndpoints.CheckSession;


        var payload = new SessionCheckRequest { userId = userId, sessionId = session };
        string jsonPayload = JsonConvert.SerializeObject(payload);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST" + ""))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            //// Apply all headers from ApiEndpoints
            //foreach (var header in ApiEndpoints.GetAuthHeaders())
            //{
            //    www.SetRequestHeader(header.Key, header.Value);
            //}

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Session check response: {www.downloadHandler.text}");

                try
                {
                    var response = JsonConvert.DeserializeObject<SessionCheckResponse>(www.downloadHandler.text);
                    if (!response.valid)
                    {
                        Debug.LogWarning($"⛔ Session Invalid: {response.message}");
                        // Only logout if it's a forced logout message
                        if (response.message.Contains("forced to disembark") || response.message.Contains("logged on other devices") || response.message.Contains("User not found"))
                        {
                            Debug.Log("🚫 Forced logout detected - another device logged in 1");
                            if (!string.IsNullOrEmpty(response.message))
                            {
                                //Debug.Log("🚫 Forced logout detected - another device logged in 2");
                                _massage = response.message;
                                HandleForcedLogout();
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Error parsing session response: {ex.Message}");
                }
            }
            else if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, url, jsonPayload, "POST", () => CheckSessionStatusCoroutine());
                yield break;
            }
            else
            {
                if (www.error.Contains("HTTP/1.1 400 Bad Request"))
                {
                    _massage = "Network Error";
                    HandleForcedLogout();
                }
                else
                {
                    Debug.LogError($"❌ Session check failed: {www.error}");
                    //HandleForcedLogout();
                    // Don't logout on network errors
                }
            }
        }
    }

    public void ForceLogout()
    {
        HandleForcedLogout();
    }
    public void ForceLogoutDueToInactivity()
    {
        _massage = "You were signed out due to inactivity.";
        HandleForcedLogout();
    }

    public void ChangePasswordLogout()
    {
        _massage = "Sign in again with your new password.";
        HandleForcedLogout();
    }

    private void HandleForcedLogout()
    {
        Debug.Log("�� Logging out due to session conflict...");
        StopSessionWatch();

        if (sessionEndPanel == null)
        {
            if (SessionLogoutPanelPrefab != null)
                sessionEndPanel = Instantiate(SessionLogoutPanelPrefab);
            else
                sessionEndPanel = Instantiate(Resources.Load<GameObject>("SessionLogoutPopup"));
            //GameObject logoutPopupPrefab = Resources.Load<GameObject>("SessionLogoutPopup");
            //if (logoutPopupPrefab != null)
            //{
            //    sessionEndPanel = Instantiate(logoutPopupPrefab);
            //}

            if (Screen.orientation == ScreenOrientation.Portrait)
            {
                sessionEndPanel.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(700, 455);
                MainMenuUIManager.Instance.ShowPopup(sessionEndPanel);
            }
            else
            {
                MainMenuUIManager.Instance.ShowPopup(sessionEndPanel);
            }
            confirmBtm = sessionEndPanel.transform.GetChild(0).GetChild(2).GetComponent<Button>();
            AddListenee();
            if (!string.IsNullOrEmpty(_massage))
            {
                sessionEndPanel.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = _massage;
            }
        }

        OnForcedLogout?.Invoke();
    }

    void LoadLoginScene()
    {
        Time.timeScale = 1;

        if (ClickSessionManager.Instance != null)
            ClickSessionManager.Instance.StopInactivityTracking();

        //CasinoUIManager.Instance.ShowErrorCanvas(1, _massage);
        UserManager.Instance.isLoginProcess = false;
        ApiEndpoints.AuthToken = null;
        sessionEndPanel = null;
        PlayerPrefs.DeleteKey("userId");
        PlayerPrefs.DeleteKey("profileImageUrl");
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        SceneManagement.ResetSceneManagementData();

        if (WebSocketManager.Instance != null)
            Destroy(WebSocketManager.Instance.gameObject);

        if (InternetWatchdog.Instance != null)
            Destroy(InternetWatchdog.Instance.gameObject);

        SceneManager.LoadScene("Login");

        if (Instance != null)
            Destroy(Instance.gameObject);
    }
}

[System.Serializable]
public class SessionCheckRequest
{
    public string userId;
    public string sessionId;
}

[System.Serializable]
public class SessionCheckResponse
{
    public bool valid;
    public string message;
}