using System.Collections.Generic;
using UnityEngine;

public static class FishBatchSpawner
{
    public static void GenerateBatchPositions(
        FishData fishData,
        Vector3 baseSpawnPos,
        Vector3 baseDestPos,
        Vector3 dir,
        Vector3 perp,
        SpriteRenderer spriteRenderer,
        Direction spawnDir,
        Direction destDir,
        float spawnExtra,
        float destExtra,
        out List<Vector3> spawnPositions,
        out List<Vector3> destPositions)
    {
        spawnPositions = new List<Vector3>();
        destPositions = new List<Vector3>();

        for (int i = 0; i < fishData.batchSize; i++)
        {
            float offsetAmt = (i - (fishData.batchSize - 1) * 0.5f) * fishData.batchSpacing;

            Vector3 spawnPos = baseSpawnPos;
            Vector3 destPos = baseDestPos;

            switch (fishData.spawnPattern)
            {
                case BatchPattern.Line:
                    spawnPos += perp * offsetAmt;
                    destPos += perp * offsetAmt;
                    break;

                case BatchPattern.Arc:
                    spawnPos += perp * offsetAmt + Vector3.up * Mathf.Sin(i * 0.5f);
                    destPos += perp * offsetAmt + Vector3.up * Mathf.Sin(i * 0.5f);
                    break;

                case BatchPattern.VShape:
                    spawnPos += perp * offsetAmt + dir * Mathf.Abs(offsetAmt) * -0.5f;
                    destPos += perp * offsetAmt + dir * Mathf.Abs(offsetAmt) * -0.5f;
                    break;

                case BatchPattern.Circle:
                    float angle = (360f / fishData.batchSize) * i;
                    Vector3 circleOffset = Quaternion.Euler(0, 0, angle) * Vector3.right * 1.5f;
                    spawnPos += circleOffset;
                    destPos += circleOffset;
                    break;
            }

            // Final directional offset (based on sprite size and edge alignment)
            Vector3 spawnOffsetVec = MovementPointUtil.GetOffsetVector(spriteRenderer, spawnDir, spawnExtra);
            Vector3 destOffsetVec = MovementPointUtil.GetOffsetVector(spriteRenderer, destDir, destExtra);

            spawnPos += spawnOffsetVec;
            destPos += destOffsetVec;

            spawnPositions.Add(spawnPos);
            destPositions.Add(destPos);
        }
    }
}
