using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class RoomSpawnCellUtility
{
    public static List<Vector3Int> GetAvailableSpawnCells(Tilemap walkableTilemap, RoomGrid roomGrid, int edgePadding = 0)
    {
        var result = new List<Vector3Int>();
        if (roomGrid == null || walkableTilemap == null)
            return result;

        BoundsInt bounds = walkableTilemap.cellBounds;
        bool hasWalkableTiles = false;
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;

        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (!walkableTilemap.HasTile(cell))
                continue;

            hasWalkableTiles = true;
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        if (!hasWalkableTiles)
            return result;

        int padding = Mathf.Max(0, edgePadding);
        for (int x = minX + padding; x <= maxX - padding; x++)
        {
            for (int y = minY + padding; y <= maxY - padding; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                if (!walkableTilemap.HasTile(cell))
                    continue;

                if (!roomGrid.IsCellWalkable(cell))
                    continue;

                result.Add(cell);
            }
        }

        return result;
    }
}
