using System.Collections.Generic;
using UnityEngine;

public sealed class UnitMovementPlanner
{
    private readonly List<Vector3Int> _cachedPath = new();

    private float _repathInterval;
    private Unit _cachedTargetUnit;
    private Vector3Int _cachedTargetCell;
    private bool _hasCachedTargetCell;
    private int _cachedTargetRange;
    private float _nextRepathTime;

    public UnitMovementPlanner(float repathInterval)
    {
        SetRepathInterval(repathInterval);
    }

    public void SetRepathInterval(float repathInterval)
    {
        _repathInterval = Mathf.Max(0f, repathInterval);
    }

    public void InvalidatePathCache()
    {
        _cachedTargetUnit = null;
        _cachedTargetCell = Vector3Int.zero;
        _hasCachedTargetCell = false;
        _cachedTargetRange = 0;
        _nextRepathTime = 0f;
        _cachedPath.Clear();
    }

    public UnitMovementDecision PlanTowards(
        RoomGrid grid,
        Unit movingUnit,
        Vector3Int originCell,
        Unit targetUnit,
        int rangeInCells,
        string debugName)
    {
        if (grid == null || movingUnit == null || targetUnit == null || !targetUnit.IsAlive)
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.InvalidRequest, originCell, originCell);

        Vector3Int targetCell = grid.WorldToCell(targetUnit.Position);
        Vector3Int desiredCell = targetCell;
        int resolvedRange = Mathf.Max(0, rangeInCells);

