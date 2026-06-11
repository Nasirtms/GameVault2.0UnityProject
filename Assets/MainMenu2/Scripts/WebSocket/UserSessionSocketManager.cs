using NativeWebSocket;
using System;
using System.Collections;
using System.Text;
using UnityEngine;

public class UserSessionSocketManager : MonoBehaviour
{
    public static UserSessionSocketManager Instance;

    private WebSocket socket;
    private Coroutine pingRoutine;
    private bool isConnecting;

    [SerializeField] private float pingIntervalSeconds = 5f;

    [Serializable]
    private class SessionPingRequest
    {
        public string type = "ping";
        public string userId;
        public string sessionId;
    }

    [Serializable]
    private class SessionPongResponse
    {
        public string type;
        public string message;
        public bool isValid;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //private async void Start()
    //{
    //    await Connect();
    //}


    public async void ReConnectUserSessionSocket()
    {
        await Connect();
    }


    public static UserSessionSocketManager GetOrFind()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<UserSessionSocketManager>();
        return Instance;
    }

    public async System.Threading.Tasks.Task Connect()
    {
        if (isConnecting || (socket != null && socket.State == WebSocketState.Open))
            return;

        isConnecting = true;
        try
        {
            var rawBaseUrl = BackendBaseUrlController.instance.GetSocketBaseUrl()?.Trim();

            if (string.IsNullOrEmpty(rawBaseUrl))
            {
                Debug.LogError("UserSession socket base URL is empty.");
                return;
            }

            // Convert HTTP(S) -> WS(S) for NativeWebSocket
            string wsBaseUrl;
            if (rawBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                wsBaseUrl = "wss://" + rawBaseUrl.Substring("https://".Length);
            }
            else if (rawBaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                wsBaseUrl = "ws://" + rawBaseUrl.Substring("http://".Length);
            }
            else if (rawBaseUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase) ||
                     rawBaseUrl.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            {
                wsBaseUrl = rawBaseUrl;
            }
            else
            {
                Debug.LogError("Unsupported base URL scheme for WebSocket: " + rawBaseUrl);
                return;
            }

            wsBaseUrl = wsBaseUrl.TrimEnd('/');
            var wsUrl = $"{wsBaseUrl}/ws/user-session";

            Debug.Log("Connecting UserSession socket: " + wsUrl);

            socket = new WebSocket(wsUrl);

            socket.OnOpen += () =>
            {
                Debug.Log("UserSession socket connected");
                if (pingRoutine != null) StopCoroutine(pingRoutine);
                pingRoutine = StartCoroutine(PingLoop());
            };

            socket.OnMessage += (bytes) =>
            {
                var json = Encoding.UTF8.GetString(bytes);
                HandleMessage(json);
            };

            socket.OnError += (e) =>
            {
                Debug.LogError("UserSession socket error: " + e);
            };

            socket.OnClose += (e) =>
            {
                Debug.Log("UserSession socket closed: " + e);
                if (pingRoutine != null)
                {
                    StopCoroutine(pingRoutine);
                    pingRoutine = null;
                }
            };

            await socket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError("UserSession socket connect exception: " + ex.Message);

            if (pingRoutine != null)
            {
                StopCoroutine(pingRoutine);
                pingRoutine = null;
            }
        }
        finally
        {
            isConnecting = false;
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        socket?.DispatchMessageQueue();
#endif
    }

    private IEnumerator PingLoop()
    {
        while (socket != null && socket.State == WebSocketState.Open)
        {
            SendSessionPing();
            yield return new WaitForSeconds(pingIntervalSeconds);
        }
    }

    private async void SendSessionPing()
    {
        if (socket == null || socket.State != WebSocketState.Open)
            return;

        var userId = UserManager.Instance.UserId;      // set this where you store logged-in user id
        var sessionId = UserManager.Instance.sessionId; // set this where you store current session id

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(sessionId))
            return;

        var payload = new SessionPingRequest
        {
            type = "ping",
            userId = userId,
            sessionId = sessionId
        };

        var json = JsonUtility.ToJson(payload);
        await socket.SendText(json);
    }

    private void HandleMessage(string json)
    {
        Debug.Log("UserSession socket recv: " + json);

        SessionPongResponse pong;
        try
        {
            pong = JsonUtility.FromJson<SessionPongResponse>(json);
        }
        catch
        {
            return;
        }

        if (pong == null) return;
        if (!string.Equals(pong.type, "pong", StringComparison.OrdinalIgnoreCase)) return;

        // Core rule: invalid session => instant logout
        if (!pong.isValid || string.Equals(pong.message, "logout", StringComparison.OrdinalIgnoreCase))
        {
            ForceLogout();
        }
    }

    private async void OnDestroy()
    {
        Instance = null;
        if (pingRoutine != null) StopCoroutine(pingRoutine);

        if (socket != null)
            await socket.Close();
    }

    private async void ForceLogout()
    {
        if (pingRoutine != null) { StopCoroutine(pingRoutine); pingRoutine = null; }

        try
        {
            if (socket != null && (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting))
                await socket.Close();
        }
        catch { }
        finally
        {
            socket = null;
            isConnecting = false;
        }

        UnitySessionManager.Instance.ForceLogout();

        //ApiEndpoints.AuthToken = "";
        //UserManager.Instance.sessionId = "";
        //UserManager.Instance.UserId = "";

        //UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
    }
}