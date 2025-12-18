using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager instance;
    public bool isDragging = false;
    public bool draggingwhileMovingWithButtons = false;
    public bool tapMoveWhileMovingWithButtons = false;
    public CameraFollowOnMove mainCamera;

    private void Awake()
    {
       instance = this;
    }
}
