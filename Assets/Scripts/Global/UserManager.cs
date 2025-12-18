using Supabase.Gotrue;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static SerializableClasses;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get;  set; }
    public bool isLoginProcess = false;
    public bool IsLoggedIn { get; private set; } = false;
    public string _userGameId { get; private set; }
    [SerializeField]
    private string userId;

    public string UserId
    {
        get => userId;
        private set => userId = value;
    }

    public string sessionId;
    public string Username { get; private set; }
    public int avatarIndex { get;  set; }
    public string Email { get; private set; }
    public float Coins { get;  set; }
    public string AvatarUrl { get; set; }

    public string userType;
    public Sprite AvatarImage { get; set; }

    public float currentBetAmount;

    private bool updatingPreviousCall = false;
    public event Action UpdateGameCoins;

    public string Bio { get; set; }

    [Header("Avatar Library")]
    public Sprite[] avatarSprites;
    public string[] avatarIds; // e.g. "avatar_1", "avatar_2", etc.
    private Dictionary<string, Sprite> avatarMap;
    public string FormatCoins(double coins)
    {
        double truncated;
        if (coins >= 1_000_000_000)
        {
            truncated = Math.Truncate(coins / 1_000_000_000 * 100) / 100;
            return truncated.ToString("0.00") + "B";
        }
        else if (coins >= 1_000_000)
        {
            truncated = Math.Truncate(coins / 1_000_000 * 100) / 100;
            return truncated.ToString("0.00") + "M";
        }
        else if (coins >= 1_000)
        {
            truncated = Math.Truncate(coins / 1_000 * 100) / 100;
            return truncated.ToString("0.00") + "K";
        }
        else if (coins < 1 && coins > 0)
        {
            truncated = Math.Truncate(coins * 100000000) / 100000000; 
            return truncated.ToString("0.########");
        }
        else
        {
            truncated = Math.Truncate(coins * 100) / 100;
            return truncated.ToString("0.##");
        }
    }
    private float userCurrentCoin;
    public bool useCanGetCoinFromDB;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildAvatarMap();
    }

    public void SetAvatarDownloadedImage(Sprite avatarSprite)
    {
        Debug.Log("Hide Spinner ");
        AvatarImage = avatarSprite;
    }

    private void BuildAvatarMap()
    {
        avatarMap = new Dictionary<string, Sprite>();

        for (int i = 0; i < avatarIds.Length && i < avatarSprites.Length; i++)
        {
            avatarMap[avatarIds[i]] = avatarSprites[i];
        }
    }

    public void SetUserData(string id, string username, string email, float coins, string avatarUrl, string userGameId,int _avatarIndex, string _usertype, string _sessionID, string bio = "")
    {
        UserId = id;
        Username = username;
        avatarIndex = _avatarIndex;
        Email = email;
        Coins = coins;
        Bio = bio;
        IsLoggedIn = true;
        AvatarUrl = avatarUrl;
        _userGameId = userGameId;
        userType = _usertype;
        sessionId = _sessionID;
        StartUpdateCanAddCoin(true);
    }


    public void UpdateCoins(float newAmount)
    {
        Coins = newAmount;
        userCurrentCoin = newAmount;
        if (MainMenuUIManager.Instance != null)
        {
            MainMenuUIManager.Instance.SetUserData();
        }
    }



    public void ClearSession()
    {
        UserId = 0.ToString();
        Username = null;
        avatarIndex = 0;
        Email = null;
        Coins = 0;
        AvatarUrl = null;
        AvatarImage = null;
        Bio = null;
        IsLoggedIn = false;

        Debug.Log("[UserManager] Session cleared.");
    }



    #region Toogle User Current Scene Bool

    public event Action StopUpdateCanAddCoin_fun;
    public event Action StartUpdateCanAddCoin_fun;
    public void StopUpdateCanAddCoin()
    {
        StartUpdateCanAddCoin(true);
    }

    public void StartUpdateCanAddCoin()
    {
        StartUpdateCanAddCoin(true);
    }

    public void StartUpdateCanAddCoin(bool b)
    {
        sendCountToGetWinDataList = true;
        StartCoroutine(SendCanAddCoinUpdate(userId, b));
    }

    IEnumerator SendCanAddCoinUpdate(string userId, bool canAddCoin)
    {
        CanAddCoinRequest requestData = new CanAddCoinRequest
        {
            userId = userId,
            canAddCoin = canAddCoin
        };


        string json = JsonUtility.ToJson(requestData);
        Debug.Log("Json - " + json);
        UnityWebRequest request = new UnityWebRequest(ApiEndpoints.canaddcoin, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        foreach (var header in ApiEndpoints.GetAuthHeaders())
            request.SetRequestHeader(header.Key, header.Value);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ CanAddCoin updated successfully!");
            Debug.Log(request.downloadHandler.text);

            CanAddCoinResponse response = JsonUtility.FromJson<CanAddCoinResponse>(request.downloadHandler.text);
            if (response.success)
            {
                Debug.Log("✅ Server confirmed update: success = " + canAddCoin);
                useCanGetCoinFromDB = canAddCoin;
            }
            else
            {
                Debug.LogWarning("⚠️ Server responded but success = false");
            }
        }
        else if (request.responseCode == 401)
        {
            yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.canaddcoin, json, "PUT", () => SendCanAddCoinUpdate(userId, canAddCoin));
            yield break;
        }
        else
        {
            Debug.LogError("❌ Error updating canAddCoin: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    #endregion


    #region Get User Profile Data

    public bool sendCountToGetWinDataList;
    public static event Action<UserProfileResponse> FetchedNotifictionData;

    /// <summary>
    /// Fetch profile only. If sendCount is true, force isCount100 = true.
    /// Otherwise use the bool passed from caller.
    /// </summary>
    public void GetUserCurrentCoin()
    {
        StartCoroutine(GetProfileOnly());
    }



    /// <summary>
    /// Profile-only request.
    /// </summary>
    IEnumerator GetProfileOnly(bool countFlag = false)
    {
        bool flag = sendCountToGetWinDataList ? true : false;
        string finalUrl = $"{ApiEndpoints.UserProfile}?isCount100={sendCountToGetWinDataList.ToString().ToLower()}";

        UnityWebRequest request = new UnityWebRequest(finalUrl, "GET");
        request.downloadHandler = new DownloadHandlerBuffer();

        foreach (var header in ApiEndpoints.GetAuthHeaders())
            request.SetRequestHeader(header.Key, header.Value);

        Debug.Log($"Nasir Profile : {finalUrl}");
        yield return request.SendWebRequest();

        if (request.responseCode == 401)
        {
            yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.UserProfile, "", "GET", () => GetProfileOnly(sendCountToGetWinDataList));
            yield break;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Nasir Profile received successfully (profile-only):");
            Debug.Log(request.downloadHandler.text);

            var response = JsonUtility.FromJson<UserProfileResponse>(request.downloadHandler.text);

            if (sendCountToGetWinDataList)
            {
                sendCountToGetWinDataList = false;
            }

            // Notify listeners
            FetchedNotifictionData?.Invoke(response);

            // Update local and UI
            Coins = response.user.coinBalance;
            MainMenuUIManager.Instance?.SetUserData();
            updateUserCoinIntoCurrentGame();
        }
        else
        {
            Debug.LogError("❌ Failed to fetch profile: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    #endregion


    void updateUserCoinIntoCurrentGame()
    {
        string gameName = SceneManagement.currentGameName;
        switch (gameName)
        {
            case "cleopatra":
                CleopatraUIManager.Instance.UpdateCoins();
                break;
            case "crazy7":
                CrazySevenUIManager.Instance.UpdateCoins();
                break;
            //case "superballkeno":
            //    SuperBallKeno.Instance.UpdateBalanceDisplay();
            //    break;
            //case "hexakeno":
            //    HexagonKeno.Instance.UpdateBalanceDisplay();
            //    break;
            //case "octagonkeno":
            //    OctagonKeno.Instance.UpdateBalanceDisplay();
            //    break;
            //case "rockpaperscissors":
            //    UIManager_RPS.Instance.UpdateBalance(Coins);
            //    break;
            default:
                break;
        }
    }
}

[System.Serializable]
public class CoinBalanceRequest
{
    public string user_id;
    public float score;
}

[System.Serializable]
public class CoinBalanceUser
{
    public string id;
    public string username;
    public string email;
    public float coin_balance;
    public float score_added;
}

[System.Serializable]
public class CoinBalanceResponse
{
    public string message;
    public CoinBalanceUser user;
}


[System.Serializable]
public class CanAddCoinRequest
{
    public string userId;
    public bool canAddCoin;
}
[System.Serializable]
public class CanAddCoinResponse
{
    public bool success;
}



