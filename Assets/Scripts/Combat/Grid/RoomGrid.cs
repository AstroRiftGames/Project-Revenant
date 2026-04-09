using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class RoomGridTopology
{
    private static readonly Vector3Int[] CardinalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    private Tilemap _walkableTilemap;
    private Tilemap _blockedTilemap;
    private float _cellSize = 1f;
    private Vector2 _cellCheckSize = new(0.8f, 0.8f);
    private bool _usePhysicsBlockedCells;
    private LayerMask _blockedCellsMask;

    public void Configure(
        Tilemap walkableTilemap,
        Tilemap blockedTilemap,
        float cellSize,
        Vector2 cellCheckSize,
        bool usePhysicsBlockedCells,
        LayerMask blockedCellsMask)
    {
        _walkableTilemap = walkableTilemap;
        _blockedTilemap = blockedTilemap;
        _cellSize = cellSize;
        _cellCheckSize = cellCheckSize;
        _usePhysicsBlockedCells = usePhysicsBlockedCells;
        _blockedCellsMask = blockedCellsMask;
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

    public bool IsCellInsideWalkableBounds(Vector3Int cell)
    {
        if (_walkableTilemap == null)
            return true;

        return _walkableTilemap.cellBounds.Contains(cell);
    }

    public bool IsCellStaticallyWalkable(Vector3Int cell)
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

        return true;
    }

    public List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        var neighbors = new List<Vector3Int>(CardinalDirections.Length);

        for (int i = 0; i < CardinalDirections.Length; i++)
            neighbors.Add(cell + CardinalDirections[i]);

        return neighbors;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(GridOccupancyTracker))]
public class RoomGrid : MonoBehaviour
{
    private static readonly IGridCellMovementValidator DefaultMovementValidator = new GridCellMovementValidator();

    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;
    [SerializeField] private RoomContext _roomContext;
    [SerializeField] private GridOccupancyTracker _occupancyService;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector2 _cellCheckSize = new(0.8f, 0.8f);
    [SerializeField] private bool _usePhysicsBlockedCells;
    [SerializeField] private LayerMask _blockedCellsMask;
    [SerializeField] private bool _drawGridGizmos;
    [SerializeField] private Vector2Int _gizmoExtents = new(12, 12);
    [SerializeField] private int _maxClosestCellSearch = 512;
    private bool _hasLoggedMissingOccupancyService;
    private readonly RoomGridTopology _topology = new();

    public float CellSize => CellWorldSize.x;
    public GridOccupancyTracker OccupancyService => RequireOccupancyService();
    public RoomGridTopology Topology => _topology;

    private void Awake()
    {
        ResolveDependencies();
    }

    public void Configure(Tilemap walkable, Tilemap blocked)
    {
        _walkableTilemap = walkable;
        _blockedTilemap = blocked;
        ResolveDependencies();
    }

    public Vector2 CellWorldSize
    {
        get => _topology.CellWorldSize;
    }
    private void OnEnable()
    {
        ResolveDependencies();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_occupancyService == null)
            _occupancyService = GetComponent<GridOccupancyTracker>();
    }
#endif

    private void ResolveDependencies()
    {
        ResolveRoomContext();
        TryResolveOccupancyService();
        ConfigureTopology();
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return _topology.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return _topology.CellToWorld(cell);
    }

    public bool IsCellStaticallyWalkable(Vector3Int cell)
    {
        return _topology.IsCellStaticallyWalkable(cell);
    }

    public bool IsCellOccupied(Vector3Int cell, IGridOccupant movingOccupant = null)
    {
        return RequireOccupancyService().IsOccupied(cell, movingOccupant);
    }

    public bool DoesCellBlockMovement(Vector3Int cell, IGridOccupant movingOccupant = null)
    {
        return RequireOccupancyService().DoesCellBlockMovement(cell, movingOccupant);
    }

    public bool IsCellEnterable(Vector3Int cell, IGridOccupant movingOccupant = null)
    {
        return DefaultMovementValidator.CanEnter(this, new GridCellMovementQuery(cell, movingOccupant));
    }

    public bool IsCellWalkable(Vector3Int cell, IGridOccupant movingOccupant = null)
    {
        return IsCellEnterable(cell, movingOccupant);
    }

    private void ResolveRoomContext()
    {
        if (_roomContext != null)
            return;

        _roomContext = GetComponentInParent<RoomContext>(includeInactive: true);
    }

    private void TryResolveOccupancyService()
    {
        if (_occupancyService == null)
            _occupancyService = GetComponent<GridOccupancyTracker>();
        
        if (_occupancyService != null)
            return;

        if (_hasLoggedMissingOccupancyService)
            return;

        _hasLoggedMissingOccupancyService = true;
        Debug.LogError(
            $"[RoomGrid] '{name}' requiere un GridOccupancyTracker en el mismo GameObject. " +
            "Se elimino la autocreacion silenciosa para evitar escenas mal armadas y dependencias ocultas de inicializacion.",
            this);
    }

    private GridOccupancyTracker RequireOccupancyService()
    {
        TryResolveOccupancyService();
        if (_occupancyService != null)
            return _occupancyService;

        throw new InvalidOperationException(
            $"[RoomGrid] '{name}' no puede operar sin GridOccupancyTracker en el mismo GameObject.");
    }

    private void ConfigureTopology()
    {
        _topology.Configure(
            _walkableTilemap,
            _blockedTilemap,
            _cellSize,
            _cellCheckSize,
            _usePhysicsBlockedCells,
            _blockedCellsMask);
    }

    public bool IsCellInsideWalkableBounds(Vector3Int cell)
    {
        return _topology.IsCellInsideWalkableBounds(cell);
    }

    public List<Vector3Int> GetNeighbors(Vector3Int cell)
    {
        return _topology.GetNeighbors(cell);
    }

    public Vector3Int FindClosestWalkableCell(Vector3Int targetCell, Vector3Int fallbackCell, IGridOccupant movingOccupant = null)
    {
        if (!IsCellInsideWalkableBounds(targetCell))
            return fallbackCell;

        if (IsCellEnterable(targetCell, movingOccupant))
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

                if (IsCellEnterable(neighbor, movingOccupant))
                    return neighbor;

                queue.Enqueue(neighbor);
            }
        }

        return fallbackCell;
    }

    public Vector3Int FindClosestWalkableCell(Vector3Int targetCell, Unit movingUnit)
    {
        Vector3Int fallbackCell = movingUnit != null
            ? WorldToCell(movingUnit.Position)
            : targetCell;

        return FindClosestWalkableCell(targetCell, fallbackCell, movingUnit);
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
                if (!IsCellEnterable(candidateCell, movingUnit))
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
