using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Sound Buttons")]
    public GameObject musicOnButton;
    public GameObject musicOffButton;

    public GameObject soundOnButton;
    public GameObject soundOffButton;

    public GameObject shockOnButton;
    public GameObject shockOffButton;


    [Header("Logout")]
    private GameObject logoutPopup;
    [SerializeField] private GameObject logoutPopupPrefab;
    public Button logoutButton;
    //public GameObject logoutPanel;
    private Button confirmLogoutButton;
    private Button cancelLogoutButton;

    [Header("Settings Panel")]
    public GameObject settingsPanel;


    private bool isLoggingOut = false;


#if UNITY_EDITOR
    private const string LogoutFlag = "ForceLogoutInProgress";

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChange;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChange;
    }

    private void OnPlayModeChange(PlayModeStateChange state)
    {
        // Trigger logout ONLY when user presses STOP in Unity
        if (state == PlayModeStateChange.ExitingPlayMode && !isLoggingOut)
        {
            Debug.Log("🛑 Unity Stop pressed — Logging out before exit...");
            isLoggingOut = true;

            // Tell other editor scripts (like PlayFromFirstScene) not to run
            EditorPrefs.SetBool(LogoutFlag, true);

            // Prevent play mode from ending until logout is complete
            EditorApplication.isPlaying = true;

            // Run your logout routine
            StartCoroutine(LogoutAndExit());
        }
    }

    private IEnumerator LogoutAndExit()
    {
        yield return StartCoroutine(ConfirmLogout());

        Debug.Log("✔ Logout completed — stopping play mode.");

        // cleanup flag
        EditorPrefs.SetBool(LogoutFlag, false);

        // now safely stop Unity
        EditorApplication.isPlaying = false;
    }
#endif



    private void Start()
    {
        SetupSoundButtons();
        SetupLogoutPanel();
    }

    private void SetupSoundButtons()
    {
        musicOnButton.GetComponent<Button>().onClick.AddListener(() => ToggleSoundOption("Music", false));
        musicOffButton.GetComponent<Button>().onClick.AddListener(() => ToggleSoundOption("Music", true));

        soundOnButton.GetComponent<Button>().onClick.AddListener(() => ToggleSoundOption("Sound", false));
        soundOffButton.GetComponent<Button>().onClick.AddListener(() => ToggleSoundOption("Sound", true));

        shockOnButton.GetComponent<Button>().onClick.AddListener(() => ToggleSoundOption("Shock", false));
        shockOffButton.GetComponent<Button>().onClick.AddListener(() => ToggleSoundOption("Shock", true));
    }

    private void ToggleSoundOption(string option, bool enable)
    {
        switch (option)
        {
            case "Music":
                musicOnButton.SetActive(enable);
                musicOffButton.SetActive(!enable);
                if (enable)
                {
                    GlobleSoundManager.Instance.PlayMusic("MainMenuMusic");
                }
                else
                {
                    GlobleSoundManager.Instance.StopMusic("MainMenuMusic");
                }
                break;
            case "Sound":
                soundOnButton.SetActive(enable);
                soundOffButton.SetActive(!enable);
                if (enable)
                {
                    GlobleSoundManager.Instance.MuteSFX(false);

                }
                else
                {
                    GlobleSoundManager.Instance.MuteSFX(true);
                }
                break;
            case "Shock":
                shockOnButton.SetActive(enable);
                shockOffButton.SetActive(!enable);
                break;
        }

        //// Save state to PlayerPrefs if needed
        //PlayerPrefs.SetInt(option, enable ? 1 : 0);
    }

    private void SetupLogoutPanel()
    {
        //logoutButton.onClick.AddListener(() => logoutPanel.SetActive(true));
        logoutButton.onClick.AddListener(LogoutPopup);
        //confirmLogoutButton.onClick.AddListener(Logout);
        //cancelLogoutButton.onClick.AddListener(() => logoutPanel.SetActive(false));
    }

    void Logout()
    {
        if (ClickSessionManager.Instance != null)
            ClickSessionManager.Instance.StopInactivityTracking();
        StartCoroutine(ConfirmLogout());
    }

    private IEnumerator ConfirmLogout()
    {
        CasinoUIManager.Instance.ShowErrorCanvas(0, "");

        // Prepare logout request
        using (UnityWebRequest www = new UnityWebRequest(ApiEndpoints.Logout, "POST"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();
            if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, ApiEndpoints.Logout, "", "POST", () => ConfirmLogout());
                yield break;
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Logout failed: " + www.error);
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
                yield break;
            }

            //Debug.Log("User successfully logged out from server.");
            LogoutSuccess();
        }


    }


    void LogoutSuccess()
    {

        // Now clear local session after successful logout
        UnitySessionManager.Instance.StopSessionWatch();
        UserManager.Instance.isLoginProcess = false;
        UserManager.Instance.ClearSession();
        SceneManagement.ResetSceneManagementData();
        //logoutPanel.SetActive(false);
        settingsPanel.SetActive(false);

        Destroy(logoutPopup);


        #region For WebGL Mobile Browser

        JSInputHandler.OnLogoutSuccess();
        #endregion
        CasinoUIManager.Instance.ShowErrorCanvas(2, "");
        SceneManager.LoadScene("Login");
        //Debug.Log("User logged out.");
    }



    private void LogoutPopup()
    {
        logoutPopup = Instantiate(logoutPopupPrefab, MainMenuUIManager.Instance.PopupParent);
        var popup = logoutPopup.transform.GetChild(0).gameObject;
        cancelLogoutButton = popup.transform.GetChild(2).GetChild(0).GetComponent<Button>();
        confirmLogoutButton = popup.transform.GetChild(2).GetChild(1).GetComponent<Button>();

        cancelLogoutButton.onClick.AddListener(() => Destroy(logoutPopup));
        confirmLogoutButton.onClick.AddListener(Logout);

        MainMenuUIManager.Instance.DoTweenAnim(MainMenuUIManager.TweenType.Panel, popup, 1f, 0.3f);
    }



}
