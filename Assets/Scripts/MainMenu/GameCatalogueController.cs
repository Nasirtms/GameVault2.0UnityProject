using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class GameItem
{
    public string id;
    public string name;
    public bool is_publish;
    public string image_url;
    public bool is_active;
    public double min_bet;
    public int max_bet;
    public string category;
    public DateTime created_at;
    public DateTime updated_at;
    public bool is_favorite;
    public bool isGameCardShine;
    public string Gametitle;
    [PreviewField] public Sprite gameImage;
    public string sceneName;
    public string addressableLabel;
}

public class GameCatalogueController : MonoBehaviour
{
    public static GameCatalogueController instance;
    [SerializeField] private bool loadGameCardImageForCash = true;

    [Header("Game Data")]
    public List<GameItem> gameItems;

    [Header("Card Scale")]
    [SerializeField] float cardXScale = 1;
    [SerializeField] float cardYScale = 1;

    [Header("Games Loading")]
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float rotationSpeed = 180f;
    private Tween loadTween;
    private CanvasGroup canvasGroup;
    private int spawnedItemsCount;
    [SerializeField] private List<GameObject> spawnedItems = new List<GameObject>();
    private FavoriteAnimationSpawner sceneFavoriteAnimationSpawner;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (gameItems.Count > 0) gameItems.Clear();
        sceneFavoriteAnimationSpawner = FindObjectOfType<FavoriteAnimationSpawner>();
        if (sceneFavoriteAnimationSpawner == null)
            Debug.LogWarning("[GameCatalogueController] FavoriteAnimationSpawner not found in scene!");
        StartCoroutine(LoadAllGameDataAndImages());
    }

    public void StartRotation()
    {
        if (spinnerImage == null) return;
        spinnerImage.gameObject.SetActive(true);
        loadTween?.Kill();
        float duration = 360f / rotationSpeed;
        loadTween = spinnerImage
            .DORotate(new Vector3(0, 0, -360f), duration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }


    private IEnumerator LoadAllGameDataAndImages()
    {
        StartRotation();
        int totalImages = SceneManagement.games.Count;
        int imagesLoaded = 0;
        if (SceneManagement.games.Count <= 0)
        {
            yield return null;
        }

        foreach (var game in SceneManagement.games)
        {
            GameItem newItem = new GameItem
            {
                id = game.id,
                name = game.name,
                is_publish = game.is_publish,
                image_url = game.image_url,
                is_active = game.is_active,
                min_bet = game.min_bet,
                max_bet = game.max_bet,
                category = game.category,
                created_at = game.created_at,
                updated_at = game.updated_at,
                is_favorite = game.is_favorite,
                isGameCardShine = game.shine,
                Gametitle = game.title,
                sceneName = "Game" + game.name.Replace(" ", ""),
                gameImage = null,
                addressableLabel = game.name.Replace(" ", "_").ToLower()
            };

            //Debug.Log("Game Catalogue: Game Name = " + newItem.name + " Game Label = " + newItem.addressableLabel);
            gameItems.Add(newItem);
            StartCoroutine(LoadImageFromUrlParallel(newItem.image_url, newItem, () => { imagesLoaded++; }));
        }

        while (imagesLoaded < totalImages)
            yield return null;

        //Debug.Log("✅ All images loaded. Instantiating game UI.");
        //MainMenuUIManager.Instance.SetSceneData();
    }
    public IEnumerator LoadImageFromUrlParallel(string url, GameItem gameItem, System.Action onComplete)
    {
        if (!string.IsNullOrEmpty(url))
        {
            string fileName = Path.GetFileName(new System.Uri(url).LocalPath);
            string filePath = Path.Combine(Application.persistentDataPath, fileName);

            if (loadGameCardImageForCash)
            {
                if (File.Exists(filePath))
                {
                    byte[] imageData = File.ReadAllBytes(filePath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageData))
                    {
                        gameItem.gameImage = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f));
                    }
                    else
                    {
                        Debug.LogWarning("❌ Failed to load texture from cached data.");
                    }

                    onComplete?.Invoke();
                    yield break;
                }
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"❌ Failed to load image from {url}: {request.error}");
                    onComplete?.Invoke();
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (texture == null)
                {
                    Debug.LogError("❌ Texture is null.");
                    onComplete?.Invoke();
                    yield break;
                }

                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngData);
                gameItem.gameImage = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }

            onComplete?.Invoke();
        }
        else
        {
            Debug.Log($"<b><color=cyan>[GameCard]</color></b> <color=white>Image URL is empty →</color> <color=magenta>{gameItem.name}</color>");
        }
    }

    string selectedCategory;

    public IEnumerator DownloadAndLaunchGame(string label, string sceneKey, string sceneID, Action<float> onProgress = null)
    {
        Debug.Log($"[Addressables] Starting init for label={label}");
        var init = Addressables.InitializeAsync();
        yield return init;

        var sizeHandle = Addressables.GetDownloadSizeAsync(label);
        yield return sizeHandle;

        long bytesToDownload = 0;
        if (sizeHandle.Status == AsyncOperationStatus.Succeeded) bytesToDownload = sizeHandle.Result;
        Debug.Log($"[Addressables] bytesToDownload={bytesToDownload}");
//#if UNITY_WEBGL && !UNITY_EDITOR
//        if (bytesToDownload == 0)
//        {
//            CasinoUIManager.Instance.ShowErrorCanvas(3, "");
//            yield break;
//        }
//#endif
        float uiProgress = 0f;
        if (bytesToDownload > 0)
        {
            Debug.LogError("ManhoosLogs: label: " + label);
            var downloadHandle = Addressables.DownloadDependenciesAsync(label);
            float smoothVelocity = 0f;

            while (!downloadHandle.IsDone)
            {
                float realProgress = downloadHandle.PercentComplete;
                uiProgress = Mathf.SmoothDamp(uiProgress, realProgress, ref smoothVelocity, 0.15f);
                uiProgress = Mathf.Clamp01(uiProgress);
                onProgress?.Invoke(uiProgress);
                yield return null;
            }

            onProgress?.Invoke(1f);

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Addressables] Download failed: " + downloadHandle.OperationException);
                CasinoUIManager.Instance.ShowErrorCanvas(3, "");
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            Addressables.Release(downloadHandle);
        }
        else
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 2f;
                onProgress?.Invoke(t);
                yield return null;
            }
        }


        LoadGame(sceneKey, sceneID);
    }

    public void LoadGame(string scene, string sceneID)
    {
        //if (MainMenuUIManager.Instance.gamesNames.Contains(scene))
        //{

        LoadingBridge.SceneToLoad = scene;
        LoadingBridge.ShowExtraImage = true;
        LoadingBridge.IsAddressableScene = true;
        LoadingBridge.pauseProfileApiCall = true;
        SceneManagement.currentGameID = sceneID;
        Addressables.LoadSceneAsync(MainMenuAddressableHandler.sceneLoaderSceneKey, LoadSceneMode.Single, true);    //SceneManager.LoadScene("SceneLoader");
        //}
        //else
        //{
        //    CasinoUIManager.Instance.ShowErrorCanvas(1, "Game is currently unavailable.");
        //}
    }

    public void UpdateGameItemList(string gameId, bool isfavorite, GameCardController gameCard)
    {
        // ✅ Update in gameItems
        var item = gameItems.FirstOrDefault(x => x.id == gameId);
        if (item != null)
            item.is_favorite = isfavorite;

        // ✅ Update in SceneManagement.games
        var game = SceneManagement.games.FirstOrDefault(x => x.id == gameId);
        if (game != null)
            game.is_favorite = isfavorite;

        MainMenu.MainMenuManager.instance.SetFavoriteStatus(item, gameCard);
    }

    public int SpawnedItemsCount() => spawnedItemsCount;

    //void SpawnFilteredItems(List<GameItem> items)
    //{
    //    int splitPoint = Mathf.CeilToInt(items.Count / 2f);
    //    for (int i = 0; i < items.Count; i++)
    //    {
    //        var data = items[i];
    //        if (!data.is_active) { return; }
    //        if (SceneManagement.sceneAccessType == SceneAccessType.Publish && !data.is_publish) continue;
    //        if (SceneManagement.sceneAccessType == SceneAccessType.Dev && data.is_publish) continue;

    //        GameObject item = Instantiate(prefab, contentParent);
    //        item.transform.SetSiblingIndex(i);
    //        item.name = data.name;
    //        spawnedItems.Add(item);

    //        if (item.GetComponent<GameCardController>() == null)
    //        {
    //            item.AddComponent<GameCardController>();
    //        }

    //        var gameCardController = item.GetComponent<GameCardController>();
    //        gameCardController.gameCatalogueController = this;
    //        gameCardController.addressableLabel = data.addressableLabel;
    //        gameCardController.SetGameCardData(data.Gametitle, data.is_favorite, data.isGameCardShine, data.id);
    //        gameCardController.SetClickEffectSpawner(sceneFavoriteAnimationSpawner);

    //        RectTransform rt = item.GetComponent<RectTransform>();
    //        Image image = rt.GetChild(0).GetChild(0).GetComponent<Image>();

    //        if (rt != null)
    //        {
    //            if (spawnedItemsCount <= 2)
    //            {
    //                rt.pivot = new Vector2(0.85f, 0.5f);
    //            }
    //            else if (spawnedItemsCount <= 4)
    //            {
    //                rt.pivot = new Vector2(0.3f, 0.5f);
    //            }
    //            else if (spawnedItemsCount <= 6)
    //            {
    //                rt.pivot = new Vector2(0.1f, 0.5f);
    //            }
    //            else
    //            {
    //                rt.pivot = new Vector2(1f, 0.5f);
    //            }

    //            if (spawnedItemsCount <= 8)
    //            {
    //                CylindricalUIWarpSwipe.isDragable = false;
    //            }
    //            else
    //            {
    //                CylindricalUIWarpSwipe.isDragable = true;
    //            }

    //            rt.anchoredPosition = Vector2.zero;
    //            rt.localScale = new Vector3(cardXScale, cardYScale, 1);
    //            if (image != null) image.sprite = data.gameImage;

    //            if (gameCardController != null)
    //            {
    //                string sceneName = data.sceneName;
    //                string gameID = data.id;
    //                gameCardController.onLongPress.AddListener(() => gameCardController.OnHoldStart(sceneName, gameID, data.addressableLabel));
    //            }
    //        }
    //    }

    //    if (!isAddSpaceObject)
    //    {
    //        addTwoChildrenForSpaceAtLastCard();
    //    }
    //}
}
