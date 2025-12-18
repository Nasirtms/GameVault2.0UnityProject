// WheelManager.cs
using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using Coffee.UIEffects; // ← DOTween namespace

public class WheelManager_RPS : MonoBehaviour
{
    [SerializeField] private GameManager_RPS gameManager;
    [SerializeField] private ComputerChoiceManager_RPS compChoiceManager;
    [SerializeField] private HeartbeatAnimation heartAnimation;

    [Header("Wheel Setup")]
    [SerializeField] private Image glowImage;
    [SerializeField] public Transform pointerPivot;      // Empty GameObject at wheel center
    [SerializeField] private Transform[] segments;      // 20 segment Transforms (clockwise)
    [SerializeField] private GameObject[] ruby_Diamond;
    [SerializeField] private Text chosenIndex;
    [SerializeField] public bool stopSpin;

    [SerializeField] private GameObject pointerNormal;
    [SerializeField] private GameObject pointerSpecial;
    public  UIShiny UIShiny;

    private readonly int[] specialSegments = { 0, 4, 8, 12, 16 };
    private int previousSegment = -1;



    // Predefined payout tables (multipliers)
    private readonly float[][] payoutTables = new float[][]
    {
        new float[] {300,1,4,2,25,3,5,2,150,1,7,2,150,3,10,2,50,1,12,2},           // L1
        new float[] {300,1.5f,6,3,37.5f,4.5f,7.5f,3,150,1.5f,10.5f,3,150,4.5f,15,3,75,1.5f,18,3}, // L2
        new float[] {300,1.5f,6,3,37.5f,4.5f,7.5f,3,150,1.5f,10.5f,3,150,4.5f,15,3,75,1.5f,18,3}, // L3
        new float[] {300,2.5f,10,5,62.5f,7.5f,12.5f,5,150,2.5f,17.5f,5,150,7.5f,25,5,125,2.5f,30,5},// L4
        new float[] {300,3,12,6,75,9,15,6,150,3,21,6,150,9,30,6,150,3,36,6}         // L5+
    };


    public IEnumerator SpinWheel(int level, Action<float> onComplete)
    {
        UIManager_RPS.Instance.PlaySound("Spin");
        gameManager.LockBet();

        UIManager_RPS.Instance.WheelSpinPlayMusic("SpinStop");
        float[] table = payoutTables[Mathf.Clamp(level - 1, 0, payoutTables.Length - 1)];
        int chosen = gameManager.chosen;
        if (chosen < 0 || chosen >= segments.Length)
        {
            yield break;
        }
        chosenIndex.text = chosen.ToString();

        float segmentAngle = 360f / segments.Length;
        float loopSpeed = 360f;
        pointerPivot.localEulerAngles = Vector3.zero;
        float currentAngle = 0f;

        // Phase 1: Loop spin until stopSpin is triggered
        while (!stopSpin)
        {
            float deltaAngle = loopSpeed * Time.deltaTime;
            currentAngle += deltaAngle;
            pointerPivot.Rotate(0f, 0f, -deltaAngle);
            float normalizedAngle = (currentAngle % 360f + 360f) % 360f;
            int segmentIndex = Mathf.FloorToInt(normalizedAngle / segmentAngle) % segments.Length;

            if (segmentIndex != previousSegment)
            {
                previousSegment = segmentIndex;

                bool isSpecial = Array.IndexOf(specialSegments, segmentIndex) >= 0;
                pointerNormal.SetActive(!isSpecial);
                pointerSpecial.SetActive(isSpecial);
            }
            yield return null;
        }

        // Phase 2: Deceleration to target angle
        float finalRotation = chosen * segmentAngle;
        float baseSpins = Mathf.Floor(currentAngle / 360f);
        int extraSpins = UnityEngine.Random.Range(3, 6);
        float targetAngle = (baseSpins + extraSpins) * 360f + finalRotation;


        float remainingAngle = targetAngle - currentAngle;

        while (currentAngle < targetAngle)
        {
            // Gradually slow down as we approach target
            float t = (targetAngle - currentAngle) / remainingAngle; // goes from 1 → 0
            float speed = Mathf.Lerp(360f, 50f, 1 - t); // starts at 60, ends at 5

            float deltaAngle = speed * Time.deltaTime;
            currentAngle += deltaAngle;
            pointerPivot.Rotate(0f, 0f, -deltaAngle);
            float normalizedAngle = (currentAngle % 360f + 360f) % 360f;
            int segmentIndex = Mathf.FloorToInt(normalizedAngle / segmentAngle) % segments.Length;

            if (segmentIndex != previousSegment)
            {
                previousSegment = segmentIndex;

                bool isSpecial = Array.IndexOf(specialSegments, segmentIndex) >= 0;
                pointerNormal.SetActive(!isSpecial);
                pointerSpecial.SetActive(isSpecial);
            }
            yield return null;
        }

        pointerPivot.localEulerAngles = new Vector3(0, 0, -targetAngle);
        if (chosen == 8)
        {
            heartAnimation.PlayHeartbeat(ruby_Diamond[1].gameObject);
            UIShiny = ruby_Diamond[1].gameObject.GetComponent<UIShiny>();
            if (UIShiny != null)
            {
                UIShiny.enabled = true;
            }
        }
        else if (chosen == 12)
        {
            heartAnimation.PlayHeartbeat(ruby_Diamond[2].gameObject);
            UIShiny = ruby_Diamond[2].gameObject.GetComponent<UIShiny>();
            if (UIShiny != null)
            {
                UIShiny.enabled = true;
            }
        }
        else if (chosen == 0)
        {
            heartAnimation.PlayHeartbeat(ruby_Diamond[0].gameObject);
            UIShiny = ruby_Diamond[0].gameObject.GetComponent<UIShiny>();
            if (UIShiny != null)
            {
                UIShiny.enabled = true;
            }
        }
        UIManager_RPS.Instance.WheelSpinStopMusic("SpinStop");
        onComplete?.Invoke(table[chosen]);
        gameManager.UnlockBet();
    }
    public float[] UpdateSegmentLabels(float currentBet, int wheelLevel)
    {
        float[] table = payoutTables[Mathf.Clamp(wheelLevel - 1, 0, payoutTables.Length - 1)];
        for (int i = 0; i < segments.Length; i++)
        {
            var txt = segments[i].GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null)
            {
                txt.text = (table[i] * currentBet).ToString("F2");
            }
        }
        return table;
    }
    public IEnumerator FadeGlow(float duration = 0.5f, float delayBetween = 0.2f)
    {
        if (glowImage == null) yield break;

        glowImage.gameObject.SetActive(true);
        Color originalColor = glowImage.color;
        glowImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        yield return glowImage.DOFade(0.7f, duration).SetEase(Ease.InOutSine).WaitForCompletion();
        yield return new WaitForSeconds(delayBetween);
        yield return glowImage.DOFade(0f, duration).SetEase(Ease.InOutSine).WaitForCompletion();
        glowImage.gameObject.SetActive(false);
        compChoiceManager.StartCycle(0.1f);
    }
}
