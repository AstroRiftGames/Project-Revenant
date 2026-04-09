using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour, IRoomContextUnitComponent
{
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private bool _allowSerializedGridFallback;
    [SerializeField] private float _repathInterval = 0.2f;

    private Unit _unit;
    private Vector3Int _currentCell;
    private float _nextStepTime;
    private Unit _cachedTargetUnit;
    private Vector3Int _cachedTargetCell;
    private bool _hasCachedTargetCell;
    private int _cachedTargetRange;
    private float _nextRepathTime;
    private readonly List<Vector3Int> _cachedPath = new();
    private RoomGrid _registeredGrid;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    private void Start()
    {
        if (!_allowSerializedGridFallback || _grid == null)
            return;

        SnapToCurrentCell();
    }

    private void OnEnable()
    {
        TryRegisterCurrentOccupancy();
    }

    private void OnDisable()
    {
        ReleaseCurrentOccupancy();
    }

    public void SetGrid(RoomGrid grid)
    {
        if (!ReferenceEquals(_grid, grid))
        {
            ReleaseOccupancyFrom(_grid);
            _grid = grid;
        }

        InvalidatePathCache();
        ClearCorpseOccupancy();
        SnapToCurrentCell();
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        SetGrid(roomContext != null ? roomContext.BattleGrid : null);
    }

    public bool SetDestinationCell(Vector3Int destinationCell)
    {
        if (_grid == null || _unit == null)
            return false;

        if (!_grid.IsCellEnterable(destinationCell, _unit))
            return false;

        _grid.OccupancyService.MoveOccupant(_unit, destinationCell);
        _currentCell = destinationCell;
        transform.position = _grid.CellToWorld(destinationCell);
        return true;
    }

    public bool SetTarget(Unit targetUnit, int rangeInCells)
    {
        if (_grid == null || _unit == null || targetUnit == null || !targetUnit.IsAlive)
            return false;

        if (IsWithinRange(targetUnit, rangeInCells))
            return false;

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        Vector3Int desiredCell = targetCell;

        if (!_grid.TryFindWalkableCellInRange(targetCell, originCell, Mathf.Max(0, rangeInCells), _unit, out desiredCell))
            return false;

        RefreshPathCache(originCell, desiredCell, targetUnit, Mathf.Max(0, rangeInCells));

        Vector3Int nextStep = GetNextStepTowards(originCell, desiredCell);

        if (nextStep == originCell)
            return false;

        if (!SetDestinationCell(nextStep))
            return false;

        float moveSpeed = Mathf.Max(0.01f, _unit.MoveSpeed);
        _nextStepTime = Time.time + (1f / moveSpeed);
        return true;
    }

    public bool MoveTowards(Unit targetUnit, int desiredDistance)
    {
        return SetTarget(targetUnit, Mathf.Max(0, desiredDistance));
    }

    public bool MoveAway(Unit targetUnit, int desiredDistance)
    {
        if (_grid == null || _unit == null || targetUnit == null || !targetUnit.IsAlive)
            return false;

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        int currentDistance = ManhattanDistance(originCell, targetCell);
        if (currentDistance >= desiredDistance)
            return false;

        Vector3Int nextStep = GetNextStepAway(originCell, targetCell, desiredDistance);
        if (nextStep == originCell)
            return false;

        if (!SetDestinationCell(nextStep))
            return false;

        float moveSpeed = Mathf.Max(0.01f, _unit.MoveSpeed);
        _nextStepTime = Time.time + (1f / moveSpeed);
        return true;
    }

    public bool IsWithinRange(Unit targetUnit, int rangeInCells)
    {
        if (_grid == null || _unit == null || targetUnit == null || !targetUnit.IsAlive)
            return false;

        Vector3Int selfCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        return ManhattanDistance(selfCell, targetCell) <= Mathf.Max(0, rangeInCells);
    }

    public void ClearDestination()
    {
        InvalidatePathCache();
    }

    public void ClearPath()
    {
        InvalidatePathCache();
    }

    private void SnapToCurrentCell()
    {
        if (_grid == null)
            return;

        Vector3Int rawCell = _grid.WorldToCell(transform.position);
        _currentCell = _grid.IsCellEnterable(rawCell, _unit)
            ? rawCell
            : _grid.FindClosestWalkableCell(rawCell, _unit);
        transform.position = _grid.CellToWorld(_currentCell);
        TryRegisterCurrentOccupancy();
    }

    private Vector3Int GetCurrentCell()
    {
        if (_grid == null)
            return Vector3Int.zero;

        _currentCell = _grid.WorldToCell(transform.position);
        return _currentCell;
    }

    private Vector3Int GetNextStepTowards(Vector3Int originCell, Vector3Int targetCell)
    {
        if (_grid == null || _unit == null)
            return originCell;

        if (_cachedPath.Count > 1 && _cachedPath[0] == originCell)
            return _cachedPath[1];

        int originDistance = ManhattanDistance(originCell, targetCell);
        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistance;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            if (!_grid.IsCellEnterable(candidate, _unit))
                continue;

            int candidateDistance = ManhattanDistance(candidate, targetCell);

            if (candidateDistance < bestImprovingDistance)
            {
                bestImprovingStep = candidate;
                bestImprovingDistance = candidateDistance;
            }

            if (candidateDistance <= originDistance && candidateDistance < bestFallbackDistance)
            {
                bestFallbackStep = candidate;
                bestFallbackDistance = candidateDistance;
            }
        }

        if (bestImprovingStep != originCell)
            return bestImprovingStep;

        if (bestFallbackStep != originCell)
            return bestFallbackStep;

        return originCell;
    }

    private Vector3Int GetNextStepAway(Vector3Int originCell, Vector3Int targetCell, int desiredDistance)
    {
        if (_grid == null || _unit == null)
            return originCell;

        int currentDistance = ManhattanDistance(originCell, targetCell);
        Vector3Int bestCandidate = originCell;
        int bestDistanceGap = int.MaxValue;
        int bestCandidateDistance = currentDistance;
        Vector3Int fallbackCandidate = originCell;
        int fallbackDistance = currentDistance;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            if (!_grid.IsCellEnterable(candidate, _unit))
                continue;

            int candidateDistance = ManhattanDistance(candidate, targetCell);
            if (candidateDistance <= currentDistance)
                continue;

            int distanceGap = Mathf.Abs(desiredDistance - candidateDistance);
            if (distanceGap < bestDistanceGap ||
                (distanceGap == bestDistanceGap && candidateDistance > bestCandidateDistance))
            {
                bestCandidate = candidate;
                bestDistanceGap = distanceGap;
                bestCandidateDistance = candidateDistance;
            }

            if (candidateDistance > fallbackDistance)
            {
                fallbackCandidate = candidate;
                fallbackDistance = candidateDistance;
            }
        }

        if (bestCandidate != originCell)
            return bestCandidate;

        if (fallbackCandidate != originCell)
            return fallbackCandidate;

        return originCell;
    }

    private void RefreshPathCache(Vector3Int originCell, Vector3Int targetCell, Unit targetUnit, int rangeInCells)
    {
        bool targetChanged = _cachedTargetUnit != targetUnit;
        bool targetCellChanged = !_hasCachedTargetCell || _cachedTargetCell != targetCell;
        bool targetRangeChanged = _cachedTargetRange != rangeInCells;
        bool pathInvalid = !IsCachedPathStillValid(originCell);
        bool shouldRepath = targetChanged || targetCellChanged || targetRangeChanged || pathInvalid || Time.time >= _nextRepathTime;

        if (!shouldRepath)
            return;

        _cachedTargetUnit = targetUnit;
        _cachedTargetCell = targetCell;
        _hasCachedTargetCell = true;
        _cachedTargetRange = rangeInCells;
        _nextRepathTime = Time.time + _repathInterval;

        _cachedPath.Clear();
        _cachedPath.AddRange(FindPath(originCell, targetCell));
    }

    private bool IsCachedPathStillValid(Vector3Int originCell)
    {
        if (_cachedPath.Count <= 1)
            return false;

        if (_cachedPath[0] != originCell)
            return false;

        for (int i = 1; i < _cachedPath.Count; i++)
        {
            if (!_grid.IsCellEnterable(_cachedPath[i], _unit))
                return false;
        }

        return true;
    }

    private List<Vector3Int> FindPath(Vector3Int startCell, Vector3Int targetCell)
    {
        if (_grid == null || _unit == null)
            return new List<Vector3Int>();

        return GridPathfinder.FindPath(_grid, startCell, targetCell, _unit);
    }

    private void InvalidatePathCache()
    {
        _cachedTargetUnit = null;
        _cachedTargetCell = Vector3Int.zero;
        _hasCachedTargetCell = false;
        _cachedTargetRange = 0;
        _nextRepathTime = 0f;
        _cachedPath.Clear();
    }

    public void CaptureCorpseOccupancy()
    {
        if (_grid == null || _unit == null)
            return;

        ReleaseCurrentOccupancy();
        _grid.OccupancyService.RegisterPersistentBlocker(_unit, GetCurrentCell());
    }

    public void ClearCorpseOccupancy()
    {
        _grid?.OccupancyService.ReleasePersistentBlocker(_unit);
    }

    private void TryRegisterCurrentOccupancy()
    {
        if (_grid == null || _unit == null || !isActiveAndEnabled)
            return;

        if (!ReferenceEquals(_registeredGrid, _grid))
            ReleaseCurrentOccupancy();

        _grid.OccupancyService.RegisterOccupant(_unit, GetCurrentCell());
        _registeredGrid = _grid;
    }

    private void ReleaseCurrentOccupancy()
    {
        if (_unit == null || _registeredGrid == null)
            return;

        _registeredGrid.OccupancyService.ReleaseOccupant(_unit);
        _registeredGrid = null;
    }

    private void ReleaseOccupancyFrom(RoomGrid grid)
    {
        if (grid == null || _unit == null)
            return;

        grid.OccupancyService.ReleaseOccupant(_unit);
        grid.OccupancyService.ReleasePersistentBlocker(_unit);

        if (ReferenceEquals(_registeredGrid, grid))
            _registeredGrid = null;
    }

    private static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
