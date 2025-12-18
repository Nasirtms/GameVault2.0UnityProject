using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SpinWheelButtonAnimator : MonoBehaviour
{
    public static SpinWheelButtonAnimator Instance;
    [Header("State Objects")]
    public GameObject inactiveVisual;
    public GameObject activeVisual;

    [Header("Active Visual Parts")]
    public RectTransform wheel;          // Rotates
    public RectTransform activeRoot;     // Scales

    [Header("Animation Settings")]
    public float rotationSpeed = 60f;    // Degrees per second
    public float scaleAmount = 1.1f;
    public float scaleDuration = 0.5f;

    private bool isActive = false;
    private Tween scaleTween;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Prevent duplicates
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (isActive && wheel != null)
        {
            wheel.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }

    public void SetActiveState(bool active)
    {
        if (isActive == active) return; // Prevent duplicate triggers
        isActive = active;

        inactiveVisual.SetActive(!active);
        activeVisual.SetActive(active);

        if (active)
        {
            // Start scaling animation
            scaleTween = activeRoot
                .DOScale(scaleAmount, scaleDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            // Stop scaling and reset scale
            scaleTween?.Kill();
            activeRoot.localScale = Vector3.one;
        }
    }
}
