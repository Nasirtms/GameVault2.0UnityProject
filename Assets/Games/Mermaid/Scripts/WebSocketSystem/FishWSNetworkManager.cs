using NativeWebSocket;
using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FishWSNetworkManager : MonoBehaviour
{
    public static FishWSNetworkManager Instance;
    private WebSocket socket;

    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

    void Awake()
    {
        //if (Instance != null)
        //{
        //    Destroy(gameObject);
        //    return;
        //}
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Connect();
    }

    async Task Connect()
    {
        socket = new WebSocket($"{BackendBaseUrlController.instance.GetSocketBaseUrl()}/ws/fish?access_token={ApiEndpoints.AuthToken}");
        //socket = new WebSocket($"ws://localhost:5036/ws/fish?access_token={ApiEndpoints.AuthToken}");
        //socket = new WebSocket($"wss://gamevault222.com/ws/fish?access_token={ApiEndpoints.AuthToken}");

        socket.OnOpen += () =>
        {
            Debug.Log(gameObject.name + " " + "Socket connected");
        };

        socket.OnMessage += (bytes) =>
        {
            string msg = Encoding.UTF8.GetString(bytes);
            HandleMessage(msg);
        };

        socket.OnError += (e) =>
        {
            Debug.LogError(gameObject.name + " " + "Socket error: " + e);
        };

        socket.OnClose += (e) =>
        {
            Debug.Log(gameObject.name + " " + "Socket closed.. Code: " + e.ToString());
        };

        await socket.Connect();
    }

    void Update()
    {
        //if (Application.platform == RuntimePlatform.WebGLPlayer)
        #if !UNITY_WEBGL || UNITY_EDITOR
                socket?.DispatchMessageQueue();
        #endif
    }

    public async void Send(object data)
    {
        if (!IsConnected)
        {
            //await Connect();
        }

        string json = JsonUtility.ToJson(data);
        Debug.Log(gameObject.name + " " + "Web Socket Message Send: " + json);
        await socket.SendText(json);
    }

    async void OnDestroy()
    {
        Instance = null;

        if (socket != null)
            await socket.Close();
    }

    void HandleMessage(string json)
    {
        FishWSNetworkMessages.Response_Base base_response = JsonUtility.FromJson<FishWSNetworkMessages.Response_Base>(json);

        Debug.Log(gameObject.name + " " + "Web Socket Message Received: " + json);

        if (!base_response.success)
        {
            Debug.Log(gameObject.name + " " + "Web Socket Message Error: " + base_response.error);
            return;
        }

        if (base_response.type == "fire")
        {
            //FishWSNetworkMessages.Fire_Response response = (FishWSNetworkMessages.Fire_Response)base_response;
            var response = JsonUtility.FromJson<FishWSNetworkMessages.BulletFire_Response>(json);
            GunManager.Instance.BulletFireResponse(response);
        }
        else if (base_response.type == "hit")
        {
            //FishWSNetworkMessages.Hit_Response response = (FishWSNetworkMessages.Hit_Response)base_response;
            var response = JsonUtility.FromJson<FishWSNetworkMessages.FishHit_Response>(json);
            FishManager.Instance.FishHitResponse(response);
        }
        else if (base_response.type == "fish-despawn")
        {
            //FishWSNetworkMessages.FishHidden_Response response = (FishWSNetworkMessages.FishHidden_Response)base_response;
            var response = JsonUtility.FromJson<FishWSNetworkMessages.FishDespawn_Response>(json);
            //FishManager.Instance.OnHitResult(response);
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            StopCoroutine("ReconnectIfDisconnectedAfterFocus");
            StartCoroutine("ReconnectIfDisconnectedAfterFocus");
        }
    }

    IEnumerator ReconnectIfDisconnectedAfterFocus()
    {
        float timer = 0;
        float totalTime = 5;

        while (timer < totalTime)
        {
            yield return new WaitForEndOfFrame();

            timer += Time.deltaTime;

            if (!IsConnected)
            {
                Connect();
                timer = totalTime;
            }
        }
    }
}