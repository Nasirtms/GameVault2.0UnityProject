using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class Backend_RPS : MonoBehaviour
{
    public static Backend_RPS Instance;

    [SerializeField] private GameManager_RPS gameManager;
    [SerializeField] private UIManager_RPS uiManager;
    [SerializeField] private ComputerChoiceManager_RPS compChoiceManager;
    [SerializeField] private BetManager_RPS betManager;
    public SerializableClasses.RPSResponse _current_RPSResponse;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void SendRPSChoice(string choice)
    {
        SerializableClasses.RPSRequest req = new SerializableClasses.RPSRequest
        {
            requestId = Guid.NewGuid().ToString(),
            gameId = SceneManagement.currentGameID,
            playerChoice = choice,
            betAmount = betManager.CurrentBet,
            currentLevel = gameManager.CurrentWheelLevel
        };

        StartCoroutine(SendRPSRequest(req));
    }

    private IEnumerator SendRPSRequest(SerializableClasses.RPSRequest data)
    {
        string json = JsonUtility.ToJson(data);
        Debug.Log("📤 Sending RPS request: " + json);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(ApiEndpoints.rockPaperScissors, "POST"))
        using (UploadHandlerRaw uploadHandler = new UploadHandlerRaw(bodyRaw))
        using (DownloadHandlerBuffer downloadHandler = new DownloadHandlerBuffer())
        {
            request.uploadHandler = uploadHandler;
            request.downloadHandler = downloadHandler;

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                request.SetRequestHeader(header.Key, header.Value);

            // Send request
            yield return request.SendWebRequest();

            // 🔁 Handle token expiry
            if (request.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.rockPaperScissors, json, "POST", () => SendRPSRequest(data));
                yield break;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ RPS Response: " + downloadHandler.text);

                SerializableClasses.RPSResponse response =
                    JsonUtility.FromJson<SerializableClasses.RPSResponse>(downloadHandler.text);

                _current_RPSResponse = response;
                HandleRPSResult();
                yield break;
            }
        }

        // ❌ Failure
        HandleRequestError();
    }

    private void HandleRequestError()
    {
        CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
        GameBetServices.Instance.UpdateCoins(betManager.CurrentBet + UserManager.Instance.Coins);
        gameManager.ResetLevel();

        uiManager.playButton.image.sprite = uiManager.playButtonSprite1;
        uiManager.playButton.interactable = true;

        uiManager.rockButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.paperButton.transform.GetChild(1).gameObject.SetActive(false);
        uiManager.scissorsButton.transform.GetChild(1).gameObject.SetActive(false);

        compChoiceManager.StopCycle();
    }

    private void HandleRPSResult()
    {
        int val = -1;
        if (_current_RPSResponse.botChoice.Equals("rock"))
        {
            val = 0;
        }
        else if (_current_RPSResponse.botChoice.Equals("paper"))
        {
            val = 1;
        }
        else if (_current_RPSResponse.botChoice.Equals("scissors"))
        {
            val = 2;
        }


        compChoiceManager.StopCycleAndReveal(val);
        val = -1;
        // Set forced controls
        gameManager.forceWin = _current_RPSResponse.result.Equals("win");
        gameManager.forceLose = _current_RPSResponse.result.Equals("lose");
        gameManager.forceTie = _current_RPSResponse.result.Equals("tie");
        gameManager.chosen = !string.IsNullOrEmpty(_current_RPSResponse.wheelIndex) ? int.Parse(_current_RPSResponse.wheelIndex) : -1;

        if (_current_RPSResponse.result.Equals("win"))
        {
            uiManager.wheelManager.stopSpin = true;
        }

        gameManager.OnPlayerChoice(_current_RPSResponse.result);
    }
}
