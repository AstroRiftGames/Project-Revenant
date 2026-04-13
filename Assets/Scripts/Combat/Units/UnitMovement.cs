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
    private bool _hasCurrentCell;
    private float _nextStepTime;
    private Unit _cachedTargetUnit;
    private Vector3Int _cachedTargetCell;
    private bool _hasCachedTargetCell;
    private int _cachedTargetRange;
    private float _nextRepathTime;
    private readonly List<Vector3Int> _cachedPath = new();
    private RoomGrid _registeredGrid;
    private bool _isMoving;
    private Vector3 _stepStartWorld;
    private Vector3 _stepTargetWorld;
    private float _stepProgress;

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

    private void Update()
    {
        UpdateStepPresentation();
    }

    public bool IsMoving => _isMoving;

    public void SetGrid(RoomGrid grid)
    {
        if (!ReferenceEquals(_grid, grid))
        {
            ReleaseOccupancyFrom(_grid);
            _grid = grid;
        }

        InvalidatePathCache();
        ClearCorpseOccupancy();
        ResetVisualStep();
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

        if (_isMoving)
            return false;

        Vector3Int originCell = GetCurrentCell();
        if (destinationCell == originCell)
            return false;

        if (!_grid.IsCellEnterable(destinationCell, _unit))
            return false;

        _grid.OccupancyService.MoveOccupant(_unit, destinationCell);
        _currentCell = destinationCell;
        _hasCurrentCell = true;
        BeginVisualStep(originCell, destinationCell);
        return true;
    }

    public bool SetTarget(Unit targetUnit, int rangeInCells)
    {
        if (!CanEvaluateTarget(targetUnit))
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
        return TryCommitMovementStep(originCell, nextStep);
    }

    public bool MoveTowards(Unit targetUnit, int desiredDistance)
    {
        return SetTarget(targetUnit, Mathf.Max(0, desiredDistance));
    }

    public bool MoveAway(Unit targetUnit, int desiredDistance)
    {
        if (!CanEvaluateTarget(targetUnit))
            return false;

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        int currentDistance = GridNavigationUtility.GetCellDistance(originCell, targetCell);
        if (currentDistance >= desiredDistance)
            return false;

        Vector3Int nextStep = GetNextStepAway(originCell, targetCell, desiredDistance);
        return TryCommitMovementStep(originCell, nextStep);
    }

    public bool IsWithinRange(Unit targetUnit, int rangeInCells)
    {
        if (!CanEvaluateTarget(targetUnit))
            return false;

        Vector3Int selfCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        return GridNavigationUtility.IsWithinCellRange(selfCell, targetCell, rangeInCells);
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
        {
            _hasCurrentCell = false;
            return;
        }

        _currentCell = GridNavigationUtility.ResolvePlacementCell(_grid, transform.position, _unit);
        _hasCurrentCell = true;
        transform.position = _grid.CellToWorld(_currentCell);
        TryRegisterCurrentOccupancy();
    }

    private Vector3Int GetCurrentCell()
    {
        if (_grid == null)
            return Vector3Int.zero;

        if (!_hasCurrentCell)
        {
            _currentCell = GridNavigationUtility.ResolvePlacementCell(_grid, transform.position, _unit);
            _hasCurrentCell = true;
        }

        return _currentCell;
    }

    public bool TryGetLogicalCell(out Vector3Int cell)
    {
        if (_grid == null)
        {
            cell = Vector3Int.zero;
            return false;
        }

        cell = GetCurrentCell();
        return true;
    }

    public bool TryGetLogicalWorldPosition(out Vector3 worldPosition)
    {
        if (_grid == null)
        {
            worldPosition = transform.position;
            return false;
        }

        worldPosition = _grid.CellToWorld(GetCurrentCell());
        return true;
    }

    private Vector3Int GetNextStepTowards(Vector3Int originCell, Vector3Int targetCell)
    {
        if (_grid == null || _unit == null)
            return originCell;

        if (_cachedPath.Count > 1 && _cachedPath[0] == originCell)
            return _cachedPath[1];

        int originDistance = GridNavigationUtility.GetCellDistance(originCell, targetCell);
        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistance;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell, _unit);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            int candidateDistance = GridNavigationUtility.GetCellDistance(candidate, targetCell);

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

        int currentDistance = GridNavigationUtility.GetCellDistance(originCell, targetCell);
        Vector3Int bestCandidate = originCell;
        int bestDistanceGap = int.MaxValue;
        int bestCandidateDistance = currentDistance;
        Vector3Int fallbackCandidate = originCell;
        int fallbackDistance = currentDistance;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell, _unit);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            int candidateDistance = GridNavigationUtility.GetCellDistance(candidate, targetCell);
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

        Vector3Int previousCell = _cachedPath[0];
        for (int i = 1; i < _cachedPath.Count; i++)
        {
            Vector3Int currentCell = _cachedPath[i];
            if (!_grid.IsStepAllowed(previousCell, currentCell, _unit))
                return false;

            previousCell = currentCell;
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

    private bool CanEvaluateTarget(Unit targetUnit)
    {
        return _grid != null &&
               _unit != null &&
               targetUnit != null &&
               targetUnit.IsAlive;
    }

    private bool TryCommitMovementStep(Vector3Int originCell, Vector3Int nextStep)
    {
        if (nextStep == originCell)
            return false;

        if (!SetDestinationCell(nextStep))
            return false;

        ScheduleNextStepTime();
        return true;
    }

    private void ScheduleNextStepTime()
    {
        float moveSpeed = Mathf.Max(0.01f, _unit.MoveSpeed);
        _nextStepTime = Time.time + (1f / moveSpeed);
    }

    private void BeginVisualStep(Vector3Int originCell, Vector3Int destinationCell)
    {
        if (_grid == null)
            return;

        _stepStartWorld = _grid.CellToWorld(originCell);
        _stepTargetWorld = _grid.CellToWorld(destinationCell);
        _stepProgress = 0f;
        _isMoving = true;
        transform.position = _stepStartWorld;
    }

    private void UpdateStepPresentation()
    {
        if (!_isMoving || _grid == null)
            return;

        float stepDuration = Mathf.Max(0.0001f, 1f / Mathf.Max(0.01f, _unit.MoveSpeed));
        _stepProgress = Mathf.Min(1f, _stepProgress + (Time.deltaTime / stepDuration));
        transform.position = Vector3.Lerp(_stepStartWorld, _stepTargetWorld, _stepProgress);

        if (_stepProgress < 1f)
            return;

        transform.position = _stepTargetWorld;
        ResetVisualStep();
    }

    private void ResetVisualStep()
    {
        _isMoving = false;
        _stepProgress = 0f;
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
}
