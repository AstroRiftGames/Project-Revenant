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

public static class GridPathfinder
{
    public static List<Vector3Int> FindPath(RoomGrid grid, Vector3Int start, Vector3Int goal, IGridOccupant movingUnit = null)
    {
        if (start == goal)
            return new List<Vector3Int> { start };

        if (!grid.IsCellEnterable(goal, movingUnit))
            return new List<Vector3Int>();

        var frontier = new Queue<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var visited = new HashSet<Vector3Int> { start };

        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            Vector3Int current = frontier.Dequeue();
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            List<Vector3Int> neighbors = grid.Topology.GetNeighbors(current);

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Int neighbor = neighbors[i];
                if (!grid.Topology.IsCellInsideWalkableBounds(neighbor))
                    continue;

                if (!grid.IsCellEnterable(neighbor, movingUnit))
                    continue;

                if (!visited.Add(neighbor))
                    continue;

                cameFrom[neighbor] = current;
                frontier.Enqueue(neighbor);
            }
        }

        return new List<Vector3Int>();
    }

    private static List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var path = new List<Vector3Int> { current };

        while (cameFrom.TryGetValue(current, out Vector3Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
