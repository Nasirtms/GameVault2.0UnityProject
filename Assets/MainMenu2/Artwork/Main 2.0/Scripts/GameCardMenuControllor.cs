using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


namespace MainMenu
{
    public class GameCardMenuControllor : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button MenuButton;
        [SerializeField] private Button CloseButton;
        [SerializeField] private Button NextButton;
        [SerializeField] private Button PreviousButton;

        [Header("Panels")]
        [SerializeField] private RectTransform PanalsParent;
        [SerializeField] private List<GamaCatagoryPanalControllor> panals = new List<GamaCatagoryPanalControllor>();

        [Header("Carousel Animation")]
        public float slideDistance = 1600f;
        public float animationDuration = 0.3f;
        public Ease moveEase = Ease.OutCubic;
        public Ease scaleEase = Ease.OutCubic;

        [Header("Swipe Settings")]
        public float swipeThreshold = 100f;

        [Header("Prefabs")]
        public GameObject GameCardPanalPrefab;
        //public UIGameCardButton GameCardPrefab;
        public GameCardController GameCardPrefab;
        public GameObject PageIndicatorPrefab;

        private int _currentIndex = 0;
        private bool _isAnimating = false;

        public int CurrentIndex => _currentIndex;
        public int CategoryCount => panals != null ? panals.Count : 0;
        public event Action<int> OnCategoryChanged;
        public event Action<int> OnMenuOpened;

    
        private void Awake()
        {
            if (MenuButton != null)
                MenuButton.onClick.AddListener(OpenMenu);

            if (CloseButton != null)
                CloseButton.onClick.AddListener(CloseMenu);

            if (NextButton != null)
                NextButton.onClick.AddListener(NextCategory);

            if (PreviousButton != null)
                PreviousButton.onClick.AddListener(PreviousCategory);

            // hide container initially if you want
            if (PanalsParent != null)
                PanalsParent.gameObject.SetActive(false);

            ApplyLayoutImmediate();
        }

        

        private void OpenMenu()
        {
            GlobleSoundManager.Instance.PlaySFX("Swipe");
            if (PanalsParent != null)
                PanalsParent.gameObject.SetActive(true);
            toggleMEnuBTn();
            ApplyLayoutImmediate();

            // little pop on current panel
            if (panals != null && _currentIndex >= 0 && _currentIndex < panals.Count)
            {
                RectTransform cur = panals[_currentIndex].GetComponent<RectTransform>();
                if (cur != null)
                {
                    cur.localScale = Vector3.one * 0.85f;
                    cur.DOScale(1f, 0.3f).SetEase(scaleEase);
                }
            }

            UpdateNavButtons();
            OnCategoryChanged?.Invoke(_currentIndex);
            OnMenuOpened?.Invoke(_currentIndex);
        }


        void toggleMEnuBTn()
        {
            MainMenuUIManager.Instance?.ToggleMenuButtonsUI(false);
        }

        private void CloseMenu()
        {
            toggleMEnuBTn();
            GlobleSoundManager.Instance.PlaySFX("Swipe");
            if (PanalsParent != null)
                PanalsParent.gameObject.SetActive(false);
        }

        private void UpdateNavButtons()
        {
            bool hasPanels = panals != null && panals.Count > 0;

            if (PreviousButton != null)
                PreviousButton.interactable = hasPanels && !_isAnimating && _currentIndex > 0;

            if (NextButton != null)
                NextButton.interactable = hasPanels && !_isAnimating && _currentIndex < panals.Count - 1;
        }

        /// <summary>
        /// Snap all panels into place around _currentIndex.
        /// Center = scale 1, others = 0.9, all active.
        /// </summary>
        private void ApplyLayoutImmediate()
        {
            if (panals == null) return;

            for (int i = 0; i < panals.Count; i++)
            {
                var panel = panals[i];
                if (panel == null) continue;

                RectTransform rt = panel.GetComponent<RectTransform>();
                if (rt == null) continue;

                float offset = (i - _currentIndex) * slideDistance;
                rt.anchoredPosition = new Vector2(offset, rt.anchoredPosition.y);

                float scale = (i == _currentIndex) ? 1f : 0.9f;
                rt.localScale = Vector3.one * scale;

                // always active – no SetActive(false) here
                if (!panel.gameObject.activeSelf)
                    panel.gameObject.SetActive(true);
            }

            _isAnimating = false;
            UpdateNavButtons();
        }

