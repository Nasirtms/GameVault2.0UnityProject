using DG.Tweening;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Text Fields")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    //[SerializeField] private Button forgotPasswordButton;

    [Header("Toggle")]
    [SerializeField] private Toggle rememberToggle;

    [Header("UI References")]
    [SerializeField] private Transform mainCanvas;
    [SerializeField] private GameObject login;

    [Header("Logout")]
    private GameObject logoutPopup;
    [SerializeField] private GameObject logoutPopupPrefab;
    private Button confirmLogoutButton;
    private Button cancelLogoutButton;

    [Header("Settings")]
    [SerializeField] private string actualPassword = "";
    [SerializeField] private bool autofillMasked = false;

    [Header("Loading UI")]
    [SerializeField] private MainMenu.UILoadingPanel loadingPanel;
    private bool isLoadingMainMenu;

    #endregion

    #region Private Fields

    [SerializeField] private bool isPopupActive = false;
    [SerializeField] private bool isGetEventData;
    [SerializeField] private bool isGetNotificationData;
    [SerializeField] private bool isGetGamesData;
    [SerializeField] private bool isGetMainMenuData;

    private SerializableClasses.LoginResponseWrapper loginResponse;
    private SerializableClasses.SceneDataResponse sceneData;

    private enum TweenType { Panel, Login }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        DoTweenAnim(TweenType.Login, login, 1.1f, 0.3f);
        loginButton.onClick.AddListener(HandleLogin);
        emailInput.onSubmit.AddListener((str) => EmailFieldSubmit());
        passwordInput.onSubmit.AddListener((str) => HandleLogin());
        //forgotPasswordButton.onClick.AddListener(HandleForgotPassword);

        passwordInput.contentType = TMP_InputField.ContentType.Password;
        passwordInput.onSelect.AddListener(OnPasswordSelect);

        LoadSavedCredentials();

