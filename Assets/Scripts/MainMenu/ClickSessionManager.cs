using UnityEngine;
using System.Collections;

public class ClickSessionManager : MonoBehaviour
{
    [Header("Inactivity Settings")]

    [SerializeField] private float inactivityTimeout = 300f; // seconds
    public static ClickSessionManager Instance;



    private float lastInteractionTime;
    private bool popupShown = false;
    private bool isTrackingActive = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

#if UNITY_EDITOR
        inactivityTimeout = 1200f; // 20 minutes in editor
#endif
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        lastInteractionTime = Time.time;
        StartCoroutine(CheckInactivity());
    }

    private void Update()
    {
        if (!isTrackingActive) return;

        // Detect any user activity
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            lastInteractionTime = Time.time;
            popupShown = false;
        }
    }

    private IEnumerator CheckInactivity()
    {
        while (isTrackingActive)
        {
            yield return new WaitForSeconds(1f);

            if (popupShown) continue;

            if (Time.time - lastInteractionTime >= inactivityTimeout)
            {
                popupShown = true;
                OnUserInactive();
            }
        }
    }

    public void StartInactivityTracking()
    {
        if (isTrackingActive) return;

        Debug.Log("▶️ Inactivity tracking started.");
        isTrackingActive = true;
        popupShown = false;
        lastInteractionTime = Time.time;
        StartCoroutine(CheckInactivity());
    }

    public void StopInactivityTracking()
    {
        Debug.Log("⏹️ Inactivity tracking stopped.");
        isTrackingActive = false;
        StopAllCoroutines();
    }

    private void OnUserInactive()
    {
        if (UnitySessionManager.Instance != null)
        {
            UnitySessionManager.Instance.ForceLogoutDueToInactivity();
        }
        else
        {
            Debug.LogWarning("⚠️ UnitySessionManager.Instance not found — cannot show popup.");
        }
    }
    public void ResetInactivityTimer()
    {
        lastInteractionTime = Time.time;
        popupShown = false;
        //Debug.Log("🔄 Inactivity timer reset (by spin or user action)");
    }
}