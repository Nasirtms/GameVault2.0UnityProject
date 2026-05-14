using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class FishKilledData
{
    public string fishName;
    public float lastBetAmount;

    public FishKilledData(string fishName, float lastBetAmount)
    {
        this.fishName = fishName;
        this.lastBetAmount = lastBetAmount;
    }
}

[System.Serializable]
public class MermaidRequestBody
{
    public string requestId;
    public string gameId;
    public int fishCount;
    public float betAmount;
    public float winAmount;
    public float currentCoinsAmount;
    public bool isBonusRound;
    public List<string> bonusFishesName;
    public Dictionary<string, float> fishKilledList;
    //public List<FishKilledData> fishKilledList;
}
[System.Serializable]
public class SpawnedFishesListToServer_RequestBody
{
    [System.Serializable]
    public class FishGroup
    {
        public string fishName;
        public List<string> fishIds = new List<string>();
    }

    /// <summary>Optional; no business logic depends on it.</summary>
    public string requestId;
    public string gameId;
    public List<FishGroup> fishGroups = new List<FishGroup>();

    public SpawnedFishesListToServer_RequestBody()
    {
        requestId = Guid.NewGuid().ToString();
        gameId = SceneManagement.currentGameID;
    }
}


public class MermaidAPIManager : MonoBehaviour
{
    [Header("API Configuration")]
    private string baseUrl = "http://localhost:5036";
    public string apiControllerUrl = "/api/mermaid/";
    public string BaseUrl => baseUrl + apiControllerUrl;
    public string jwtToken;
    public string gameId = ""; // Set your Mermaid game GUID here

    [Header("Debug Options")]
    public bool showLogs = true;

    [Header("Spawn Profile Data (Inspector View)")]
    [SerializeField] public List<FishProfile> fishProfilesInInspector = new();

