using System.Collections.Generic;
using UnityEngine;

public readonly struct GridAvoidanceCellDebugInfo
{
    public GridAvoidanceCellDebugInfo(Vector3Int cell, int cost, bool isBlocked)
    {
        Cell = cell;
        Cost = cost;
        IsBlocked = isBlocked;
    }

    public Vector3Int Cell { get; }
    public int Cost { get; }
    public bool IsBlocked { get; }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(RoomGrid))]
public class GridAvoidanceObstacleRegistry : MonoBehaviour
{
    private struct CellAvoidanceData
    {
        public int Cost;
        public bool IsBlocked;
    }

    [SerializeField] private int _maxAggregatedAvoidanceCost = 64;

    private readonly HashSet<GridAvoidanceObstacle> _obstacles = new();
    private readonly Dictionary<Vector3Int, CellAvoidanceData> _cellData = new();
    private readonly List<GridAvoidanceObstacle> _obstacleBuffer = new();
    private readonly List<Vector3Int> _cellBuffer = new();

    private RoomGrid _grid;

    public RoomGrid Grid => _grid;

    private void Awake()
    {
        _grid = GetComponent<RoomGrid>();
    }

    private void OnEnable()
    {
        Rebuild();
    }

    public void RegisterObstacle(GridAvoidanceObstacle obstacle)
    {
        if (obstacle == null)
            return;

        if (_obstacles.Add(obstacle))
            Rebuild();
    }

    public void UnregisterObstacle(GridAvoidanceObstacle obstacle)
    {
        if (obstacle == null)
            return;

        if (_obstacles.Remove(obstacle))
            Rebuild();
    }

    public int GetAvoidanceCost(Vector3Int cell)
    {
        return _cellData.TryGetValue(cell, out CellAvoidanceData data) ? data.Cost : 0;
    }

    public bool IsCellBlockedByAvoidanceObstacle(Vector3Int cell)
    {
        return _cellData.TryGetValue(cell, out CellAvoidanceData data) && data.IsBlocked;
    }

    public bool TryGetAvoidanceCost(Vector3Int cell, out int cost)
    {
        if (_cellData.TryGetValue(cell, out CellAvoidanceData data))
        {
            cost = data.Cost;
            return true;
        }

        cost = 0;
        return false;
    }

    public void GetDebugCellData(List<GridAvoidanceCellDebugInfo> results)
    {
        if (results == null)
            return;

        results.Clear();

        foreach (KeyValuePair<Vector3Int, CellAvoidanceData> pair in _cellData)
            results.Add(new GridAvoidanceCellDebugInfo(pair.Key, pair.Value.Cost, pair.Value.IsBlocked));
    }

    public void Rebuild()
    {
        _cellData.Clear();
        _obstacleBuffer.Clear();

        foreach (GridAvoidanceObstacle obstacle in _obstacles)
            _obstacleBuffer.Add(obstacle);

        for (int obstacleIndex = 0; obstacleIndex < _obstacleBuffer.Count; obstacleIndex++)
        {
            GridAvoidanceObstacle obstacle = _obstacleBuffer[obstacleIndex];
            if (obstacle == null || !obstacle.IsRuntimeActive)
                continue;

            if (!obstacle.TryGetCenterCell(out Vector3Int centerCell))
                continue;

            _cellBuffer.Clear();
            obstacle.GetAffectedCells(_cellBuffer);

            for (int cellIndex = 0; cellIndex < _cellBuffer.Count; cellIndex++)
            {
                Vector3Int cell = _cellBuffer[cellIndex];
                CellAvoidanceData data = _cellData.TryGetValue(cell, out CellAvoidanceData existingData)
                    ? existingData
                    : default;

                data.Cost = Mathf.Clamp(data.Cost + obstacle.AvoidanceCost, 0, Mathf.Max(0, _maxAggregatedAvoidanceCost));

                if (obstacle.BlocksCenterCell && cell == centerCell)
                    data.IsBlocked = true;

                _cellData[cell] = data;
            }
        }
    }
}
