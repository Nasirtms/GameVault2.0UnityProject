using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjustTransformByScreenWidth : MonoBehaviour
{
    public SpritePositionHandler spritePositionHandler;

    [Space]
    public bool xPosition;
    public bool negative = false;
    //public bool relativeToStartingPosition;
    public float xPositionOffset;

    [Space]
    public bool xScale;

    //private float startingDistanceFromEdge;
    private float initialXScale;
    private float currentAspectRatio = 0;

    private const float defaultAspectRatio = 1.78f;

    private void Start()
    {
        //if (relativeToStartingPosition)
        //{
        //    startingDistanceFromEdge = negative switch
        //    {
        //        false => spritePositionHandler.fixedPosition.x - (Camera.main.aspect * Camera.main.orthographicSize),
        //        true => -(Camera.main.aspect * Camera.main.orthographicSize) - spritePositionHandler.fixedPosition.x
        //    };
        //}
        initialXScale = spritePositionHandler.transform.localScale.x;
    }

    private void Update()
    {
        if (spritePositionHandler == null)
            return;

        if (Camera.main.aspect != currentAspectRatio)
        {
            UpdateValues();
            currentAspectRatio = Camera.main.aspect;
        }

    }

    void UpdateValues()
    {
        if (xPosition)
            spritePositionHandler.fixedPosition.x = (negative ? -1 : 1) * (Camera.main.aspect * Camera.main.orthographicSize)/* + (relativeToStartingPosition ? startingDistanceFromEdge : 0)*/ + xPositionOffset;

        if (xScale)
            spritePositionHandler.transform.localScale = new Vector3(((float)Camera.main.aspect / defaultAspectRatio), spritePositionHandler.transform.localScale.y, spritePositionHandler.transform.localScale.z);
    }
}
