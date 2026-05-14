using DG.Tweening;
using DG.Tweening.Core.Easing;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Deepak. date:20/05/2025
public class SpinWheel : MonoBehaviour
{
    public int forcedPrizeIndex; //fixedresult
    public event Action OnSpinFinished;

    [Header("SpinWheel Manager")]
    [SerializeField] private SpinWheelManager spinWheelManager;
    [SerializeField] private SpinResponse currentSpinResponse;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private GameObject resultpopup;
    [SerializeField] private Image blinkImage; // assign in inspector
    //[SerializeField] private Button collectButton;

    [Header("UI References")]
    [SerializeField] private RectTransform wheelTransform;
    [SerializeField] private Button spinButton;
    [SerializeField] private Button cutButton;

    [Header("Spin Settings")]
    [SerializeField] private float spinDuration = 5f;
    [SerializeField] private int fullRotations = 5;           //NUMBER OF ROTATION
    [SerializeField] private AnimationCurve easeCurve;

    [Header("Segments & Prizes")]
    [Tooltip("List of prizes in the same order as segments around the wheel (clockwise).")]

    public List<float> prizeValues = new List<float>
    {
        5.00f, 10.00f, 50.00f, //UH-Prize
        0f, 0f, 0f, 0f, // GOOD LUCK
        0.01f, 0.02f, 0.03f, 0.05f, // L-Prize
        0.10f, 0.15f, 0.20f, 0.25f, 0.50f, 0.77f, //M-Prize
        1.00f, 1.50f, 2.00f //H-Prize
    };
 

    //PROBABLITIES OF APPEARNCE OF EACH SEGMENT
    [Tooltip("Relative category weights: Low > GoodLuck > Medium > High > Ultra")]
    public float weightLow = 75f;
    public float weightGoodLuck = 40f;
    public float weightMedium = 15f;
    public float weightHigh = 7f;
    public float weightUltra = 2f;
    private bool isSpinning = false;
    public float pointerAngle = 90f;

    void Start()
    {
        cutButton.onClick.AddListener(DestroySpinWheel);

        spinButton.onClick.AddListener(() =>
        {
            if (!isSpinning)
                StartCoroutine(DoSpin());
        });
        Button btn = spinButton.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        Color disabled = colors.disabledColor;
        disabled.a = 1f;
        colors.disabledColor = disabled;
        btn.colors = colors;
        if (easeCurve == null || easeCurve.length == 0)
        {
            easeCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(1f, 1f)
            );
            easeCurve.keys[0].outTangent = 0f;
            easeCurve.keys[1].inTangent = 0f;
        }
        if (resultPanel != null)
            resultPanel.SetActive(false);

