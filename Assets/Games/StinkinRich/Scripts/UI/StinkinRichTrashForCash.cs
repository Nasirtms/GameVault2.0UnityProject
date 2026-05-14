using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StinkinRichTrashForCash : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log("Trash for Cash Clicked");
        if (!StinkinRichSlotMachine.Instance.isBonusGame) return;

        if(!StinkinRichSlotMachine.Instance.canClickTrashSlots) return;

        StinkinRichPaylineController.Instance.ShowTrashMultipliers(transform.GetComponentInParent<StinkinRichSlotScript>());
        StinkinRichSlotMachine.Instance.isBonusGame = false;
        StinkinRichSlotMachine.Instance.ShowWinAfterTrash();
    }
}
