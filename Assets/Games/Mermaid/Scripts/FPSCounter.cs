using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    [Header("UI Display (Optional)")]
    public TextMeshProUGUI fpsText; // Assign a UI Text element in the Inspector

    [Header("Settings")]
    public float updateInterval = 0.5f; // Seconds between updates

    private float timeLeft;
    private float accum;
    private int frames;

    void Start()
    {
        timeLeft = updateInterval;
    }

    void Update()
    {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Update and reset
        if (timeLeft <= 0.0)
        {
            float fps = accum / frames;
            string format = $"{fps:F1} FPS";

            if (fpsText != null)
                fpsText.text = format;
            else
                Debug.Log(format);

            // Reset
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}
