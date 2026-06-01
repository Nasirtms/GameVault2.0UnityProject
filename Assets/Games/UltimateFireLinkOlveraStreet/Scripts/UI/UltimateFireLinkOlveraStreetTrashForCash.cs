using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkOlveraStreetTrashForCash : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log("Trash for Cash Clicked");
        if (!UltimateFireLinkOlveraStreetSlotMachine.Instance.isBonusGame) return;

        //if(!UltimateFireLinkChinaStreetSlotMachine.Instance.canClickTrashSlots) return;

        UltimateFireLinkOlveraStreetSlotMachine.Instance.isBonusGame = false;
        //UltimateFireLinkChinaStreetSlotMachine.Instance.ShowWinAfterTrash();
    }
}