        //collectButton.onClick.AddListener(OnCollectClicked);
    }

    /*private void OnCollectClicked()
    {
        resultPanel.SetActive(false);
    }*/
    private IEnumerator DoSpin()
    {
        isSpinning = true;
        forcedPrizeIndex = -1;

        Coroutine spinLoop = StartCoroutine(SpinWhileWaiting());

        // Call API
        bool apiSuccess = false;
        yield return StartCoroutine(CallSpinApi(success => apiSuccess = success));
        //Debug.Log("forcedPrizeIndex  1: " + forcedPrizeIndex);
        StopCoroutine(spinLoop);

        //Debug.Log("forcedPrizeIndex  2: " + forcedPrizeIndex);
        float segmentAngle = 360f / prizeValues.Count;
        float targetSegmentAngle = (forcedPrizeIndex * segmentAngle) % 360f;
        float currentAngle = wheelTransform.localEulerAngles.z % 360f;
        //Debug.Log("forcedPrizeIndex  3: " + forcedPrizeIndex);
        float deltaToTarget = Mathf.DeltaAngle(currentAngle, targetSegmentAngle);
        if (deltaToTarget > 0) deltaToTarget -= 360f;
        //Debug.Log("forcedPrizeIndex  4: " + forcedPrizeIndex);
        float totalSpinAngle = deltaToTarget - (2 * 360f);
        float finalAngle = currentAngle + totalSpinAngle;
        //Debug.Log("forcedPrizeIndex  5: " + forcedPrizeIndex);
        Tween spinTween = wheelTransform
            .DORotate(new Vector3(0f, 0f, finalAngle), 4f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic);
        //Debug.Log("forcedPrizeIndex  6: " + forcedPrizeIndex);
        yield return spinTween.WaitForCompletion();

        wheelTransform.localEulerAngles = new Vector3(0f, 0f, targetSegmentAngle);
        //Debug.Log("forcedPrizeIndex  7: " + forcedPrizeIndex);
        if (forcedPrizeIndex == -1)
        {
            //Debug.Log("forcedPrizeIndex  8: " + forcedPrizeIndex);
            yield return null;
        }

        // 🌟 Blink effect at prize
        yield return StartCoroutine(BlinkAtPrize());
        //Debug.Log("forcedPrizeIndex  9: " + forcedPrizeIndex);
        // 🎁 Show result UI
        float prize = prizeValues[forcedPrizeIndex];
        yield return new WaitForSeconds(0.5f);
        //Debug.Log("forcedPrizeIndex  10: " + forcedPrizeIndex);
        if (prize > 0)
        {
            //Debug.Log("forcedPrizeIndex  11: " + forcedPrizeIndex);
            resultText.text = $"{prize:F2}";
            resultPanel.SetActive(true);
            MainMenuUIManager.Instance.DoTweenAnim(MainMenuUIManager.TweenType.SpinWheel, resultpopup.transform.gameObject, 1f, 0.5f);
        }
        else if (prize == 0)
        {
            spinButton.interactable = false;
            resultPanel.gameObject.transform.GetChild(0).gameObject.SetActive(false);
            var img = resultPanel.gameObject.transform.GetComponent<Image>();
            var c = img.color;
            c.a = 0f;
            img.color = c;
            resultPanel.SetActive(true);
            MainMenuUIManager.Instance.DoTweenAnim(MainMenuUIManager.TweenType.SpinWheel, resultpopup.transform.gameObject, 1f, 0.5f);
        }
        else
        {
            spinButton.interactable = false;
            resultPanel.gameObject.transform.GetChild(0).gameObject.SetActive(false);
            var img = resultPanel.gameObject.transform.GetComponent<Image>();
            var c = img.color;
            c.a = 0f;
            img.color = c;
            resultPanel.SetActive(true);
            MainMenuUIManager.Instance.DoTweenAnim(MainMenuUIManager.TweenType.SpinWheel, resultpopup.transform.gameObject, 1f, 0.5f);
        }
        //Debug.Log("forcedPrizeIndex  12: " + forcedPrizeIndex);
        isSpinning = false;
        OnSpinFinished?.Invoke();
        //Debug.Log("forcedPrizeIndex  13: " + forcedPrizeIndex);
    }

    private IEnumerator BlinkAtPrize()
    {

        blinkImage.gameObject.SetActive(true);

        CanvasGroup cg = blinkImage.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = blinkImage.gameObject.AddComponent<CanvasGroup>();

        for (int i = 0; i < 2; i++)
        {
            // Fade in
            yield return DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, 0.2f).WaitForCompletion();
            // Fade out
            yield return DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, 0.2f).WaitForCompletion();
        }

        blinkImage.gameObject.SetActive(false);
    }

    private Tween spinTween;

    private IEnumerator SpinWhileWaiting()
    {
        float spinSpeed = 720f; // degrees per second
        while (true)
        {
            float angle = wheelTransform.localEulerAngles.z - spinSpeed * Time.deltaTime;
            wheelTransform.localEulerAngles = new Vector3(0f, 0f, (angle + 360f) % 360f);
            yield return null;
        }
    }

    private IEnumerator CallSpinApi(Action<bool> onComplete)
    {
        string url = ApiEndpoints.Spin;

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.uploadHandler = new UploadHandlerRaw(new byte[0]); // Empty POST

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();

            if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, url, "", "POST", () => CallSpinApi(onComplete));
                yield break;
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Spin API failed: " + www.error);
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Network Error");
                forcedPrizeIndex = 0;
                onComplete?.Invoke(false);
                yield break;
            }

            try
            {
                currentSpinResponse = JsonConvert.DeserializeObject<SpinResponse>(www.downloadHandler.text);
                Debug.Log($"API Spin Result: Prize = {currentSpinResponse.prize}, Type = {currentSpinResponse.prizeType}");

                // 🔁 Map the prize to index in prizeValues
                forcedPrizeIndex = prizeValues.IndexOf(currentSpinResponse.prize);

                if (forcedPrizeIndex < 0)
                {
                    Debug.LogError("Prize from server not found in prizeValues list.");
                    CasinoUIManager.Instance.ShowErrorCanvas(1, "Invalid prize received.");
                    onComplete?.Invoke(false);
                    yield break;
                }

                DateTime nextUtc;
                if (DateTime.TryParse(currentSpinResponse.nextSpinAvailable, null,
                    System.Globalization.DateTimeStyles.AdjustToUniversal, out nextUtc))
                {
                    PlayerPrefs.SetString("FreeSpinNextUtcTicks", nextUtc.Ticks.ToString());
                    PlayerPrefs.Save();
                }

                Invoke(nameof(UpdateGameCoin), 1f);

                onComplete?.Invoke(true);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse Spin API response: " + e.Message);
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Response format error");
                onComplete?.Invoke(false);
            }
        }
    }
    void UpdateGameCoin()
    {
        GameBetServices.Instance.UpdateCoins(currentSpinResponse.newBalance);
    }

    private void DestroySpinWheel()
    {
        if(!isSpinning)
        {
            GlobleSoundManager.Instance.PlaySFX("Swipe");
            Destroy(gameObject);
        }
        
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Ensure prizeValues.Count == number of sections in spin wheel. 
        if (prizeValues != null && prizeValues.Count != 20)
            Debug.LogWarning("SpinWheel: prizeValues should contain exactly 22 entries.");
    }
#endif
}

[Serializable]
public class SpinResponse
{
    public string id;
    public string userId;
    public float prize;
    public string prizeType;
    public string spinTime;
    public bool isJackpot;
    public float? multiplier;
    public float newBalance;
    public string status;
    public int dailySpinCount;
    public int totalSpinCount;
    public int maxDailySpins;
    public string nextSpinAvailable;
}