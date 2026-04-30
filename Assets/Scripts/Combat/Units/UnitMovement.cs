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
    private Vector2 _currentMovementDirection;
    
    private Vector3Int _stepOriginCell;
    private Vector3Int _reservedDestinationCell;
    
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
    public Vector2 CurrentMovementDirection => _currentMovementDirection;

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
        SetGrid(roomContext != null ? roomContext.RoomGrid : null);
    }

public bool SetDestinationCell(Vector3Int destinationCell)
    {
        if (_grid == null || _unit == null)
            return false;

        if (_unit.StatusEffects != null && !_unit.StatusEffects.CanMove)
            return false;

        if (_isMoving)
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
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        Vector3Int desiredCell = targetCell;

        if (!_grid.TryFindWalkableCellInRange(targetCell, originCell, Mathf.Max(0, rangeInCells), _unit, out desiredCell))
            return false;

        if (!TryResolveBlockedDesiredCell(desiredCell, targetCell, originCell, rangeInCells, targetUnit, out Vector3Int resolvedCell))
        {
            return false;
        }

        if (resolvedCell != desiredCell)
        {
            desiredCell = resolvedCell;
            Debug.Log($"[UnitMovement] {name} - DesiredCellResolved: original={targetCell} resolved={desiredCell}");
        }

        RefreshPathCache(originCell, targetCell, desiredCell, targetUnit, Mathf.Max(0, rangeInCells));

        Vector3Int nextStep = GetNextStepTowards(originCell);
        return TryCommitMovementStep(originCell, nextStep);
    }

    private bool TryResolveBlockedDesiredCell(Vector3Int desiredCell, Vector3Int targetCell, Vector3Int originCell, int rangeInCells, Unit targetUnit, out Vector3Int resolvedCell)
    {
        resolvedCell = desiredCell;

        if (_grid.OccupancyService.IsCellBlockedFor(_unit, desiredCell))
        {
            IGridOccupant blockingOccupant = _grid.OccupancyService.GetBlockingOccupant(desiredCell, _unit);
            IGridOccupant reservingOccupant = _grid.OccupancyService.GetReservingOccupant(desiredCell);

            Unit blockerAsUnit = blockingOccupant as Unit;
            bool isEnemyBlocker = blockerAsUnit != null && _unit.IsHostileTo(blockerAsUnit);
            bool isAllyBlocker = blockerAsUnit != null && !_unit.IsHostileTo(blockerAsUnit);

            bool isReservedByAlly = false;
            if (reservingOccupant != null && reservingOccupant != _unit)
            {
                Unit reserverAsUnit = reservingOccupant as Unit;
                isReservedByAlly = reserverAsUnit != null && !_unit.IsHostileTo(reserverAsUnit);
            }

            if (isEnemyBlocker)
            {
                Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByEnemy: {desiredCell} blocker={blockerAsUnit?.name}");

                if (_grid.TryFindAttackPositionFromBlockedDesiredCell(desiredCell, targetCell, originCell, rangeInCells, _unit, targetUnit, out Vector3Int attackPosition))
                {
                    if (attackPosition == originCell)
                    {
                        Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByEnemy_AlreadyInRange: can attack from current position");
                        resolvedCell = originCell;
                        return true;
                    }

                    Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByEnemy_AttackFromNearestValid: {attackPosition}");
                    resolvedCell = attackPosition;
                    return true;
                }

                Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByEnemy_NoValidAttackPosition");
                return false;
            }

            if (isAllyBlocker || isReservedByAlly)
            {
                string blockType = isAllyBlocker ? "Occupied" : "Reserved";
                string blockerName = isAllyBlocker ? blockerAsUnit?.name : (reservingOccupant as Unit)?.name;
                Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByAlly_{blockType}: {desiredCell} blocker={blockerName}");

                if (_grid.TryFindNearbyAlternativeCell(desiredCell, targetCell, originCell, rangeInCells, _unit, out Vector3Int alternativeCell))
                {
                    Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByAlly_RepositionNearby: {alternativeCell}");
                    resolvedCell = alternativeCell;
                    return true;
                }

                Debug.Log($"[UnitMovement] {name} - DesiredCellBlockedByAlly_NoNearbyCellFound");
                return false;
            }
        }

        return true;
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

    public void InterruptMovement()
    {
        InvalidatePathCache();
        
        if (_reservedDestinationCell != Vector3Int.zero)
        {
            _grid?.OccupancyService.ReleaseReservation(_unit);
            _stepOriginCell = Vector3Int.zero;
            _reservedDestinationCell = Vector3Int.zero;
        }

        ResetVisualStep();

        if (_grid != null && _hasCurrentCell)
            transform.position = _grid.CellToWorld(_currentCell);
    }

    public void ForceSyncPosition()
    {
        InvalidatePathCache();
        _hasCurrentCell = false;
        if (_grid != null)
            _currentCell = GridNavigationUtility.ResolvePlacementCell(_grid, transform.position, _unit);
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

    private Vector3Int GetNextStepTowards(Vector3Int originCell)
    {
        if (_grid == null || _unit == null)
            return originCell;

        if (_cachedPath.Count > 1 && _cachedPath[0] == originCell)
        {
            Vector3Int cachedNextStep = _cachedPath[1];
            

            if (!_grid.OccupancyService.IsCellBlockedFor(_unit, cachedNextStep))
            {
                if (_grid.IsStepAllowed(originCell, cachedNextStep, _unit))
                {
                    return cachedNextStep;
                }
            }
            
            Debug.Log($"[UnitMovement] {name} - CachedStepInvalidated: {cachedNextStep}");
        }

        Vector3Int targetReference = _cachedTargetCell != Vector3Int.zero ? _cachedTargetCell : 
                                    (_cachedPath.Count > 2 ? _cachedPath[_cachedPath.Count - 1] : originCell);
        
        int originDistance = GridNavigationUtility.GetCellDistance(originCell, targetReference);
        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistance;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell, _unit);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            
            if (_grid.OccupancyService.IsCellBlockedFor(_unit, candidate))
                continue;
            
            int candidateDistance = GridNavigationUtility.GetCellDistance(candidate, targetReference);

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

    private void RefreshPathCache(Vector3Int originCell, Vector3Int targetCell, Vector3Int desiredCell, Unit targetUnit, int rangeInCells)
    {
        bool targetChanged = _cachedTargetUnit != targetUnit;
        bool targetCellChanged = !_hasCachedTargetCell || _cachedTargetCell != targetCell;
        bool targetRangeChanged = _cachedTargetRange != rangeInCells;
        
        bool pathStillValid = IsCachedPathStillValid(originCell);
        
        string invalidationReason = null;
        if (pathStillValid && _cachedPath.Count > 1)
        {
            Vector3Int nextStep = _cachedPath[1];
            if (_grid.OccupancyService.IsOccupied(nextStep, _unit))
            {
                invalidationReason = "NextStepOccupied";
                pathStillValid = false;
            }
            else if (_grid.OccupancyService.IsCellReserved(nextStep, _unit))
            {
                invalidationReason = "NextStepReserved";
                pathStillValid = false;
            }
        }
        
        bool shouldRepath = targetChanged || targetCellChanged || targetRangeChanged || 
                            !pathStillValid || Time.time >= _nextRepathTime;

        if (!shouldRepath)
            return;

        if (invalidationReason != null)
        {
            Debug.Log($"[UnitMovement] {name} - PathInvalidated: {invalidationReason}");
        }

        _cachedTargetUnit = targetUnit;
        _cachedTargetCell = targetCell;
        _hasCachedTargetCell = true;
        _cachedTargetRange = rangeInCells;
        _nextRepathTime = Time.time + _repathInterval;

        _cachedPath.Clear();
        _cachedPath.AddRange(FindPath(originCell, desiredCell));
        
        if (_cachedPath.Count > 0)
        {
            Debug.Log($"[UnitMovement] {name} - PathRecalculated: {_cachedPath.Count} steps from {originCell} to {desiredCell}");
        }
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
            
            InvalidatePathCache();
            
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
            InvalidatePathCache();
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
        
        CommitStepOccupancy();
    }

private void ResetVisualStep()
    {
        _isMoving = false;
        _stepProgress = 0f;
        _currentMovementDirection = Vector2.zero;
    }
    
    private void CommitStepOccupancy()
    {
        if (_grid == null)
        {
            ResetVisualStep();
            return;
        }
        
        if (_reservedDestinationCell != Vector3Int.zero)
        {
            _grid.OccupancyService.MoveOccupant(_unit, _reservedDestinationCell);
            _currentCell = _reservedDestinationCell;
            _hasCurrentCell = true;
            
            _grid.OccupancyService.ReleaseReservation(_unit);
            
            Debug.Log($"[UnitMovement] {name} - StepCommitted: {_stepOriginCell} -> {_reservedDestinationCell}");
            
            _stepOriginCell = Vector3Int.zero;
            _reservedDestinationCell = Vector3Int.zero;
        }
        
        ResetVisualStep();
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
