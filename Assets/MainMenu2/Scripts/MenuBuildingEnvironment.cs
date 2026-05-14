using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MainMenu
{
    public class MenuBuildingEnvironment : MenuEnvironment_Base
    {
        [Header("Building Env Settings")]
        public MenuBuilding lastEnteredBuilding;
        public List<MenuBuilding> buildings = new List<MenuBuilding>();
        public Transform buildingsSpawnPoint;
        public float buildingDistance = 2;
        public int currentSelectedBuildingIndex = -1;

        protected override void Initialize()
        {
            base.Initialize();

            if (lastEnteredBuilding != null)
            {
                moveTarget.position = new Vector3(lastEnteredBuilding.entryPoint.position.x, moveTarget.position.y, moveTarget.position.z);
                moveTargetPositionTemp = moveTarget.position;
                MainMenuManager.instance.player.transform.position = new Vector3(moveTargetPositionTemp.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
                MainMenuManager.instance.mainCamera.transform.position = new Vector3(moveTargetPositionTemp.x, MainMenuManager.instance.mainCamera.transform.position.y, MainMenuManager.instance.mainCamera.transform.position.z);
                lastEnteredBuilding = null;
            }
            else
            {
                moveTarget.position = moveTargetStartPosition;
                moveTargetPositionTemp = moveTarget.position;
                MainMenuManager.instance.player.transform.position = new Vector3(playerTargetStartPosition.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
            }
        }

        public void ForceInitializePositions()
        {
            moveTarget.position = moveTargetStartPosition;
            moveTargetPositionTemp = moveTarget.position;
            MainMenuManager.instance.player.transform.position = new Vector3(playerTargetStartPosition.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
        }

        protected override void MoveLeftOnePage()
        {
            if (isGoingInside)
                return;

            if (MainMenuManager.instance.player.isWalking && moveRightButtonConsecutivePressCount > 0)
                return;

            bool moveToEnd = false;
            moveRightButtonConsecutivePressCount = 0;
            //moveLeftButtonConsecutivePressCount++;

            if (!MainMenuManager.instance.player.isWalking)
            {
                moveLeftButtonConsecutivePressCount = 1;
            }
            else if (MainMenuManager.instance.player.isWalking && moveLeftButtonConsecutivePressCount < MainMenuManager.instance.moveToEndButtonPressThreshold)
            {
                moveLeftButtonConsecutivePressCount++;
                return;
            }
            else if (MainMenuManager.instance.player.isWalking && moveLeftButtonConsecutivePressCount >= MainMenuManager.instance.moveToEndButtonPressThreshold)
            {
                moveLeftButtonConsecutivePressCount = 0;
                moveToEnd = true;
            }
            else if (MainMenuManager.instance.player.isWalking && moveLeftButtonConsecutivePressCount > MainMenuManager.instance.moveToEndButtonPressThreshold)
            {
                return;
            }

            MenuBuilding buildingToMoveTo = null;
            if (!moveToEnd)
            {
                for (int i = buildings.Count - 1; i >= 0 && buildingToMoveTo == null; i--)
                {
                    if (buildings[i].entryPoint.position.x < moveTarget.position.x - 0.2f)
                    {
                        buildingToMoveTo = buildings[i];
                    }
                }
            }
            else
            {
                if (buildings.Count > 0)
                {
                    buildingToMoveTo = buildings[0];
                }
            }

            if (buildingToMoveTo != null)
            {
                MainMenuManager.instance.movingWithButtons = true;
                moveTargetPositionTemp.x = buildingToMoveTo.entryPoint.position.x;
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y); ;
                moveTarget.position = moveTargetPositionTemp;
                MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.x;
                MainMenuManager.instance.mainCamera.followTarget = MainMenuManager.instance.moveTarget.transform;
                MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
                if (!moveToEnd)
                    MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
                else
                {
                    MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.y;
                    MainMenuManager.instance.player.transform.position = new Vector3(moveTargetPositionTemp.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
                    MainMenuManager.instance.player.SetAvatarDirection(false);
                    //MainMenuManager.instance.player.moveSpeed = (((MainMenuManager.instance.player.moveSpeedMinMax.y - MainMenuManager.instance.player.moveSpeedMinMax.x) * .1f) + MainMenuManager.instance.player.moveSpeedMinMax.x);
                }
            }
        }

        protected override void MoveRightOnePage()
        {
            if (isGoingInside)
                return;

            if (MainMenuManager.instance.player.isWalking && moveLeftButtonConsecutivePressCount > 0)
                return;

            bool moveToEnd = false;
            moveLeftButtonConsecutivePressCount = 0;
            //moveRightButtonConsecutivePressCount++;

            if (!MainMenuManager.instance.player.isWalking)
            {
                moveRightButtonConsecutivePressCount = 1;
            }
            else if (MainMenuManager.instance.player.isWalking && moveRightButtonConsecutivePressCount < MainMenuManager.instance.moveToEndButtonPressThreshold)
            {
                moveRightButtonConsecutivePressCount++;
                return;
            }
            else if (MainMenuManager.instance.player.isWalking && moveRightButtonConsecutivePressCount >= MainMenuManager.instance.moveToEndButtonPressThreshold)
            {
                moveRightButtonConsecutivePressCount = 0;
                moveToEnd = true;
            }
            else if (MainMenuManager.instance.player.isWalking && moveRightButtonConsecutivePressCount > MainMenuManager.instance.moveToEndButtonPressThreshold)
            {
                return;
            }

            MenuBuilding buildingToMoveTo = null;
            if (!moveToEnd)
            {
                for (int i = 0; i < buildings.Count && buildingToMoveTo == null; i++)
                {
                    if (buildings[i].entryPoint.position.x > moveTarget.position.x + 0.2f)
                    {
                        buildingToMoveTo = buildings[i];
                    }
                }
            }
            else
            {
                if (buildings.Count > 0)
                {
                    buildingToMoveTo = buildings[buildings.Count - 1];
                }
            }

            if (buildingToMoveTo != null)
            {
                MainMenuManager.instance.movingWithButtons = true;
                moveTargetPositionTemp.x = buildingToMoveTo.entryPoint.position.x;
                moveTargetPositionTemp.x = Mathf.Clamp(moveTargetPositionTemp.x, environmentBounds.x, environmentBounds.y); ;
                moveTarget.position = moveTargetPositionTemp;
                MainMenuManager.instance.mainCamera.followDamping = MainMenuManager.instance.mainCamera.followDampingMinMax.x;
                MainMenuManager.instance.mainCamera.followTarget = MainMenuManager.instance.moveTarget.transform;
                MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
                if (!moveToEnd)
                    MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
                else
                {
                    MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.y;
                    MainMenuManager.instance.player.transform.position = new Vector3(moveTargetPositionTemp.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
                    MainMenuManager.instance.player.SetAvatarDirection(true);
                    //MainMenuManager.instance.player.moveSpeed = (((MainMenuManager.instance.player.moveSpeedMinMax.y - MainMenuManager.instance.player.moveSpeedMinMax.x) * .1f) + MainMenuManager.instance.player.moveSpeedMinMax.x);
                }
            }
        }

        public override void Clicked2DObject(GameObject clickedObject)
        {
            MenuBuilding mb = clickedObject.GetComponent<MenuBuilding>();
            if (mb != null)
            {
                int buildingIndex = buildings.IndexOf(mb);
                currentSelectedBuildingIndex = buildingIndex;
                GoInside(buildingIndex);
            }
        }

        void GoInside(int buildingIndex)
        {
            if (isGoingInside)
                return;

            isGoingInside = true;

            lastEnteredBuilding = buildings[buildingIndex];
            MainMenuManager.instance.player.followActive = false;
            MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
            MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
            MainMenuManager.instance.player.GoToTarget(buildings[buildingIndex].entryPoint.position);
            MainMenuManager.instance.player.transform.DOMoveX(buildings[buildingIndex].entryPoint.position.x, MainMenuManager.instance.player.moveSpeed).SetSpeedBased(true).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    float tweenTime = 1.5f;
                    MainMenuManager.instance.player.GoInside(tweenTime);
                    MainMenuManager.instance.mainCamera.PlayGoingInsideAnimation(tweenTime);
                    DOVirtual.DelayedCall(tweenTime + 0.5f, () =>
                    {
                        lastEnteredBuilding.OnClicked();
                        MainMenuManager.instance.GoToCategoryEnvironment(buildings[buildingIndex].categoryName);
                    });
                });
        }
    }
}