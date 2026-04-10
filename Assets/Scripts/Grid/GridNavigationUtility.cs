using System.Collections.Generic;
using UnityEngine;

public static class GridNavigationUtility
{
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