//        // Only executes when running as a WebGL build
//#if UNITY_WEBGL && !UNITY_EDITOR
//        WebGLInput.mobileKeyboardSupport = false;
//#endif
    }

    private void Update()
    {
        if (isLoadingMainMenu)
        {
            loadingPanel.SetLoadingBarValue(Mathf.Lerp(loadingPanel.GetLoadingBarValue(), MainMenuAddressableHandler.TotalProgress, Time.unscaledDeltaTime * 3));
        }

#if UNITY_ANDROID
        if (Input.GetKeyDown(KeyCode.Escape))
            LogoutPopup();
        else
            isPopupActive = false;
#endif
    }

    #endregion

    #region UI Logic

    private void EmailFieldSubmit()
    {
        StopCoroutine("EmailFieldSubmit_Coroutine");
        StartCoroutine("EmailFieldSubmit_Coroutine");
    }

    IEnumerator EmailFieldSubmit_Coroutine()
    {
        yield return new WaitForSeconds(.6f);
        passwordInput.Select();
        passwordInput.ActivateInputField();
    }


    private void HandleLogin()
    {
        if (UserManager.Instance != null)
        {
            if (UserManager.Instance.isLoginProcess) return;
        }        
        string email = emailInput.text.Trim();
        string password = autofillMasked ? actualPassword : passwordInput.text;

        if (!ValidateLoginInput(email, password))
            return;

        if (rememberToggle.isOn)
            UserPrefsManager.SaveCredentials(email, password);
        else
            UserPrefsManager.ClearCredentials();

        ButtonAnimation(loginButton.gameObject, 1f, 0.2f);
        StartCoroutine(LoginCoroutine(email, password));
    }

    //private void HandleForgotPassword()
    //{
    //    Debug.Log("Redirect to Forgot Password UI.");
    //    // TODO: Implement navigation
    //}

    private bool ValidateLoginInput(string email, string password)
    {
        if (string.IsNullOrEmpty(email))
        {
            ShowError("Please enter a valid email address.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter your password.");
            return false;
        }

        bool hasLetter = false;
        bool hasNumber = false;

        foreach (char c in password)
        {

            if (char.IsLetter(c))
            {
                hasLetter = true;
            }
            if (char.IsNumber(c))
            {
                hasNumber = true;
            }
        }

        //if (!(hasNumber && hasLetter))
        //{
        //    ShowError("Use both letters and numbers.");
        //    return false;
        //}

        return true;
    }

    private void ButtonAnimation(GameObject obj, float scale, float duration)
    {
        if (!obj) return;
        obj.transform.DOKill();

        obj.transform.localScale = Vector3.one * scale;
        obj.transform.DOScale(scale * 0.9f, duration)
            .SetEase(Ease.InQuad)
            .OnComplete(() => obj.transform.DOScale(scale, duration).SetEase(Ease.OutQuad));
    }

    public void LogoutPopup()
    {
        if (logoutPopupPrefab == null) return;

        logoutPopup = Instantiate(logoutPopupPrefab, mainCanvas);
        var popup = logoutPopup.transform.GetChild(0).gameObject;
        cancelLogoutButton = popup.transform.GetChild(1).GetChild(0).GetComponent<Button>();
        confirmLogoutButton = popup.transform.GetChild(1).GetChild(1).GetComponent<Button>();

        cancelLogoutButton.onClick.AddListener(() => Destroy(logoutPopup));
        confirmLogoutButton.onClick.AddListener(() => Application.Quit());

        DoTweenAnim(TweenType.Panel, popup, 1f, 0.3f);

        //if (!backButtonPopup) return;

        //if (!isPopupActive)
        //{
        //    backButtonPopup.SetActive(true);
        //    DoTweenAnim(TweenType.BackButton, backButtonPopup, 1f, 0.3f);
        //    isPopupActive = true;
        //}
    }

    private void DoTweenAnim(TweenType type, GameObject obj, float scale, float duration)
    {
        if (!obj) return;
        obj.transform.DOKill();

        switch (type)
        {
            case TweenType.Panel:
                obj.transform.localScale = Vector3.one * 0.5f;
                obj.transform.DOScale(scale, duration * 1.2f).SetEase(Ease.OutBack);
                break;

            case TweenType.Login:
                obj.transform.localScale = Vector3.one * 0.5f;
                DOTween.Sequence()
                    .Append(obj.transform.DOScale(1.1f, 0.6f).SetEase(Ease.OutBack))
                    .Append(obj.transform.DOScale(scale, duration).SetEase(Ease.OutBack));
                break;
        }
    }

    private void OnPasswordSelect(string _)
    {
        if (autofillMasked)
        {
            passwordInput.text = "";
            autofillMasked = false;
        }
    }

    private void LoadSavedCredentials()
    {
        if (UserPrefsManager.HasSavedCredentials())
        {
            emailInput.text = UserPrefsManager.LoadEmail();
            actualPassword = UserPrefsManager.LoadPassword();
            passwordInput.text = new string('*', 20);
            autofillMasked = true;
            rememberToggle.isOn = true;
        }
        else
        {
            rememberToggle.isOn = false;
        }
    }

    #endregion

    #region Backend Login Flow

    private IEnumerator LoginCoroutine(string email, string password)
    {
        ShowError("", 0); // Hide any visible errors

        var loginData = new SerializableClasses.LoginRequest { email = email, password = password };
        string jsonData = JsonConvert.SerializeObject(loginData);

        /*using (UnityWebRequest www = new UnityWebRequest(ApiEndpoints.Login, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (HasNetworkError(www)) yield break;

            try
            {
                loginResponse = JsonConvert.DeserializeObject<SerializableClasses.LoginResponseWrapper>(www.downloadHandler.text);
                DateTime nextUtc;
                if (DateTime.TryParse(loginResponse.user.nextSpinTime, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal, out nextUtc))
                {
                    PlayerPrefs.SetString("FreeSpinNextUtcTicks", nextUtc.Ticks.ToString());
                    PlayerPrefs.Save();
                }
                //Debug.Log($"String Time: {loginResponse.user.nextSpinTime} | Current Time Login: {DateTime.Parse(loginResponse.user.nextSpinTime)} | Adjust Time: {nextUtc}");
                //Debug.Log("Login Text: " + www.downloadHandler.text);
                //Debug.Log("Login Response (parsed):\n" + JsonConvert.SerializeObject(loginResponse, Formatting.Indented));
                if (loginResponse?.user == null || string.IsNullOrEmpty(loginResponse.user.id))
                {
                    ShowError("Invalid response format");
                    yield break;
                }
                if (!loginResponse.user.isActive)
                {
                    ShowError("User is not active");
                    yield break;
                }
                JSInputHandler.OnLoginSuccess(ApiEndpoints.AuthToken);
                loginResponse.user.EnsureDefaults();
                ApiEndpoints.AuthToken = loginResponse.user.token;
                ApiEndpoints.RefreshToken = loginResponse.user.refreshToken;
                PlayerPrefs.SetString("userId", loginResponse.user.id);
                PlayerPrefs.SetString("profileImageUrl", loginResponse.user.avatarUrl);
                UserManager.Instance.isLoginProcess = true;
                SceneManagement.profile_iconUrls.AddRange(loginResponse.available_avatars.Select(a => a.image_url));
                ApiEndpoints.updateTokenIntoJSFile();
                LoginSuccess();
                StartCoroutine(GetMainMenuData());
            }
            catch (Exception ex)
            {
                ShowError("Network Error");
                JSInputHandler.OnLoginField();
                Debug.LogError($"Login parse error: {ex.Message}");
            }
        }*/

        using (UnityWebRequest www = new UnityWebRequest(ApiEndpoints.Login, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData));
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (HasNetworkError(www)) yield break;

            try
            {
                string responseText = www.downloadHandler.text;
                Debug.Log("responseText: " + responseText);
                // Check for region-block response (200 OK with only "message", no "user")
                var regionBlock = JsonConvert.DeserializeObject<RegionBlockResponse>(responseText);
                if (regionBlock != null && !string.IsNullOrEmpty(regionBlock.message) &&
                    (responseText.Contains("user") == false || responseText.IndexOf("\"user\"") < 0))
                {
                    ShowError(regionBlock.message);
                    yield break;
                }

                loginResponse = JsonConvert.DeserializeObject<SerializableClasses.LoginResponseWrapper>(responseText);
                DateTime nextUtc;
                if (DateTime.TryParse(loginResponse.nextSpinTime, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal, out nextUtc))
                {
                    PlayerPrefs.SetString("FreeSpinNextUtcTicks", nextUtc.Ticks.ToString());
                    PlayerPrefs.Save();
                }
                if (loginResponse?.user == null || string.IsNullOrEmpty(loginResponse.user.id))
                {
                    ShowError("Invalid response format");
                    yield break;
                }
                if (!loginResponse.user.isActive)
                {
                    ShowError("User is not active");
                    yield break;
                }
                JSInputHandler.OnLoginSuccess(ApiEndpoints.AuthToken);
                //loginResponse.user.EnsureDefaults();
                ApiEndpoints.AuthToken = loginResponse.token;
                ApiEndpoints.RefreshToken = loginResponse.refreshToken;
                PlayerPrefs.SetString("userId", loginResponse.user.id);
                PlayerPrefs.SetString("profileImageUrl", loginResponse.user.avatarUrl);
                UserManager.Instance.isLoginProcess = true;
                //SceneManagement.profile_iconUrls.AddRange(loginResponse.available_avatars.Select(a => a.image_url));
                ApiEndpoints.updateTokenIntoJSFile();
                LoginSuccess();
                StartCoroutine(GetMainMenuData());
                UserSessionSocketManager.GetOrFind();
                UserSessionSocketManager.Instance.ReConnectUserSessionSocket();
            }
            catch (Exception ex)
            {
                ShowError("Network Error");
                JSInputHandler.OnLoginField();
                Debug.LogError($"Login parse error: {ex.Message}");
            }
        }
    }



    private IEnumerator GetMainMenuData()
    {
        //StartCoroutine(GetGamesData());
        yield return SendAuthorizedRequest(ApiEndpoints.SceneData, json =>
        {
            Debug.Log($"SceneData Response: {json}");
            sceneData = JsonConvert.DeserializeObject<SerializableClasses.SceneDataResponse>(json);
            Debug.Log($"SceneData: {JsonConvert.SerializeObject(sceneData)}");

            isGetMainMenuData = true;
            StartCoroutine(GetGamesData());
        });
    }

    private IEnumerator GetGamesData()
    {
        yield return SendAuthorizedRequest(ApiEndpoints.GetAllGames, json =>
        {
            SceneManagement.games = JsonConvert.DeserializeObject<List<SerializableClasses.Game>>(json);
            isGetGamesData = true;
            StartCoroutine(GetEventData());
            GetLoginNotificationData();
        });
    }

    private IEnumerator GetEventData()
    {
        ApplySceneData();
        yield return null;
        //yield return SendAuthorizedRequest(ApiEndpoints.ActiveEvents, json =>
        //{
        //    var eventsList = JsonConvert.DeserializeObject<List<SerializableClasses.Events>>(json);

        //    if (eventsList != null && eventsList.Count > 0)
        //    {
        //        Debug.Log("Event Data Count: " + eventsList.Count);

        //        // Store the full list
        //        SceneManagement.events = eventsList;

        //        // Take the first event
        //        var firstEvent = eventsList[0];

        //        // ✅ Assign values into your static fields
        //        SceneManagement.EventshowOnce = firstEvent.showOnce;
        //        SceneManagement.eventIsActive = firstEvent.isActive;

        //        // ended = check if event end time is in the past
        //        DateTime endTime;
        //        if (DateTime.TryParse(firstEvent.endTime, out endTime))
        //            SceneManagement.eventIsEnded = DateTime.UtcNow > endTime;
        //        else
        //            SceneManagement.eventIsEnded = false;

        //        SceneManagement.EventtargetUser = firstEvent.targetType;
        //        SceneManagement.eventHeading = firstEvent.title;
        //        SceneManagement.evntBottom = firstEvent.eventBottom;
        //        SceneManagement.evntMassage = firstEvent.description;

        //        isGetEventData = true;
                
        //    }
        //    else
        //    {
        //        Debug.LogWarning("No event data found.");
        //        isGetEventData = false;
        //    }
        //    ApplySceneData();
        //});
    }



    private void ApplySceneData()
    {
        SceneManagement.isShowSpinWheel = sceneData.isSpinWheelEnabled;
        SceneManagement.isShowEventPopup = sceneData.showEventsPopup;
        SceneManagement.isShowNotificationAfterLogin = sceneData.showNotificationAfterLogin;
        SceneManagement.DailyRewardValue = sceneData.rewardCoin;
        LoadMainScene();
    }

    public void OpenKeypadInMobileBrowser()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    Application.ExternalCall("showMobileKeypad");
