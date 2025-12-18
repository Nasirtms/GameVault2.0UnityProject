using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static SerializableClasses;
using Debug = UnityEngine.Debug;

public static class ApiEndpoints
{
    //private const string BaseUrl = "http://192.168.2.138:5036";
    private static string BaseUrl = "";
    public static string baseUrl = "";
    private const string ApiPrefix = "/api";

    //Game Name 
    public static string slotGameName = string.Empty;

    public static void UpdataBaseUrl(string _baseUrl)
    {
        BaseUrl = _baseUrl;
        baseUrl = _baseUrl;
        //Debug.Log("Update Base URL : " + BaseUrl);
    }

    // ==============
    // Auth Token
    // ==============
    public static string AuthToken = string.Empty;
    public static string RefreshToken = string.Empty;

    #region Main Menu And User Profile
    public static Dictionary<string, string> GetAuthHeaders()
    {
        return new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {AuthToken}" },
            { "Content-Type", "application/json" }
        };
    }

    // ==============
    // Authentication
    // ==============
    public static string Register => $"{BaseUrl}{ApiPrefix}/auth/register";       // POST
    public static string Login => $"{BaseUrl}{ApiPrefix}/auth/login";             // POST
    public static string Logout => $"{BaseUrl}{ApiPrefix}/auth/logout";           // POST
    public static string RefreshTokenUrl => $"{BaseUrl}{ApiPrefix}/auth/refresh"; // POST

    public static string changePawword => $"{BaseUrl}{ApiPrefix}/users/change-password";

    // ==============
    // User Management
    // ==============
    public static string UserProfile => $"{BaseUrl}{ApiPrefix}/users/profile";    // GET

    public static string canaddcoin => $"{BaseUrl}{ApiPrefix}/users/update-canaddcoin";    // GET
    public static string UpdateUserProfile => $"{BaseUrl}{ApiPrefix}/users/profile"; // PUT
    public static string UpdateUserProfileImage => $"{BaseUrl}{ApiPrefix}/users/profile"; // PUT
    public static string CheckSession => $"{BaseUrl}{ApiPrefix}/users/check-session"; // PUT


    // ==============
    // Game Data
    // ==============
    public static string SpinResult => $"{BaseUrl}{ApiPrefix}/users/profile";    // GET


    // ==============
    // Game Data
    // ==============
    public static string GetAllGames => $"{BaseUrl}{ApiPrefix}/games";            // GET
    public static string GetGameDetails(string gameId) => $"{BaseUrl}{ApiPrefix}/games/{gameId}"; // GET
    public static string AddGameIntoFavorites => $"{BaseUrl}{ApiPrefix}/favorites/add";    // POST
    public static string RemoveGameFromFavorites => $"{BaseUrl}{ApiPrefix}/favorites/remove"; // POST

    // ==============
    // Configuration        
    // ==============
    public static string SceneData => $"{BaseUrl}{ApiPrefix}/config/main-menu";   // GET
    public static string GameSettings => $"{BaseUrl}{ApiPrefix}/config/settings"; // GET

    // ==============
    // Leaderboard
    // ==============
    public static string weeklyLeaderboard => $"{BaseUrl}{ApiPrefix}/users/weekly-leaderboard"; // GET
    public static string GlobalLeaderboard => $"{BaseUrl}{ApiPrefix}/leaderboard"; // GET
    public static string FriendsLeaderboard => $"{BaseUrl}{ApiPrefix}/leaderboard/friends"; // GET
    public static string GetLeaderboard(string boardId) => $"{BaseUrl}{ApiPrefix}/leaderboard/{boardId}"; // GET
    public static string GameLeaderboard(string gameId) => $"{BaseUrl}{ApiPrefix}/leaderboard/game/{gameId}"; // GET

    // ==============
    // Transactions
    // ==============
    public static string AllTransactions => $"{BaseUrl}{ApiPrefix}/transactions"; // GET
    public static string GetTransaction(string id) => $"{BaseUrl}{ApiPrefix}/transactions/{id}"; // GET

    // ==============
    // Game Progress
    // ==============
    public static string GetGameProgress(string gameId) => $"{BaseUrl}{ApiPrefix}/gameprogress/{gameId}"; // GET
    public static string UpdateGameProgress(string gameId) => $"{BaseUrl}{ApiPrefix}/gameprogress/{gameId}"; // PUT

    // ==============
    // Notifications
    // ==============
    public static string AllNotifications => $"{BaseUrl}{ApiPrefix}/notifications"; // GET
    public static string MarkNotificationAsRead(string id) => $"{BaseUrl}{ApiPrefix}/notifications/{id}/read"; // PUT

    //Login Notification 
    public static string ActiveLoginNotifications => $"{BaseUrl}{ApiPrefix}/login-notifications"; // PUT


    // ==============
    // Daily Rewards
    // ==============
    public static string ClaimDailyReward => $"{BaseUrl}{ApiPrefix}/daily-rewards/claim"; // POST

    // ==============
    // Connectivity
    // ==============
    public static string Ping => $"{BaseUrl}{ApiPrefix}/ping"; // GET

    // ==============
    // Achievements
    // ==============
    public static string AllAchievements => $"{BaseUrl}{ApiPrefix}/achievements"; // GET
    public static string UserAchievements => $"{BaseUrl}{ApiPrefix}/achievements/user"; // GET

    // ==============
    // Spin Wheel
    // ==============
    public static string SpinHistory => $"{BaseUrl}{ApiPrefix}/spinwheel/history"; // GET
    public static string Spin => $"{BaseUrl}{ApiPrefix}/spinwheel/spin";           // POST
    public static string SpinStatus => $"{BaseUrl}{ApiPrefix}/spinwheel/status";   // GET

    // ==============
    // Tournaments
    // ==============
    public static string AllTournaments => $"{BaseUrl}{ApiPrefix}/tournaments";    // GET
    public static string JoinTournament(string id) => $"{BaseUrl}{ApiPrefix}/tournaments/{id}/join"; // POST
    public static string LeaveTournament(string id) => $"{BaseUrl}{ApiPrefix}/tournaments/{id}/leave"; // POST
    public static string GetTournament(string id) => $"{BaseUrl}{ApiPrefix}/tournaments/{id}"; // GET


    // ==============
    // Events
    // ==============
    public static string AllEvents => $"{BaseUrl}{ApiPrefix}/event";                          // GET
    public static string ActiveEvents => $"{BaseUrl}{ApiPrefix}/event/active";                // GET
    public static string GetEventById(string eventId) => $"{BaseUrl}{ApiPrefix}/event/{eventId}"; // GET

    #endregion
    // Golden Dragon Mini Game
    public static string GoldenDragonMiniGameCoinUpdate => $"{BaseUrl}{ApiPrefix}/slot/{slotGameName}/CoinUpdateForMiniGame";


    public static string slotGameSpin => $"{BaseUrl}{ApiPrefix}/slot/{slotGameName}/spin";   //POST
    public static string slot2FreeSpin => $"{BaseUrl}{ApiPrefix}/slot/{slotGameName}/test-freespin?wildColumns=2"; //POST
    public static string slot3FreeSpin => $"{BaseUrl}{ApiPrefix}/slot/{slotGameName}/test-freespin?wildColumns=3";   //POST
    public static string slot4FreeSpin => $"{BaseUrl}{ApiPrefix}/slot/{slotGameName}/test-freespin?wildColumns=4";   //POST


    //Sahara Riches Test
    public static string saharaRichesTest => $"{BaseUrl}{ApiPrefix}/slot/starburstslots/spin";


    //Keno Games 
    public static string superBallKeno => $"{BaseUrl}{ApiPrefix}/superballkeno/spin";
    public static string hexagonekeno => $"{BaseUrl}{ApiPrefix}/hexagonekeno/spin";
    public static string octagonekeno => $"{BaseUrl}{ApiPrefix}/octagonekeno/spin";
    public static string rockPaperScissors => $"{BaseUrl}{ApiPrefix}/rps/play";

    // HeadsNTails
    public static string HeadsNTails => $"{BaseUrl}{ApiPrefix}/headntails/play";


    #region Refresh Token Code

    public static List<Func<IEnumerator>> PendingCoroutines = new List<Func<IEnumerator>>();
    private static bool _isRetrying = false;
    public static IEnumerator CheckApiResponse(UnityWebRequest request, string url, string jsonBody, string method, Func<IEnumerator> coroutineToRetry)
    {
        if (request.responseCode == 401)
        {
            Debug.LogWarning($"⚠️refreshed Unauthorized. Saving coroutine to retry: {method} {url}");

            // Add the function to the list
            PendingCoroutines.Add(coroutineToRetry);

            // Refresh token
            yield return RefreshTokenMethod();
        }
        else
        {
            yield break;
        }
    }

    public static IEnumerator RefreshTokenMethod()
    {
        if (_isRetrying)
            yield break;

        _isRetrying = true;

        string url = RefreshTokenUrl;

        var refreshReq = new RefreshTokenRequest
        {
            RefreshToken = RefreshToken
        };

        var bodyJson = JsonUtility.ToJson(refreshReq);
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("🔑 Token refreshed! Retrying ALL pending coroutines...");

                var response = JsonUtility.FromJson<RefreshTokenResponse>(request.downloadHandler.text);
                AuthToken = response.accessToken;
                RefreshToken = response.refreshToken;

                var coroutinesToRun = PendingCoroutines.ToList();
                PendingCoroutines.Clear();
                updateTokenIntoJSFile();
                foreach (var coroutineFunc in coroutinesToRun)
                {
                    Debug.Log($"🔁refreshed Re-running saved coroutine: {coroutineFunc.Method.Name}");
                    yield return coroutineFunc.Invoke();
                }
            }
            else
            {
                Debug.LogError($"❌refreshed Token refresh failed: {request.error}");
            }

            _isRetrying = false;
        }
    }

    #endregion

    public static void updateTokenIntoJSFile()
    {
        string _token = AuthToken;
        string url = baseUrl;
        JSInputHandler.SendTokenToJS(_token, url);
    }

}

