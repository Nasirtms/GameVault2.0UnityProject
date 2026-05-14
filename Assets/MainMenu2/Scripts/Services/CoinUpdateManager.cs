using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_STANDALONE || UNITY_EDITOR
using System.Net;
using System.Text;
#endif

public class CoinUpdateManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pollingInterval = 3f;
    [SerializeField] private int serverPort = 3001;
    [SerializeField] private bool enableServerInEditor = true;
    [SerializeField] private string localUrl = "http://localhost:3001";

    public static event Action<CoinUpdateData> OnCoinUpdateReceived;
    public static event Action<float> OnCoinBalanceChanged;

    private string currentUserGameId = "";
    private float lastPollTime = 0f;
    private string lastProcessedTimestamp = "";
    private bool isInitialQueueCleared = false;
    private bool isProcessingUpdate = false;
    private DateTime startTime;
    private Coroutine clearQueueCoroutine;
    private float lastClearTime = 0f;
    private const float CLEAR_QUEUE_BATCH_INTERVAL = 1f;

    private static CoinUpdateManager instance;

#if UNITY_STANDALONE || UNITY_EDITOR
    private HttpListener httpListener;
    private bool isServerRunning = false;
    private UnityMainThreadDispatcher mainThreadDispatcher;
#endif

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        startTime = DateTime.UtcNow;
        currentUserGameId = GetCurrentUserGameId();
        
        if (!string.IsNullOrEmpty(currentUserGameId))
        {
            StartCoroutine(InitializeWithQueueClear());
        }
        else
        {
            StartNormalOperation();
        }
    }

    private void Update()
    {
#if UNITY_WEBGL || (!UNITY_EDITOR && !UNITY_STANDALONE)
        if (isInitialQueueCleared && Time.time - lastPollTime >= pollingInterval)
        {
            lastPollTime = Time.time;
            StartCoroutine(PollForUpdates());
        }
#endif
    }

    private void OnDestroy()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        StopServer();
#endif
        if (clearQueueCoroutine != null)
        {
            StopCoroutine(clearQueueCoroutine);
        }
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        StopServer();
#endif
    }

    private string GetCurrentUserGameId()
    {
        try
        {
            if (UserManager.Instance != null && !string.IsNullOrEmpty(UserManager.Instance.UserId))
            {
                return UserManager.Instance.UserId;
            }
        }
        catch { }
        return string.Empty;
    }

    private IEnumerator InitializeWithQueueClear()
    {
        yield return StartCoroutine(ClearProcessedUpdates());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(ClearProcessedUpdates());
        
        isInitialQueueCleared = true;
        StartNormalOperation();
    }

    private void StartNormalOperation()
    {
#if UNITY_EDITOR
        if (enableServerInEditor)
        {
            StartServer();
        }
        else
        {
            StartPolling();
        }
#elif UNITY_WEBGL
        StartPolling();
#else
        StartServer();
#endif
    }

