using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkChinaStreetTrashForCash : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log("Trash for Cash Clicked");
        if (!UltimateFireLinkChinaStreetSlotMachine.Instance.isBonusGame) return;

        //if(!UltimateFireLinkChinaStreetSlotMachine.Instance.canClickTrashSlots) return;

        UltimateFireLinkChinaStreetSlotMachine.Instance.isBonusGame = false;
        //UltimateFireLinkChinaStreetSlotMachine.Instance.ShowWinAfterTrash();
    }
}
