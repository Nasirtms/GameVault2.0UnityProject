using MainMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainMenu
{
    public class MenuEnvironment_Base : MonoBehaviour
    {
        public Transform environmentBoundsMinTransform;
        public Transform environmentBoundsMaxTransform;
        public Vector2 environmentBounds;
        public Vector2 moveTargetStartPosition;
        public Vector2 playerTargetStartPosition;

        [Header("Camera Settings")]
        public float cameraStartPositionY = -0.9f;
        public float cameraStartOrthoSize = 4f;

        [Header("DragSettings")]
        public float dragSpeed = 10;
        public float pageWidth = 15;
        public Transform moveTarget;

        protected Vector3 moveTargetPositionTemp;
        protected bool isGoingInside = false;

        public bool isActive;

        //public int moveToEndButtonPressThreshold = 3;
        protected int moveLeftButtonConsecutivePressCount = 0;
        protected int moveRightButtonConsecutivePressCount = 0;

        private void OnEnable()
        {
            Initialize();

            UIDragHandler.OnDragStartedEvemt += DragStarted;
            UIDragHandler.OnDragEvent += Dragged;
            UIDragHandler.OnClickedEvent += Clicked;
            UIDragHandler.OnMouseUpEvent += MouseUp;
            MainMenuManager.OnLeftButtonClicked += MoveLeftOnePage;
            MainMenuManager.OnRightButtonClicked += MoveRightOnePage;
        }

        private void OnDisable()
        {
            isActive = false;

            UIDragHandler.OnDragStartedEvemt -= DragStarted;
            UIDragHandler.OnDragEvent -= Dragged;
            UIDragHandler.OnClickedEvent -= Clicked;
            UIDragHandler.OnMouseUpEvent -= MouseUp;
            MainMenuManager.OnLeftButtonClicked -= MoveLeftOnePage;
            MainMenuManager.OnRightButtonClicked -= MoveRightOnePage;
        }

        protected virtual void Initialize()
        {
            isActive = true;
            isGoingInside = false;
            UpdateBoundsValues();
            MainMenuManager.instance.moveTarget = moveTarget;
            MainMenuManager.instance.player.followTarget = moveTarget;
            MainMenuManager.instance.player.Initialize();
            MainMenuManager.instance.player.followActive = true;
            MainMenuManager.instance.mainCamera.UpdateBounds(environmentBounds.x, environmentBounds.y);
            MainMenuManager.instance.mainCamera.Initialize();
            MainMenuManager.instance.mainCamera.transform.position = new Vector3(MainMenuManager.instance.mainCamera.transform.position.x, cameraStartPositionY, MainMenuManager.instance.mainCamera.transform.position.z);
            MainMenuManager.instance.mainCamera.camera.orthographicSize = cameraStartOrthoSize;
            MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.moveTarget;

            moveTarget.position = moveTargetStartPosition;
            moveTargetPositionTemp = moveTarget.position;
            MainMenuManager.instance.player.transform.position = new Vector3(playerTargetStartPosition.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
            

        }

        protected void UpdateBoundsValues()
        {
            environmentBounds.x = environmentBoundsMinTransform.position.x;
            environmentBounds.y = environmentBoundsMaxTransform.position.x;
        }

        void DragStarted()
        {
            if (isGoingInside)
                return;

            if (!MainMenuManager.instance.movingWithButtons)
            {
                moveTargetPositionTemp = new Vector3(MainMenuManager.instance.player.transform.position.x, moveTargetPositionTemp.y, moveTargetPositionTemp.z);
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y);
                moveTarget.position = moveTargetPositionTemp;
            }
        }

        void Dragged(float deltaX)
        {
            if (isGoingInside)
                return;

            if (!isActive)
                return;

            MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;

            MainMenuManager.instance.isDragging = true;

            if (MainMenuManager.instance.movingWithButtons)
            {
                MainMenuManager.instance.draggingwhileMovingWithButtons = true;
            }
            else
            {
                MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
            }
            if (MainMenuManager.instance.draggingwhileMovingWithButtons)
            {
                MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
                //MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.x;

                moveTargetPositionTemp += new Vector3(-deltaX * dragSpeed, 0, 0);
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y);
                moveTarget.position = Vector3.Lerp(moveTarget.position, moveTargetPositionTemp, Time.deltaTime * 3);
            }
            else
            {
                MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.y;

                moveTargetPositionTemp += new Vector3(-deltaX * dragSpeed, 0, 0);
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y);
                moveTarget.position = Vector3.Lerp(moveTarget.position, moveTargetPositionTemp, Time.deltaTime * 3);
            }
        }

        void Clicked(Vector2 mousePosition)
        {
            if (isGoingInside)
                return;

            if (!isActive)
                return;

            //GoInside
            GameObject clickedObject = UIDragHandler.GetClicked2DObject();
            if (!MainMenuManager.instance.movingWithButtons && clickedObject != null)
            {
                Clicked2DObject(clickedObject);
            }
            else
            {
                moveTargetPositionTemp = new Vector3(mousePosition.x, moveTargetPositionTemp.y, moveTargetPositionTemp.z);
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y);
                moveTarget.position = moveTargetPositionTemp;
                MainMenuManager.instance.mainCamera.followTarget = MainMenuManager.instance.player.transform;
                MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.moveTarget;
                MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;

                if (MainMenuManager.instance.movingWithButtons)
                {
                    MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.x;
                }
            }
        }

        private void MouseUp(bool wasDragging)
        {
            if (isGoingInside)
                return;

            if (!isActive)
                return;

            if (wasDragging && !MainMenuManager.instance.draggingwhileMovingWithButtons)
            {
                moveTargetPositionTemp = new Vector3(MainMenuManager.instance.player.transform.position.x, moveTargetPositionTemp.y, moveTargetPositionTemp.z);
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y);
                moveTarget.position = moveTargetPositionTemp;
            }
            MainMenuManager.instance.isDragging = false;
            MainMenuManager.instance.draggingwhileMovingWithButtons = false;
        }

        protected virtual void MoveLeftOnePage()
        {
            if (isGoingInside)
                return;

            if (MainMenuManager.instance.player.isWalking)
                return;

            MainMenuManager.instance.movingWithButtons = true;
            moveTargetPositionTemp.x += -pageWidth;
            moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y); ;
            moveTarget.position = moveTargetPositionTemp;
            MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.x;
            MainMenuManager.instance.mainCamera.followTarget = MainMenuManager.instance.moveTarget.transform;
            MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
            MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
        }

        protected virtual void MoveRightOnePage()
        {
            if (isGoingInside)
                return;

            if (MainMenuManager.instance.player.isWalking)
                return;

            MainMenuManager.instance.movingWithButtons = true;
            moveTargetPositionTemp.x += pageWidth;
            moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y); ;
            moveTarget.position = moveTargetPositionTemp;
            MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.x;
            MainMenuManager.instance.mainCamera.followTarget = MainMenuManager.instance.moveTarget.transform;
            MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
            MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
        }

        public virtual void Clicked2DObject(GameObject clickedObject)
        {

        }
    }
}