using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SerializableClasses;

namespace MainMenu
{
    [System.Serializable]
    public class PageIndicator
    {
        public Button button;
        public GameObject normalImage;
        public GameObject selectedImage;
    }

    public class GamaCatagoryPanalControllor : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public eGameCategories categoryName;

        [Header("Inner Game Panels (pages inside this category)")]
        [SerializeField] public List<GameCardControllor> gameCardsPanals = new List<GameCardControllor>();
        [SerializeField] private List<RectTransform> gameCardPanalsRect = new List<RectTransform>();
        //[SerializeField] public List<UIGameCardButton> gameCards = new List<UIGameCardButton>();
        [SerializeField] public List<GameCardController> gameCards = new List<GameCardController>();
        [SerializeField] private TextMeshProUGUI categoryEmptyText;
        [SerializeField] private int cardsPerPage = 10;

        //[Header("GameCards count")]
        //[SerializeField] private int gameCardsCount;
        private int gameCardPanalsCount;

        //[Header("Inner Carousel Animation")]
        //[SerializeField] private float slideDistance = 1650f;
        //[SerializeField] private float animationDuration = 0.3f;
        //[SerializeField] private Ease moveEase = Ease.OutCubic;

        //[Header("Swipe Settings")]
        //[SerializeField] private float swipeThreshold = 100f;

        [Header("Parent Menu (for cross-category jump)")]
        [SerializeField] private GameCardMenuControllor menuController;

        [Header("Page Indicator Buttons")]
        [SerializeField] private List<PageIndicator> pageIndicators = new List<PageIndicator>();

        [SerializeField] private Transform GameCardPanalParent;
        [SerializeField] private Transform PageIndicatorParent;

        private int _currentIndex = 0;
        private bool _isAnimating = false;

        private Vector2 _dragStartPos;
        private bool _isDragging = false;

        private void Start()
        {
            PopulateGameCards();
        }

        public void ResetPanels()
        {
            for (int i = gameCards.Count - 1; i >= 0; i--)
            {
                Destroy(gameCards[i].gameObject);
            }
            gameCards.Clear();
            for (int i = pageIndicators.Count - 1; i >= 0; i--)
            {
                Destroy(pageIndicators[i].button.gameObject);
            }
            pageIndicators.Clear();
            for (int i = gameCardsPanals.Count - 1; i >= 0; i--)
            {
                Destroy(gameCardsPanals[i].gameObject);
            }
            gameCardsPanals.Clear();
            gameCardPanalsRect.Clear();

            gameCardPanalsCount = 0;
        }

