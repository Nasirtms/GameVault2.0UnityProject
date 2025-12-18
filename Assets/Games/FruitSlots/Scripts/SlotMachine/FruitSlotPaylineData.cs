using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FruitSlotPaylineData
{
    public int paylineNumber;

    [Tooltip("Flattened 3x3 matrix (row-major). Index = y * 5 + x")]
    public List<int> pattern = new List<int>(new int[15]);

    public int[,] ToMatrix()
    {
        int[,] matrix = new int[5, 3];
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                matrix[x, y] = pattern[y * 5 + x];
            }
        }
        return matrix;
    }
}