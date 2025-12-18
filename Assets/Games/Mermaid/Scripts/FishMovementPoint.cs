using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Right,          // +X
    Left,           // -X
    Top,            // +Y
    Bottom,         // -Y
    TopRight,       // +X +Y
    TopLeft,        // -X +Y
    BottomRight,    // +X -Y
    BottomLeft      // -X -Y
}

public class FishMovementPoint : MonoBehaviour
{
    public Direction direction;

    [Tooltip("Extra world units to offset spawn or destroy position along the direction")]
    public float extraOffset = 20f;
}
