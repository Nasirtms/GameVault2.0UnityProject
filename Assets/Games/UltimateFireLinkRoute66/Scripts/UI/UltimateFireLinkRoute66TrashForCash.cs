using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkRoute66TrashForCash : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log("Trash for Cash Clicked");
        if (!UltimateFireLinkRoute66SlotMachine.Instance.isBonusGame) return;

        //if(!UltimateFireLinkChinaStreetSlotMachine.Instance.canClickTrashSlots) return;

        UltimateFireLinkRoute66SlotMachine.Instance.isBonusGame = false;
        //UltimateFireLinkChinaStreetSlotMachine.Instance.ShowWinAfterTrash();
    }
}
