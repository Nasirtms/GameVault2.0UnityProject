using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.U2D;
using UnityEngine.UI;

public class AvatarImageLoader : MonoBehaviour
{
    [Header("Setup")]
    public GameObject avatarButtonPrefab;
    public Transform avatarGridContainer;

    [Header("Sample Data")]
    public List<string> avatarUrls; // Add all avatar image URLs here or load from API

    private Dictionary<string, Texture2D> cachedTextures = new();
    public string _url;
    public Sprite _sprite;
    bool founUserAvatarImage =false;

    void Start()
    {
        if (SceneManagement.profile_iconUrls == null || SceneManagement.profile_iconUrls.Count == 0)
        {
            return;
        }

        avatarUrls.AddRange(SceneManagement.profile_iconUrls);

        //StartCoroutine(LoadImagesInOrder()); // comment this because of juwa 2.0 full body avatar ____ nasir_comment __ 12/10/2025
    }

    IEnumerator LoadImagesInOrder()
    {
        string userAvatarUrl = UserManager.Instance.AvatarUrl;

        // First pass: Find and download the matching user avatar first
        if (!string.IsNullOrEmpty(userAvatarUrl))
        {
            foreach (string url in avatarUrls)
            {
                if (IsMatchingUrl(url, userAvatarUrl))
                {
                    yield return StartCoroutine(LoadAvatarImage(url));
                    break; // Only one match expected
                }
            }
        }

        // Second pass: Load all other avatars
        foreach (string url in avatarUrls)
        {
            if (!IsMatchingUrl(url, UserManager.Instance.AvatarUrl))
            {
                yield return StartCoroutine(LoadAvatarImage(url));
            }
        }

        if (founUserAvatarImage)
        {
            //Debug.Log("✅ Profile Image URL Set: " + _url);
            UserManager.Instance.SetAvatarDownloadedImage(_sprite);
        }
    }

    bool IsMatchingUrl(string url1, string url2)
    {
        try
        {
            string file1 = Path.GetFileName(new Uri(url1.Trim()).AbsolutePath);
            string file2 = Path.GetFileName(new Uri(url2.Trim()).AbsolutePath);
            return string.Equals(file1, file2, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }


    IEnumerator LoadAvatarImage(string url)
    {
        string fileName = GetFileNameFromUrl(url);
        string localPath = Path.Combine(Application.persistentDataPath, fileName);

        Texture2D texture = null;

        // 🔍 Check if image is already cached locally
        if (File.Exists(localPath))
        {
            byte[] imageData = File.ReadAllBytes(localPath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            Debug.Log($"✅ Loaded from cache: {fileName}");
        }
        else
        {
            // 🌐 Download from web
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                // 💾 Save to cache
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(localPath, bytes);
                //Debug.Log($"⬇️ Downloaded and cached: {fileName}");
            }
            else
            {
                Debug.LogError($"❌ Failed to download image from {url}: {request.error}");
                yield break;
            }
        }

        // ✅ Convert to Sprite for Image component
        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        // 🧩 Instantiate prefab and assign Image sprite
        GameObject avatarGO = Instantiate(avatarButtonPrefab, avatarGridContainer);
        Image imageComponent = avatarGO.GetComponent<Image>();
        if (imageComponent != null)
            imageComponent.sprite = sprite;


        var imageUrl = avatarGO;
        var outline = avatarGO.transform.GetChild(0).gameObject;
        outline.SetActive(false);

        // Log the raw URLs
        //Debug.Log($"UserManager.Instance.AvatarUrl : {UserManager.Instance.AvatarUrl}");
        //Debug.Log($"UserManager URL               : {url}");

        // Normalize and compare by filename (or path, depending on strictness)
        if (!string.IsNullOrEmpty(UserManager.Instance.AvatarUrl))
        {
            try
            {
                Uri avatarUri = new Uri(UserManager.Instance.AvatarUrl.Trim());
                Uri urlUri = new Uri(url.Trim());

                string avatarFilename = Path.GetFileName(avatarUri.AbsolutePath);
                string urlFilename = Path.GetFileName(urlUri.AbsolutePath);

                //Debug.Log($"Comparing filenames: Avatar = '{avatarFilename}', URL = '{urlFilename}'");

                if (string.Equals(avatarFilename, urlFilename, StringComparison.OrdinalIgnoreCase))
                {
                    //Debug.Log("✅ Set Profile Sprite (matched by filename)");
                    _url = url;
                    _sprite = imageComponent.sprite;
                    founUserAvatarImage = true;
                }
                else
                {
                    Debug.Log("❌ Filenames do not match.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❗ URL comparison failed: {e.Message}");
            }
           

        }
        else
        {
            //transform.GetComponent<ProfileImageSelectorPanelManager>().addButtonListner();
        }
    }

    string GetFileNameFromUrl(string url)
    {
        // Create a filename based on the URL's hash or unique path
        return Path.GetFileName(url.Split('?')[0]);
    }
}
