using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class FruitMaryFruiMaryGameSlotScript : MonoBehaviour
{
    [ReadOnly] public FruitMaryFruitMaryGameSlotResource currentResource;
    [SerializeField] private Image icon;

    public void GetRandom()
    {
        var random = FruitMaryFruitMaryGameSlotMachine.Instance.settings.slotResources[
                Random.Range(0, FruitMaryFruitMaryGameSlotMachine.Instance.settings.slotResources.Count)];

        SetType(random);
    }

    public void SetType(FruitMaryFruitMaryGameSlotResource newType)
    {
        this.currentResource = newType;
        icon.sprite = newType.slotSprite;
    }
}