using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static SerializableClasses;

public class ProfilePanelManager : MonoBehaviour
{
    [Header("UI Elements")]

    public TextMeshProUGUI usernameText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI userIdText;

    public Image profileImage;

    public Button copyIdButton;
    public Button UpdateDataConfirm;

    public GameObject inputFieldIcon;
    public GameObject placeholderIcon;



    private void Start()
    {
        LoadUserData();

        // Subscribe to user data update event
        MainMenuUIManager.Instance.OnUserDataUpdated.AddListener(LoadUserData);


        copyIdButton.onClick.AddListener(OnCopyIdClicked);
    }

    private void OnDestroy()
    {
        // Clean up listeners to avoid memory leaks
        if (MainMenuUIManager.Instance != null)
        {
            MainMenuUIManager.Instance.OnUserDataUpdated.RemoveListener(LoadUserData);
        }
    }

    private void LoadUserData()
    {
        var manager = MainMenuUIManager.Instance;
        if (manager == null) return;

        usernameText.text = manager.Username;

        if (UserManager.Instance != null)
        {
            string coin = UserManager.Instance.FormatCoins(manager.Coins);
            coinsText.text = $"<color=#ffc208>{coin}</color>";
        }



        userIdText.richText = true;
        userIdText.text = "<color=#006bff>ID:</color> <color=#11b300>" + manager.UserID + "</color>";

        profileImage.sprite = manager.AvatarImage;
    }

    public void OnCopyIdClicked()
    {
        GlobleSoundManager.Instance.PlaySFX("ProfileClick");
        JSInputHandler.CopyToClipboard(MainMenuUIManager.Instance.UserID);

        Debug.Log("User ID copied!");
        CasinoUIManager.Instance.ShowErrorCanvas(1, "COPIED");
    }

}
