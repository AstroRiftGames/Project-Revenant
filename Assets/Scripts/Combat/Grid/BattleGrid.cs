using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BattleGrid : MonoBehaviour
{
    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector2 _cellCheckSize = new(0.8f, 0.8f);
    [SerializeField] private bool _usePhysicsBlockedCells;
    [SerializeField] private LayerMask _blockedCellsMask;
    [SerializeField] private bool _drawGridGizmos;
    [SerializeField] private Vector2Int _gizmoExtents = new(12, 12);
    [SerializeField] private int _maxClosestCellSearch = 512;

    public static BattleGrid Instance { get; private set; }

    public float CellSize => CellWorldSize.x;

    /// <summary>
    /// Reconfigura los tilemaps del grid.
    /// Usado por RoomContext cuando el BattleGrid es un servicio global compartido:
    /// al entrar a una sala, RoomContext lo reconfigura con los tilemaps locales de esa sala.
    /// </summary>
    public void Configure(Tilemap walkable, Tilemap blocked)
    {
        _walkableTilemap = walkable;
        _blockedTilemap = blocked;
    }

    public Vector2 CellWorldSize
    {
        get
        {
            if (_walkableTilemap != null)
            {
                Vector3 cellSize = _walkableTilemap.layoutGrid.cellSize;
                return new Vector2(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
            }

            return new Vector2(_cellSize, _cellSize);
        }
    }
    private void Awake()
    {
        Instance = this;
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        if (_walkableTilemap != null)
            return _walkableTilemap.WorldToCell(worldPosition);

        int x = Mathf.RoundToInt(worldPosition.x / _cellSize);
        int y = Mathf.RoundToInt(worldPosition.y / _cellSize);
        return new Vector3Int(x, y, 0);
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        if (_walkableTilemap != null)
        {
            Vector3 center = _walkableTilemap.GetCellCenterWorld(cell);
            center.z = 0f;
            return center;
        }

        return new Vector3(cell.x * _cellSize, cell.y * _cellSize, 0f);
    }

    public bool IsCellWalkable(Vector3Int cell, Unit movingUnit = null)
    {
        if (!IsCellInsideWalkableBounds(cell))
            return false;

        if (_walkableTilemap != null && !_walkableTilemap.HasTile(cell))
            return false;

        if (_blockedTilemap != null && _blockedTilemap.HasTile(cell))
            return false;

        if (_usePhysicsBlockedCells)
        {
            Vector2 center = CellToWorld(cell);
            Vector2 checkSize = Vector2.Scale(_cellCheckSize, CellWorldSize);
            Collider2D hit = Physics2D.OverlapBox(center, checkSize, 0f, _blockedCellsMask);
            if (hit != null)
                return false;
        }

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null || ReferenceEquals(units[i], movingUnit))
                continue;

            if (WorldToCell(units[i].Position) == cell)
                return false;
        }

        return true;
    }

    public bool IsCellInsideWalkableBounds(Vector3Int cell)
    {
        if (_walkableTilemap == null)
            return true;

        return _walkableTilemap.cellBounds.Contains(cell);
    }

    public List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        var neighbors = new List<Vector3Int>(4);

        neighbors.Add(cell + Vector3Int.right);
        neighbors.Add(cell + Vector3Int.left);
        neighbors.Add(cell + Vector3Int.up);
        neighbors.Add(cell + Vector3Int.down);

        return neighbors;
    }

    public Vector3Int FindClosestWalkableCell(Vector3Int targetCell, Unit movingUnit)
    {
        if (!IsCellInsideWalkableBounds(targetCell))
            return WorldToCell(movingUnit.Position);

        if (IsCellWalkable(targetCell, movingUnit))
            return targetCell;

        var visited = new HashSet<Vector3Int> { targetCell };
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(targetCell);
        int explored = 0;

        while (queue.Count > 0 && explored < _maxClosestCellSearch)
        {
            Vector3Int current = queue.Dequeue();
            explored++;
            List<Vector3Int> neighbors = GetNeighbors(current);

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Int neighbor = neighbors[i];
                if (!IsCellInsideWalkableBounds(neighbor))
                    continue;

                if (!visited.Add(neighbor))
                    continue;

                if (IsCellWalkable(neighbor, movingUnit))
                    return neighbor;

                queue.Enqueue(neighbor);
            }
        }

        return WorldToCell(movingUnit.Position);
    }

    public bool TryFindWalkableCellInRange(Vector3Int targetCell, Vector3Int originCell, int rangeInCells, Unit movingUnit, out Vector3Int resultCell)
    {
        resultCell = originCell;

        if (rangeInCells < 0)
            return false;

        bool found = false;
        float bestDistance = float.MaxValue;

        for (int x = -rangeInCells; x <= rangeInCells; x++)
        {
            for (int y = -rangeInCells; y <= rangeInCells; y++)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) > rangeInCells)
                    continue;

                Vector3Int candidateCell = targetCell + new Vector3Int(x, y, 0);
                if (!IsCellWalkable(candidateCell, movingUnit))
                    continue;

                float distance = Vector3Int.Distance(originCell, candidateCell);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                resultCell = candidateCell;
                found = true;
            }
        }

        return found;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGridGizmos)
            return;

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.2f);

        if (_walkableTilemap != null)
        {
            BoundsInt bounds = _walkableTilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int cell = new Vector3Int(x, y, 0);
                    if (!_walkableTilemap.HasTile(cell))
                        continue;

                    Vector3 center = CellToWorld(cell);
                    Gizmos.color = _blockedTilemap != null && _blockedTilemap.HasTile(cell)
                        ? new Color(1f, 0.35f, 0.35f, 0.45f)
                        : new Color(0.3f, 0.8f, 1f, 0.2f);
                    Gizmos.DrawWireCube(center, new Vector3(CellWorldSize.x, CellWorldSize.y, 0f));
                }
            }

            return;
        }

        for (int x = -_gizmoExtents.x; x <= _gizmoExtents.x; x++)
        {
            for (int y = -_gizmoExtents.y; y <= _gizmoExtents.y; y++)
            {
                Vector3 center = CellToWorld(new Vector3Int(x, y, 0));
                Gizmos.DrawWireCube(center, new Vector3(CellWorldSize.x, CellWorldSize.y, 0f));
            }
        }
    }
}
