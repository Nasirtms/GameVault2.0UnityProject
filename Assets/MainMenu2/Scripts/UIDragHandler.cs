using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MainMenu
{
    public class UIDragHandler : MonoBehaviour//, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public static UIDragHandler instance;

        public Camera mainCamera;
        public bool isDragging;
        public float dragThreshold = .3f;

        private Vector2 mousePosition_current;
        private Vector2 mousePosition_previous;
        float deltaX;
        bool mouseDown;

        public static Action OnDragStartedEvemt;
        public static Action<float> OnDragEvent;
        public static Action<Vector2> OnClickedEvent;
        public static Action<bool> OnMouseUpEvent;

        public bool isActive;

        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            mainCamera = MainMenuManager.instance.mainCamera.camera;
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        //public void OnPointerDown(PointerEventData eventData)
        //{
        //    mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //    mousePosition_previous = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //}

        //public void OnDrag(PointerEventData eventData)
        //{
        //    if (!isDragging)
        //    {
        //        mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        //        deltaX = mousePosition_current.x - mousePosition_previous.x;
        //        deltaX /= mainCamera.orthographicSize * 2;

        //        if (Mathf.Abs(deltaX) > dragThreshold)
        //        {
        //            isDragging = true;
        //            OnDragEvent?.Invoke(deltaX);
        //            mousePosition_previous = mousePosition_current;
        //        }
        //    }
        //    else
        //    {
        //        mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        //        deltaX = mousePosition_current.x - mousePosition_previous.x;
        //        deltaX /= mainCamera.orthographicSize * 2;

        //        if (Mathf.Abs(deltaX) > 0.05f)
        //        {
        //            OnDragEvent?.Invoke(deltaX);
        //            mousePosition_previous = mousePosition_current;
        //        }
        //    }
        //}

        //public void OnPointerUp(PointerEventData eventData)
        //{
        //    if (!isDragging)
        //    {
        //        //Point and Move
        //        mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //        OnClickedEvent?.Invoke(mousePosition_current);
        //    }

        //    isDragging = false;

        //    OnMouseUpEvent?.Invoke();
        //}

        private void Update()
        {
            HandleDrag();
        }

        //void HandleDrag()
        //{
        //    if (isActive && Input.GetMouseButtonDown(0))
        //    {

        //        if (IsPointerOverObject())
        //            return;

        //        mouseDown = true;
        //        mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //        mousePosition_previous = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //    }
        //    else if (mouseDown && Input.GetMouseButton(0) && !isDragging)
        //    {
        //        mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        //        deltaX = mousePosition_current.x - mousePosition_previous.x;
        //        deltaX /= mainCamera.orthographicSize * 2;

        //        if (Mathf.Abs(deltaX) > dragThreshold)
        //        {
        //            isDragging = true;
        //            OnDragStartedEvemt?.Invoke();
        //            OnDragEvent?.Invoke(deltaX);
        //            mousePosition_previous = mousePosition_current;
        //        }
        //    }
        //    else if (mouseDown && Input.GetMouseButton(0) && isDragging)
        //    {
        //        mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        //        deltaX = mousePosition_current.x - mousePosition_previous.x;
        //        deltaX /= mainCamera.orthographicSize * 2;

        //        //if (Mathf.Abs(deltaX) > 0.05f)
        //        //{
        //        OnDragEvent?.Invoke(deltaX);
        //        mousePosition_previous = mousePosition_current;
        //        //}
        //    }
        //    if (mouseDown && Input.GetMouseButtonUp(0))
        //    {
        //        if (!isDragging)
        //        {
        //            //Point and Move
        //            mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        //            OnClickedEvent?.Invoke(mousePosition_current);
        //        }

        //        OnMouseUpEvent?.Invoke(isDragging);

        //        isDragging = false;
        //        mouseDown = false;
        //    }
        //}

        void HandleDrag()
        {
            // Unified pointer snapshot for this frame
            bool hasTouch = Input.touchCount > 0;

            bool down, held, up;
            Vector2 screenPos;

            if (hasTouch)
            {
                Touch t = Input.GetTouch(0);
                screenPos = t.position;

                down = t.phase == TouchPhase.Began;
                held = (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary);
                up = (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled);
            }
            else
            {
                screenPos = Input.mousePosition;

                down = Input.GetMouseButtonDown(0);
                held = Input.GetMouseButton(0);
                up = Input.GetMouseButtonUp(0);
            }

            // DOWN: keep your original "isActive only gates the down"
            if (isActive && down)
            {
                // Your old IsPointerOverObject() was mouse-centric.
                // This version blocks consistently for both mouse & touch.
                if (IsPointerOverUI(screenPos))
                    return;

                mouseDown = true;
                mousePosition_current = mainCamera.ScreenToWorldPoint(screenPos);
                mousePosition_previous = mainCamera.ScreenToWorldPoint(screenPos);
            }
            else if (mouseDown && held && !isDragging)
            {
                mousePosition_current = mainCamera.ScreenToWorldPoint(screenPos);

                deltaX = mousePosition_current.x - mousePosition_previous.x;
                deltaX /= mainCamera.orthographicSize * 2;

                if (Mathf.Abs(deltaX) > dragThreshold)
                {
                    isDragging = true;
                    OnDragStartedEvemt?.Invoke();
                    OnDragEvent?.Invoke(deltaX);
                    mousePosition_previous = mousePosition_current;
                }
            }
            else if (mouseDown && held && isDragging)
            {
                mousePosition_current = mainCamera.ScreenToWorldPoint(screenPos);

                deltaX = mousePosition_current.x - mousePosition_previous.x;
                deltaX /= mainCamera.orthographicSize * 2;

                OnDragEvent?.Invoke(deltaX);
                mousePosition_previous = mousePosition_current;
            }

            // UP: completes even if isActive became false mid-touch (your requirement)
            if (mouseDown && up)
            {
                if (!isDragging)
                {
                    mousePosition_current = mainCamera.ScreenToWorldPoint(screenPos);
                    OnClickedEvent?.Invoke(mousePosition_current);
                }

                OnMouseUpEvent?.Invoke(isDragging);

                isDragging = false;
                mouseDown = false;
            }
        }


        static bool IsPointerOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPos
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            // "Any UI element blocks"
            return results.Count > 0;
        }


        public static bool IsPointerOverObject()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> raycastResult = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResult);
            return EventSystem.current.IsPointerOverGameObject();
        }

        public static bool IsPointerOverUIObject()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                // UI only → GraphicRaycaster
                if (result.module is GraphicRaycaster)
                    return true;
            }

            return false;
        }

        public static GameObject GetClicked2DObject()
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            return (hit.collider != null) ? hit.collider.gameObject : null;
        }
    }
}