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
                lastEnteredBuilding = null;
            }
            else
            {
                moveTarget.position = moveTargetStartPosition;
                moveTargetPositionTemp = moveTarget.position;
                MainMenuManager.instance.player.transform.position = new Vector3(playerTargetStartPosition.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
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