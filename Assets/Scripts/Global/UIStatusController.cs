using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIStatusController : MonoBehaviour
{
    //public static UIStatusController Instance;

    [Header("Loader Elements")]
    public GameObject loadingPanel;
    public Image spinnerImage;
    public TextMeshProUGUI loadingText;

    [Header("Message Elements")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;
    public Animator messageAnimator;

    [Header("Settings")]
    public float rotationSpeed = 200f;
    public float dotCycleSpeed = 0.5f;
    public float defaultMessageDuration = 2f;

    private Coroutine loadingRoutine;
    private Coroutine messageRoutine;
    private bool isLoading = false;


    private void Awake()
    {
        //if (Instance != null && Instance != this)
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        //Instance = this;

        loadingPanel.SetActive(false);
        messagePanel.SetActive(false);
    }

    //public void Update()
    //{
    //    if (isLoading && spinnerImage != null)
    //    {
    //        spinnerImage.rectTransform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
    //    }
    //}

    //================== Loader ==================//

    private Coroutine fillCoroutine;
    private bool isFilling = false;

    public void ShowLoader()
    {
        if (isLoading) return;

        isFilling = true;
        isLoading = true;
        loadingPanel.SetActive(true);

        if (fillCoroutine != null)
            StopCoroutine(fillCoroutine);
        if (loadingRoutine != null)
            StopCoroutine(loadingRoutine);

        spinnerImage.fillAmount = 0;

        fillCoroutine = StartCoroutine(fillAnimation());
        loadingRoutine = StartCoroutine(AnimateLoadingText());
    }

    private IEnumerator fillAnimation()
    {
        while (isFilling)
        {
            spinnerImage.fillAmount += 0.01f;

            yield return new WaitForSeconds(0.01f);

            if (spinnerImage.fillAmount >= 1f)
            {
                spinnerImage.fillAmount = 0f;
            }
        }
    }

    public void HideLoader()
    {
        if (!isLoading) return;

        isFilling = false;
        isLoading = false;
        loadingPanel.SetActive(false);

        if (loadingRoutine != null)
            StopCoroutine(loadingRoutine);

        if (fillCoroutine != null)
            StopCoroutine(fillCoroutine);
    }

    private IEnumerator AnimateLoadingText()
    {
        string baseText = "Loading";
        int dotCount = 0;

        while (isLoading)
        {
            loadingText.text = baseText + new string('.', dotCount);
            dotCount = (dotCount + 1) % 4;
            yield return new WaitForSeconds(dotCycleSpeed);
        }
    }

    //================== Message Popup ==================//

    public void ShowMessage(string message, float duration = -1f)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messagePanel.SetActive(true);
        messageText.text = message;
        messageAnimator.enabled = true;

        messageRoutine = StartCoroutine(HideMessageAfterDelay(duration < 0 ? defaultMessageDuration : duration));
    }

    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageAnimator.enabled = false;
        messagePanel.SetActive(false);
    }
}
