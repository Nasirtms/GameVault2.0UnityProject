using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PasswordChangeManager : MonoBehaviour
{
    [SerializeField] private Button onConfirm;
    [SerializeField] private Button onCancel;

    [SerializeField] private TMP_InputField oldPass;
    [SerializeField] private TMP_InputField newPass;
    [SerializeField] private TMP_InputField confirmPass;

    string _oldPass;
    string _newPass;
    string _confirmPass;

    private void Start()
    {
        onConfirm.onClick.AddListener(() => UpdatePassword());
        onCancel.onClick.AddListener(() => onPasswordCancel());
    }

    private void onPasswordCancel()
    {
        oldPass.text = "";
        newPass.text = "";
        confirmPass.text = "";

        var manager = transform.GetComponent<MainMenuUIManager>();

        if (manager != null)
            manager.HidePopup(manager.changePasswordPopup);
    }

    private void UpdatePassword()
    {
        _oldPass = oldPass.text;
        _newPass = newPass.text;
        _confirmPass = confirmPass.text;

        // ❌ Show error if any field is empty
        if (string.IsNullOrEmpty(_oldPass))
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Enter current password.");
            return;
        }
        else if (string.IsNullOrEmpty(_newPass))
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Enter new password.");
            return;
        }
        else if (string.IsNullOrEmpty(_confirmPass))
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Confirm new password.");
            return;
        }

        bool hasLetter = false;
        bool hasNumber = false;

        foreach (char c in _newPass)
        {
            if (char.IsLetter(c))
            {
                hasLetter = true;
            }
            if (char.IsNumber(c))
            {
                hasNumber = true;
            }
        }

        if (!(hasLetter && hasNumber))
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Password must contain both letters and numbers");
            return;
        }

        // ❌ Mismatched new/confirm password
        if (!_newPass.Equals(_confirmPass))
        {
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Passwords do not match");
            return;
        }

        // ✅ All good — proceed
        StartCoroutine(ChangePassword());
    }

    IEnumerator ChangePassword()
    {
        CasinoUIManager.Instance.ShowErrorCanvas(0, ""); // Clear UI

        var body = new
        {
            oldPassword = _oldPass,
            newPassword = _newPass,
            confirmPassword = _confirmPass
        };

        string jsonBody = JsonUtility.ToJson(new SerializableClasses.ChangePasswordRequest(body));

        UnityWebRequest request = new UnityWebRequest(ApiEndpoints.changePawword, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        foreach (var header in ApiEndpoints.GetAuthHeaders())
            request.SetRequestHeader(header.Key, header.Value);

        yield return request.SendWebRequest();
        Debug.Log("🔁 Password Manager Response: " + request.downloadHandler.text);

        if (request.responseCode == 401)
        {
            yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.changePawword, jsonBody, "POST", () => ChangePassword());
            yield break;
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            // ✅ Deserialize the server response
            SerializableClasses.ChangePasswordResponse response =
                JsonConvert.DeserializeObject<SerializableClasses.ChangePasswordResponse>(request.downloadHandler.text);

            if (response.status == 200)
            {
                Debug.Log("✅ Password changed successfully: " + response.message);
                CasinoUIManager.Instance.ShowErrorCanvas(1, response.message);
                //UserPrefsManager.SaveCredentials(_newPass);

                Debug.Log("🔐 Updated PlayerPrefs password to: " + _newPass);
                onPasswordCancel();
                UnitySessionManager.Instance.ChangePasswordLogout();
            }
            else if (response.status == 201)
            {
                Debug.LogWarning("⚠️ Password change issue: " + response.message);
                CasinoUIManager.Instance.ShowErrorCanvas(1, response.message);
            }
            else
            {
                Debug.LogError("❌ Unexpected status: " + response.status + " | " + response.message);
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Unexpected error: " + response.message);
            }
        }
        else
        {
            Debug.LogError($"❌ HTTP Error ({request.responseCode}): {request.error}");
            CasinoUIManager.Instance.ShowErrorCanvas(1, "Change password failed (network error)");
        }

    }


    public void SelectPasswordInputField()
    {
        JSInputHandler.exitFullscreenIfActive();
    }

}

