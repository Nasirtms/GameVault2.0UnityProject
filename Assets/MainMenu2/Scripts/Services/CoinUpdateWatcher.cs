using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinUpdateWatcher : MonoBehaviour
{
    private static CoinUpdateWatcher instance;

    [Header("Settings")]
    [SerializeField] private float pollingInterval = 3f;
    [SerializeField] private float pollingIntervalTimer = 0;

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {

//#if UNITY_WEBGL// || (!UNITY_EDITOR && !UNITY_STANDALONE)

        pollingIntervalTimer += Time.deltaTime;
        if (pollingIntervalTimer >= pollingInterval)
        {
            pollingIntervalTimer = 0;
            PollForUpdatesMethod();
        }
//#endif
    }

    private void PollForUpdatesMethod()
    {
        WebSocketManager.Instance?.Send(new WebSocketMessages.CoinPollMessage_Sent());
    }
}