#if UNITY_STANDALONE || UNITY_EDITOR
    private void StartServer()
    {
        if (isServerRunning) return;
        
        try
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{serverPort}/api/coin-update/");
            httpListener.Start();
            isServerRunning = true;
            mainThreadDispatcher = UnityMainThreadDispatcher.Instance();
            StartCoroutine(ListenForRequests());
        }
        catch
        {
            StartPolling();
        }
    }

    private void StopServer()
    {
        if (!isServerRunning) return;
        
        try
        {
            isServerRunning = false;
            httpListener?.Stop();
            httpListener?.Close();
            httpListener = null;
        }
        catch { }
    }

    private IEnumerator ListenForRequests()
    {
        while (isServerRunning && httpListener != null && httpListener.IsListening)
        {
            if (httpListener.IsListening)
            {
                httpListener.BeginGetContext(OnRequestReceived, null);
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    private void OnRequestReceived(IAsyncResult result)
    {
        if (!isServerRunning || httpListener == null || !httpListener.IsListening) return;
        
        try
        {
            HttpListenerContext context = httpListener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, OPTIONS, GET");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.ContentLength64 = 0;
                response.Close();
                if (isServerRunning && httpListener != null && httpListener.IsListening)
                {
                    httpListener.BeginGetContext(OnRequestReceived, null);
                }
                return;
            }

            string requestPath = request.Url.AbsolutePath.TrimEnd('/');
            if (request.HttpMethod == "POST" && requestPath == "/api/coin-update")
            {
                byte[] buffer = new byte[request.ContentLength64];
                request.InputStream.Read(buffer, 0, buffer.Length);
                string jsonData = Encoding.UTF8.GetString(buffer);

                mainThreadDispatcher.Enqueue(() => ProcessCoinUpdate(jsonData));

                string responseString = "{\"status\":\"success\",\"message\":\"Coin update received\"}";
                byte[] responseBuffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentType = "application/json";
                response.StatusCode = 200;
                response.ContentLength64 = responseBuffer.Length;
                response.OutputStream.Write(responseBuffer, 0, responseBuffer.Length);
            }
            else
            {
                response.StatusCode = 404;
            }

            response.Close();

            if (isServerRunning && httpListener != null && httpListener.IsListening)
            {
                httpListener.BeginGetContext(OnRequestReceived, null);
            }
        }
        catch { }
    }
#endif

    private void StartPolling()
    {
        lastPollTime = Time.time;
    }

    private IEnumerator PollForUpdates()
    {
        if (isProcessingUpdate || !isInitialQueueCleared) yield break;
        
        if (string.IsNullOrEmpty(currentUserGameId))
        {
            currentUserGameId = GetCurrentUserGameId();
            if (string.IsNullOrEmpty(currentUserGameId)) yield break;
        }

#if UNITY_WEBGL
        string baseUrl = ApiEndpoints.baseUrl;
#else
        string baseUrl = localUrl;
#endif

        string pollUrl = $"{baseUrl}/api/coin-update/poll?userId={UnityWebRequest.EscapeURL(currentUserGameId)}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                if (!string.IsNullOrEmpty(response) && response != "null" && response != "[]" && response != "{}")
                {
                    isProcessingUpdate = true;
                    ProcessPollingResponse(response);
                    isProcessingUpdate = false;
                }
            }
        }
    }

    private void ProcessPollingResponse(string jsonResponse)
    {
        try
        {
            if (jsonResponse.Trim().StartsWith("["))
            {
                string cleanedJson = jsonResponse.Trim();
                cleanedJson = cleanedJson.Substring(1, cleanedJson.Length - 2).Trim();
                
                if (!string.IsNullOrEmpty(cleanedJson))
                {
                    string[] updateStrings = cleanedJson.Contains("},{") 
                        ? cleanedJson.Split(new string[] { "},{" }, System.StringSplitOptions.None)
                        : new string[] { cleanedJson };
                    
                    foreach (string updateJson in updateStrings)
                    {
                        string json = updateJson.Trim();
                        if (!json.StartsWith("{")) json = "{" + json;
                        if (!json.EndsWith("}")) json = json + "}";
                        
                        CoinUpdateData data = JsonUtility.FromJson<CoinUpdateData>(json);
                        if (data != null)
                        {
                            ProcessCoinUpdateData(data);
                        }
                    }
                }
            }
            else
            {
                CoinUpdateData data = JsonUtility.FromJson<CoinUpdateData>(jsonResponse);
                if (data != null)
                {
                    ProcessCoinUpdateData(data);
                }
            }
        }
        catch { }
    }

    private IEnumerator ClearProcessedUpdates()
    {
        if (string.IsNullOrEmpty(currentUserGameId))
        {
            currentUserGameId = GetCurrentUserGameId();
            if (string.IsNullOrEmpty(currentUserGameId)) yield break;
        }
        
#if UNITY_WEBGL
        string baseUrl = ApiEndpoints.baseUrl;
#else
        string baseUrl = localUrl;
#endif
        
        string clearUrl = $"{baseUrl}/api/coin-update/clear?userId={UnityWebRequest.EscapeURL(currentUserGameId)}";
        
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(clearUrl, ""))
        {
            yield return request.SendWebRequest();
        }
    }

    private void ScheduleQueueClear()
    {
        if (Time.time - lastClearTime < CLEAR_QUEUE_BATCH_INTERVAL)
        {
            if (clearQueueCoroutine == null)
            {
                clearQueueCoroutine = StartCoroutine(BatchedQueueClear());
            }
        }
        else
        {
            lastClearTime = Time.time;
            StartCoroutine(ClearProcessedUpdates());
        }
    }

    private IEnumerator BatchedQueueClear()
    {
        yield return new WaitForSeconds(CLEAR_QUEUE_BATCH_INTERVAL);
        lastClearTime = Time.time;
        yield return StartCoroutine(ClearProcessedUpdates());
        clearQueueCoroutine = null;
    }

    private void ProcessCoinUpdate(string jsonData)
    {
        try
        {
            CoinUpdateData updateData = JsonUtility.FromJson<CoinUpdateData>(jsonData);
            if (updateData == null) return;
            ProcessCoinUpdateData(updateData);
        }
        catch { }
    }

    private void ProcessCoinUpdateData(CoinUpdateData updateData)
    {
        if (updateData == null) return;

        if (updateData.userGameId != currentUserGameId && 
            updateData.userId != currentUserGameId &&
            !string.Equals(updateData.userGameId, currentUserGameId, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(updateData.userId, currentUserGameId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.IsNullOrEmpty(updateData.timestamp))
        {
            try
            {
                DateTime updateTime = DateTime.Parse(updateData.timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
                if (updateTime < startTime)
                {
                    ScheduleQueueClear();
                    return;
                }
            }
            catch { }
        }

        if (!string.IsNullOrEmpty(updateData.timestamp) && !string.IsNullOrEmpty(lastProcessedTimestamp))
        {
            if (string.Compare(updateData.timestamp, lastProcessedTimestamp, StringComparison.Ordinal) <= 0)
            {
                return;
            }
        }

        float currentBalance = 0f;
        try
        {
            if (UserManager.Instance != null)
            {
                currentBalance = UserManager.Instance.Coins;
            }
        }
        catch { }

        if (!Mathf.Approximately(currentBalance, updateData.balanceAfter))
        {
            UpdateCoinBalance(updateData.balanceAfter);
            OnCoinUpdateReceived?.Invoke(updateData);
            OnCoinBalanceChanged?.Invoke(updateData.balanceAfter);
        }

        if (!string.IsNullOrEmpty(updateData.timestamp))
        {
            lastProcessedTimestamp = updateData.timestamp;
        }

        ScheduleQueueClear();
    }

    private void UpdateCoinBalance(float newBalance)
    {
        try
        {
            isProcessingUpdate = true;
            UserManager.Instance.Coins = newBalance;
            MainMenuUIManager.Instance?.SetUserData();
            StartCoroutine(ResetProcessingFlag());
            UserManager.OnCoinsUpdate?.Invoke(newBalance);
        }
        catch
        {
            isProcessingUpdate = false;
        }
    }

    private IEnumerator ResetProcessingFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isProcessingUpdate = false;
    }

    public static bool IsProcessingCoinUpdate()
    {
        return instance != null && instance.isProcessingUpdate;
    }
}

[Serializable]
public class CoinUpdateData
{
    public string userId;
    public string userGameId;
    public string username;
    public string action;
    public float amount;
    public float balanceBefore;
    public float balanceAfter;
    public string timestamp;
}

#if UNITY_STANDALONE || UNITY_EDITOR
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    private Queue<Action> actionQueue = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("UnityMainThreadDispatcher");
            instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return instance;
    }

    private void Update()
    {
        while (actionQueue.Count > 0)
        {
            actionQueue.Dequeue()?.Invoke();
        }
    }

    public void Enqueue(Action action)
    {
        actionQueue.Enqueue(action);
    }
}
#endif
