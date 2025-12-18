using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace MainMenu
{
    public class GameCatagoryCardControllor : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [Header("References")]
        [Tooltip("Main menu controller that manages which category panel is open.")]
        [SerializeField] private GameCardMenuControllor menuController;

        [Header("Category Cards (Buttons)")]
        [Tooltip("UI buttons representing each category. Order MUST match panels.")]
        [SerializeField] private List<Button> categoryButtons;

        [Header("Category Panels")]
        [Tooltip("Panels corresponding to category buttons. Same order as categoryButtons.")]
        [SerializeField] private List<GamaCatagoryPanalControllor> gamecatagorypanal;

        [Header("Card Visuals")]
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float inactiveAlpha = 0.4f;

        [SerializeField] private float normalScale = 1f;
        [SerializeField] private float selectedScale = 1.3f;
        [SerializeField] private float scaleDuration = 0.25f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        [Header("Carousel Layout")]
        [Tooltip("Distance between card centers in local X.")]
        [SerializeField] private float cardSpacing = 250f;
        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;

        [Header("Swipe Settings")]
        [SerializeField] private float swipeThreshold = 100f;

        private Vector2 _dragStartPos;
        private bool _isDragging = false;
        private RectTransform _containerRect;

        private void Awake()
        {
            _containerRect = GetComponent<RectTransform>();

            // Hook up button clicks and reset scales
            int count = Mathf.Min(categoryButtons.Count, gamecatagorypanal.Count);
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button btn = categoryButtons[index];
                if (btn == null) continue;

                btn.onClick.AddListener(() => OnCategoryClicked(index));
                btn.transform.localScale = Vector3.one * normalScale;
            }

            LayoutCards();

            // Listen to menu index changes (panels -> cards)
            if (menuController != null)
            {
                menuController.OnCategoryChanged += HandleCategoryChanged;
                menuController.OnMenuOpened += HandleMenuOpened;   // NEW
            }
        }

        private void Start()
        {
            int startIndex = 0;
            if (menuController != null && menuController.CategoryCount > 0)
            {
                startIndex = Mathf.Clamp(menuController.CurrentIndex, 0, categoryButtons.Count - 1);
            }

            HandleCategoryChanged(startIndex);
        }

        private void OnDestroy()
        {
            if (menuController != null)
            {
                menuController.OnCategoryChanged -= HandleCategoryChanged;
                menuController.OnMenuOpened -= HandleMenuOpened;   // NEW
            }
        }

        // ----------------- LAYOUT -----------------

        private void LayoutCards()
        {
            if (_containerRect == null) return;

            for (int i = 0; i < categoryButtons.Count; i++)
            {
                var btn = categoryButtons[i];
                if (btn == null) continue;

                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt == null) continue;

                // Arrange them in a line: ..., -1, 0, +1, ...
                float x = i * cardSpacing;
                rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y);
            }
        }

        private void CenterOnIndex(int index)
        {
            if (_containerRect == null) return;
            if (index < 0 || index >= categoryButtons.Count) return;

            float targetX = -index * cardSpacing; // move container so card[index] comes to x = 0

            _containerRect.DOKill();
            _containerRect.DOAnchorPosX(targetX, moveDuration)
                          .SetEase(moveEase);
        }

        // ----------------- CLICK HANDLING -----------------

        private void OnCategoryClicked(int index)
        {
            if (index < 0 || index >= gamecatagorypanal.Count) return;

            var targetPanel = gamecatagorypanal[index];
            if (targetPanel == null) return;

            if (menuController != null)
            {
                // Use menu animation so panels + cards stay synced
                menuController.OpenCategory(targetPanel, true);
                // HandleCategoryChanged will be called via event
            }
            else
            {
                // Fallback: just switch panels manually
                for (int i = 0; i < gamecatagorypanal.Count; i++)
                {
                    var panel = gamecatagorypanal[i];
                    if (panel == null) continue;
                    //panel.SetActive(i == index);
                }

                HandleCategoryChanged(index);
            }
        }

        // ----------------- SWIPE HANDLING ON CARDS -----------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _dragStartPos = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            _isDragging = false;

            float deltaX = eventData.position.x - _dragStartPos.x;

            if (Mathf.Abs(deltaX) < swipeThreshold)
            {
                // Not enough movement to count as swipe
                return;
            }

            if (menuController == null)
                return;

            if (deltaX < 0f)
            {
                // Swipe left -> next category
                menuController.SwipeToNextCatagory();
            }
            else
            {
                // Swipe right -> previous category
                menuController.SwipeToPreviousCatagory();
            }

            // Visuals will update when menuController fires OnCategoryChanged
        }

        public void OnDrag(PointerEventData eventData)
        {
            // We don't drag continuously; we just detect swipe at end.
        }

        // ----------------- VISUAL UPDATE (ALPHA + SCALE + CENTER) -----------------

        private void HandleCategoryChanged(int currentIndex)
        {
            // Center carousel on this card
            CenterOnIndex(currentIndex);

            // Update each card's alpha + scale
            for (int i = 0; i < categoryButtons.Count; i++)
            {
                Button btn = categoryButtons[i];
                if (btn == null) continue;

                // Alpha
                Graphic g = btn.targetGraphic;
                if (g != null)
                {
                    Color c = g.color;
                    c.a = (i == currentIndex) ? activeAlpha : inactiveAlpha;
                    g.color = c;
                }

                // Scale tween
                Transform t = btn.transform;
                t.DOKill();

                float targetScale = (i == currentIndex) ? selectedScale : normalScale;

                t.DOScale(targetScale, scaleDuration)
                 .SetEase(scaleEase);
            }
        }

        private void HandleMenuOpened(int currentIndex)
        {
            // safety checks
            if (currentIndex < 0 || currentIndex >= categoryButtons.Count) return;

            Button btn = categoryButtons[currentIndex];
            if (btn == null) return;

            // Only the current card should pop from normalScale -> selectedScale
            Transform t = btn.transform;

            // Kill any running tweens on this card (including the generic one from HandleCategoryChanged)
            t.DOKill();

            // Force it to start from normalScale, then tween up
            t.localScale = Vector3.one * normalScale;
            t.DOScale(selectedScale, 0.4f)
             .SetEase(scaleEase);
        }

    }
}