using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MovementPointUtil
{
    public static Vector3 GetOffsetVector(SpriteRenderer sr, Direction dir, float extraOffset)
    {
        float xOffset = sr.bounds.extents.x;
        float yOffset = sr.bounds.extents.y;


        switch (dir)
        {
            case Direction.Right:
                return new Vector3(xOffset + extraOffset, 0f, 0f);
            case Direction.Left:
                return new Vector3(-(xOffset + extraOffset), 0f, 0f);
            case Direction.Top:
                return new Vector3(0f, yOffset + extraOffset, 0f);
            case Direction.Bottom:
                return new Vector3(0f, -(yOffset + extraOffset), 0f);
            case Direction.TopRight:
                return (new Vector3(xOffset, yOffset, 0f).normalized * (Mathf.Max(xOffset, yOffset) + extraOffset));
            case Direction.TopLeft:
                return (new Vector3(-xOffset, yOffset, 0f).normalized * (Mathf.Max(xOffset, yOffset) + extraOffset));
            case Direction.BottomRight:
                return (new Vector3(xOffset, -yOffset, 0f).normalized * (Mathf.Max(xOffset, yOffset) + extraOffset));
            case Direction.BottomLeft:
                return (new Vector3(-xOffset, -yOffset, 0f).normalized * (Mathf.Max(xOffset, yOffset) + extraOffset));
            default:
                return Vector3.zero;
        }
    }
}
