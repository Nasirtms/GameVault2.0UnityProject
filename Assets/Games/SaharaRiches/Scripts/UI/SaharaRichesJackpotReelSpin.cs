using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
public enum SaharaRichesJackpotType { Mini = 0, Minor = 1, Major = 2, Grand = 3 }

public class SaharaRichesJackpotReelSpin : MonoBehaviour
{
    [SerializeField] private RectTransform prizeReel;

    [SerializeField] private float slotHeight = 256f;
    [SerializeField] private float reelHeight = 2048f;
    [SerializeField] private float minScrollSpeed = 1000f;
    [SerializeField] private float maxScrollSpeed = 1000f;
    [SerializeField] private float minSpinDuration = 2.5f;
    [SerializeField] private float maxSpinDuration = 2.5f;
    [SerializeField] private float minStopTime = 2.5f;
    [SerializeField] private float maxStopTime = 2.5f;

    private float maskedContainerHeight;
    private bool isSpinning = false;


    private void Start()
    {
        RectTransform maskedContainer = prizeReel.transform.parent.GetComponent<RectTransform>();
        maskedContainerHeight = maskedContainer.rect.height;
    }

    public void StartSpin(SaharaRichesJackpotType result)
    {
        if (!isSpinning)
            StartCoroutine(SpinRoutine((int)result));
    }

    IEnumerator SpinRoutine(int targetIndex)
    {
        float spinDuration = Random.Range(minSpinDuration, maxSpinDuration);
        float scrollSpeed = Random.Range(minScrollSpeed, maxScrollSpeed);

        isSpinning = true;

        float currentY = 0f;
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            currentY -= scrollSpeed * Time.deltaTime;

            if (currentY <= -reelHeight/2)
            {
                currentY = 0;
            }

            prizeReel.anchoredPosition = new Vector2(0, currentY);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float stopTime = Random.Range(minStopTime, maxStopTime);
        float t = 0f;
        float startY = currentY;

        int stopIndex = targetIndex + 4;
        float targetY = -stopIndex * slotHeight + maskedContainerHeight/3f;

        while (t < stopTime)
        {
            float eased = EaseOutCubic(t / stopTime);

            if (prizeReel.anchoredPosition.y <= -reelHeight / 2 && stopIndex % 4 == 3)
            {
                startY = 0;
                prizeReel.anchoredPosition = new Vector2(0, startY);
                targetY = -targetIndex * slotHeight + maskedContainerHeight/3f;
            }

            float interpY = Mathf.Lerp(startY, targetY, eased);
            prizeReel.anchoredPosition = new Vector2(0, interpY);

            t += Time.deltaTime;

            yield return null;
        }

        prizeReel.anchoredPosition = new Vector2(0, targetY);
        isSpinning = false;
    }

    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3);
    }

    public bool IsSpinning() => isSpinning;
}
