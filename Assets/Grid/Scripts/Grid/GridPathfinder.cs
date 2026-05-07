using System.Collections.Generic;
using UnityEngine;

public readonly struct GridPathDebugInfo
{
    public GridPathDebugInfo(Vector3Int start, Vector3Int goal, IReadOnlyList<Vector3Int> path)
    {
        Start = start;
        Goal = goal;
        Path = path;
    }

    public Vector3Int Start { get; }
    public Vector3Int Goal { get; }
    public IReadOnlyList<Vector3Int> Path { get; }
}

public static class GridPathfinder
{
    private sealed class PathDebugSnapshot
    {
        public Vector3Int Start;
        public Vector3Int Goal;
        public readonly List<Vector3Int> Path = new();
    }

    private static readonly Dictionary<RoomGrid, PathDebugSnapshot> DebugSnapshotsByGrid = new();

    public static List<Vector3Int> FindPath(RoomGrid grid, Vector3Int start, Vector3Int goal, IGridOccupant movingUnit = null)
    {
        if (grid == null)
            return new List<Vector3Int>();

        if (start == goal)
        {
            List<Vector3Int> trivialPath = new List<Vector3Int> { start };
            UpdateDebugSnapshot(grid, start, goal, trivialPath);
            LogDebugSummary(grid, start, goal, trivialPath);
            return trivialPath;
        }

        if (!grid.IsCellEnterable(goal, movingUnit))
        {
            ClearDebugSnapshot(grid);
            return new List<Vector3Int>();
        }

        if (!grid.TryGetTraversalCost(goal, out _))
        {
            ClearDebugSnapshot(grid);
            return new List<Vector3Int>();
        }

        var openSet = new HashSet<Vector3Int> { start };
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector3Int, int> { [start] = GetHeuristicCost(start, goal) };

        while (openSet.Count > 0)
        {
            Vector3Int current = GetLowestFScoreNode(openSet, fScore, gScore);
            if (current == goal)
            {
                List<Vector3Int> path = ReconstructPath(cameFrom, current);
                UpdateDebugSnapshot(grid, start, goal, path);
                LogDebugSummary(grid, start, goal, path);
                return path;
            }

            openSet.Remove(current);

            List<Vector3Int> neighbors = grid.GetNeighbors(current, movingUnit);

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Int neighbor = neighbors[i];

                if (!grid.TryGetTraversalCost(neighbor, out int traversalCost))
                    continue;

                int tentativeGScore = GetScore(gScore, current) + Mathf.Max(1, traversalCost);
                if (tentativeGScore >= GetScore(gScore, neighbor))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + GetHeuristicCost(neighbor, goal);
                openSet.Add(neighbor);
            }
        }

        ClearDebugSnapshot(grid);
        return new List<Vector3Int>();
    }

    public static bool TryGetLastPathDebugInfo(RoomGrid grid, out GridPathDebugInfo debugInfo)
    {
        if (grid != null && DebugSnapshotsByGrid.TryGetValue(grid, out PathDebugSnapshot snapshot))
        {
            debugInfo = new GridPathDebugInfo(snapshot.Start, snapshot.Goal, snapshot.Path);
            return true;
        }

        debugInfo = default;
        return false;
    }

    private static Vector3Int GetLowestFScoreNode(
        HashSet<Vector3Int> openSet,
        Dictionary<Vector3Int, int> fScore,
        Dictionary<Vector3Int, int> gScore)
    {
        bool hasBestNode = false;
        Vector3Int bestNode = Vector3Int.zero;
        int bestFScore = int.MaxValue;
        int bestGScore = int.MaxValue;

        foreach (Vector3Int candidate in openSet)
        {
            int candidateFScore = GetScore(fScore, candidate);
            int candidateGScore = GetScore(gScore, candidate);

            if (!hasBestNode ||
                candidateFScore < bestFScore ||
                (candidateFScore == bestFScore && candidateGScore < bestGScore))
            {
                hasBestNode = true;
                bestNode = candidate;
                bestFScore = candidateFScore;
                bestGScore = candidateGScore;
            }
        }

        return bestNode;
    }

    private static int GetHeuristicCost(Vector3Int from, Vector3Int goal)
    {
        return GridNavigationUtility.GetCellDistance(from, goal);
    }

    private static int GetScore(Dictionary<Vector3Int, int> scores, Vector3Int cell)
    {
        return scores.TryGetValue(cell, out int score) ? score : int.MaxValue;
    }

    private static void UpdateDebugSnapshot(RoomGrid grid, Vector3Int start, Vector3Int goal, List<Vector3Int> path)
    {
        if (grid == null)
            return;

        if (!DebugSnapshotsByGrid.TryGetValue(grid, out PathDebugSnapshot snapshot))
        {
            snapshot = new PathDebugSnapshot();
            DebugSnapshotsByGrid[grid] = snapshot;
        }

        snapshot.Start = start;
        snapshot.Goal = goal;
        snapshot.Path.Clear();

        if (path == null)
            return;

        for (int i = 0; i < path.Count; i++)
            snapshot.Path.Add(path[i]);
    }

    private static void ClearDebugSnapshot(RoomGrid grid)
    {
        if (grid == null || !DebugSnapshotsByGrid.TryGetValue(grid, out PathDebugSnapshot snapshot))
            return;

        snapshot.Path.Clear();
        snapshot.Start = Vector3Int.zero;
        snapshot.Goal = Vector3Int.zero;
    }

    private static void LogDebugSummary(RoomGrid grid, Vector3Int start, Vector3Int goal, List<Vector3Int> path)
    {
        if (grid == null || !grid.DebugPathfindingLogs || path == null || path.Count == 0)
            return;

        int penalizedSteps = 0;
        int totalAvoidanceCost = 0;

        for (int i = 0; i < path.Count; i++)
        {
            int avoidanceCost = grid.GetAvoidanceCost(path[i]);
            if (avoidanceCost <= 0)
                continue;

            penalizedSteps++;
            totalAvoidanceCost += avoidanceCost;
        }

        Debug.Log(
            $"[GridPathfinder] Path {start} -> {goal} resolved with {path.Count} cells. " +
            $"Penalized cells: {penalizedSteps}. Total avoidance cost: {totalAvoidanceCost}.",
            grid);
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
