using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FruitMaryFruitMaryGameSlotType
{
    PINEAPPLE, ORANGE, BANANA, CHERRIES, LEMON, GRAPES, WATERMELON
}

[System.Serializable]
public struct FruitMaryFruitMaryGameSlotResource
{
    public FruitMaryFruitMaryGameSlotType slotType;
    [PreviewField] public Sprite slotSprite;
}

[CreateAssetMenu(menuName = "Settings/Fruit Mary - Fruit Mary Game", fileName = "FruitMaryFruitMaryGame")]
public class FruitMaryFruitMaryGameSettings : ScriptableObject
{
    [TabGroup("Slot Resources")]
    [TableList]
    public List<FruitMaryFruitMaryGameSlotResource> slotResources;

    [TabGroup("Spin Settings")]
    [HideLabel]
    public FruitMarySpinSettings spinSettings;
}
