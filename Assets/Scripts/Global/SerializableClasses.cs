using Supabase.Gotrue;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerializableClasses : MonoBehaviour
{
    public static SerializableClasses Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    #region Refresh Token


    [Serializable]
    public class RefreshTokenRequest
    {
        public string RefreshToken;
    }

    [Serializable]
    public class RefreshTokenResponse
    {
        public string accessToken;
        public string refreshToken;
    }

    #endregion

    #region ✅ User & Login

    [Serializable]
    public class LoginRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginUserData
    {
        public string id;
        public string username;
        public string email;
        public string userGameId;
        public string avatarUrl;
        public float coinBalance;
        public int bigWinTotal;
        public int avatarIndex;
        public int megaWinTotal;
        public int totalWins;
        public int totalLosses;
        public string role;
        public string cashierId;
        public string bio;
        public DateTime createdAt;
        public int loginStreak;
        public DateTime lastDailyClaim;
        public string nextSpinTime;
        public string token;
        public string refreshToken;
        public string sessionId;
        public bool isActive;
        public bool canAddCoin;
        public bool isFeedback;
        public bool hasSetAvatarOnce;


        public void EnsureDefaults()
        {
            avatarUrl ??= "";
            cashierId ??= "";
            bio ??= "";
        }
    }

    [Serializable]
    public class LoginResponseWrapper
    {
        public LoginUserData user;
        public List<AvatarOption> available_avatars;
        public string session_message;        // Add this
        public bool is_new_session;          // Add this
        public string previous_device_info;  // Add this
        public string previous_login_at;     // Add this
    }

    [Serializable]
    public class UserData
    {
        public string id;
        public string username;
        public string email;
        public string avatarUrl;
        public float coinBalance;
        public int bigWinTotal;
        public int megaWinTotal;
        public int totalWins;
        public int totalLosses;
        public string role;
        public string cashierId;
        public string bio;
        public string createdAt;
        public int loginStreak;
        public string lastDailyClaim;
        public string token;
    }

    [System.Serializable]
    public class ChangePasswordRequest
    {
        public string oldPassword;
        public string newPassword;
        public string confirmPassword;

        public ChangePasswordRequest(string old, string newPass, string confirm)
        {
            oldPassword = old;
            newPassword = newPass;
            confirmPassword = confirm;
        }

        public ChangePasswordRequest(object anonymous)
        {
            oldPassword = (string)anonymous.GetType().GetProperty("oldPassword").GetValue(anonymous, null);
            newPassword = (string)anonymous.GetType().GetProperty("newPassword").GetValue(anonymous, null);
            confirmPassword = (string)anonymous.GetType().GetProperty("confirmPassword").GetValue(anonymous, null);
        }
    }

    [System.Serializable]
    public class ChangePasswordResponse
    {
        public string message;
        public int status;
    }
    #endregion

    #region 🖼️ Avatar & Profile

    [Serializable]
    public class ProfileUpdateRequest
    {
        public string bio;
        public string avatar_url;
    }

    [Serializable]
    public class ProfileImageUpdateRequest
    {
        public string avatar_url;
        public int avatar_index;
    }

    [Serializable]
    public class AvatarOption
    {
        public string id;
        public string image_url;
        public string image_name;
        public string category;
        public bool is_active;
        public int display_order;
        public DateTime created_at;
    }

    #endregion

    #region 🎮 Games

    [Serializable]
    public class Game
    {
        public string id;
        public string name;
        public object description;
        public string image_url;
        public bool is_publish;
        public bool is_active;
        public float min_bet;
        public int max_bet;
        public string category;
        public DateTime created_at;
        public DateTime updated_at;
        public int favorite_count;
        public float win_ratio;
        public bool shine;
        public string title;
        public bool is_favorite;
    }

    [Serializable]
    public class GameList
    {
        public List<Game> games;
    }

    #endregion


    #region RPS Game
    [Serializable]
    public class RPSRequest
    {
        public string requestId;
        public string gameId;
        public string playerChoice;
        public float betAmount;
        public int currentLevel;
    }

    [Serializable]
    public class RPSResponse
    {
        public bool success;
        public string requestId;
        public string userId;
        public string gameId;
        public RPSChoice playerChoice;
        public string botChoice;
        public string result;          // "win", "lose", "tie"
        public float winAmount;
        public float newBalance;
        public string wheelIndex;
        public int consecutiveLossCount;
        public string timestamp;
    }

    #endregion

    #region 📆 Events

    [Serializable]
    public class Events
    {
        public string id;
        public string title;
        public string description;
        public string imageUrl;
        public string startTime;
        public string endTime;
        public bool isActive;
        public int priority;
        public string createdAt;
        public string eventBottom;
        public string targetType;
        public bool showOnce;
    }

    #endregion

    #region ⚙️ Scene Settings

    [Serializable]
    public class SceneDataResponse
    {
        public int id;
        public bool isSpinWheelEnabled;
        public bool showEventsPopup;
        public bool showNotificationAfterLogin;
        public int dailySpinLimit;
        public int rewardCoin;
        public DateTime updatedAt;
    }

    #endregion

    #region Leaderboard

    [System.Serializable]
    public class LeaderboardEntry
    {
        public string id;
        public string userId;
        public string username;
        public string userGameId;
        public string avatarUrl;
        public float coinBalance;
        public int totalWins;
        public int totalLosses;
        public int bigWinTotal;
        public int megaWinTotal;
        public int vipLevel;
        public string role;
        public float winRate;
        public int rankPosition;
        public string weekStartDate;
        public string createdAt;
    }

    #endregion

    #region Add/Remove Favorite Games
    [Serializable]
    public class AddFavoriteRequest
    {
        public string userId;
        public string gameId;
    }
    [Serializable]
    public class AddFavoriteResponse
    {
        public string message;
    }

    #endregion

    #region Login notification 


    [Serializable]
    public class LoginNotification
    {
        public string id;
        public string title;
        public string message;
        public List<string> targetUsers;
        public bool showOnce;
        public string createdAt;
        public string expiresAt;
        public bool isActive;
        public string expiryMessage;
        public string type;
        public string targetType;
        public string imageUrl;
    }

    [Serializable]
    public class LoginNotificationResponse
    {
        public bool success;
        public string message;
        public List<LoginNotification> data;
    }


    #endregion

    #region GoldRushGus MiniGame
    [Serializable]
    public class GoldRushGusMiniGameCoinUpdateResposne
    {
        public bool success;
        public string requestId;
        public float WinAmount;
        public float newBalance;
    }

    [Serializable]
    public class GoldRushGusMiniGameCoinUpdateRequest
    {
        public string gameId;
        public string requestId;
        public float betAmount;
        public int CoinMultiplier;
    }
    #endregion

    [Serializable]
    public class UserProfileResponse
    {
        public User user;
        public List<RecentBigWin> recent_big_wins;
        public int recent_big_wins_count;

    }
    [Serializable]
    public class RecentBigWin
    {
        public string message;
    }
    [Serializable]
    public class User
    {
        public string id;
        public string username;
        public string email;
        public string userGameId;
        public string avatarUrl;
        public float coinBalance;
        public int bigWinTotal;
        public int megaWinTotal;
        public int totalWins;
        public int totalLosses;
        public string role;
        public string cashierId;
        public string bio;
        public int vipLevel;
        public string createdAt;
        public int loginStreak;
        public string lastDailyClaim;
        public string sessionId;
        public string lastLoginAt;
        public string deviceInfo;
        public int dailyLoginCount;
        public string lastDailyLoginReset;
        public int loginCount;
        public bool isActive;
        public bool canAddCoin;
        public string token;
        public bool isFeedback;
        public bool hasSetAvatarOnce;
    }

}
