using System.Collections.Generic;
using UnityEngine;

public class QuickHitVolcanoQuickPickLightManager : MonoBehaviour
{
    [System.Serializable]
    public class LightGroup
    {
        public QuickHitVolcanoQuickPickSymbolType type;
        public List<GameObject> lights; // 3 lights under this type
    }

    [Header("Light Groups")]
    public List<LightGroup> lightGroups = new();

    // Internal tracking of how many lights are on
    private Dictionary<QuickHitVolcanoQuickPickSymbolType, int> activeCount = new();

    private void Awake()
    {
        ResetLights();
    }

    public void ResetLights()
    {
        activeCount.Clear();

        foreach (var group in lightGroups)
        {
            activeCount[group.type] = 0;

            foreach (var light in group.lights)
            {
                light.SetActive(false);
            }
        }
    }

    public void FillLight(QuickHitVolcanoQuickPickSymbolType type)
    {
        if (!activeCount.ContainsKey(type)) return;

        int count = activeCount[type];
        if (count >= 3) return;

        var group = lightGroups.Find(g => g.type == type);
        if (group != null && group.lights.Count > count)
        {
            group.lights[count].SetActive(true);
            activeCount[type]++;
        }
    }

    public void FillLightForAll()
    {
        foreach (var group in lightGroups)
        {
            FillLight(group.type);
        }
    }
}