        public void PopulateGameCards()
        {
            if (categoryName != eGameCategories.Favorites)
            {
                if (gameCards.Count > 0)
                    return;
            }
            else
            {
                ResetPanels();
            }

            List<GameItem> gamesList = new List<GameItem>();
            gamesList = GameCatalogueController.instance.gameItems;
            //List<SerializableClasses.Game> gamesList = new List<SerializableClasses.Game>();
            //gamesList = SceneManagement.games;

            Func<GameItem, eGameCategories, bool> condition;

            if (categoryName == eGameCategories.Hot)
                condition = (game, categoryName) => game.Gametitle.ToLower().Contains("hot");
            else if (categoryName == eGameCategories.Favorites)
                condition = (game, categoryName) => game.is_favorite;
            else
                condition = (game, categoryName) => game.category.ToLower() == categoryName.ToString().ToLower();

            List<GameItem> gamesListFiltered = new List<GameItem>();

            var config = SceneManagement.buildConfig;

            foreach (GameItem game in gamesList)
            {
                if (!game.is_active) continue;

                if (!config.IsBoth() && config.IsProduction() != game.is_publish)
                    continue;

                if (condition(game, categoryName))
                    gamesListFiltered.Add(game);
            }

            gameCardPanalsCount = Mathf.CeilToInt((float)gamesListFiltered.Count / cardsPerPage);

            if (gameCardPanalsCount == 0)
                gameCardPanalsCount = 1;

            for (int i = 0; i < gameCardPanalsCount; i++)
            {
                var GCPageIndicator = Instantiate(menuController.PageIndicatorPrefab, PageIndicatorParent);
                pageIndicators.Add(new PageIndicator { button = GCPageIndicator.GetComponent<Button>(), normalImage = GCPageIndicator.transform.Find("Normal").gameObject, selectedImage = GCPageIndicator.transform.Find("Selected").gameObject });
                GCPageIndicator.name = "PageIndicator" + (i + 1);

                var GCPanal = Instantiate(menuController.GameCardPanalPrefab, GameCardPanalParent).GetComponent<GameCardControllor>();
                gameCardsPanals.Add(GCPanal);
                GCPanal.transform.SetSiblingIndex(2);
                GCPanal.transform.localPosition = new Vector2(i * menuController.slideDistance, 35f);
                GCPanal.name = "GameCardPanal" + (i + 1);

                for (int j = 0; j < cardsPerPage; j++)
                {
                    int gameCardIndex = i * cardsPerPage + j;
                    if (gameCardIndex >= gamesListFiltered.Count)
                    {
                        break;
                    }

                    // Instantiate and set data
                    GameCardController gameCardTemp = Instantiate(menuController.GameCardPrefab, GCPanal.transform);
                    gameCardTemp.gameObject.name = "GameCard" + (gameCardIndex + 1) + "-" + gamesListFiltered[gameCardIndex].name;
                    gameCardTemp.gameCatalogueController = GameCatalogueController.instance;
                    gameCardTemp.addressableLabel = gamesListFiltered[gameCardIndex].addressableLabel;
                    gameCardTemp.sceneName = gamesListFiltered[gameCardIndex].sceneName;
                    gameCardTemp.SetGameCardData(gamesListFiltered[gameCardIndex].Gametitle, gamesListFiltered[gameCardIndex].is_favorite, gamesListFiltered[gameCardIndex].isGameCardShine, gamesListFiltered[gameCardIndex].id);
                    gameCardTemp.SetImage(gamesListFiltered[gameCardIndex]);
                    //gameCardTemp.AddOnClickListener(() => MainMenu.MainMenuManager.instance.OpenLevel(gamesListFiltered[gameCardIndex].sceneName, gamesListFiltered[gameCardIndex].id, gamesListFiltered[gameCardIndex].addressableLabel, gamesListFiltered[gameCardIndex].Gametitle));
                    gameCardTemp.AddOnClickListener(() => gameCardTemp.GameCardClicked());
                    //gameCardTemp.onLongPress.AddListener(() => gameCardTemp.OnHoldStart(gamesListFiltered[gameCardIndex].sceneName, gamesListFiltered[gameCardIndex].id, gamesListFiltered[gameCardIndex].addressableLabel));
                    //gameCardTemp.SetClickEffectSpawner(sceneFavoriteAnimationSpawner);

                    gameCards.Add(gameCardTemp);
                }
            }

            if (categoryEmptyText != null)
                categoryEmptyText.gameObject.SetActive(gameCards.Count == 0);

            SetupInitialState();
            SetupIndicators();
        }

        private void SetupInitialState()
        {
            if (gameCardsPanals == null || gameCardsPanals.Count == 0)
                return;

            //for (int i = 0; i < gamecards.Count; i++)
            //{
            //    if (gamecards[i] != null)
            //        gamecards[i].gameObject.SetActive(i == 0);
            //}

            _currentIndex = 0;
            UpdatePageIndicators();
            GetGameCardsRectTransforms();
        }

        private void GetGameCardsRectTransforms()
        {
            for (int i = 0; i < gameCardsPanals.Count; i++)
            {
                gameCardPanalsRect.Add(gameCardsPanals[i].GetComponent<RectTransform>());
            }
        }

