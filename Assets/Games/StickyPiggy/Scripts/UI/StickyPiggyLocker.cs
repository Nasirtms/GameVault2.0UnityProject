using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickyPiggyLocker : MonoBehaviour
{
    public GameObject lockerObject;
    public List<GameObject> lockBoxes = new List<GameObject>();

    public List<GameObject> chainObjects = new List<GameObject>();
    public float chainAnimationDuration = 1.7f;
}
