using NativeWebSocket;
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WebSocketManager : MonoBehaviour
{
    public static WebSocketManager Instance;
    private WebSocket socket;

    public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

    void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Connect();
    }

    async Task Connect()
    {
        socket = new WebSocket($"{BackendBaseUrlController.instance.GetSocketBaseUrl()}/ws/general?access_token={ApiEndpoints.AuthToken}");
        //socket = new WebSocket($"ws://localhost:5036/ws/general?access_token={ApiEndpoints.AuthToken}");
        //socket = new WebSocket($"wss://gamevault222.com/ws/general?access_token={ApiEndpoints.AuthToken}");

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
        //Instance = null;

        if (socket != null)
            await socket.Close();
    }

    void HandleMessage(string json)
    {
        WebSocketMessages.Response_Base base_response = JsonUtility.FromJson<WebSocketMessages.Response_Base>(json);

        Debug.Log(gameObject.name + " " + "Web Socket Message Received: " + json);

        if (!base_response.success)
        {
            Debug.Log(gameObject.name + " " + "Web Socket Message Error: " + base_response.error);
            return;
        }

        if (base_response.type == "ping")
        {
            var response = JsonUtility.FromJson<WebSocketMessages.HeartbeatMessage_Received>(json);
            SendPingResponsePong();
        }
        if (base_response.type == "coin-poll")
        {
            var response = JsonUtility.FromJson<WebSocketMessages.CoinPollMessage_Received>(json);
            UpdateCoinBalance(response);
        }
    }

    void SendPingResponsePong()
    {
        WebSocketMessages.HeartbeatMessage_Sent hb_message = new WebSocketMessages.HeartbeatMessage_Sent()
        {
            requestId = Guid.NewGuid().ToString(),
        };

        Send(hb_message);
    }

    void UpdateCoinBalance(WebSocketMessages.CoinPollMessage_Received response) {
        if (response.success)
        {
            UserManager.Instance.Coins = response.coinBalance;
            MainMenuUIManager.Instance?.SetUserData();
            UserManager.OnCoinsUpdate?.Invoke(response.coinBalance);
        }
    }
}
