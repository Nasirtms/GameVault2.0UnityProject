using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameSlotRegistry
{
    private static Dictionary<string, BaseSlotMachine> slotMachines = new();

    public static List<string> GetAllKeys()
    {
        return new List<string>(slotMachines.Keys);
    }



    public static string TrimSceneName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("❌ Check Register Parameter");
            return string.Empty;
        }

        Debug.Log("gam 1 : " + name);

        string updatedName = name.ToLower();

        if (updatedName.Contains("game"))
        {
            Debug.Log("gam 2 : " + updatedName);
            updatedName = updatedName.Replace("game", "");
            Debug.Log("gam 3 : " + updatedName);
        }

        return updatedName;
    }



    public static void Register(string sceneName, BaseSlotMachine machine)
    {
        slotMachines.Clear();
        var key = sceneName.ToLower();
        if (!slotMachines.ContainsKey(key))
        {
            slotMachines[key] = machine;
            Debug.Log($"✅ Registered slot machine: {key}");
        }
    }

    public static BaseSlotMachine GetMachine(string sceneName)
    {
        //Debug.Log("Scene Name : " + sceneName);
        slotMachines.TryGetValue(sceneName.ToLower(), out var machine);
        return machine;
    }

    public static void Clear()
    {
        slotMachines.Clear();
        Debug.Log("🔄 Cleared all registered slot machines.");
    }

}
