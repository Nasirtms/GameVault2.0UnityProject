using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MainMenu
{
    [CreateAssetMenu(fileName = "MenuEnvironmentDatabase", menuName = "Scriptable/MenuEnvironmentDatabase")]
    public class MenuEnvironmentDatabase : ScriptableObject
    {
        [SerializeField] public List<MenuGameMachine> gameMachinesPrefabs = new List<MenuGameMachine>();
    }
}