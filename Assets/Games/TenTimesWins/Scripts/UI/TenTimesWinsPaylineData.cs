using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TenTimesWinsPaylineData
{
    public int paylineNumber;
    public GameObject paylineSprite;

    [Tooltip("Flattened 3x3 matrix (row-major). Index = y * 3 + x")]
    public List<int> pattern = new List<int>(new int[9]);

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[3, 3];
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                matrix[x, y] = pattern[y * 3 + x];
            }
        }
        return matrix;
    }
}