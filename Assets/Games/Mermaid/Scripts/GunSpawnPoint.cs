using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunSpawnPoint : MonoBehaviour
{
    public GunPosition gunPosition;
    [HideInInspector] public float pointRotation;
    [HideInInspector] public bool booked;
    public float uiPos;
    public GameObject uiComponent;
    public Text betText;
    public Text amount;

    private void Awake()
    {
        if (gunPosition == GunPosition.Top)
        {
            pointRotation = 180;
        }
        else
        {
            pointRotation = 0;
        }
    }
}
