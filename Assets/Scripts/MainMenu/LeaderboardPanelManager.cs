using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class LeaderboardEntry
{
    public string Username;
    public float Points;
}

public class LeaderboardPanelManager : MonoBehaviour
{
    [Header("Your Rank Display")]
    public TextMeshProUGUI yourRankText;
    public int yourRank; // Assign from DB or testing

    [Header("UI References")]
    public Transform contentParent;
    public GameObject rowPrefab;

    [Header("Rank Images")]
    public Sprite[] topRankImages;   // [0] = 1st, [1] = 2nd, [2] = 3rd
    public Sprite defaultRankImage;

    [Header("User Images")]
    public Sprite[] topUserImages;   // [0] = 1st, [1] = 2nd, [2] = 3rd
    public Sprite defaultUserImage;

    [Header("Image Effect")]
    public Sprite[] topImageEffects;

    [Header("Name Fonts")]
    //public TMP_FontAsset[] topFontAssets;
    public TMP_FontAsset defaultFontAsset;

    [Header("Name Colors")]
    public Color rankNameColor;
    public Color defaultNameColor;

    [Header("Points Colors")]
    public Color[] topColors;
    public Color defaultColor;

    [Header("Test Entries")]
    public List<LeaderboardEntry> testEntries = new List<LeaderboardEntry>(); // Populate in Inspectors

    private static bool _hasRun = false;

    //void Awake()
    //{
    //    //if (!_hasRun)
    //    //{
    //    //    _hasRun = true;

    //    //    yourRank = 10000;
    //    //    UpdateYourRankText(-1);

    //    //    StartCoroutine(FetchWeeklyLeaderboardFromServer());
    //    //}
    //}

