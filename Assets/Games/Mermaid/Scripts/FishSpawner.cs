using Supabase.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;


[Serializable]
public class FishDataSpawner {

    public string fishId;
    public List<GameObject> fishObject = new List<GameObject>();
    public int currentActive;
}

public class FishSpawner : MonoBehaviour
{
    [SerializeField] public List<FishDataSpawner> fishData = new List<FishDataSpawner>();


    private void OnEnable()
    {
        Fish.OnFishRRemoved += OnFishKilled;
    }
    private void OnDisable()
    {
        Fish.OnFishRRemoved -= OnFishKilled;
    }




    public void SetObject(GameObject g, string fishName)
    {
        if (g == null)
        {
            Debug.LogWarning("Fish GameObject reference is null, skipping...");
            return;
        }
        if (g.GetComponent<Fish>())
        {
            var getFish = fishData.Find(x => x.fishId == fishName);
            if (getFish != null)
            {
                getFish.fishObject.Add(g);

            }
            else {

                FishDataSpawner fish = new FishDataSpawner();
                fish.fishId = fishName;
                fish.fishObject.Add(g);
                fishData.Add(fish);

            }

        }
    
    
    }


    public void OverrideFishDataFromActive(FishData fish) {

        var getFishFromSpawnner = fishData.Find(x => x.fishId == fish.fishName);

        if (getFishFromSpawnner != null)
        {
            var fishobj = getFishFromSpawnner.fishObject[UnityEngine.Random.Range(0, getFishFromSpawnner.fishObject.Count)];
            fishobj.GetComponent<Fish>().fishData = fish;
        }
    }

    void OnFishKilled(Fish killedFish)
    {
        var getFishFromSpawnner = fishData.Find(x => x.fishId == killedFish.fishData.fishName);
        if (getFishFromSpawnner != null)
        {
            getFishFromSpawnner.currentActive--;
        }

    }
    [ContextMenu("GetTotalActiveCount")]
    public void getToalCount() {
        Debug.Log("getTotal " + GetTotalActiveCount());
    }

    [ContextMenu("GetUniquqNames")]
    public void getUniqueNames() {

        var getNames = fishData.FindAll(x => x.currentActive > 0);
        Debug.Log("Total Spawn ==== " + getNames.Count);
    }

    public int GetTotalActiveCount()
    {
      return fishData
     .Where(x => x.currentActive > 0)
     .Sum(x => x.currentActive);
    }

}
