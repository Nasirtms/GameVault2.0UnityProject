using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static SerializableClasses;

public class FeedbackPanel : MonoBehaviour
{
    public Transform popup;
    public Transform thankyouPopup;
    public FeedbackStar[] stars; 
    public TMP_InputField feedbackTextbox;
    public Button sendButton;
    public Button closeButton;
    public bool isActive = false;

    private bool popupAnimPlaying = false;

    private int starsGiven = 5;
    public int StarsGiven
    {
        get
        {
            return starsGiven;
        }
        set
        {
            starsGiven = value;
            SetStarsCountUI(starsGiven);
        }
    }

    private void Awake()
    {
        //Reset fields
        feedbackTextbox.text = "";
        StarsGiven = 5;

        sendButton.onClick.AddListener(SendFeedback);
        closeButton.onClick.AddListener(ClosePanel);
        feedbackTextbox.onSelect.AddListener(FeedbackInputFieldSelected);

        for (int i = 0; i < stars.Length; i++)
        {
            int x = i + 1;
            stars[i].button.onClick.AddListener(() => StarClicked(x));
        }
    }

    private void OnEnable()
    {
        OpenPanel();
    }

    //private void Start()
    //{
    //    StarsGiven = 5;
    //}

    public void OpenPanel()
    {
        gameObject.SetActive(true);

        //Reset fields
        feedbackTextbox.text = "";
        StarsGiven = 5;

        popupAnimPlaying = true;
        popup.DOKill();
        popup.localScale = Vector3.zero;
        popup.DOScale(1, .4f).SetEase(Ease.OutBack).OnComplete(() => popupAnimPlaying = false);

        thankyouPopup.DOKill();
        thankyouPopup.localScale = Vector3.zero;

        isActive = true;
    }

    public void ClosePanel()
    {
        if (!popupAnimPlaying)
        {
            FeedbackPanelSumbitOrClose();

            popupAnimPlaying = true;

            popup.DOKill();
            popup.localScale = Vector3.one;
            popup.DOScale(0, .4f).SetEase(Ease.InBack).OnComplete(() => { popupAnimPlaying = false; isActive = false; gameObject.SetActive(false); });
        }
    }

    void FeedbackInputFieldSelected(string str)
    {
        JSInputHandler.OnFeedbackPanelOpen();
    }

    void SetStarsCountUI(int count)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].SetStarFill(i < count);
        }
    }

    public void StarClicked(int number)
    {
        StarsGiven = number;
    }

    void SendFeedback()
    {
        Debug.Log("Sending Feedback: " + StarsGiven + " " + feedbackTextbox.text);

        StartCoroutine(SendFeedbackAPI(StarsGiven, feedbackTextbox.text));

        if (!popupAnimPlaying)
        {
            popupAnimPlaying = true;

            popup.DOKill();
            popup.localScale = Vector3.one;

            popup.DOScale(0, .4f).SetEase(Ease.InBack).OnComplete(() => {
                thankyouPopup.DOScale(1, .4f).SetEase(Ease.OutBack).OnComplete(() => {
                    DOVirtual.DelayedCall(2, () =>
                    {
                        thankyouPopup.DOScale(0, .4f).SetEase(Ease.InBack).OnComplete(() =>
                        {
                            popupAnimPlaying = false;
                            isActive = false;
                            gameObject.SetActive(false);
                        });
                    });
                });
            });

        }
    }

    void FeedbackPanelSumbitOrClose()
    {
        JSInputHandler.SumbilOrCloseFeedbackPanel();
    }

    private IEnumerator SendFeedbackAPI(int stars, string feedbackText)
    {
        string url = ApiEndpoints.SendFeedback;

        var payload = new FeedbackPayload
        {
            ratingStar = stars,
            feedback = feedbackText
        };

        string jsonBody = JsonConvert.SerializeObject(payload);

        Debug.Log("Sending Feedback: " + jsonBody);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
            www.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                FeedbackPanelSumbitOrClose();

                try
                {
                    //var response = JsonConvert.DeserializeObject<FeedbackPayload>(www.downloadHandler.text);
                    Debug.Log($"Feedback Sent Successfully.. Response: {www.downloadHandler.text}");
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"Feedback Sent Successfully.. Response: {ex.Message}");
                }
            }
            else if (www.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(www, url, jsonBody, "POST", () => SendFeedbackAPI(StarsGiven, feedbackTextbox.text));
                yield break;
            }

            else
            {
                Debug.LogError($"Sending Feedback Failed: {www.error}");
            }
        }
    }
}

[System.Serializable]
public class FeedbackPayload
{
    public int ratingStar;
    public string feedback;
}