using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnScreenKeyboardManager : MonoBehaviour
{
    public static OnScreenKeyboardManager Instance;
    //[SerializeField] TextMeshProUGUI textBox;
    //[SerializeField] TextMeshProUGUI printBox;

    [Header("UI References")]
    public KeyboardController keyboard;
    public Button blockerBG;
    public GameObject textPanel;
    public TextMeshProUGUI textPreviewBox;
    public GameObject clickBlocker;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip keyTapAudioClip;

    [Header("Runtime Values")]
    public bool useOnScreenKeyboard;
    public OnScreenKeyboardActivator currentActivator;
    public TMP_InputField currentInpuField;

    private RectTransform keyboardRectTransform;
    private RectTransform textPanelRectTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }

    private void Start()
    {
        useOnScreenKeyboard = DeviceDetector.IsMobile();

        blockerBG.onClick.AddListener(HideKeyboard);
        textPreviewBox.text = "";
        //printBox.text = "";
        //textBox.text = "";

        keyboardRectTransform = keyboard.GetComponent<RectTransform>();
        textPanelRectTransform = textPanel.GetComponent<RectTransform>();
    }

    public void DeleteLetter()
    {
        if (currentInpuField == null)
            return;

        if (currentInpuField.text.Length != 0) {
            currentInpuField.text = currentInpuField.text.Remove(currentInpuField.text.Length - 1, 1);
            //textPreviewBox.text = currentInpuField.text;
            SetPreviewText(currentInpuField.text);
        }
    }

    public void AddLetter(string letter)
    {
        if (currentInpuField == null)
            return;

        currentInpuField.text = currentInpuField.text + letter;
        //textPreviewBox.text = currentInpuField.text;
        SetPreviewText(currentInpuField.text);
    }

    public void SubmitWord()
    {
        if (currentInpuField == null)
            return;

        currentInpuField.onSubmit.Invoke(currentInpuField.text);

        //printBox.text = textBox.text;
        //textBox.text = "";
        // Debug.Log("Text submitted successfully!");

        HideKeyboard();
    }

    public void ShowKeyboard(OnScreenKeyboardActivator activator)
    {
        if (!useOnScreenKeyboard)
            return;

        if (activator == null)
            return;

        clickBlocker.SetActive(true);
        blockerBG.gameObject.SetActive(true);
        textPanel.SetActive(true);
        keyboard.gameObject.SetActive(true);
        currentActivator = activator;
        currentInpuField = currentActivator.inputField;
        keyboard.ShowSmallLetters();
        //textPreviewBox.text = currentInpuField.text;
        SetPreviewText(currentInpuField.text);

        keyboardRectTransform.anchoredPosition = new Vector2(0, -592.78f);
        textPanelRectTransform.anchoredPosition = new Vector2(0, -100);

        keyboardRectTransform.DOAnchorPosY(0, .3f).SetEase(Ease.InOutCirc);
        textPanelRectTransform.DOAnchorPosY(492.78f, .3f).SetEase(Ease.InOutCirc).OnComplete(() =>
        {
            keyboardRectTransform.anchoredPosition = new Vector2(0, 0);
            textPanelRectTransform.anchoredPosition = new Vector2(0, 492.78f);
            clickBlocker.SetActive(false);
        });
    }

    public void HideKeyboard()
    {
        clickBlocker.SetActive(true);

        keyboardRectTransform.anchoredPosition = new Vector2(0, 0);
        textPanelRectTransform.anchoredPosition = new Vector2(0, 492.78f);

        keyboardRectTransform.DOAnchorPosY(-592.78f, .3f).SetEase(Ease.InOutCirc);
        textPanelRectTransform.DOAnchorPosY(-100, .3f).SetEase(Ease.InOutCirc).OnComplete(() =>
        {
            keyboardRectTransform.anchoredPosition = new Vector2(0, -592.78f);
            textPanelRectTransform.anchoredPosition = new Vector2(0, -100);

            textPreviewBox.text = "";
            keyboard.gameObject.SetActive(false);
            currentActivator = null;
            currentInpuField = null;
            blockerBG.gameObject.SetActive(false);
            textPanel.SetActive(false);

            clickBlocker.SetActive(false);
        });
    }

    public void Copy()
    {
        if (!useOnScreenKeyboard)
            return;

        if (currentActivator == null)
            return;

        if (currentInpuField == null)
            return;

        WebGLClipboard.Instance.Copy(currentInpuField.text);

        OnScreenKeyboardManager.Instance.PlayTapSound();
    }

    public void Paste()
    {
        if (!useOnScreenKeyboard)
            return;

        if (currentActivator == null)
            return;

        if (currentInpuField == null)
            return;

        WebGLClipboard.Instance.Paste((str) =>
        {
            AddLetter(str);
        });

        OnScreenKeyboardManager.Instance.PlayTapSound();
    }

    public void ClearTextbox()
    {
        if (!useOnScreenKeyboard)
            return;

        if (currentActivator == null)
            return;

        if (currentInpuField == null)
            return;

        currentInpuField.text = "";
        //textPreviewBox.text = currentInpuField.text;
        SetPreviewText(currentInpuField.text);

        OnScreenKeyboardManager.Instance.PlayTapSound();
    }

    void SetPreviewText(string str)
    {
        textPreviewBox.text = str;

        if (currentActivator != null)
        {
            if (currentActivator.hiddenCharacterField)
            {
                textPreviewBox.text = new string('x', textPreviewBox.text.Length);
            }
        }
    }

    public void PlayTapSound()
    {
        if (audioSource != null)
            audioSource.PlayOneShot(keyTapAudioClip);
    }
}
