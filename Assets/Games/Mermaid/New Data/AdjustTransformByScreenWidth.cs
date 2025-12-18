using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class AdjustTransformByScreenWidth : MonoBehaviour
{
    public SpritePositionHandler spritePositionHandler;

    [Space]
    public bool xPosition;
    public bool negative = false;
    public float xPositionOffset;

    [Space]
    public bool xScale;

    private float initialXScale;
    private float currentAspectRatio = 0;

    private const float defaultAspectRatio = 1.78f;

    private void Start()
    {
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
            spritePositionHandler.fixedPosition.x = (negative ? -1 : 1) * (Camera.main.aspect * Camera.main.orthographicSize) + xPositionOffset;

        if (xScale)
            spritePositionHandler.transform.localScale = new Vector3(((float)Camera.main.aspect / defaultAspectRatio), spritePositionHandler.transform.localScale.y, spritePositionHandler.transform.localScale.z);
    }
}