        if (!grid.TryFindWalkableCellInRange(targetCell, originCell, resolvedRange, movingUnit, out desiredCell))
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.NoDesiredCell, desiredCell, originCell);

        if (!TryResolveBlockedDesiredCell(
                grid,
                movingUnit,
                debugName,
                desiredCell,
                targetCell,
                originCell,
                resolvedRange,
                targetUnit,
                out Vector3Int resolvedCell))
        {
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.DesiredCellBlocked, desiredCell, originCell);
        }

        if (resolvedCell != desiredCell)
        {
            desiredCell = resolvedCell;
            Debug.Log($"[UnitMovement] {debugName} - DesiredCellResolved: original={targetCell} resolved={desiredCell}");
        }

        bool pathChanged = RefreshPathCache(
            grid,
            movingUnit,
            debugName,
            originCell,
            targetCell,
            desiredCell,
            targetUnit,
            resolvedRange);

        StepSelectionResult nextStep = GetNextStepTowards(grid, movingUnit, debugName, originCell);
        if (nextStep.Step == originCell)
        {
            return UnitMovementDecision.NoMove(
                UnitMovementPlanReason.NoStep,
                desiredCell,
                nextStep.Step,
                pathChanged,
                nextStep.UsedFallback);
        }

        return UnitMovementDecision.Move(
            desiredCell,
            nextStep.Step,
            UnitMovementPlanReason.MoveTowardsTarget,
            pathChanged,
            nextStep.UsedFallback);
    }

    public UnitMovementDecision PlanAway(
        RoomGrid grid,
        Unit movingUnit,
        Vector3Int originCell,
        Unit targetUnit,
        int desiredDistance)
    {
        if (grid == null || movingUnit == null || targetUnit == null || !targetUnit.IsAlive)
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.InvalidRequest, originCell, originCell);

        Vector3Int targetCell = grid.WorldToCell(targetUnit.Position);
        StepSelectionResult nextStep = GetNextStepAway(grid, movingUnit, originCell, targetCell, desiredDistance);
        if (nextStep.Step == originCell)
        {
            return UnitMovementDecision.NoMove(
                UnitMovementPlanReason.NoStep,
                originCell,
                nextStep.Step,
                pathChanged: false,
                usedFallback: nextStep.UsedFallback);
        }

        return UnitMovementDecision.Move(
            nextStep.Step,
            nextStep.Step,
            UnitMovementPlanReason.MoveAwayFromTarget,
            pathChanged: false,
            usedFallback: nextStep.UsedFallback);
    }

    private bool TryResolveBlockedDesiredCell(
        RoomGrid grid,
        Unit movingUnit,
        string debugName,
        Vector3Int desiredCell,
        Vector3Int targetCell,
        Vector3Int originCell,
        int rangeInCells,
        Unit targetUnit,
        out Vector3Int resolvedCell)
    {
        resolvedCell = desiredCell;

        if (grid.OccupancyService.IsCellBlockedFor(movingUnit, desiredCell))
        {
            IGridOccupant blockingOccupant = grid.OccupancyService.GetBlockingOccupant(desiredCell, movingUnit);
            IGridOccupant reservingOccupant = grid.OccupancyService.GetReservingOccupant(desiredCell);

            Unit blockerAsUnit = blockingOccupant as Unit;
            bool isEnemyBlocker = blockerAsUnit != null && movingUnit.IsHostileTo(blockerAsUnit);
            bool isAllyBlocker = blockerAsUnit != null && !movingUnit.IsHostileTo(blockerAsUnit);

            bool isReservedByAlly = false;
            if (reservingOccupant != null && !ReferenceEquals(reservingOccupant, movingUnit))
            {
                Unit reserverAsUnit = reservingOccupant as Unit;
                isReservedByAlly = reserverAsUnit != null && !movingUnit.IsHostileTo(reserverAsUnit);
            }

            if (isEnemyBlocker)
            {
                Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByEnemy: {desiredCell} blocker={blockerAsUnit?.name}");

                if (grid.TryFindAttackPositionFromBlockedDesiredCell(desiredCell, targetCell, originCell, rangeInCells, movingUnit, targetUnit, out Vector3Int attackPosition))
                {
                    if (attackPosition == originCell)
                    {
                        Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByEnemy_AlreadyInRange: can attack from current position");
                        resolvedCell = originCell;
                        return true;
                    }

                    Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByEnemy_AttackFromNearestValid: {attackPosition}");
                    resolvedCell = attackPosition;
                    return true;
                }

                Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByEnemy_NoValidAttackPosition");
                return false;
            }

            if (isAllyBlocker || isReservedByAlly)
            {
                string blockType = isAllyBlocker ? "Occupied" : "Reserved";
                string blockerName = isAllyBlocker ? blockerAsUnit?.name : (reservingOccupant as Unit)?.name;
                Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByAlly_{blockType}: {desiredCell} blocker={blockerName}");

                if (grid.TryFindNearbyAlternativeCell(desiredCell, targetCell, originCell, rangeInCells, movingUnit, out Vector3Int alternativeCell))
                {
                    Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByAlly_RepositionNearby: {alternativeCell}");
                    resolvedCell = alternativeCell;
                    return true;
                }

                Debug.Log($"[UnitMovement] {debugName} - DesiredCellBlockedByAlly_NoNearbyCellFound");
                return false;
            }
        }

        return true;
    }

    private StepSelectionResult GetNextStepTowards(RoomGrid grid, Unit movingUnit, string debugName, Vector3Int originCell)
    {
        if (grid == null || movingUnit == null)
            return StepSelectionResult.NoStep(originCell);

        if (_cachedPath.Count > 1 && _cachedPath[0] == originCell)
        {
            Vector3Int cachedNextStep = _cachedPath[1];
            if (!grid.OccupancyService.IsCellBlockedFor(movingUnit, cachedNextStep))
            {
                if (grid.IsStepAllowed(originCell, cachedNextStep, movingUnit))
                    return StepSelectionResult.FromPath(cachedNextStep);
            }

            Debug.Log($"[UnitMovement] {debugName} - CachedStepInvalidated: {cachedNextStep}");
        }

        Vector3Int targetReference = _cachedTargetCell != Vector3Int.zero
            ? _cachedTargetCell
            : (_cachedPath.Count > 2 ? _cachedPath[_cachedPath.Count - 1] : originCell);

        int originDistance = GridNavigationUtility.GetCellDistance(originCell, targetReference);
        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistance;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        List<Vector3Int> neighbors = grid.GetNeighbors(originCell, movingUnit);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];

            if (grid.OccupancyService.IsCellBlockedFor(movingUnit, candidate))
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
            return StepSelectionResult.FromFallback(bestImprovingStep);

        if (bestFallbackStep != originCell)
            return StepSelectionResult.FromFallback(bestFallbackStep);

        return StepSelectionResult.NoStep(originCell);
    }

    private StepSelectionResult GetNextStepAway(RoomGrid grid, Unit movingUnit, Vector3Int originCell, Vector3Int targetCell, int desiredDistance)
    {
        if (grid == null || movingUnit == null)
            return StepSelectionResult.NoStep(originCell);

        int currentDistance = GridNavigationUtility.GetCellDistance(originCell, targetCell);
        Vector3Int bestCandidate = originCell;
        int bestDistanceGap = int.MaxValue;
        int bestCandidateDistance = currentDistance;
        Vector3Int fallbackCandidate = originCell;
        int fallbackDistance = currentDistance;

        List<Vector3Int> neighbors = grid.GetNeighbors(originCell, movingUnit);
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
            return StepSelectionResult.FromPath(bestCandidate);

        if (fallbackCandidate != originCell)
            return StepSelectionResult.FromFallback(fallbackCandidate);

        return StepSelectionResult.NoStep(originCell);
    }

    private bool RefreshPathCache(
        RoomGrid grid,
        Unit movingUnit,
        string debugName,
        Vector3Int originCell,
        Vector3Int targetCell,
        Vector3Int desiredCell,
        Unit targetUnit,
        int rangeInCells)
    {
        bool targetChanged = _cachedTargetUnit != targetUnit;
        bool targetCellChanged = !_hasCachedTargetCell || _cachedTargetCell != targetCell;
        bool targetRangeChanged = _cachedTargetRange != rangeInCells;

        bool pathStillValid = IsCachedPathStillValid(grid, movingUnit, originCell);

        string invalidationReason = null;
        if (pathStillValid && _cachedPath.Count > 1)
        {
            Vector3Int nextStep = _cachedPath[1];
            if (grid.OccupancyService.IsOccupied(nextStep, movingUnit))
            {
                invalidationReason = "NextStepOccupied";
                pathStillValid = false;
            }
            else if (grid.OccupancyService.IsCellReserved(nextStep, movingUnit))
            {
                invalidationReason = "NextStepReserved";
                pathStillValid = false;
            }
        }

        bool shouldRepath = targetChanged ||
                            targetCellChanged ||
                            targetRangeChanged ||
                            !pathStillValid ||
                            Time.time >= _nextRepathTime;

        if (!shouldRepath)
            return false;

        if (invalidationReason != null)
            Debug.Log($"[UnitMovement] {debugName} - PathInvalidated: {invalidationReason}");

        _cachedTargetUnit = targetUnit;
        _cachedTargetCell = targetCell;
        _hasCachedTargetCell = true;
        _cachedTargetRange = rangeInCells;
        _nextRepathTime = Time.time + _repathInterval;

        _cachedPath.Clear();
        _cachedPath.AddRange(FindPath(grid, movingUnit, originCell, desiredCell));

        if (_cachedPath.Count > 0)
            Debug.Log($"[UnitMovement] {debugName} - PathRecalculated: {_cachedPath.Count} steps from {originCell} to {desiredCell}");

        return true;
    }

    private bool IsCachedPathStillValid(RoomGrid grid, Unit movingUnit, Vector3Int originCell)
    {
        if (grid == null || movingUnit == null)
            return false;

        if (_cachedPath.Count <= 1)
            return false;

        if (_cachedPath[0] != originCell)
            return false;

        Vector3Int previousCell = _cachedPath[0];
        for (int i = 1; i < _cachedPath.Count; i++)
        {
            Vector3Int currentCell = _cachedPath[i];
            if (!grid.IsStepAllowed(previousCell, currentCell, movingUnit))
                return false;

            previousCell = currentCell;
        }

        return true;
    }

    private static List<Vector3Int> FindPath(RoomGrid grid, Unit movingUnit, Vector3Int startCell, Vector3Int targetCell)
    {
        if (grid == null || movingUnit == null)
            return new List<Vector3Int>();

        return GridPathfinder.FindPath(grid, startCell, targetCell, movingUnit);
    }

    private readonly struct StepSelectionResult
    {
        public StepSelectionResult(Vector3Int step, bool usedFallback)
        {
            Step = step;
            UsedFallback = usedFallback;
        }

        public Vector3Int Step { get; }
        public bool UsedFallback { get; }

        public static StepSelectionResult FromPath(Vector3Int step)
        {
            return new StepSelectionResult(step, usedFallback: false);
        }

        public static StepSelectionResult FromFallback(Vector3Int step)
        {
            return new StepSelectionResult(step, usedFallback: true);
        }

        public static StepSelectionResult NoStep(Vector3Int originCell)
        {
            return new StepSelectionResult(originCell, usedFallback: false);
        }
    }
}
