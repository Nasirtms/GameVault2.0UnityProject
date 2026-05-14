using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class FruitMaryFruitMaryGameSpinService : MonoBehaviour
{
    public static FruitMaryFruitMaryGameSpinService Instance { get; private set; }

    private string currentRequestId;
    private string fruitMaryGameApi;

    public event Action<FruitMaryGame> OnSpinResultReceived;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    public void Spin(float betAmount)
    {
        StartCoroutine(CallSlotSpinApi(betAmount));
    }

    private IEnumerator CallSlotSpinApi(float betAmount)
    {
        currentRequestId = Guid.NewGuid().ToString();
        var requestData = new object();

        requestData = new
        {
            betAmount = betAmount,
            requestId = currentRequestId,
            gameId = SceneManagement.currentGameID,
            isFreeSpin = FruitMarySlotMachine.Instance.isFreeGame,
            isBonusGame = true
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        FruitMaryFruitMaryGameSlotMachine.Instance.Spin();

        ApiEndpoints.slotGameName = SceneManagement.currentGameName;
        string spinApiUrl = ApiEndpoints.slotGameSpin;

        using (UnityWebRequest www = new UnityWebRequest(spinApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            // Send the request
            yield return www.SendWebRequest();

            if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, spinApiUrl, jsonData, "POST", () => CallSlotSpinApi(betAmount));
                yield break;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                AddCurrentBetCoinIntoUserCoin();
                //Debug.LogError("❌ Response text is null or empty.");
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
                yield break;
            }

            string responseText = www.downloadHandler.text;
            //Debug.Log("✅ Response Received: " + responseText);

            try
            {
                if (string.IsNullOrEmpty(responseText))
                {
                    AddCurrentBetCoinIntoUserCoin();
                    //Debug.LogError("❌ Response text is null or empty.");
                    CasinoUIManager.Instance.ShowErrorCanvas(1, "Empty server response");
                    yield break;
                }

                FruitMaryGame spinResult = JsonConvert.DeserializeObject<FruitMaryGame>(responseText);

                if (spinResult == null)
                {
                    AddCurrentBetCoinIntoUserCoin();
                    //Debug.LogError("❌ Failed to deserialize spin response.");
                    CasinoUIManager.Instance.ShowErrorCanvas(1, "Invalid response format");
                    yield break;
                }

                if (!string.IsNullOrEmpty(spinResult.requestId) && spinResult.requestId != currentRequestId)
                {
                    AddCurrentBetCoinIntoUserCoin();
                    //Debug.LogWarning($"⚠️ Received outdated response (requestId: {spinResult.requestId}). Ignoring.");
                    //Debug.Log("\nResponse ID: " + spinResult.requestId + "\nRequest ID: " + currentRequestId);
                    yield break;
                }

                HandleSpinResponse(responseText);
            }
            catch (Exception ex)
            {
                AddCurrentBetCoinIntoUserCoin();
                //Debug.LogError("❌ Exception while parsing spin result: " + ex.Message);
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
            }
        }
    }

    public void HandleSpinResponse(string json)
    {
        FruitMaryGame result = JsonConvert.DeserializeObject<FruitMaryGame>(json);

        if (result != null && result.success)
        {
            OnSpinResultReceived?.Invoke(result);
        }
        else
        {
            //Debug.Log("Error :  SpinResult parse error or unsuccessful.");
        }
    }

    private void AddCurrentBetCoinIntoUserCoin()
    {

    }
}

[System.Serializable]
public class FruitMaryGame
{
    public bool success;
    public string requestId;
    public string spinId;
    public string userId;
    public string gameId;
    public float totalWin;
    public List<List<FruitMaryGameSlot>> reels;
    public int pointerIndex;
}

[System.Serializable]
public class FruitMaryGameSlot
{
    public string id;
}