using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class DebugDashboard : MonoBehaviour
{
    public static DebugDashboard Instance { get; private set; }

    [Header("UI")]
    public Canvas Canvas;
    public TextMeshProUGUI debugText;

    [Header("Settings")]
    public float updateInterval = 0.5f;
    public string pingUrl = "https://gamevault222.com";

#if DEVELOPMENT

    private float timeLeft;
    private float accum;
    private int frames;

    private long memoryMB;
    private int deviceMemoryMB;
    private long pingMS = -1;
    private float lastApiMS = -1f;

    private bool isVisible = true;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        deviceMemoryMB = SystemInfo.systemMemorySize;
    }

    void Start()
    {
      if (debugText != null)
        debugText.raycastTarget = false;

        var group = Canvas.GetComponent<CanvasGroup>();
        if (group != null)
        {
         group.blocksRaycasts = false;
         group.interactable = false;
        }

        timeLeft = updateInterval;
        StartCoroutine(PingRoutine());
    }

    void Update()
    {
        // Toggle dashboard
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            isVisible = !isVisible;
            if (debugText != null)
                debugText.gameObject.SetActive(isVisible);
        }

        if (!isVisible || debugText == null) return;

        // FPS
        timeLeft -= Time.unscaledDeltaTime;
        accum += 1f / Time.unscaledDeltaTime;
        frames++;

        if (timeLeft <= 0f)
        {
            float fps = frames > 0 ? accum / frames : 0f;

            // Memory
            memoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);

            // 🎨 Colors
            string fpsColor = fps > 50 ? "green" : (fps > 30 ? "yellow" : "red");

            string pingColor = pingMS < 0 ? "grey" :
                               pingMS < 100 ? "green" :
                               pingMS < 200 ? "yellow" : "red";

            string apiColor = lastApiMS < 0 ? "grey" :
                              lastApiMS < 150 ? "green" :
                              lastApiMS < 300 ? "yellow" : "red";

            // 🧾 Text
            string fpsStr = $"<color={fpsColor}>FPS: {fps:0}</color>";
            string memStr = $"MEM: {memoryMB} MB / {deviceMemoryMB} MB";

            string pingStr = pingMS < 0
                ? "<color=grey>PING: --</color>"
                : $"<color={pingColor}>PING: {pingMS} ms</color>";

            string apiStr = lastApiMS < 0
                ? "<color=grey>API: --</color>"
                : $"<color={apiColor}>API: {lastApiMS:0} ms</color>";

            debugText.text = $"{fpsStr}\n{memStr}\n{pingStr}\n{apiStr}";

            // Reset
            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;
        }
    }

    IEnumerator PingRoutine()
    {
        while (true)
        {
            float start = Time.realtimeSinceStartup;

            using (UnityWebRequest req = UnityWebRequest.Get(pingUrl))
            {
                req.timeout = 5;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                    pingMS = (long)((Time.realtimeSinceStartup - start) * 1000f);
                else
                    pingMS = -1;
            }

            yield return new WaitForSecondsRealtime(2f);
        }
    }

    // 📡 API Hook
    public void SetApiTime(float ms)
    {
        lastApiMS = ms;
    }

#else

    // 🚫 Production: remove completely
    void Awake()
    {
        Destroy(gameObject);
    }

#endif
}