        private void SetupIndicators()
        {
            if (pageIndicators == null || pageIndicators.Count == 0)
                return;

            for (int i = 0; i < pageIndicators.Count; i++)
            {
                int pageIndex = i;
                var indicator = pageIndicators[i];
                if (indicator == null || indicator.button == null)
                    continue;

                indicator.button.onClick.RemoveAllListeners();
                indicator.button.onClick.AddListener(() => OnIndicatorClicked(pageIndex));
            }

            UpdatePageIndicators();
        }

        private void OnIndicatorClicked(int pageIndex)
        {
            if (gameCardsPanals == null || pageIndex < 0 || pageIndex >= gameCardsPanals.Count)
                return;

            // already there
            if (pageIndex == _currentIndex)
                return;

            if (_isAnimating)
                return;

            int direction = pageIndex > _currentIndex ? +1 : -1;
            AnimateMultiPageJump(pageIndex, direction);
        }

        // Smooth jump across multiple pages – one tween per page, chained with no frame gaps.
        private void AnimateMultiPageJump(int targetIndex, int direction)
        {
            if (gameCardsPanals == null || gameCardsPanals.Count == 0)
                return;

            targetIndex = Mathf.Clamp(targetIndex, 0, gameCardsPanals.Count - 1);
            if (_currentIndex == targetIndex)
                return;

            //StepPage(_currentIndex, targetIndex, direction);
            int stepcount = Mathf.Abs(targetIndex - _currentIndex);
            AnimateToPanel(targetIndex, direction, stepcount);
        }

        //private void StepPage(int fromIndex, int targetIndex, int direction)
        //{
        //    if (fromIndex == targetIndex)
        //        return;

        //    int nextIndex = fromIndex + direction;

        //    if (nextIndex < 0 || nextIndex >= gamecards.Count)
        //        return;

        //    AnimateToPanel(nextIndex, direction, () =>
        //    {
        //        // call next step immediately in the same frame
        //        StepPage(nextIndex, targetIndex, direction);
        //    });
        //}

        private void UpdatePageIndicators()
        {
            if (pageIndicators == null || pageIndicators.Count == 0)
                return;

            for (int i = 0; i < pageIndicators.Count; i++)
            {
                var indicator = pageIndicators[i];
                if (indicator == null)
                    continue;

                bool isCurrent = (i == _currentIndex);

                if (indicator.normalImage != null)
                    indicator.normalImage.SetActive(!isCurrent);

                if (indicator.selectedImage != null)
                    indicator.selectedImage.SetActive(isCurrent);
            }
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
            // optional hard reset when shown:
            // if (active) SetupInitialState();
        }

