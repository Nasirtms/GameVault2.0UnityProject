using UnityEngine;
using System.Collections;
using DG.Tweening;

public class SequentialPopupManager : MonoBehaviour
{
    [Header("Popup References")]
    public GameObject popup1;
    public GameObject popup2;

    [Header("Timing Settings")]
    public float delayBeforeFirstPopup = 1f;
    public float delayBetweenPopups = 1f;
    bool showBothPopup = false;
    private static bool hasShownPopups = false; // ✅ Static: persists across scene reloads

}
