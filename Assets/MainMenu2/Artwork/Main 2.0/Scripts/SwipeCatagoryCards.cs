using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MainMenu
{
    public class SwipeCatagoryCards : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        [Header("Swipe Settings")]
        [SerializeField] private float swipeThreshold = 100f;
        private Vector2 _dragStartPos;
        private bool _isDragging = false;
        [SerializeField] private GameCardMenuControllor menuController;


        public void OnDrag(PointerEventData eventData) { }
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
                // not enough movement to count as swipe
                return;
            }

            // swipe left → next
            if (deltaX < 0f)
            {
                menuController.SwipeToNextCatagory();
            }
            // swipe right → previous
            else
            {
                menuController.SwipeToPreviousCatagory();
            }
        }
    }
}