#endif
    }


    private void LoginSuccess()
    {
        UserManager.Instance?.SetUserData(
            loginResponse.user.id,
            loginResponse.user.username,
            loginResponse.user.email,
            loginResponse.user.coinBalance,
            loginResponse.user.avatarUrl,
            loginResponse.user.userGameId,
            loginResponse.user.avatarIndex,
            loginResponse.user.role,
            loginResponse.user.sessionId,
            loginResponse.user.bio,
            loginResponse.user.isFeedback,
            loginResponse.user.hasSetAvatarOnce
        );
        //InitializeSessionManagement();
    }

    void LoadMainScene()
    {
        CasinoUIManager.Instance.ShowErrorCanvas(2, "");
        Invoke(nameof(LoadScene), 0.3f);
        loadingPanel.OpenPanel(0.3f);
        isLoadingMainMenu = true;
    }

    private void LoadScene()
    {
        UserManager.Instance?.StartUpdateCanAddCoin(true);
        MainMenuAddressableHandler.LoadMainMenu();
        //SceneManager.LoadScene("Main");
    }

    #endregion

    #region Login Notifications

    public void GetLoginNotificationData()
    {
        StartCoroutine(SendAuthorizedRequest(ApiEndpoints.ActiveLoginNotifications, json =>
        {
            var response = JsonConvert.DeserializeObject<SerializableClasses.LoginNotificationResponse>(json);
            if (response?.success == true && response.data != null)
            {
                SceneManagement.LatestNotifications = new List<SerializableClasses.LoginNotification>(response.data);
                isGetNotificationData = SceneManagement.LatestNotifications.Count > 0;
            }
        }));
    }

    #endregion

    #region Utilities

    private bool HasNetworkError(UnityWebRequest www)
    {
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            ShowError("Network Error");
            Debug.LogError(www.error);
            return true;
        }
        else if(www.result == UnityWebRequest.Result.ProtocolError)
        {
            switch (www.responseCode)
            {
                case 400:
                    ShowError("Invalid login request.");
                    break;
                case 401:
                    ShowError("Incorrect Username or Password.");
                    break;
                case 500:
                    ShowError("Server Error. Please try again later.");
                    break;
                case 503:
                    ShowError("Server Error. Please try again later.");
                    break;
                default:
                    ShowError("Network Error.");
                    break;
            }
            //ShowError("Incorrect Password");
            Debug.LogError(www.error);
            return true;
        }
        return false;
    }

    private IEnumerator SendAuthorizedRequest(string url, Action<string> onSuccess)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(ApiEndpoints.AuthToken))
                www.SetRequestHeader("Authorization", $"Bearer {ApiEndpoints.AuthToken}");

            yield return www.SendWebRequest();

            if (HasNetworkError(www)) yield break;

            try
            {
                onSuccess?.Invoke(www.downloadHandler.text);
            }
            catch (Exception ex)
            {
                ShowError("Data parse error");
                Debug.LogError($"Parsing error: {ex.Message}");
            }
        }
    }

    private void ShowError(string message, int code = 1)
    {
        CasinoUIManager.Instance?.ShowErrorCanvas(code, message);
    }

    private void InitializeSessionManagement()
    {
        UnitySessionManager.Instance?.StartSessionWatch();
    }

    #endregion
}


[System.Serializable]
public class RegionBlockResponse
{
    public string message;
}