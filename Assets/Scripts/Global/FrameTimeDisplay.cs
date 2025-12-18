using TMPro;
using UnityEngine;

public class FrameTimeDisplay : MonoBehaviour
{
    public static FrameTimeDisplay Instance; 

    [SerializeField] private TMP_Text fpsText;
    [SerializeField] private float updateThreshold = 30f; 

    private float deltaTime;
    private float lastDisplayedMs;

    private void Awake()
    {
        //if (Instance != null)
        //{
        //    Destroy(gameObject);
        //    return;
        //}
        //Instance = this;

        //DontDestroyOnLoad(transform.root.gameObject);
        if (fpsText == null) fpsText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Update()
    {
        if (fpsText == null) return;

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float currentMs = deltaTime * 1000f;

        bool shouldUpdate = Mathf.Abs(currentMs - lastDisplayedMs) >= updateThreshold;
        if (shouldUpdate)
        {
            lastDisplayedMs = currentMs;

            // Update text and color based on currentMs
            fpsText.text = $"{currentMs:F0} ms";

            if (currentMs > 200f)
                fpsText.color = Color.red;
            else if (currentMs > 100f)
                fpsText.color = Color.yellow;
            else
                fpsText.color = Color.green;
        }
    }
}
