using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager instance;

        public MM_PlayerController player;
        public Transform moveTarget;
        public MoveTargetMarker moveTargetMarker;
        public MenuEnvironmentController menuEnvironment;
        public MenuBuildingEnvironment currentEnvironment;

        [Header("Panels")]
        public UIMainEnvironmentPanel mainEnvironmentPanel;
        public UILoadingPanel loadingPanel;
        public GameCardMenuControllor gameCardMenuPanel;

        [Header("DragSettings")]
        public MainMenuCamera mainCamera;
        public Vector3 touchOffset;
        public bool isDragging;
        public bool movingWithButtons;
        public bool draggingwhileMovingWithButtons;
        public float dragSpeed = 10;
        public float dragThreshold = .3f;

        private Vector2 mousePosition_current;
        private Vector2 mousePosition_previous;

        public static Action OnLeftButtonClicked;
        public static Action OnRightButtonClicked;

        float deltaX;

        private void Awake()
        {
            instance = this;
            gameCardMenuPanel.gameObject.SetActive(true);
            Application.targetFrameRate = 120;
        }

        private void OnEnable()
        {
            //UIDragHandler.instance.OnMouseUpEvent += PointerUp;
        }

        private void OnDisable()
        {
            //UIDragHandler.instance.OnMouseUpEvent -= PointerUp;
        }

        private void Update()
        {
            //HandleDrag();
        }

        private void Start()
        {
            if (!SceneManagement.lastOpenedGameCategory.IsNullOrWhitespace())
            {
                eGameCategories lastCategory;
                if (Enum.TryParse<eGameCategories>(SceneManagement.lastOpenedGameCategory, true, out lastCategory))
                {
                    loadingPanel.OpenPanel(0.01f);

                    GoToCategoryEnvironment(lastCategory);
                    //SceneManagement.lastOpenedGameCategory = "";
                }
            }
        }

        //void HandleDrag()
        //{
        //    if (Input.GetMouseButtonDown(0))
        //    {
        //        mousePosition_current = mainCamera.camera.ScreenToWorldPoint(Input.mousePosition);
        //        mousePosition_previous = mainCamera.camera.ScreenToWorldPoint(Input.mousePosition);
        //    }
        //    else if (Input.GetMouseButton(0) && !isDragging)
        //    {
        //        mousePosition_current = mainCamera.camera.ScreenToWorldPoint(Input.mousePosition);

        //        deltaX = mousePosition_current.x - mousePosition_previous.x;
        //        deltaX /= mainCamera.camera.orthographicSize * 2;

        //        if (Mathf.Abs(deltaX) > dragThreshold)
        //        {
        //            isDragging = true;
        //            moveTarget.position += new Vector3(-deltaX, 0, 0);
        //            mousePosition_previous = mousePosition_current;
        //        }
        //    }
        //    else if (Input.GetMouseButton(0) && isDragging)
        //    {
        //        mousePosition_current = mainCamera.camera.ScreenToWorldPoint(Input.mousePosition);

        //        deltaX = mousePosition_current.x - mousePosition_previous.x;
        //        deltaX /= mainCamera.camera.orthographicSize * 2;

        //        if (Mathf.Abs(deltaX) > dragThreshold)
        //        {
        //            moveTarget.position += new Vector3(-deltaX, 0, 0);
        //            mousePosition_previous = mousePosition_current;
        //        }
        //    }
        //    if (Input.GetMouseButtonUp(0))
        //    {
        //        if (!isDragging)
        //        {
        //            PointAndMove();
        //        }

        //        isDragging = false;
        //    }
        //}

        //void PointAndMove()
        //{
        //    mousePosition_current = mainCamera.camera.ScreenToWorldPoint(Input.mousePosition);
        //    moveTarget.position = new Vector3(mousePosition_current.x, moveTarget.position.y, moveTarget.position.z);
        //}

        public void LeftButtonPressed_Environment()
        {
            OnLeftButtonClicked?.Invoke();
        }

        public void RightButtonPressed_Environment()
        {
            OnRightButtonClicked?.Invoke();
        }

        //public void PointerUp()
        //{
        //    isDragging = false;
        //    draggingwhileMovingWithButtons = false;
        //}

        public void GoToCategoryEnvironment(eGameCategories categoryName)
        {
            StartCoroutine(GoToCategoryEnvironment_Coroutine(categoryName));
        }

        IEnumerator GoToCategoryEnvironment_Coroutine(eGameCategories categoryName)
        {
            //loadingPanel.OpenPanel(.5f);
            //loadingPanel.loadingBarFill.fillAmount = 0;
            //yield return new WaitForSeconds(1f);
            //loadingPanel.loadingBarFill.fillAmount = 0.7f;
            yield return new WaitForSeconds(.5f);
            currentEnvironment.gameObject.SetActive(false);
            menuEnvironment.EnableCategoryEnvironment(categoryName);
            mainEnvironmentPanel.backToCategoriesEnvButton.gameObject.SetActive(true);

            SceneManagement.lastOpenedGameCategory = categoryName.ToString();

            yield return new WaitForSeconds(.5f);
            //loadingPanel.loadingBarFill.fillAmount = 1;
            loadingPanel.ClosePanel(.5f);
        }

        public void ExitCategoryBuilding()
        {
            StartCoroutine(nameof(ExitCategoryBuilding_Coroutine));
        }

        IEnumerator ExitCategoryBuilding_Coroutine()
        {
            //loadingPanel.OpenPanel(.5f);
            //loadingPanel.loadingBarFill.fillAmount = 0;
            //yield return new WaitForSeconds(1f);
            //loadingPanel.loadingBarFill.fillAmount = 0.7f;
            yield return new WaitForSeconds(.5f);
            menuEnvironment.DisableCurrentCategoryEnvironment();
            currentEnvironment.gameObject.SetActive(true);
            mainEnvironmentPanel.backToCategoriesEnvButton.gameObject.SetActive(false);

            SceneManagement.lastOpenedGameCategory = "";

            yield return new WaitForSeconds(.5f);
            //loadingPanel.loadingBarFill.fillAmount = 1;
            //loadingPanel.ClosePanel(.5f);
        }

        public void GoToMachine(MainMenu.MenuGameMachine gameMachine)
        {
            StartCoroutine(GoToMachine_Coroutine(gameMachine));
        }

        IEnumerator GoToMachine_Coroutine(MenuGameMachine gameMachine)
        {
            loadingPanel.OpenPanel(.5f);
            yield return new WaitForSeconds(.5f);
            OpenLevel(gameMachine.sceneName, gameMachine.gameID, gameMachine.addressableLabel);
            yield return new WaitForSeconds(.5f);
            //loadingPanel.OpenPanel(.5f);
        }

        public void OpenLevel(string sceneName, string gameID, string addressableLabel)
        {
            StartCoroutine(OpenLevel_Coroutine(sceneName, gameID, addressableLabel));
        }

        private IEnumerator OpenLevel_Coroutine(string sceneName, string gameID, string addressableLabel)
        {
            loadingPanel.OpenPanel(.5f);

            Coroutine _loadRoutine;
            Vector3 loadingBarFillTargetScale = Vector3.one;
            loadingBarFillTargetScale.x = 0;

            loadingPanel.loadingBarFill.gameObject.SetActive(true);
            loadingPanel.loadingBarFill.transform.localScale = loadingBarFillTargetScale;
            //loadingPanel.loadingBarFill.fillAmount = 0f;

            //GameItem gi = GameCatalogueController.instance.gameItems.FirstOrDefault(x => x.id == gameID);
            //if (gi != null)
            //{
            //    SceneManagement.lastOpenedGameCategory = gi.category;
            //}

            // Start the download on the central catalogue; pass a callback to update this card UI
            _loadRoutine = StartCoroutine(
                GameCatalogueController.instance.DownloadAndLaunchGame(addressableLabel, sceneName, gameID, (percent) =>
                {
                    try
                    {
                        loadingBarFillTargetScale.x = percent;
                        loadingPanel.loadingBarFill.transform.localScale = loadingBarFillTargetScale;
                        //loadingPanel.loadingBarFill.fillAmount = percent;
                    }
                    catch { }
                })
            );

            // the DownloadAndLaunchGame coroutine will call LoadGame(...) when done, which loads SceneLoader.
            yield break;
        }

        public void SetFavoriteStatus(GameItem item, GameCardController gameCard)
        {
            StopCoroutine(nameof(SetFavoriteStatusInFavoritesTab_Coroutine));
            StartCoroutine(nameof(SetFavoriteStatusInFavoritesTab_Coroutine));

            StartCoroutine(SetFavoriteStatusInCategoryTab_Coroutine(item, gameCard));
        }

        IEnumerator SetFavoriteStatusInFavoritesTab_Coroutine()
        {
            yield return new WaitForSeconds(.5f);

            menuEnvironment.ResetFavoritesMachines();
            menuEnvironment.GetCategoryEnvironment(eGameCategories.Favorites)?.PopulateGameMachines();

            gameCardMenuPanel.GetCategoryPanelController(eGameCategories.Favorites)?.ResetPanels();
            gameCardMenuPanel.GetCategoryPanelController(eGameCategories.Favorites)?.PopulateGameCards();
        }

        IEnumerator SetFavoriteStatusInCategoryTab_Coroutine(GameItem item, GameCardController gameCard)
        {
            yield return new WaitForSeconds(.5f);

            gameCardMenuPanel.GetCategoryPanelController(Enum.Parse<eGameCategories>(item.category, true))?.GetGameCard(item.id)?.ReInitializeGameCardData();
            if (gameCard != null)
            {
                gameCard.SetFavoriteButtonInteractable(true);
            }
        }
    }
}