using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieParadisePaylineDrawer : MonoBehaviour
{
    public static ZombieParadisePaylineDrawer Instance { get; private set; }

    public float waitTime;
    [Header("Assign all symbol positions (columns x rows)")]
    public Transform[,] symbolPositions;

    [Tooltip("Set in Inspector: Flattened array of all positions (col-major order)")]
    public Transform[] symbolRefs;

    [Header("Payline Colors")]
    [SerializeField] private Color[] paylineColors;

    [Header("Grid Settings")]
    public int columns = 5;
    public int rows = 4;

    [Header("Payline Drawing")]
    public int paylineNumber;
    public GameObject lineSegmentPrefab;

    private List<GameObject> activeSegments = new List<GameObject>();
    private int[][] paylines;

    [SerializeField] private float scaleX;
    [SerializeField] private float scaleY;

    private void Awake()
    {
        // Creating Instance
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }

        // Build 2D array from flat references
        symbolPositions = new Transform[columns, rows];
        int index = 0;
        for (int col = 0; col < columns; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                if (index < symbolRefs.Length)
                    symbolPositions[col, row] = symbolRefs[index];
                index++;
            }
        }

        paylines = GetPaylines(); // load 50 paylines
    }

    [ContextMenu("DrawPayline")]
    public void DrawPaylineContext()
    {
        DrawPayline(paylineNumber - 1);
    }

    public void DrawPayline(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= paylines.Length) return;

        ClearPaylines();
        int[] line = paylines[lineIndex];

        //for (int i = 0; i < line.Length; i++)
        //    Debug.Log("Paylines: " + line[i]);
        
        Color lineColor = paylineColors[Random.Range(0, paylineColors.Length)];

        for (int col = 0; col < line.Length - 1; col++)
        {
            int currentRow = line[col];
            int nextRow = line[col + 1];

            Vector3 start = symbolPositions[col, currentRow].position;
            Vector3 end = symbolPositions[col + 1, nextRow].position;

            float distance = Vector3.Distance(start, end);

            // Spawn as child of start symbol
            GameObject seg = Instantiate(lineSegmentPrefab, symbolPositions[col, currentRow]);
            seg.transform.localPosition = Vector3.zero;
            Transform PaylineImageTransform = seg.transform.GetChild(1);
            Transform CircleImageTransform = seg.transform.GetChild(0);

            PaylineImageTransform.GetComponent<SpriteRenderer>().color = lineColor;
            CircleImageTransform.GetComponent<SpriteRenderer>().color = lineColor;

            if (col != 0)
            {
                int layer = PaylineImageTransform.GetComponent<SpriteRenderer>().sortingOrder;
                PaylineImageTransform.GetComponent<SpriteRenderer>().sortingOrder = layer -1;
            }


            // --- Rotation rule ---
            if (nextRow == currentRow)
            {
                seg.transform.localRotation = Quaternion.identity; // flat
            }
            else if (nextRow > currentRow)
            {
                seg.transform.localRotation = Quaternion.Euler(0, 0, 325f); // upward
            }
            else
            {
                seg.transform.localRotation = Quaternion.Euler(0, 0, 35f); // downward
            }

            // --- Target scale depends on line type ---
            Vector3 targetScale;
            if (nextRow == currentRow)
            {
                targetScale = new Vector3(0.2f, 0.2f, 1f);
                CircleImageTransform.transform.localPosition = new Vector3(2f,0f,0f);
            }
            else
            {
                targetScale = new Vector3(scaleX, scaleY, 1f); // (1.3,1.3,1)
            }

           // ---Start collapsed(Y = 0)-- -
           PaylineImageTransform.transform.localScale = targetScale;

            // --- Animate Y to final ---
            seg.transform.DOScaleY(1f, 0.4f).SetEase(Ease.OutBack);

            activeSegments.Add(seg);
        }
    }

    public void ClearPaylines()
    {
        foreach (GameObject seg in activeSegments)
        {
            if (seg != null) Destroy(seg);
        }
        activeSegments.Clear();
    }

    int num = 0;
    [ContextMenu("Draw_All_Paylines")]
    public void Draw_All_Paylines()
    {
        StopAllCoroutines();
        num = 0;
        StartCoroutine(MakeAllPaylines());
    }

    IEnumerator MakeAllPaylines()
    {
        if (num >= 50) // stop after last payline
            yield break;

        ClearPaylines();
        DrawPayline(num);
        num++;

        yield return new WaitForSeconds(waitTime);

        // call again until finished
        StartCoroutine(MakeAllPaylines());
    }




    // Define all 50 paylines
    private int[][] GetPaylines()
    {
        var paylines = new List<int[]>();

        // Paylines 1-4: Horizontal Lines
        paylines.Add(new[] { 1, 1, 1, 1, 1 }); // Payline 1
        paylines.Add(new[] { 2, 2, 2, 2, 2 }); // Payline 2
        paylines.Add(new[] { 0, 0, 0, 0, 0 }); // Payline 3
        paylines.Add(new[] { 3, 3, 3, 3, 3 }); // Payline 4

        // Paylines 5-8: Diagonal Lines
        paylines.Add(new[] { 0, 1, 2, 3, 2 }); // Payline 5
        paylines.Add(new[] { 3, 2, 1, 0, 1 }); // Payline 6
        paylines.Add(new[] { 1, 0, 1, 2, 1 }); // Payline 7
        paylines.Add(new[] { 2, 3, 2, 1, 2 }); // Payline 8

        // Paylines 9-10: V-Shapes
        paylines.Add(new[] { 1, 2, 3, 3, 3 }); // Payline 9
        paylines.Add(new[] { 2, 1, 0, 0, 0 }); // Payline 10

        // Paylines 11-12: W-Shapes
        paylines.Add(new[] { 1, 2, 2, 3, 3 }); // Payline 11
        paylines.Add(new[] { 2, 1, 1, 0, 0 }); // Payline 12

        // Paylines 13-14: Pyramid Patterns
        paylines.Add(new[] { 1, 1, 0, 1, 1 }); // Payline 13
        paylines.Add(new[] { 2, 2, 3, 2, 2 }); // Payline 14

        // Paylines 15-17: Zigzag Patterns
        paylines.Add(new[] { 0, 1, 1, 1, 0 }); // Payline 15
        paylines.Add(new[] { 3, 2, 2, 2, 3 }); // Payline 16
        paylines.Add(new[] { 1, 0, 0, 0, 1 }); // Payline 17

        // Paylines 18-21: Step Patterns
        paylines.Add(new[] { 2, 3, 3, 3, 2 }); // Payline 18
        paylines.Add(new[] { 0, 0, 1, 0, 0 }); // Payline 19
        paylines.Add(new[] { 3, 3, 2, 3, 3 }); // Payline 20
        paylines.Add(new[] { 0, 0, 1, 2, 3 }); // Payline 21

        // Paylines 22-23: Cross Patterns
        paylines.Add(new[] { 3, 3, 2, 1, 0 }); // Payline 22
        paylines.Add(new[] { 1, 2, 1, 2, 1 }); // Payline 23

        // Paylines 24-43: Geometric Patterns
        paylines.Add(new[] { 2, 1, 2, 1, 2 }); // 24
        paylines.Add(new[] { 0, 1, 0, 1, 0 }); // 25
        paylines.Add(new[] { 3, 2, 3, 2, 3 }); // 26
        paylines.Add(new[] { 1, 0, 1, 0, 1 }); // 27
        paylines.Add(new[] { 2, 3, 2, 3, 2 }); // 28
        paylines.Add(new[] { 0, 0, 0, 1, 1 }); // 29
        paylines.Add(new[] { 3, 3, 3, 2, 2 }); // 30
        paylines.Add(new[] { 1, 1, 1, 2, 3 }); // 31
        paylines.Add(new[] { 2, 2, 2, 1, 0 }); // 32
        paylines.Add(new[] { 1, 2, 3, 2, 1 }); // 33
        paylines.Add(new[] { 2, 1, 0, 1, 2 }); // 34
        paylines.Add(new[] { 0, 1, 2, 1, 0 }); // 35
        paylines.Add(new[] { 3, 2, 1, 2, 3 }); // 36
        paylines.Add(new[] { 1, 1, 2, 2, 3 }); // 37
        paylines.Add(new[] { 2, 2, 1, 1, 0 }); // 38
        paylines.Add(new[] { 0, 0, 1, 1, 2 }); // 39
        paylines.Add(new[] { 3, 3, 2, 2, 1 }); // 40
        paylines.Add(new[] { 0, 1, 0, 0, 1 }); // 41
        paylines.Add(new[] { 3, 2, 3, 3, 2 }); // 42
        paylines.Add(new[] { 1, 2, 2, 2, 1 }); // 43
        paylines.Add(new[] { 2, 1, 1, 1, 2 }); // 44
        paylines.Add(new[] { 1, 0, 0, 1, 0 }); // 45
        paylines.Add(new[] { 2, 3, 3, 2, 3 }); // 46
        paylines.Add(new[] { 0, 1, 1, 0, 1 }); // 47
        paylines.Add(new[] { 3, 2, 2, 3, 2 }); // 48
        paylines.Add(new[] { 1, 2, 1, 0, 0 }); // 49

        // Payline 50: Special
        paylines.Add(new[] { 2, 1, 2, 3, 3 }); // 50

        return paylines.ToArray();
    }
}