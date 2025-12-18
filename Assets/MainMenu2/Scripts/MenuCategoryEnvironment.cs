using DG.Tweening;
using MainMenu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MainMenu
{
    public class MenuCategoryEnvironment : MenuEnvironment_Base
    {
        [Header("Category Env Settings")]
        public eGameCategories categoryName;

        [Space]
        public MenuGameMachine lastEnteredGameMachine;
        public Transform machinesContainer;
        public List<MenuGameMachine> gameMachines = new List<MenuGameMachine>();
        public Transform machinesSpawnPoint;
        public float machineDistance = 2;
        public int currentSelectedMachineIndex = -1;

        protected override void Initialize()
        {
            PopulateGameMachines();

            base.Initialize();

            //if (lastEnteredGameMachine != null)
            //{
            //    moveTarget.position = new Vector3(lastEnteredGameMachine.entryPoint.position.x, moveTarget.position.y, moveTarget.position.z);
            //    moveTargetPositionTemp = moveTarget.position;
            //    MainMenuManager.instance.player.transform.position = new Vector3(moveTargetPositionTemp.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
            //    lastEnteredGameMachine = null;
            //}
            //else
            //{
            //    moveTarget.position = moveTargetStartPosition;
            //    moveTargetPositionTemp = moveTarget.position;
            //    MainMenuManager.instance.player.transform.position = new Vector3(playerTargetStartPosition.x, MainMenuManager.instance.player.transform.position.y, MainMenuManager.instance.player.transform.position.z);
            //}
        }

        public override void Clicked2DObject(GameObject clickedObject)
        {
            MenuGameMachine mgm = clickedObject.GetComponent<MenuGameMachine>();
            if (mgm != null)
            {
                int machineIndex = gameMachines.IndexOf(mgm);
                currentSelectedMachineIndex = machineIndex;
                GoInside(machineIndex);
            }
        }

        void GoInside(int machineIndex)
        {
            if (isGoingInside)
                return;

            isGoingInside = true;

            MainMenuManager.instance.player.followActive = false;
            MainMenuManager.instance.player.moveSpeed = MainMenuManager.instance.player.moveSpeedMinMax.x;
            MainMenuManager.instance.moveTargetMarker.followTarget = MainMenuManager.instance.player.transform;
            MainMenuManager.instance.player.GoToTarget(gameMachines[machineIndex].entryPoint.position);
            MainMenuManager.instance.player.transform.DOMoveX(gameMachines[machineIndex].entryPoint.position.x, MainMenuManager.instance.player.moveSpeed).SetSpeedBased(true).SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    float tweenTime = 1.5f;
                    MainMenuManager.instance.player.GoInsideMachine(tweenTime);
                    MainMenuManager.instance.mainCamera.PlayGoingInsideMachineAnimation(tweenTime);
                    DOVirtual.DelayedCall(tweenTime, () =>
                    {
                        MainMenuManager.instance.GoToMachine(gameMachines[machineIndex]);
                    });
                });
        }

        public void PopulateGameMachines()
        {
            if (gameMachines.Count > 0)
                return;

            List<GameItem> gamesList = new List<GameItem>();
            gamesList = GameCatalogueController.instance.gameItems;
            //List<SerializableClasses.Game> gamesList = new List<SerializableClasses.Game>();
            //gamesList = SceneManagement.games;

            Func<GameItem, eGameCategories, bool> condition;

            if (categoryName == eGameCategories.Hot)
                condition = (game, categoryName) => game.Gametitle.ToLower().Contains("new");
            else if (categoryName == eGameCategories.Favorites)
                condition = (game, categoryName) => game.is_favorite;
            else
                condition = (game, categoryName) => game.category.ToLower() == categoryName.ToString().ToLower();

            Vector3 spawnPositionTemp = machinesSpawnPoint.localPosition;

            foreach (GameItem game in gamesList)
            {
                if (!game.is_active) continue;
                if (SceneManagement.sceneAccessType == SceneAccessType.Publish && !game.is_publish) continue;
                if (SceneManagement.sceneAccessType == SceneAccessType.Dev && game.is_publish) continue;

                if (condition(game, categoryName))
                {
                    // Check if exists in MenuEnvironmentDatabase
                    MenuGameMachine machinePrefab = MainMenuManager.instance.menuEnvironment.menuEnvDatabase.gameMachinesPrefabs.FirstOrDefault(x => x.gameName.ToLower() == game.name.ToLower());
                    if (machinePrefab != null)
                    {
                        // Instantiate and set data
                        MenuGameMachine menuGameMachine = Instantiate(machinePrefab, machinesContainer);
                        menuGameMachine.gameID = game.id;
                        menuGameMachine.sceneName = "Game" + game.name.Replace(" ", "");
                        //menuGameMachine.addressableLabel = game.name.Replace(" ", "_").ToLower();
                        menuGameMachine.addressableLabel = game.addressableLabel;

                        // Set position
                        if (gameMachines.Count > 0)
                        {
                            spawnPositionTemp.x += (gameMachines[gameMachines.Count - 1].machineSpriteWidth / 2) + machineDistance;
                        }
                        spawnPositionTemp.x += menuGameMachine.machineSpriteWidth / 2;
                        menuGameMachine.transform.localPosition = spawnPositionTemp;
                        
                        gameMachines.Add(menuGameMachine);
                    }
                    else
                    {
                        Debug.LogError($"Game Machine Prefab not found for game: {game.name}");
                    }
                }
            }

            // Set bounds max position
            environmentBoundsMaxTransform.localPosition = new Vector3(spawnPositionTemp.x + machineDistance + (gameMachines.Count > 0 ? (gameMachines[gameMachines.Count - 1].machineSpriteWidth / 2) : 0),
                environmentBoundsMaxTransform.localPosition.y,
                environmentBoundsMaxTransform.localPosition.z);
            UpdateBoundsValues();
            MainMenuManager.instance.mainCamera.UpdateBounds(environmentBounds.x, environmentBounds.y);
        }

        public void ResetMachinesList()
        {
            for (int i = gameMachines.Count-1; i >=0; i--)
            {
                Destroy(gameMachines[i].gameObject);
            }
            gameMachines.Clear();

            environmentBoundsMaxTransform.localPosition = environmentBoundsMinTransform.localPosition + new Vector3(machineDistance, 0, 0);
            UpdateBoundsValues();
            MainMenuManager.instance.mainCamera.UpdateBounds(environmentBounds.x, environmentBounds.y);
        }
    }
}