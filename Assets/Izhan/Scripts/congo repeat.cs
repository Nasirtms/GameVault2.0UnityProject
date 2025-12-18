using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class congorepeat : MonoBehaviour
{
    [Tooltip("Assign the GameObject with the AnimationTimer script")]
    public GameObject animationTimerObject;

    [Tooltip("Time between reactivations")]
    public float reactivateInterval = 5f;

    void Start()
    {
        InvokeRepeating(nameof(ReactivateTimerObject), reactivateInterval, reactivateInterval);
    }

    void ReactivateTimerObject()
    {
        if (animationTimerObject != null && !animationTimerObject.activeSelf)
        {
            animationTimerObject.SetActive(true);
        }
    }
}
