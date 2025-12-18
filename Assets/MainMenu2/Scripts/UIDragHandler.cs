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

        void HandleDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {

                if (IsPointerOverUIObject())
                    return;

                mouseDown = true;
                mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePosition_previous = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (mouseDown && Input.GetMouseButton(0) && !isDragging)
            {
                mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);

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
            else if (mouseDown && Input.GetMouseButton(0) && isDragging)
            {
                mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);

                deltaX = mousePosition_current.x - mousePosition_previous.x;
                deltaX /= mainCamera.orthographicSize * 2;

                //if (Mathf.Abs(deltaX) > 0.05f)
                //{
                OnDragEvent?.Invoke(deltaX);
                mousePosition_previous = mousePosition_current;
                //}
            }
            if (mouseDown && Input.GetMouseButtonUp(0))
            {
                if (!isDragging)
                {
                    //Point and Move
                    mousePosition_current = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    OnClickedEvent?.Invoke(mousePosition_current);
                }

                OnMouseUpEvent?.Invoke(isDragging);

                isDragging = false;
                mouseDown = false;
            }
        }

        public static bool IsPointerOverUIObject()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            List<RaycastResult> raycastResult = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResult);
            return EventSystem.current.IsPointerOverGameObject();
        }

        public static GameObject GetClicked2DObject()
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            return (hit.collider != null) ? hit.collider.gameObject : null;
        }
    }
}