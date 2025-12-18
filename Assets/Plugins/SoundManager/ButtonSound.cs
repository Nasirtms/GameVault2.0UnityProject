using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, IPointerEnterHandler
{
    
    public void OnPointerEnter(PointerEventData eventData)
    {

        SoundManager.Instance.PlaySFX("ButtonSelect");

    }
}