    public void PopulateLeaderboard(List<LeaderboardEntry> entries)
    {
        // Clear old rows
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        for (int i = 0; i < entries.Count && i < 100; i++)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            var entry = entries[i];

            // Rank section
            Transform rank = row.transform.GetChild(0);
            Image rankImage = rank.GetComponent<Image>();
            TextMeshProUGUI rankText = rank.GetComponentInChildren<TextMeshProUGUI>(true); // Include inactive

            // User section
            Transform user = row.transform.GetChild(1);
            Image userImage = user.GetComponent<Image>();
            Image imageEffect = user.Find("Winning").GetComponent<Image>();
            TextMeshProUGUI usernameText = user.Find("Name").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI pointsText = user.Find("Winning/Points").GetComponent<TextMeshProUGUI>();

            if (i < 3)
            {
                rankImage.sprite = topRankImages[i];
                rankText.text = "";

                userImage.sprite = topUserImages[i];
                imageEffect.enabled = true;
                imageEffect.sprite = topImageEffects[i];

                //usernameText.font = topFontAssets[i];
                usernameText.color = rankNameColor;
                pointsText.color = topColors[i];
            }
            else
            {
                rankImage.sprite = defaultRankImage;
                rankText.text = (i + 1).ToString();

                userImage.sprite = defaultUserImage;
                imageEffect.enabled = false;

                //usernameText.font = defaultFontAsset;
                usernameText.color = defaultNameColor;
                pointsText.color = defaultColor;
            }

            usernameText.font = defaultFontAsset;
            usernameText.text = entry.Username;
            if (UserManager.Instance != null)
            {
                pointsText.text = UserManager.Instance.FormatCoins(entry.Points);
            }

            if (entry.Username == UserManager.Instance.Username)
                yourRank = i + 1;
        }

    }

    private void UpdateYourRankText(int rank)
    {
        if (rank <= 0)
        {
            yourRankText.text = "--";
        }
        else if (rank <= 100)
        {
            yourRankText.text = yourRank.ToString();
        }
        else
        {
            yourRankText.text = "100+";
        }
    }

    public void ResetRank()
    {
        UpdateYourRankText(-1);
    }

    private IEnumerator LoadEffect()
    {
        VerticalLayoutGroup layout = contentParent.GetComponent<VerticalLayoutGroup>();
        int paddingTop = 500;
        layout.padding.top = paddingTop;
        layout.SetLayoutVertical();

        while (paddingTop > -100)
        {
            paddingTop -= 40;

            layout.padding.top = paddingTop;
            layout.SetLayoutVertical();
            yield return new WaitForSeconds(0.001f);
        }

        while (paddingTop < 0)
        {
            paddingTop += 10;

            layout.padding.top = paddingTop;
            layout.SetLayoutVertical();
            yield return new WaitForSeconds(0.01f);
        }

        paddingTop = 0;
        layout.padding.top = paddingTop;
        layout.SetLayoutVertical();
    }

    #region Get Data From Server
    public IEnumerator FetchWeeklyLeaderboardFromServer()
    {
        Debug.Log("🌐 Fetching weekly leaderboard from server...");

        UnityWebRequest request = UnityWebRequest.Get(ApiEndpoints.weeklyLeaderboard);
        request.SetRequestHeader("Authorization", $"Bearer {ApiEndpoints.AuthToken}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("⬇️ Weekly leaderboard fetched successfully");
            Debug.Log($"📄 JSON Response: {json}");

            DeserializeAndStoreLeaderboard(json);
        }
        else
        {
            Debug.LogError($"❌ Failed to fetch leaderboard: {request.responseCode} | {request.error}");
            Debug.LogError($"🔗 URL: {ApiEndpoints.weeklyLeaderboard}");
            Debug.LogError($"🔑 Token: {ApiEndpoints.AuthToken}");
        }
    }

    List<SerializableClasses.LeaderboardEntry> leaderboard; // ✅ Correct type

    private void DeserializeAndStoreLeaderboard(string json)
    {
        try
        {
            leaderboard = JsonConvert.DeserializeObject<List<SerializableClasses.LeaderboardEntry>>(json);

            if (leaderboard != null && leaderboard.Count > 0)
            {
                SceneManagement.weeklyLeaderboard.Clear();
                SceneManagement.weeklyLeaderboard.AddRange(leaderboard);

                Debug.Log($"✅ {leaderboard.Count} entries loaded into weeklyLeaderboard");

                // Immediately load the data into the UI
                LoadFromAPI(leaderboard);
            }
            else
            {
                Debug.LogWarning("⚠️ Deserialized leaderboard is null or empty");
                Debug.LogWarning($"📄 JSON content: {json}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Failed to deserialize leaderboard: " + ex.Message);
            Debug.LogError("JSON content: " + json);
        }
    }

    public void ShowLeaderboard()
    {
        // Always fetch fresh data from server
        Debug.Log("🔄 Fetching fresh leaderboard data from server");

        yourRank = 10000;
        UpdateYourRankText(-1);

        StartCoroutine(FetchWeeklyLeaderboardFromServer());
    }

    public void RemoveData()
    {
        Debug.Log("🗑️ Removing leaderboard data");
        testEntries.Clear();
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void LoadFromAPI(List<SerializableClasses.LeaderboardEntry> response)
    {
        Debug.Log($"📊 Loading {response?.Count ?? 0} leaderboard entries from API");

        if (response == null || response.Count == 0)
        {
            Debug.LogWarning("⚠️ No leaderboard entries to display");
            return;
        }

        testEntries.Clear();

        foreach (var entry in response)
        {
            testEntries.Add(new LeaderboardEntry
            {
                Username = entry.username ?? "Unknown",
                Points = entry.coinBalance
            });
        }

        PopulateLeaderboard(testEntries);
        UpdateYourRankText(yourRank);
        StartCoroutine(LoadEffect());
        Debug.Log($"✅ Loaded {testEntries.Count} leaderboard entries.");
    }

    // Method to manually refresh leaderboard
    public void RefreshLeaderboard()
    {
        Debug.Log("🔄 Manual refresh - fetching fresh leaderboard data");
        StartCoroutine(FetchWeeklyLeaderboardFromServer());
    }
    #endregion
}