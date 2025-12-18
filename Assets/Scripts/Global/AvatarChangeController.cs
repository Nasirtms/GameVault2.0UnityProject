using DG.Tweening;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static SerializableClasses;

namespace MainMenu
{
    public class AvatarChangeController : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button leftArrow_Btn;
        [SerializeField] private Button rightArrow_Btn;
        [SerializeField] private Button avatarSelect_Btn;
        [SerializeField] private Button avatarSelectClose_Btn;

        [Header("Avatar Settings")]
        [SerializeField] private GameObject player;
        public List<GameObject> avatars = new List<GameObject>();
        public int currentIndex = 0;
        public int SavedAvatarIndex = 0;

        [Header("UI Avatar Display")]
        public RawImage avatarDisplayCurrent_RI;
        public RawImage transitionImage_RI;

        [Header("Transition Settings")]
        public float transitionDuration = 0.5f;

        private RectTransform currentRT;
        private RectTransform transitionRT;
        [SerializeField] private MM_PlayerController mm_PlayerController;


        private void Start()
        {
            MakeReference();
            AddListeners();
            SetupUIReferences();
            toggleAvatarSelectButton(SavedAvatarIndex);
            ShowAvatar(currentIndex);
        }

        ProfilePanelManager profilePanelManager;

        private void MakeReference()
        {
            // Get player reference

            player = MainMenuManager.instance.player.gameObject;

            if (profilePanelManager == null)
            {
                profilePanelManager = MainMenuManager.instance?.gameObject.GetComponent<ProfilePanelManager>();
            }
            if (profilePanelManager != null)
            {
                profilePanelManager.UpdateDataConfirm?.onClick.AddListener(UpdateUserDataOnDB);
            }

            MainMenuUIManager.Instance.ToggleAvatarSelectButton += HandleAvatarSelectBtn;

            // Load avatars under player
            avatars.Clear();
            int index = 0;

            foreach (Transform child in player.transform)
            {
                if (!child.gameObject.name.Equals("AvatarCamera"))
                {
                    avatars.Add(child.gameObject);
                    if (child.gameObject.activeSelf)
                    {
                        SavedAvatarIndex = index;
                        currentIndex = index;
                    }
                    index++;
                }
            }
        }

        private void SetupUIReferences()
        {
            // Reference rect transforms
            currentRT = avatarDisplayCurrent_RI.GetComponent<RectTransform>();
            transitionRT = transitionImage_RI.GetComponent<RectTransform>();

            // Ensure transition image starts hidden
            transitionImage_RI.gameObject.SetActive(false);
        }

        private void AddListeners()
        {
            leftArrow_Btn = MainMenuUIManager.Instance.AvaarChangeLeftArrow_Btn;
            rightArrow_Btn = MainMenuUIManager.Instance.AvaarChangeRightArrow_Btn;
            avatarSelect_Btn = MainMenuUIManager.Instance.AvatarSelect_Btn;
            avatarSelectClose_Btn = MainMenuUIManager.Instance.AvatarSelectClose_Btn;

            avatarDisplayCurrent_RI = MainMenuUIManager.Instance.AvaarChangeAvatar_RI;
            transitionImage_RI = MainMenuUIManager.Instance.AvaarChangeAvatarTrans_RI;

            if (leftArrow_Btn != null) leftArrow_Btn.onClick.AddListener(PreviousAvatar);
            if (rightArrow_Btn != null) rightArrow_Btn.onClick.AddListener(NextAvatar);
            if (avatarSelect_Btn != null) avatarSelect_Btn.onClick.AddListener(UpdateAvatar);
            if (avatarSelectClose_Btn != null) avatarSelectClose_Btn.onClick.AddListener(CloseSettingPanel);
        }

        public void NextAvatar()
        {
            int newIndex = (currentIndex + 1) % avatars.Count;
            toggleAvatarSelectButton(newIndex);
            AnimateSwitch(newIndex, +1);
        }

        public void PreviousAvatar()
        {
            int newIndex = (currentIndex - 1 + avatars.Count) % avatars.Count;
            toggleAvatarSelectButton(newIndex);
            AnimateSwitch(newIndex, -1);
        }

        public void toggleAvatarSelectButton(int newindex)
        {
            if (newindex == SavedAvatarIndex)
            {
                avatarSelect_Btn.gameObject.SetActive(false);
            }
            else
            {
                avatarSelect_Btn.gameObject.SetActive(true);

            }
        }

