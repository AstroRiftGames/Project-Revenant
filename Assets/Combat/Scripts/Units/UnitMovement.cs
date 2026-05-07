using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour, IRoomContextUnitComponent
{
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private bool _allowSerializedGridFallback;
    [SerializeField] private float _repathInterval = 0.2f;

    private Unit _unit;
    private UnitMovementPlanner _planner;
    private Vector3Int _currentCell;
    private bool _hasCurrentCell;
    private float _nextStepTime;
    private RoomGrid _registeredGrid;
    private bool _hasActiveStep;
    private bool _isMoving;
    private Vector3 _stepStartWorld;
    private Vector3 _stepTargetWorld;
    private float _stepProgress;
    private Vector2 _currentMovementDirection;
    
    private Vector3Int _stepOriginCell;
    private Vector3Int _reservedDestinationCell;
    private bool _hasReservedDestination;
    
    [SerializeField] private float _softBlockRetryDelay = 0.1f;
    [SerializeField] private float _hardBlockRetryDelay = 0.3f;
    [SerializeField] private int _maxSoftRetries = 3;
    [SerializeField] private int _deadlockMaxAttempts = 5;
    [SerializeField] private float _deadlockCooldown = 1.0f;
    
    private int _consecutiveBlockedSteps;
    private float _nextRetryTime;
    
    private Vector3Int _lastOriginCell;
    private Vector3Int _lastFailedStep;
    private int _sameStepFailureCount;
    private int _totalAttemptsWithoutProgress;
    private float _lastProgressTime;
    private bool _isInDeadlock;
    private float _deadlockCooldownEndTime;
    private readonly int _maxHistorySize = 8;
    private readonly List<Vector3Int> _recentFailedSteps = new();

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _planner = new UnitMovementPlanner(_repathInterval);
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
        StopMovementAndReleaseReservation(snapToCurrentCell: true, invalidatePlanner: true);
        ReleaseCurrentOccupancy();
    }

    private void Update()
    {
        UpdateStepPresentation();
    }

    public bool IsMoving => _isMoving;
    public Vector2 CurrentMovementDirection => _currentMovementDirection;

    private UnitMovementPlanner Planner
    {
        get
        {
            _planner ??= new UnitMovementPlanner(_repathInterval);
            _planner.SetRepathInterval(_repathInterval);
            return _planner;
        }
    }

    public void SetGrid(RoomGrid grid)
    {
        bool gridChanged = !ReferenceEquals(_grid, grid);
        bool requiresRuntimeReset = gridChanged || _hasActiveStep || _hasReservedDestination || _isMoving;
        if (requiresRuntimeReset)
            StopMovementAndReleaseReservation(snapToCurrentCell: false, invalidatePlanner: true);
        else
            Planner.InvalidatePathCache();

        if (gridChanged)
        {
            ReleaseOccupancyFrom(_grid);
            _grid = grid;
        }

        ClearCorpseOccupancy();
        SnapToCurrentCell();
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        SetGrid(roomContext != null ? roomContext.RoomGrid : null);
    }

    public bool SetDestinationCell(Vector3Int destinationCell)
    {
        if (_grid == null || _unit == null)
            return false;

        if (_unit.StatusEffects != null && !_unit.StatusEffects.CanMove)
            return false;

        if (_hasActiveStep || _isMoving)
            return false;

        Vector3Int originCell = GetCurrentCell();
        if (destinationCell == originCell)
            return false;

        if (!_grid.IsCellEnterable(destinationCell, _unit))
            return false;

        if (!_grid.OccupancyService.TryReserveCell(destinationCell, _unit))
        {
            Debug.Log($"[UnitMovement] {name} - ReservationFailed: {destinationCell}");
            return false;
        }

        _stepOriginCell = originCell;
        _reservedDestinationCell = destinationCell;
        _hasReservedDestination = true;

        BeginVisualStep(originCell, destinationCell);
        return true;
    }

    public bool SetTarget(Unit targetUnit, int rangeInCells)
    {
        if (!CanEvaluateTarget(targetUnit))
            return false;

        if (_unit != null && _unit.StatusEffects != null && !_unit.StatusEffects.CanMoveTowardTarget)
            return false;

        if (IsWithinRange(targetUnit, rangeInCells))
        {
            Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByEnemy_AlreadyInRange: {targetUnit.name}");
            return false;
        }

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        UnitMovementDecision decision = Planner.PlanTowards(
            _grid,
            _unit,
            originCell,
            targetUnit,
            Mathf.Max(0, rangeInCells),
            name);

        if (!decision.HasMove)
            return false;

        return TryCommitMovementStep(originCell, decision.NextStepCell);
    }

    public bool MoveTowards(Unit targetUnit, int desiredDistance)
    {
        return SetTarget(targetUnit, Mathf.Max(0, desiredDistance));
    }

    public bool MoveAway(Unit targetUnit, int desiredDistance)
    {
        if (!CanEvaluateTarget(targetUnit))
            return false;

        if (_unit != null && _unit.StatusEffects != null && !_unit.StatusEffects.CanMove)
            return false;

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        int currentDistance = GridNavigationUtility.GetCellDistance(originCell, targetCell);
        if (currentDistance >= desiredDistance)
            return false;

        UnitMovementDecision decision = Planner.PlanAway(_grid, _unit, originCell, targetUnit, desiredDistance);
        if (!decision.HasMove)
            return false;

        return TryCommitMovementStep(originCell, decision.NextStepCell);
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
        Planner.InvalidatePathCache();
    }

    public void ClearPath()
    {
        Planner.InvalidatePathCache();
    }

    public void InterruptMovement()
    {
        StopMovementAndReleaseReservation(snapToCurrentCell: true, invalidatePlanner: true);
    }

    public void ForceSyncPosition()
    {
        ForceSyncToWorldPosition(transform.position);
    }

    public bool ForceRelocateToCell(Vector3Int cell)
    {
        if (_grid == null || _unit == null)
            return false;

        if (!_grid.IsCellEnterable(cell, _unit))
            return false;

        return RelocateToCellInternal(cell);
    }

    public bool ForceSyncToCell(Vector3Int cell)
    {
        return ForceRelocateToCell(cell);
    }

    public bool ForceSyncToWorldPosition(Vector3 worldPosition)
    {
        StopMovementAndReleaseReservation(snapToCurrentCell: false, invalidatePlanner: false);
        Planner.InvalidatePathCache();

        if (_grid == null || _unit == null)
        {
            _hasCurrentCell = false;
            transform.position = worldPosition;
            return false;
        }

        Vector3Int resolvedCell = GridNavigationUtility.ResolvePlacementCell(_grid, worldPosition, _unit);
        return RelocateToCellInternal(resolvedCell);
    }

    private void SnapToCurrentCell()
    {
        if (_grid == null)
        {
            _hasCurrentCell = false;
            ResetVisualStepRuntime();
            return;
        }

        _currentCell = GridNavigationUtility.ResolvePlacementCell(_grid, transform.position, _unit);
        _hasCurrentCell = true;
        transform.position = _grid.CellToWorld(_currentCell);
        ResetVisualStepRuntime();
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

    private enum DeadlockType
    {
        None,
        RepeatedStepFailure,  //Misma celda fallida repetidamente
        MutualBlock,           //Dos unidades bloqueandose mutuamente
        ChainBlock,            //Multiples unidades aliadas en cadena
        StalledNoProgress      //Demasiados intentos sin avance real
    }
    
    private void UpdateDeadlockState(Vector3Int originCell, Vector3Int failedStep)
    {
        _recentFailedSteps.Add(failedStep);
        if (_recentFailedSteps.Count > _maxHistorySize)
            _recentFailedSteps.RemoveAt(0);
        
        if (_lastOriginCell != originCell)
        {
            _lastOriginCell = originCell;
            _sameStepFailureCount = 0;
        }
        
        if (failedStep == _lastFailedStep)
        {
            _sameStepFailureCount++;
        }
        else
        {
            _sameStepFailureCount = 1;
        }
        
        _lastFailedStep = failedStep;
    }
    
    private DeadlockType DetectDeadlockType(Vector3Int originCell, Vector3Int failedStep)
    {
        if (_sameStepFailureCount >= _deadlockMaxAttempts)
        {
            return DeadlockType.RepeatedStepFailure;
        }
        
        if (_totalAttemptsWithoutProgress >= _deadlockMaxAttempts * 2)
        {
            return DeadlockType.StalledNoProgress;
        }
        
        if (_recentFailedSteps.Count >= 4)
        {
            int lastIndex = _recentFailedSteps.Count - 1;
            if (lastIndex >= 1 && _recentFailedSteps[lastIndex] == _recentFailedSteps[lastIndex - 2] &&
                _recentFailedSteps[lastIndex - 1] != _recentFailedSteps[lastIndex])
            {
                return DeadlockType.MutualBlock;
            }
        }
        
        if (_consecutiveBlockedSteps >= _deadlockMaxAttempts && _recentFailedSteps.Count >= 3)
        {
            HashSet<Vector3Int> uniqueFailed = new(_recentFailedSteps);
            if (uniqueFailed.Count >= 2)
            {
                return DeadlockType.ChainBlock;
            }
        }
        
        return DeadlockType.None;
    }
    
    private bool ResolveDeadlock(DeadlockType type, Vector3Int originCell)
    {
        Debug.Log($"[UnitMovement] {name} - DeadlockResolved: {type}");
        
        _recentFailedSteps.Clear();
        
        switch (type)
        {
            case DeadlockType.RepeatedStepFailure:
            case DeadlockType.MutualBlock:
            case DeadlockType.ChainBlock:
            case DeadlockType.StalledNoProgress:
                _isInDeadlock = true;
                _deadlockCooldownEndTime = Time.time + _deadlockCooldown;
                _totalAttemptsWithoutProgress = 0;
                _sameStepFailureCount = 0;
                _nextRetryTime = _deadlockCooldownEndTime;
                
                Debug.Log($"[UnitMovement] {name} - DeadlockCooldown_Started: {_deadlockCooldown}s");
                return false;
                
            default:
                return false;
        }
    }
    
    private void OnProgressMade()
    {
        _totalAttemptsWithoutProgress = 0;
        _sameStepFailureCount = 0;
        _recentFailedSteps.Clear();
        
        if (_isInDeadlock)
        {
            Debug.Log($"[UnitMovement] {name} - DeadlockCooldown_Cleared");
            _isInDeadlock = false;
            _deadlockCooldownEndTime = 0f;
        }
        
        _lastProgressTime = Time.time;
    }
    
    public bool IsInDeadlock => _isInDeadlock && Time.time < _deadlockCooldownEndTime;

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

    private void StopMovementAndReleaseReservation(bool snapToCurrentCell, bool invalidatePlanner)
    {
        if (invalidatePlanner)
            Planner.InvalidatePathCache();

        ReleaseActiveReservation();
        ClearActiveStepCells();
        ResetVisualStepRuntime();

        if (snapToCurrentCell && _grid != null && _hasCurrentCell)
            transform.position = _grid.CellToWorld(_currentCell);
    }

    private void ReleaseActiveReservation()
    {
        if (_hasReservedDestination)
            _grid?.OccupancyService.ReleaseReservation(_unit);

        _hasReservedDestination = false;
    }

    private void ResetVisualStepRuntime()
    {
        _hasActiveStep = false;
        _isMoving = false;
        _stepProgress = 0f;
        _currentMovementDirection = Vector2.zero;
    }

    private void ClearActiveStepCells()
    {
        _stepOriginCell = default;
        _reservedDestinationCell = default;
        _hasReservedDestination = false;
    }

    private bool RelocateToCellInternal(Vector3Int cell)
    {
        if (_grid == null || _unit == null)
            return false;

        StopMovementAndReleaseReservation(snapToCurrentCell: false, invalidatePlanner: false);

        _currentCell = cell;
        _hasCurrentCell = true;
        transform.position = _grid.CellToWorld(cell);
        _grid.OccupancyService.RegisterOccupant(_unit, cell);
        _registeredGrid = _grid;
        Planner.InvalidatePathCache();
        return true;
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
        if (Time.time < _nextRetryTime)
        {
            return false;
        }
        
        if (nextStep == originCell)
            return false;

        if (_isInDeadlock && Time.time < _deadlockCooldownEndTime)
        {
            return false;
        }
        
        if (Time.time < _nextRetryTime)
        {
            return false;
        }

        if (_grid.OccupancyService.IsCellBlockedFor(_unit, nextStep))
        {
            bool isOccupied = _grid.OccupancyService.IsOccupied(nextStep, _unit);
            bool isReserved = _grid.OccupancyService.IsCellReserved(nextStep, _unit);
            
            Debug.Log($"[UnitMovement] {name} - StepBlocked: {(isOccupied ? "Occupied" : isReserved ? "Reserved" : "Unknown")} at {nextStep}");
            
            UpdateDeadlockState(originCell, nextStep);
            
            DeadlockType deadlockType = DetectDeadlockType(originCell, nextStep);
            
            if (deadlockType != DeadlockType.None)
            {
                Debug.Log($"[UnitMovement] {name} - DeadlockDetected: {deadlockType} | attempts: {_totalAttemptsWithoutProgress}, sameStep: {_sameStepFailureCount}");
                return ResolveDeadlock(deadlockType, originCell);
            }
            
            Planner.InvalidatePathCache();
            
            _consecutiveBlockedSteps++;
            _totalAttemptsWithoutProgress++;
            
            if (isReserved)
            {
                if (_consecutiveBlockedSteps > _maxSoftRetries)
                {
                    _nextRetryTime = Time.time + _hardBlockRetryDelay;
                    Debug.Log($"[UnitMovement] {name} - SoftBlockEscalated: hard retry in {_hardBlockRetryDelay}s");
                }
                else
                {
                    _nextRetryTime = Time.time + _softBlockRetryDelay;
                }
            }
            else
            {
                _nextRetryTime = Time.time + _hardBlockRetryDelay;
                Debug.Log($"[UnitMovement] {name} - HardBlockCooldown: {_hardBlockRetryDelay}s");
            }
            
            return false;
        }

        if (!SetDestinationCell(nextStep))
        {
            Debug.Log($"[UnitMovement] {name} - ReservationFailed at {nextStep}");
            Planner.InvalidatePathCache();
            return false;
        }

        OnProgressMade();
        
        _consecutiveBlockedSteps = 0;
        _nextRetryTime = 0f;
        
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
        Vector3 stepDelta = _stepTargetWorld - _stepStartWorld;
        _currentMovementDirection = new Vector2(stepDelta.x, stepDelta.y).normalized;
        _stepProgress = 0f;
        _hasActiveStep = true;
        _isMoving = true;
        transform.position = _stepStartWorld;
    }

    private void UpdateStepPresentation()
    {
        if (!_isMoving || !_hasActiveStep || _grid == null)
            return;

        float stepDuration = Mathf.Max(0.0001f, 1f / Mathf.Max(0.01f, _unit.MoveSpeed));
        _stepProgress = Mathf.Min(1f, _stepProgress + (Time.deltaTime / stepDuration));
        transform.position = Vector3.Lerp(_stepStartWorld, _stepTargetWorld, _stepProgress);

        if (_stepProgress < 1f)
            return;

        transform.position = _stepTargetWorld;
        
        CommitStepOccupancy();
    }

    private void CommitStepOccupancy()
    {
        if (_grid == null)
        {
            ResetVisualStepRuntime();
            return;
        }

        if (!_hasActiveStep || !_hasReservedDestination)
        {
            ResetVisualStepRuntime();
            return;
        }

        bool reservationStillOwned = _grid.OccupancyService.IsCellReservedBy(_reservedDestinationCell, _unit);
        bool destinationStillEnterable = _grid.IsCellEnterable(_reservedDestinationCell, _unit);
        bool destinationStillAllowed = _grid.IsStepAllowed(_stepOriginCell, _reservedDestinationCell, _unit);
        bool destinationOccupiedByOther = _grid.OccupancyService.IsOccupied(_reservedDestinationCell, _unit);

        if (!reservationStillOwned || !destinationStillEnterable || !destinationStillAllowed || destinationOccupiedByOther)
        {
            StopMovementAndReleaseReservation(snapToCurrentCell: true, invalidatePlanner: true);
            return;
        }

        _grid.OccupancyService.MoveOccupant(_unit, _reservedDestinationCell);
        _currentCell = _reservedDestinationCell;
        _hasCurrentCell = true;

        _grid.OccupancyService.ReleaseReservation(_unit);

        Debug.Log($"[UnitMovement] {name} - StepCommitted: {_stepOriginCell} -> {_reservedDestinationCell}");

        ResetVisualStepRuntime();
        ClearActiveStepCells();
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

        grid.OccupancyService.ReleaseReservation(_unit);
        grid.OccupancyService.ReleaseOccupant(_unit);
        grid.OccupancyService.ReleasePersistentBlocker(_unit);

        if (ReferenceEquals(_registeredGrid, grid))
            _registeredGrid = null;
    }
}
