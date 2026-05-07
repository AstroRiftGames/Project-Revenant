using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class RoomGridTopology
{
    private static readonly Vector3Int[] CardinalDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    private static readonly Vector3Int[] EightDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(-1, -1, 0)
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

    public bool TryGetCellVisualCornersWorld(Vector3Int cell, Vector3[] corners)
    {
        if (corners == null || corners.Length < 4)
            return false;

        if (_walkableTilemap != null)
        {
            GridLayout layoutGrid = _walkableTilemap.layoutGrid;
            if (layoutGrid != null)
            {
                corners[0] = CellInterpolatedToWorld(layoutGrid, new Vector3(cell.x + 0.5f, cell.y + 1f, cell.z));
                corners[1] = CellInterpolatedToWorld(layoutGrid, new Vector3(cell.x + 1f, cell.y + 0.5f, cell.z));
                corners[2] = CellInterpolatedToWorld(layoutGrid, new Vector3(cell.x + 0.5f, cell.y, cell.z));
                corners[3] = CellInterpolatedToWorld(layoutGrid, new Vector3(cell.x, cell.y + 0.5f, cell.z));
                return true;
            }
        }

        Vector3 center = CellToWorld(cell);
        Vector2 cellWorldSize = CellWorldSize;
        corners[0] = center + new Vector3(0f, cellWorldSize.y * 0.5f, 0f);
        corners[1] = center + new Vector3(cellWorldSize.x * 0.5f, 0f, 0f);
        corners[2] = center + new Vector3(0f, -cellWorldSize.y * 0.5f, 0f);
        corners[3] = center + new Vector3(-cellWorldSize.x * 0.5f, 0f, 0f);
        return true;
    }

    private static Vector3 CellInterpolatedToWorld(GridLayout gridLayout, Vector3 cellPosition)
    {
        Vector3 localPosition = gridLayout.CellToLocalInterpolated(cellPosition);
        Vector3 worldPosition = gridLayout.LocalToWorld(localPosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    public bool IsCellInsideWalkableBounds(Vector3Int cell)
    {
        if (_walkableTilemap == null)
            return true;

        return _walkableTilemap.cellBounds.Contains(cell);
    }

    public bool HasCell(Vector3Int cell)
    {
        if (!IsCellInsideWalkableBounds(cell))
            return false;

        bool hasWalkableTile = _walkableTilemap != null && _walkableTilemap.HasTile(cell);
        bool hasBlockedTile = _blockedTilemap != null && _blockedTilemap.HasTile(cell);

        if (_walkableTilemap != null || _blockedTilemap != null)
            return hasWalkableTile || hasBlockedTile;

        return true;
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

    public bool TryGetCardinalNeighborsInsideBounds(Vector3Int cell, List<Vector3Int> results)
    {
        if (results == null)
            return false;

        results.Clear();

        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector3Int neighbor = cell + CardinalDirections[i];
            if (IsCellInsideWalkableBounds(neighbor))
                results.Add(neighbor);
        }

        return results.Count > 0;
    }

    public bool TryGetNeighborsInsideBounds(Vector3Int cell, List<Vector3Int> results, bool includeDiagonals = true)
    {
        if (results == null)
            return false;

        results.Clear();

        Vector3Int[] directions = includeDiagonals ? EightDirections : CardinalDirections;
        for (int i = 0; i < directions.Length; i++)
        {
            Vector3Int neighbor = cell + directions[i];
            if (IsCellInsideWalkableBounds(neighbor))
                results.Add(neighbor);
        }

        return results.Count > 0;
    }

    public List<Vector3Int> GetNeighbors(Vector3Int cell, bool includeDiagonals = true)
    {
        Vector3Int[] directions = includeDiagonals ? EightDirections : CardinalDirections;
        var neighbors = new List<Vector3Int>(directions.Length);

        for (int i = 0; i < directions.Length; i++)
            neighbors.Add(cell + directions[i]);

        return neighbors;
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(GridOccupancyTracker))]
[RequireComponent(typeof(GridAvoidanceObstacleRegistry))]
public class RoomGrid : MonoBehaviour
{
    private static readonly IGridCellMovementValidator DefaultMovementValidator = new GridCellMovementValidator();
    private static readonly Vector3[] SharedGizmoCellCorners = new Vector3[4];

    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;
    [SerializeField] private GridOccupancyTracker _occupancyService;
    [SerializeField] private GridAvoidanceObstacleRegistry _avoidanceObstacleRegistry;
    [SerializeField] private float _cellSize = 1f;
    [SerializeField] private Vector2 _cellCheckSize = new(0.8f, 0.8f);
    [SerializeField] private bool _usePhysicsBlockedCells;
    [SerializeField] private LayerMask _blockedCellsMask;
    [SerializeField] private bool _drawGridGizmos;
    [SerializeField] private bool _drawAvoidanceDebug;
    [SerializeField] private bool _drawAvoidanceBlockedCells = true;
    [SerializeField] private bool _drawAvoidancePenalizedCells = true;
    [SerializeField] private bool _drawAvoidanceCosts;
    [SerializeField] private bool _drawLastPathDebug;
    [SerializeField] private bool _debugPathfindingLogs;
    [SerializeField] private bool _useCustomIsometricDebugShape = true;
    [SerializeField] private Vector2 _isometricDebugCellSize = new(1f, 0.5f);
    [SerializeField] private Vector2Int _gizmoExtents = new(12, 12);
    [SerializeField] private int _maxClosestCellSearch = 512;
    private bool _hasLoggedMissingOccupancyService;
    private readonly RoomGridTopology _topology = new();
    private readonly List<GridAvoidanceCellDebugInfo> _avoidanceDebugBuffer = new();

    public float CellSize => CellWorldSize.x;
    public GridOccupancyTracker OccupancyService => RequireOccupancyService();
    public GridAvoidanceObstacleRegistry AvoidanceObstacleRegistry => ResolveAvoidanceObstacleRegistry();
    public RoomGridTopology Topology => _topology;
    public bool DebugPathfindingLogs => _debugPathfindingLogs;

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

        if (_avoidanceObstacleRegistry == null)
            _avoidanceObstacleRegistry = GetComponent<GridAvoidanceObstacleRegistry>();

        _isometricDebugCellSize.x = Mathf.Max(0.01f, _isometricDebugCellSize.x);
        _isometricDebugCellSize.y = Mathf.Max(0.01f, _isometricDebugCellSize.y);
    }
#endif

    private void ResolveDependencies()
    {
        TryResolveOccupancyService();
        TryResolveAvoidanceObstacleRegistry();
        ConfigureTopology();
    }

    [ContextMenu("Log Grid Debug Info")]
    private void LogGridDebugInfo()
    {
        GridLayout layoutGrid = _walkableTilemap != null ? _walkableTilemap.layoutGrid : null;
        string layoutName = layoutGrid != null ? layoutGrid.cellLayout.ToString() : "None";
        Vector3 layoutCellSize = layoutGrid != null ? layoutGrid.cellSize : new Vector3(_cellSize, _cellSize, 1f);
        Vector3 layoutCellGap = layoutGrid != null ? layoutGrid.cellGap : Vector3.zero;
        string tilemapOrientation = _walkableTilemap != null ? _walkableTilemap.orientation.ToString() : "None";

        Debug.Log(
            $"[RoomGrid] Grid debug info for '{name}': " +
            $"cellLayout={layoutName}, cellSize={layoutCellSize}, cellGap={layoutCellGap}, " +
            $"tilemapOrientation={tilemapOrientation}, useCustomIsometricDebugShape={_useCustomIsometricDebugShape}, " +
            $"isometricDebugCellSize={_isometricDebugCellSize}",
            this);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return _topology.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorld(Vector3Int cell)
    {
        return _topology.CellToWorld(cell);
    }

    public bool TryGetCellVisualCornersWorld(Vector3Int cell, Vector3[] corners)
    {
        if (_useCustomIsometricDebugShape)
            return TryGetCellIsometricDebugCornersWorld(cell, corners);

        return _topology.TryGetCellVisualCornersWorld(cell, corners);
    }

    public bool TryGetCellIsometricDebugCornersWorld(Vector3Int cell, Vector3[] corners)
    {
        if (corners == null || corners.Length < 4)
            return false;

        Vector3 center = CellToWorld(cell);
        float halfWidth = Mathf.Max(0.01f, Mathf.Abs(_isometricDebugCellSize.x) * 0.5f);
        float halfHeight = Mathf.Max(0.01f, Mathf.Abs(_isometricDebugCellSize.y) * 0.5f);
        corners[0] = center + Vector3.up * halfHeight;
        corners[1] = center + Vector3.right * halfWidth;
        corners[2] = center + Vector3.down * halfHeight;
        corners[3] = center + Vector3.left * halfWidth;
        return true;
    }

    public bool IsCellStaticallyWalkable(Vector3Int cell)
    {
        return _topology.IsCellStaticallyWalkable(cell);
    }

    public bool HasCell(Vector3Int cell)
    {
        return _topology.HasCell(cell);
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

    public bool IsCellHardBlocked(Vector3Int cell)
    {
        return !HasCell(cell) || !Topology.IsCellStaticallyWalkable(cell) || IsCellBlockedByAvoidanceObstacle(cell);
    }

    public int GetAvoidanceCost(Vector3Int cell)
    {
        GridAvoidanceObstacleRegistry registry = ResolveAvoidanceObstacleRegistry();
        return registry != null ? registry.GetAvoidanceCost(cell) : 0;
    }

    public bool IsCellBlockedByAvoidanceObstacle(Vector3Int cell)
    {
        GridAvoidanceObstacleRegistry registry = ResolveAvoidanceObstacleRegistry();
        return registry != null && registry.IsCellBlockedByAvoidanceObstacle(cell);
    }

    public bool TryGetTraversalCost(Vector3Int cell, out int cost)
    {
        cost = 0;

        if (IsCellHardBlocked(cell))
            return false;

        cost = 1 + Mathf.Max(0, GetAvoidanceCost(cell));
        return true;
    }

    public bool TryFindAttackPositionFromBlockedDesiredCell(Vector3Int blockedCell, Vector3Int targetCell, Vector3Int originCell, int rangeInCells, Unit movingUnit, Unit targetUnit, out Vector3Int attackPosition)
    {
        attackPosition = originCell;
        
        if (movingUnit == null || targetUnit == null)
            return false;
        
        if (GridNavigationUtility.IsWithinCellRange(originCell, targetCell, rangeInCells))
        {
            attackPosition = originCell;
            return true;
        }
        
        for (int searchRadius = 1; searchRadius <= rangeInCells; searchRadius++)
        {
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    Vector3Int candidate = targetCell + new Vector3Int(x, y, 0);
                    
                    if (!IsCellEnterable(candidate, movingUnit))
                        continue;
                    
                    if (!GridNavigationUtility.IsWithinCellRange(candidate, targetCell, rangeInCells))
                        continue;
                    
                    int distFromOrigin = GridNavigationUtility.GetCellDistance(originCell, candidate);
                    if (distFromOrigin > rangeInCells)
                        continue;
                    
                    attackPosition = candidate;
                    return true;
                }
            }
        }
        
        return false;
    }

    public bool TryFindNearbyAlternativeCell(Vector3Int blockedCell, Vector3Int targetCell, Vector3Int originCell, int rangeInCells, Unit movingUnit, out Vector3Int alternativeCell)
    {
        alternativeCell = originCell;
        
        if (movingUnit == null)
            return false;
        
        int bestDistanceFromBlocked = int.MaxValue;
        int bestScore = int.MaxValue;
        Vector3Int bestCell = originCell;
        bool found = false;
        
        int searchRadius = Mathf.Max(2, rangeInCells);
        
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dy = -searchRadius; dy <= searchRadius; dy++)
            {
                Vector3Int candidate = blockedCell + new Vector3Int(dx, dy, 0);
                
                if (candidate == blockedCell)
                    continue;
                
                if (!IsCellEnterable(candidate, movingUnit))
                    continue;
                
                int distFromTarget = GridNavigationUtility.GetCellDistance(candidate, targetCell);
                if (distFromTarget > rangeInCells)
                    continue;
                
                int distFromOrigin = GridNavigationUtility.GetCellDistance(originCell, candidate);
                int distFromBlocked = Mathf.Abs(dx) + Mathf.Abs(dy);
                
                int crowdingPenalty = CalculateCrowdingPenalty(candidate, movingUnit);
                
                int score = distFromOrigin + crowdingPenalty;
                
                if (distFromBlocked < bestDistanceFromBlocked || (distFromBlocked == bestDistanceFromBlocked && score < bestScore))
                {
                    bestDistanceFromBlocked = distFromBlocked;
                    bestScore = score;
                    bestCell = candidate;
                    found = true;
                }
            }
        }
        
        if (found)
        {
            alternativeCell = bestCell;
            return true;
        }
        
        return false;
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

    private void TryResolveAvoidanceObstacleRegistry()
    {
        if (_avoidanceObstacleRegistry == null)
            _avoidanceObstacleRegistry = GetComponent<GridAvoidanceObstacleRegistry>();
    }

    private GridOccupancyTracker RequireOccupancyService()
    {
        TryResolveOccupancyService();
        if (_occupancyService != null)
            return _occupancyService;

        throw new InvalidOperationException(
            $"[RoomGrid] '{name}' no puede operar sin GridOccupancyTracker en el mismo GameObject.");
    }

    private GridAvoidanceObstacleRegistry ResolveAvoidanceObstacleRegistry()
    {
        TryResolveAvoidanceObstacleRegistry();
        return _avoidanceObstacleRegistry;
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
        return GetNeighbors(cell, null);
    }

    public List<Vector3Int> GetNeighbors(Vector3Int cell, IGridOccupant movingOccupant)
    {
        List<Vector3Int> neighbors = _topology.GetNeighbors(cell);
        for (int i = neighbors.Count - 1; i >= 0; i--)
        {
            if (!IsStepAllowed(cell, neighbors[i], movingOccupant))
                neighbors.RemoveAt(i);
        }

        return neighbors;
    }

    public bool IsStepAllowed(Vector3Int fromCell, Vector3Int toCell, IGridOccupant movingOccupant = null)
    {
        Vector3Int delta = toCell - fromCell;
        bool isNeighbor =
            delta.z == 0 &&
            Mathf.Abs(delta.x) <= 1 &&
            Mathf.Abs(delta.y) <= 1 &&
            (delta.x != 0 || delta.y != 0);

        if (!isNeighbor)
            return false;

        if (!Topology.IsCellInsideWalkableBounds(toCell))
            return false;

        if (!IsCellEnterable(toCell, movingOccupant))
            return false;

        if (!GridNavigationUtility.IsDiagonalStep(fromCell, toCell))
            return true;

        Vector3Int horizontalStep = new Vector3Int(fromCell.x + delta.x, fromCell.y, fromCell.z);
        Vector3Int verticalStep = new Vector3Int(fromCell.x, fromCell.y + delta.y, fromCell.z);
        bool horizontalBlocked = !Topology.IsCellStaticallyWalkable(horizontalStep);
        bool verticalBlocked = !Topology.IsCellStaticallyWalkable(verticalStep);
        return !(horizontalBlocked && verticalBlocked);
    }

    public Vector3Int FindClosestWalkableCell(Vector3Int targetCell, Vector3Int fallbackCell, IGridOccupant movingOccupant = null)
    {
        if (!Topology.IsCellInsideWalkableBounds(targetCell))
            return fallbackCell;

        if (IsCellEnterable(targetCell, movingOccupant))
            return targetCell;

        var visited = new HashSet<Vector3Int> { targetCell };
        var queue = new Queue<Vector3Int>();
        var neighbors = new List<Vector3Int>(8);
        queue.Enqueue(targetCell);
        int explored = 0;

        while (queue.Count > 0 && explored < _maxClosestCellSearch)
        {
            Vector3Int current = queue.Dequeue();
            explored++;
            Topology.TryGetNeighborsInsideBounds(current, neighbors);

            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Int neighbor = neighbors[i];
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
        int bestScore = int.MaxValue;
        Vector3Int bestCell = originCell;
        
        UnitRole? role = movingUnit?.Role;
        bool isRangedOrSupport = role == UnitRole.DPS && movingUnit.CombatStyle == UnitCombatStyle.Ranged ||
                                  role == UnitRole.Support;

        for (int x = -rangeInCells; x <= rangeInCells; x++)
        {
            for (int y = -rangeInCells; y <= rangeInCells; y++)
            {
                Vector3Int candidateCell = targetCell + new Vector3Int(x, y, 0);
                if (!GridNavigationUtility.IsWithinCellRange(targetCell, candidateCell, rangeInCells))
                    continue;

                if (!IsCellEnterable(candidateCell, movingUnit))
                    continue;

                int distanceToOrigin = GridNavigationUtility.GetCellDistance(originCell, candidateCell);
                
                int neighborPenalty = CalculateCrowdingPenalty(candidateCell, movingUnit);
                
                int rolePenalty = 0;
                if (isRangedOrSupport && distanceToOrigin == 0)
                {
                    rolePenalty = 2;
                }
                
                int score = distanceToOrigin + neighborPenalty + rolePenalty;
                
                if (score < bestScore)
                {
                    bestScore = score;
                    bestCell = candidateCell;
                    found = true;
                    
                    if (neighborPenalty > 0)
                    {
                        Debug.Log($"[RoomGrid] DesiredCellScored: {candidateCell} score={score} (dist={distanceToOrigin}, crowdPenalty={neighborPenalty}, rolePenalty={rolePenalty})");
                    }
                }
            }
        }

        if (found)
        {
            resultCell = bestCell;
            Debug.Log($"[RoomGrid] DesiredCellChosen_BestScore: {resultCell} score={bestScore}");
        }
        
        return found;
    }
    
    private int CalculateCrowdingPenalty(Vector3Int cell, IGridOccupant movingOccupant)
    {
        int occupiedNeighbors = 0;
        int maxPenalty = 3;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                    
                Vector3Int neighbor = cell + new Vector3Int(dx, dy, 0);
                
                if (OccupancyService.IsOccupied(neighbor, movingOccupant) || 
                    OccupancyService.IsCellReserved(neighbor, movingOccupant))
                {
                    occupiedNeighbors++;
                }
            }
        }
        
        if (occupiedNeighbors >= 5)
            return maxPenalty;
        if (occupiedNeighbors >= 3)
            return 2;
        if (occupiedNeighbors >= 1)
            return 1;
        
        return 0;
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

            DrawAvoidanceDebugGizmos();
            DrawLastPathDebugGizmos();
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

        DrawAvoidanceDebugGizmos();
        DrawLastPathDebugGizmos();
    }

    private void DrawAvoidanceDebugGizmos()
    {
        if (!_drawAvoidanceDebug)
            return;

        GridAvoidanceObstacleRegistry registry = ResolveAvoidanceObstacleRegistry();
        if (registry == null)
            return;

        registry.GetDebugCellData(_avoidanceDebugBuffer);
        if (_avoidanceDebugBuffer.Count == 0)
            return;

        for (int i = 0; i < _avoidanceDebugBuffer.Count; i++)
        {
            GridAvoidanceCellDebugInfo debugInfo = _avoidanceDebugBuffer[i];
            Vector3 world = CellToWorld(debugInfo.Cell);
            if (!TryGetCellVisualCornersWorld(debugInfo.Cell, SharedGizmoCellCorners))
                continue;

            if (debugInfo.IsBlocked && _drawAvoidanceBlockedCells)
            {
                DrawCellDebugGizmo(
                    SharedGizmoCellCorners,
                    new Color(1f, 0.15f, 0.15f, 0.32f),
                    new Color(1f, 0.15f, 0.15f, 0.95f));
            }
            else if (debugInfo.Cost > 0 && _drawAvoidancePenalizedCells)
            {
                float normalizedCost = Mathf.Clamp01(debugInfo.Cost / 12f);
                DrawCellDebugGizmo(
                    SharedGizmoCellCorners,
                    Color.Lerp(
                        new Color(1f, 0.9f, 0.1f, 0.12f),
                        new Color(1f, 0.45f, 0.05f, 0.3f),
                        normalizedCost),
                    Color.Lerp(
                        new Color(1f, 0.9f, 0.1f, 0.85f),
                        new Color(1f, 0.45f, 0.05f, 1f),
                        normalizedCost));
            }

#if UNITY_EDITOR
            if (_drawAvoidanceCosts && debugInfo.Cost > 0)
            {
                Handles.color = debugInfo.IsBlocked ? new Color(1f, 0.85f, 0.85f, 1f) : new Color(1f, 0.95f, 0.55f, 1f);
                float labelOffset = Mathf.Abs(SharedGizmoCellCorners[0].y - SharedGizmoCellCorners[2].y) * 0.18f;
                Handles.Label(world + new Vector3(0f, labelOffset, 0f), debugInfo.Cost.ToString());
            }
#endif
        }
    }

    private static void DrawCellDebugGizmo(Vector3[] corners, Color fillColor, Color outlineColor)
    {
        if (corners == null || corners.Length < 4)
            return;

#if UNITY_EDITOR
        Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);
#else
        Gizmos.color = outlineColor;
        for (int i = 0; i < 4; i++)
        {
            Vector3 from = corners[i];
            Vector3 to = corners[(i + 1) % 4];
            Gizmos.DrawLine(from, to);
        }
#endif
    }

    private void DrawLastPathDebugGizmos()
    {
        if (!_drawLastPathDebug)
            return;

        if (!GridPathfinder.TryGetLastPathDebugInfo(this, out GridPathDebugInfo debugInfo))
            return;

        IReadOnlyList<Vector3Int> path = debugInfo.Path;
        if (path == null || path.Count == 0)
            return;

        Vector2 cellSize = CellWorldSize;
        Vector3 previousWorld = CellToWorld(path[0]);

        Gizmos.color = new Color(0.2f, 1f, 0.85f, 0.95f);
        Gizmos.DrawSphere(previousWorld, Mathf.Min(cellSize.x, cellSize.y) * 0.12f);

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 currentWorld = CellToWorld(path[i]);
            Gizmos.DrawLine(previousWorld, currentWorld);
            Gizmos.DrawSphere(currentWorld, Mathf.Min(cellSize.x, cellSize.y) * 0.09f);
            previousWorld = currentWorld;
        }
    }
}
