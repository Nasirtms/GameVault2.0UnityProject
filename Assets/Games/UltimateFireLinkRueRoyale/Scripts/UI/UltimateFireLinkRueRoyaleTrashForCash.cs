using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltimateFireLinkRueRoyaleTrashForCash : MonoBehaviour
{
    private void OnMouseDown()
    {
        Debug.Log("Trash for Cash Clicked");
        if (!UltimateFireLinkRueRoyaleSlotMachine.Instance.isBonusGame) return;

        //if(!UltimateFireLinkChinaStreetSlotMachine.Instance.canClickTrashSlots) return;

        UltimateFireLinkRueRoyaleSlotMachine.Instance.isBonusGame = false;
        //UltimateFireLinkChinaStreetSlotMachine.Instance.ShowWinAfterTrash();
    }
}
