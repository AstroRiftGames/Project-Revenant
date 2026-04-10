using System.Collections.Generic;
using UnityEngine;

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
