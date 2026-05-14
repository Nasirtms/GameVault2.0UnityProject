using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkRiverWalkTrashForCash : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log("Trash for Cash Clicked");
        if (!UltimateFireLinkRiverWalkSlotMachine.Instance.isBonusGame) return;

        //if(!UltimateFireLinkChinaStreetSlotMachine.Instance.canClickTrashSlots) return;

        UltimateFireLinkRiverWalkSlotMachine.Instance.isBonusGame = false;
        //UltimateFireLinkChinaStreetSlotMachine.Instance.ShowWinAfterTrash();
    }
}