        // --------- Drag handling ---------

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isAnimating) return;
            _isDragging = true;
            _dragStartPos = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging || _isAnimating)
                return;

            _isDragging = false;

            float deltaX = eventData.position.x - _dragStartPos.x;

            if (Mathf.Abs(deltaX) < menuController.swipeThreshold)
                return;

            if (deltaX < 0f)
            {
                HandleSwipeLeft();
            }
            else
            {
                HandleSwipeRight();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // swipe detection only, no continuous dragging of content
        }

        private void HandleSwipeLeft()
        {
            if (gameCardsPanals == null || gameCardsPanals.Count == 0) return;

            if (_currentIndex < gameCardsPanals.Count - 1)
            {
                AnimateToPanel(_currentIndex + 1, +1, 1);
            }
            else
            {
                // last page → ask menu to go next category
                if (menuController != null)
                {
                    menuController.GoToNextCategoryFromLastGameCardsPage(this);
                }
            }
        }

        private void HandleSwipeRight()
        {
            if (gameCardsPanals == null || gameCardsPanals.Count == 0) return;

            if (_currentIndex > 0)
            {
                AnimateToPanel(_currentIndex - 1, -1, 1);
            }
            else
            {
                // first page → previous category (if you want that)
                if (menuController != null)
                {
                    menuController.GoToPreviousCategoryFromFirstGameCardsPage(this);
                }
            }
        }

        private GameObject[] inBetweenPanals;
        private RectTransform[] inBetweenRects;

        private void AnimateToPanel(int newIndex, int direction, int stepcount)
        {
            if (newIndex == _currentIndex) return;
            if (newIndex < 0 || newIndex >= gameCardsPanals.Count) return;

            float offset = direction > 0 ? -menuController.slideDistance : menuController.slideDistance;
            _isAnimating = true;
            for (int i = 0; i < gameCardsPanals.Count; i++)
            {
                gameCardPanalsRect[i].DOAnchorPosX(gameCardPanalsRect[i].anchoredPosition.x + (offset * stepcount), menuController.animationDuration).SetEase(menuController.moveEase);
            }

            DOVirtual.DelayedCall(menuController.animationDuration, () =>
            {
                //fromPanel.gameObject.SetActive(false);
                _currentIndex = newIndex;
                _isAnimating = false;

                UpdatePageIndicators();
            });

            //var fromPanel = gamecards[_currentIndex];
            //var toPanel = gamecards[newIndex];
            //var inBetweenCount = Mathf.Abs(newIndex - _currentIndex) - 1;

            //if (newIndex - _currentIndex > 1)
            //{
            //    for (int i = 0; i < inBetweenCount; i++)
            //    {
            //        inBetweenPanals[i] = gamecards[_currentIndex + i + 1].gameObject;
            //    }
            //}

            //if (fromPanel == null || toPanel == null) return;

            //RectTransform fromRect = fromPanel.GetComponent<RectTransform>();
            //RectTransform toRect = toPanel.GetComponent<RectTransform>();

            //if (newIndex - _currentIndex > 1)
            //{
            //    for (int i = 0; i < inBetweenPanals.Length; i++)
            //    {
            //        inBetweenRects[i] = inBetweenPanals[i].GetComponent<RectTransform>();
            //    }
            //}

            //if (fromRect == null || toRect == null) return;

            //_isAnimating = true;

            ////toPanel.gameObject.SetActive(true);

            //float startOffset = direction > 0 ? slideDistance : -slideDistance;
            //float endOffset = -startOffset;

            //toRect.anchoredPosition = new Vector2(startOffset, toRect.anchoredPosition.y);

            //Sequence seq = DOTween.Sequence();

            //seq.Join(fromRect.DOAnchorPosX(endOffset * stepcount, animationDuration).SetEase(moveEase));
            //if(newIndex - _currentIndex > 1)
            //{
            //    for (int i = 0; i < inBetweenRects.Length; i++)
            //    {
            //        seq.Join(inBetweenRects[i].DOAnchorPosX(endOffset * (stepcount-i-1), animationDuration).SetEase(moveEase));
            //    }
            //}
            //seq.Join(toRect.DOAnchorPosX(0f, animationDuration).SetEase(moveEase));

            //seq.OnComplete(() =>
            //{
            //    //fromPanel.gameObject.SetActive(false);
            //    _currentIndex = newIndex;
            //    _isAnimating = false;

            //    UpdatePageIndicators();
            //});
        }

        public GameCardController GetGameCard(string gameID)
        {
            return gameCards.FirstOrDefault(x => x.GetGameID() == gameID);
        }

        public void ReInitializeGameCardData(GameCardController gameCard)
        {
            GameItem gi = gameCard.GetGameItem();

            if (gi != null)
            {
                gameCard.gameCatalogueController = GameCatalogueController.instance;
                gameCard.addressableLabel = gi.addressableLabel;
                gameCard.SetGameCardData(gi.Gametitle, gi.is_favorite, gi.isGameCardShine, gi.id);
                gameCard.SetImage(gi);
            }
        }
    }

}