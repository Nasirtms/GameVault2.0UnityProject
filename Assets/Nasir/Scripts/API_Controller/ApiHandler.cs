using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ApiHandler : MonoBehaviour
{
    public static ApiHandler instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    public void GameStarted(string gameId) => SendGameActionEvent(ApiClasses.GameActionType.GameOpen, gameId);

    public void GameExited(string gameId) => SendGameActionEvent(ApiClasses.GameActionType.GameClose, gameId);

    public void SendGameActionEvent(ApiClasses.GameActionType action, string gameId)
    {
        Debug.Log($"Sending ActionEvent: {action.ToString()} ___ gameId: {gameId}");

        StartCoroutine(SendGameActionEvent_API(action, gameId));
    }

    private IEnumerator SendGameActionEvent_API(ApiClasses.GameActionType action, string gameId)
    {
        string url = ApiEndpoints.SendGameActionEvent;

        var payload = new ApiClasses.GameActionEvent_Request
        {
            gameId = gameId,
            action = action.ToString()
        };

        string jsonBody = JsonConvert.SerializeObject(payload);

        Debug.Log("Sending ActionEvent: " + jsonBody);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Debug.Log($"ActionEvent Sent Successfully.. Response: {www.downloadHandler.text}");
                    var response = JsonUtility.FromJson<ApiClasses.GameActionEvent_Response>(www.downloadHandler.text);
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"ActionEvent Sent Successfully.. Response: {ex.Message}");
                }
            }
            else if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, url, jsonBody, "POST", () => SendGameActionEvent_API(action, gameId));
                yield break;
            }
            else
            {
                Debug.LogError($"Sending ActionEvent Failed: {www.error}");
            }
        }
    }
}