    // Singleton instance
    public static MermaidAPIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        baseUrl = ApiEndpoints.baseUrl;
    }

    public IEnumerator GetSpawnProfile(int fishcount,float betAmount,float winAmount,Action<FishProfile[], double> onSuccess, Action<string> onError, bool isBonus = false,List<string> bonusFishesName = null)
    {
        //string url = $"{baseUrl}/api/mermaid/spawn-profile";
        string url = $"{BaseUrl}spawn-profile";
        //string url = $"{baseUrl}/api/{SceneManagement.currentGameName.ToLower().Replace(" ", "")}/spawn-profile";

        jwtToken = ApiEndpoints.AuthToken;
        var requestData = new MermaidRequestBody
        {
            requestId = Guid.NewGuid().ToString(),
            gameId = SceneManagement.currentGameID,
            fishCount = fishcount,
            //betAmount = betAmount,
            //winAmount = winAmount,
            betAmount = Manager.totalBetAmountPerInterval,
            winAmount = Manager.totalFishWinAmountPerInterval,
            currentCoinsAmount = Manager.Instance.balance,
            isBonusRound = isBonus,
            bonusFishesName = bonusFishesName,
            fishKilledList = new Dictionary<string, float>(Manager.fishKilledListPerInterval)

        };

        if (!isBonus)
        {
            Manager.totalBetAmountPerInterval = 0;
            Manager.totalFishWinAmountPerInterval = 0;
            Manager.fishKilledListPerInterval.Clear();
        }

        string body = JsonUtility.ToJson(requestData);
        Debug.Log($"spawn-profile: activeFishCount: {FishManager.Instance.ActiveFishCount} ___ coins: {Manager.Instance.balance} ___ body: {body}");
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

        if (showLogs)
        {
            Debug.Log($"Mermaid Game Request Body : {body}");
            Debug.Log($"🐟 Requesting spawn profile from: {url}");
        }

        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            // 1. Pass the coroutine function reference as a new argument
            yield return ApiEndpoints.CheckApiResponse(
                request,
                url,
                body,
                "POST",
                () => GetSpawnProfile(fishcount, betAmount, winAmount, onSuccess, onError, isBonus, bonusFishesName) // <-- The function to be saved
            );

            //if (InternetWatchdog.Instance != null)
            //    InternetWatchdog.Instance.ForceShowOfflinePopup();

            yield break; // Stops execution here
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            if (showLogs)
            {
                Debug.Log($"✅ Spawn Profile Response - Request Body: coins: {Manager.Instance.balance} ___ {System.Text.Encoding.UTF8.GetString(request.uploadHandler.data)}");
                Debug.Log($"✅ Spawn Profile Response: coins: {Manager.Instance.balance} ___ {json}");
            }

            try
            {
                SpawnProfileResponse response = JsonUtility.FromJson<SpawnProfileResponse>(json);

                if (response.success && response.fishProfiles != null)
                {
                    // ✅ Save to inspector-visible list
                    fishProfilesInInspector = new List<FishProfile>(response.fishProfiles);

                    if (showLogs)
                        Debug.Log($"✅ Loaded {fishProfilesInInspector.Count} fish profiles into inspector");

                    onSuccess?.Invoke(response.fishProfiles, response.targetRtp);
                }
                else
                {
                    string errorMsg = "Invalid response from server";
                    Debug.LogError($"❌ {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to parse response: {e.Message}");
                onError?.Invoke(e.Message);
            }
        }
        else
        {
            string errorMsg = $"Request failed: {request.error}";
            Debug.LogError($"❌ {errorMsg}");
            onError?.Invoke(errorMsg);

            //if (InternetWatchdog.Instance != null)
            //    InternetWatchdog.Instance.ForceShowOfflinePopup();
        }
    }

    public IEnumerator SendSpawnedFishesListToServer(List<SpawnedFishesListToServer_RequestBody.FishGroup> spawnedFishGroups)
    {
        string url = $"{BaseUrl}register-spawn-batch";

        jwtToken = ApiEndpoints.AuthToken;

        var requestData = new SpawnedFishesListToServer_RequestBody()
        {
            fishGroups = spawnedFishGroups.ToList()
        };

        string body = JsonUtility.ToJson(requestData);
        Debug.Log("SendSpawnedFishesListToServer: body: " + body);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            // 1. Pass the coroutine function reference as a new argument
            yield return ApiEndpoints.CheckApiResponse(
                request,
                url,
                body,
                "POST",
                () => SendSpawnedFishesListToServer(spawnedFishGroups) // <-- The function to be saved
            );
            yield break; // Stops execution here
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;

            Debug.Log("SendSpawnedFishesListToServer: Success: " + json);
        }
        else
        {
            string errorMsg = $"Request failed: {request.error}";
            Debug.LogError($"❌ {errorMsg}");
        }
    }

    /// <summary>
    /// Send fish catch result to backend and update balance
    /// </summary>
    public IEnumerator CatchFish(string fishName, float betAmount, float payout, bool win,
        Action<CatchFishResponse> onSuccess, Action<string> onError)
    {
        string url = $"{BaseUrl}/api/mermaid/catch-fish";
        //string url = $"{baseUrl}/api/{SceneManagement.currentGameName.ToLower().Replace(" ", "")}/catch-fish";

        CatchFishRequest catchRequest = new CatchFishRequest
        {
            requestId = Guid.NewGuid().ToString(),
            gameId = gameId,
            fishName = fishName,
            betAmount = betAmount,
            payout = payout,
            win = win
        };

        string json = JsonUtility.ToJson(catchRequest);

        if (showLogs)
            Debug.Log($"🎣 Sending catch request: {json}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            // 1. Pass the coroutine function reference as a new argument
            yield return ApiEndpoints.CheckApiResponse(
                request,
                url,
                json,
                "POST",
                () => CatchFish(fishName, betAmount, payout, win, onSuccess, onError) // <-- The function to be saved
            );
            yield break; // Stops execution here
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;

            if (showLogs)
                Debug.Log($"✅ Catch Response: {responseJson}");

            try
            {
                CatchFishResponse response = JsonUtility.FromJson<CatchFishResponse>(responseJson);

                if (response.success)
                {
                    if (showLogs)
                        Debug.Log($"✅ Caught {response.fishName}! New Balance: {response.newBalance}, RTP: {response.currentRtp:P2}");

                    onSuccess?.Invoke(response);
                }
                else
                {
                    Debug.LogError("❌ Catch failed on server");
                    onError?.Invoke("Server returned failure");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to parse catch response: {e.Message}");
                onError?.Invoke(e.Message);
            }
        }
        else
        {
            string errorMsg = $"Catch request failed: {request.error}";
            Debug.LogError($"❌ {errorMsg}");
            onError?.Invoke(errorMsg);
        }
    }
}

// ========================================
// DATA MODELS
// ========================================

[Serializable]
public class SpawnProfileResponse
{
    public bool success;
    public string userId;
    public FishProfile[] fishProfiles;
    public double targetRtp;
    public string timestamp;
}

[Serializable]
public class FishProfile
{
    public string fishId;
    public List<string> fishIds = new List<string>();
    public string fishName;
    public int spawnWeight;
    public int adjustedHealth;
    public float adjustedPrize;
    public string rarity;
    public int batchSize;
    public int fishMultiplyer;
    public float damageMultiplier = 1.0f; // Backend-controlled damage scaling
}

[Serializable]
public class CatchFishRequest
{
    public string requestId;
    public string gameId;
    public string fishName;
    public float betAmount;
    public float payout;
    public bool win;
}

[Serializable]
public class CatchFishResponse
{
    public bool success;
    public string requestId;
    public string fishName;
    public bool won;
    public float payout;
    public float balanceChange;
    public float newBalance;
    public double currentRtp;
    public string timestamp;
}