        private void AnimateSwitch(int newIndex, int direction)
        {
            // Bring old image to transition layer
            transitionImage_RI.texture = avatarDisplayCurrent_RI.texture;
            transitionImage_RI.color = Color.white;
            transitionImage_RI.gameObject.SetActive(true);

            // Reset positions before anim
            currentRT.anchoredPosition = new Vector2(500 * direction, 0);
            transitionRT.anchoredPosition = Vector2.zero;

            // Hide old (slide + fade)
            transitionRT.DOAnchorPos(new Vector2(-500 * direction, 0), transitionDuration).SetEase(Ease.OutCubic);
            transitionImage_RI.DOFade(0f, transitionDuration);

            // Change active avatar
            avatars[currentIndex].SetActive(false);

            if (mm_PlayerController == null)
            {
                mm_PlayerController = player.GetComponent<MM_PlayerController>();
            }
            mm_PlayerController.SetCurrentPlayer(newIndex);
            //avatars[newIndex].SetActive(true);
            currentIndex = newIndex;

            // Fade + slide new avatar display
            avatarDisplayCurrent_RI.color = new Color(1, 1, 1, 0);
            avatarDisplayCurrent_RI.DOFade(1, transitionDuration);

            currentRT.DOAnchorPos(Vector2.zero, transitionDuration).SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                transitionImage_RI.gameObject.SetActive(false);
            });
        }

        private void ShowAvatar(int index)
        {
            for (int i = 0; i < avatars.Count; i++)
                avatars[i].SetActive(i == index);

            avatarDisplayCurrent_RI.color = Color.white;
            currentIndex = index;
        }

        private void UpdateAvatar()
        {
            if (mm_PlayerController == null)
            {
                mm_PlayerController = player.GetComponent<MM_PlayerController>();
            }
            SavedAvatarIndex = currentIndex;
            mm_PlayerController.SetCurrentPlayer(currentIndex);
        }

        private void CloseSettingPanel()
        {
            currentIndex = SavedAvatarIndex;
            UpdateAvatar();
            MainMenuUIManager.Instance.HidePopup(MainMenuUIManager.Instance.settingsPopup);
        }

        void UpdateUserDataOnDB()
        {
            StartCoroutine(UpdateUserProfile(currentIndex));
        }

        public IEnumerator UpdateUserProfile(int index = 0)
        {
            CasinoUIManager.Instance.ShowErrorCanvas(0, "Updating profile...");

            // ?? Create JSON body
            string jsonBody = JsonUtility.ToJson(new ProfileImageUpdateRequest
            {
                //avatar_url = avatarUrl,
                avatar_index = index
            });

            UnityWebRequest request = new UnityWebRequest(ApiEndpoints.UpdateUserProfileImage, "PUT");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            foreach (var header in ApiEndpoints.GetAuthHeaders())
                request.SetRequestHeader(header.Key, header.Value);

            // ? Send request
            yield return request.SendWebRequest();

            string json = request.downloadHandler.text;
            Debug.Log("?? Response JSON: " + json);


            if (request.responseCode == 401)
            {
                yield return ApiEndpoints.CheckApiResponse(request, ApiEndpoints.UpdateUserProfileImage, jsonBody, "PUT", () => UpdateUserProfile(index));
                yield break;
            }


            if (request.result == UnityWebRequest.Result.Success && request.responseCode == 200)
            {

                Debug.Log("Profile Response: " + JsonConvert.DeserializeObject<UserData>(json));

                // ? Parse single object, not list
                UserData updatedUser = JsonConvert.DeserializeObject<UserData>(json);


                Debug.Log("? Profile updated successfully.");
                HandleAvatarSelectBtn();
                SavedAvatarIndex = currentIndex;
                CasinoUIManager.Instance.ShowErrorCanvas(1, "User Information Modified Successfully");
            }
            else
            {
                Debug.LogError($"? Update failed. Code: {request.responseCode}, Error: {request.error}");
                CasinoUIManager.Instance.ShowErrorCanvas(1, "Update failed");
            }
        }


        private void HandleAvatarSelectBtn()
        {
            toggleAvatarSelectButton(currentIndex);
        }

        private void OnDisable()
        {
            MainMenuUIManager.Instance.ToggleAvatarSelectButton -= HandleAvatarSelectBtn;
        }
    }
}
