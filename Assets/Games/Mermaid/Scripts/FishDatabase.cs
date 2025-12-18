using System.Collections.Generic;
using UnityEngine;


public enum BatchPattern
{
    Line,
    Arc,
    VShape,
    Circle
}


[System.Serializable]
public class FishData
{
    public string fishName;
    public GameObject sprite;
    public int maxHealth = 1;
    public float prizeAmount = 1f;
    public float minSpeed = 1f;
    public float maxSpeed = 3f;
    //public float spawnInterval = 2f;
    public float spawnOffset = 0.5f;
    public int batchSize = 1;
    public float batchSpacing = 0.5f;
    public bool isRotatable;
    public float rotationSpeed = 0.5f;
    public int fishMultiplyer;
    public bool isBomb;
    public bool isPowerUp;
    //// NEW
    //public RuntimeAnimatorController animatorController;

    [Header("Spawn Settings")]
    public bool allowOnlyOne = false;   // ✅ only one at a time
    [Range(1, 100)] public int rarityWeight = 50; // ✅ common/rare control

    [Header("Batch Pattern")]
    public BatchPattern spawnPattern = BatchPattern.Line;

}

[System.Serializable]
public class PatternFishGroup
{
    [Tooltip("Pattern fish data to apply")]
    public FishData fishData;

}

[System.Serializable]
public class BonusFishSet
{
    public float bonusFishSpeed;

    [Tooltip("Movement type for this bonus set (Forward or Rotate)")]
    public BonusFishMovementType movementType;

    [Tooltip("Optional offset applied to the base spawn point")]
    public Vector2 spawnOffset;

    [Tooltip("Big fish data list (applied to all children from bigFishRoot)")]
    public List<FishData> bigFishes;

    [Tooltip("Pattern fish groups (FishData + Root GameObject)")]
    public List<PatternFishGroup> patternFishGroups;
}




[CreateAssetMenu(fileName = "FishDatabase", menuName = "Scriptable/Fish Database")]
public class FishDatabase : ScriptableObject
{
    public List<FishData> fishList;
    public List<BonusFishSet> bonusFishes;
}