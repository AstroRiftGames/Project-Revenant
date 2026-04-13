using System.Collections.Generic;
using UnityEngine;

public static class GridNavigationUtility
{
    public static int GetCellDistance(Vector3Int a, Vector3Int b)
    {
        int deltaX = Mathf.Abs(a.x - b.x);
        int deltaY = Mathf.Abs(a.y - b.y);
        return Mathf.Max(deltaX, deltaY);
    }

    public static bool IsWithinCellRange(Vector3Int originCell, Vector3Int targetCell, int rangeInCells)
    {
        return GetCellDistance(originCell, targetCell) <= Mathf.Max(0, rangeInCells);
    }

    public static bool IsDiagonalStep(Vector3Int from, Vector3Int to)
    {
        Vector3Int delta = to - from;
        return Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1 && delta.z == 0;
    }

    public static Vector3 SnapWorldPositionToCell(RoomGrid grid, Vector3 worldPosition)
    {
        if (grid == null)
            return worldPosition;

        return grid.CellToWorld(grid.WorldToCell(worldPosition));
    }

    public static Vector3Int ResolvePlacementCell(RoomGrid grid, Vector3 worldPosition, IGridOccupant movingOccupant = null)
    {
        if (grid == null)
            return Vector3Int.zero;

        Vector3Int desiredCell = grid.WorldToCell(worldPosition);
        if (grid.IsCellEnterable(desiredCell, movingOccupant))
            return desiredCell;

        return grid.FindClosestWalkableCell(desiredCell, desiredCell, movingOccupant);
    }

    public static Vector3 ResolvePlacementWorldPosition(RoomGrid grid, Vector3 worldPosition, IGridOccupant movingOccupant = null)
    {
        if (grid == null)
            return worldPosition;

        return grid.CellToWorld(ResolvePlacementCell(grid, worldPosition, movingOccupant));
    }

    public static bool TryBuildWorldPath(RoomGrid grid, Vector3Int startCell, Vector3Int goalCell, Queue<Vector3> worldWaypoints, IGridOccupant movingOccupant = null)
    {
        if (grid == null || worldWaypoints == null)
            return false;

        worldWaypoints.Clear();

        List<Vector3Int> path = GridPathfinder.FindPath(grid, startCell, goalCell, movingOccupant);
        if (path.Count <= 1)
            return false;

        for (int i = 1; i < path.Count; i++)
            worldWaypoints.Enqueue(grid.CellToWorld(path[i]));

        return worldWaypoints.Count > 0;
    }
}
