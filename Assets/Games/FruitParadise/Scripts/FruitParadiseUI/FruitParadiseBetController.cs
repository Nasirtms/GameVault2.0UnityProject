using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FruitParadiseBetController : MonoBehaviour
{
    //[SerializeField] private GameObject image1;
    //[SerializeField] private GameObject image2;

    [SerializeField] private float rotationStep = 13.75f;
    [SerializeField] private float rotationDuration = 0.25f;

    [Header("Chip & Bet Settings")]
    [SerializeField]
    private float[] chipValues = new float[] {
        0.18f, 0.54f, 0.90f, 1.80f,
        2.70f, 3.60f, 4.50f, 5.40f,
        6.30f, 7.20f, 8.10f, 9.00f
    };

    private int currentIndex = 0;


    [Header("UI References")]
    [SerializeField] private TMP_Text chipText;

    public delegate void FruitParadiseBetControllerEvents();
    public static event FruitParadiseBetControllerEvents OnBetValueChanged;

    private void Start()
    {
        UpdateBetUI();
    }

    // Replace your rotationStep usage with absolute mapping
    [SerializeField] private float baseAngle = -220f;   // index 0
    [SerializeField] private float maxAngle = -360f;   // last index

    private float AngleForIndex(int index)
    {
        int last = chipValues.Length - 1;
        if (last <= 0) return baseAngle;

        // Evenly distribute angles from -220 to -360
        float t = index / (float)last;                 // 0..1
        float angle = Mathf.Lerp(baseAngle, maxAngle, t);

        // Clamp endpoints to kill float fuzz
        if (index == 0) angle = baseAngle;
        else if (index == last) angle = maxAngle;

        return angle;
    }

    private void RotateRingToAngle(GameObject go, float targetAngle)
    {
        go.transform.DOKill(); // stop any previous tweens to avoid blending/overshoot
        go.transform.DORotate(new Vector3(0f, 0f, targetAngle), rotationDuration, RotateMode.Fast);
    }

    // image2 follows [-220 .. -360], image1 mirrors it
    private void RotateBothToIndex(int index)
    {
        float a2 = AngleForIndex(index);
        float a1 = -a2; // mirror for the opposite ring

        //RotateRingToAngle(image2, a2);
        //RotateRingToAngle(image1, a1);
    }

    public void IncreaseChipValue()
    {
        int maxIndex = chipValues.Length - 1;
        currentIndex = (currentIndex < maxIndex) ? currentIndex + 1 : 0; // wrap
        RotateBothToIndex(currentIndex);
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public void DecreaseChipValue()
    {
        int maxIndex = chipValues.Length - 1;
        currentIndex = (currentIndex > 0) ? currentIndex - 1 : maxIndex; // wrap
        RotateBothToIndex(currentIndex);
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    public void SetMaxBet()
    {
        int maxIndex = chipValues.Length - 1;

        if (currentIndex == maxIndex)
        {
            // Already at -360: do the extra sweep -220 -> -360 as you wanted
            Sequence seq = DOTween.Sequence();
            //seq.Append(image2.transform.DORotate(new Vector3(0, 0, baseAngle), rotationDuration, RotateMode.Fast));
            //seq.Join(image1.transform.DORotate(new Vector3(0, 0, -baseAngle), rotationDuration, RotateMode.Fast));
            //seq.Append(image2.transform.DORotate(new Vector3(0, 0, maxAngle), rotationDuration, RotateMode.Fast));
            //seq.Join(image1.transform.DORotate(new Vector3(0, 0, -maxAngle), rotationDuration, RotateMode.Fast));
        }
        else
        {
            currentIndex = maxIndex;
            RotateBothToIndex(currentIndex);
        }

        currentIndex = maxIndex;
        OnBetValueChanged?.Invoke();
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        float chip = chipValues[currentIndex];
        chipText.text = chip.ToString("0.00");
    }

    public float GetCurrentBet() => chipValues[currentIndex];
}
