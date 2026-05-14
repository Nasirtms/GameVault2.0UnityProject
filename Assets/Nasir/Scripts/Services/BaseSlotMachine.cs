using UnityEngine;
using System.Collections.Generic;


public abstract class BaseSlotMachine : MonoBehaviour
{
    public bool isFreeGame { get; set; }
    public bool InSpin { get; set; }
    public bool isStopBtnPressed { get; set; }
    public abstract void ClearPaylines();
    public abstract void Spin();
    public abstract void StopSpinGettingError(); 

    public List<List<SymbolData>> spinSymbolMatrix = new();
}