        private void AnimateToIndex(int newIndex)
        {
            if (panals == null || panals.Count == 0) return;
            if (_isAnimating) return;

            newIndex = Mathf.Clamp(newIndex, 0, panals.Count - 1);
            if (newIndex == _currentIndex) return;

            int oldIndex = _currentIndex;
            _currentIndex = newIndex;
            _isAnimating = true;
            UpdateNavButtons();
            OnCategoryChanged?.Invoke(_currentIndex);

            int steps = Mathf.Abs(newIndex - oldIndex);
            float duration = animationDuration * Mathf.Max(1, steps);

            Sequence seq = DOTween.Sequence();

            for (int i = 0; i < panals.Count; i++)
            {
                var panel = panals[i];
                if (panel == null) continue;

                RectTransform rt = panel.GetComponent<RectTransform>();
                if (rt == null) continue;

                float targetX = (i - _currentIndex) * slideDistance;
                float targetScale = (i == _currentIndex) ? 1f : 0.9f;

                rt.DOKill();

                seq.Join(rt.DOAnchorPosX(targetX, duration).SetEase(moveEase));
                seq.Join(rt.DOScale(targetScale, duration).SetEase(scaleEase));
            }

            seq.OnComplete(() =>
            {
                _isAnimating = false;
                UpdateNavButtons();
            });
        }

        public void NextCategory()
        {
            if (_isAnimating) return;
            if (panals == null || panals.Count == 0) return;
            if (_currentIndex >= panals.Count - 1) return;
            GlobleSoundManager.Instance.PlaySFX("Swipe");
            AnimateToIndex(_currentIndex + 1);
        }

        public void PreviousCategory()
        {
            if (_isAnimating) return;
            if (panals == null || panals.Count == 0) return;
            if (_currentIndex <= 0) return;
            GlobleSoundManager.Instance.PlaySFX("Swipe");
            AnimateToIndex(_currentIndex - 1);
        }

        // called from inner page swipe when on last page
        public void GoToNextCategoryFromLastGameCardsPage(GamaCatagoryPanalControllor caller)
        {
            if (panals == null) return;
            int index = panals.IndexOf(caller);
            if (index < 0) return;
            if (index >= panals.Count - 1) return;

            AnimateToIndex(index + 1);
        }

        // called from inner page swipe when on first page
        public void GoToPreviousCategoryFromFirstGameCardsPage(GamaCatagoryPanalControllor caller)
        {
            if (panals == null) return;
            int index = panals.IndexOf(caller);
            if (index <= 0) return;

            AnimateToIndex(index - 1);
        }

        public void SwipeToNextCatagory()
        {
            NextCategory();
        }

        public void SwipeToPreviousCatagory()
        {
            PreviousCategory();
        }

        /// <summary>
        /// Called from GameCatagoryCardControllor when clicking a category card.
        /// </summary>
        public void OpenCategory(GamaCatagoryPanalControllor targetPanel, bool animate = false)
        {
            if (targetPanel == null || panals == null || panals.Count == 0)
                return;

            int newIndex = panals.IndexOf(targetPanel);
            if (newIndex == -1)
                return;

            if (animate)
            {
                AnimateToIndex(newIndex);
            }
            else
            {
                _currentIndex = newIndex;
                ApplyLayoutImmediate();
                OnCategoryChanged?.Invoke(_currentIndex);
            }
        }

        public GamaCatagoryPanalControllor GetCategoryPanelController(eGameCategories gameCategory)
        {
            return panals.FirstOrDefault(x => x.categoryName == gameCategory);
        }
    }
}