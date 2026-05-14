using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnScreenKeyboardManager : MonoBehaviour
{
    public static OnScreenKeyboardManager Instance;
    //[SerializeField] TextMeshProUGUI textBox;
    //[SerializeField] TextMeshProUGUI printBox;

    public KeyboardController keyboard;
    public Button blockerBG;
    public GameObject textPanel;
    public TextMeshProUGUI textPreviewBox;

    public OnScreenKeyboardActivator currentActivator;
    public TMP_InputField currentInpuField;

    public bool useOnScreenKeyboard;

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

        blockerBG.gameObject.SetActive(true);
        textPanel.SetActive(true);
        currentActivator = activator;
        currentInpuField = currentActivator.inputField;
        keyboard.gameObject.SetActive(true);
        keyboard.ShowSmallLetters();
        //textPreviewBox.text = currentInpuField.text;
        SetPreviewText(currentInpuField.text);
    }

    public void HideKeyboard()
    {
        textPreviewBox.text = "";
        keyboard.gameObject.SetActive(false);
        currentActivator = null;
        currentInpuField = null;
        blockerBG.gameObject.SetActive(false);
        textPanel.SetActive(false);
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
}
