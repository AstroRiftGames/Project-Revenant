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
    private Vector3Int _cachedDesiredAttackCell;
    private bool _hasCachedDesiredAttackCell;

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
        _cachedDesiredAttackCell = Vector3Int.zero;
        _hasCachedDesiredAttackCell = false;
        _cachedPath.Clear();
    }

    public UnitMovementDecision PlanTowards(
        RoomGrid grid,
        Unit movingUnit,
        Vector3Int originCell,
        Unit targetUnit,
        int rangeInCells,
        string debugName,
        bool debugLogs = false)
    {
        if (grid == null || movingUnit == null || !IsCombatAliveTarget(targetUnit))
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.InvalidRequest, originCell, originCell);

        Vector3Int targetCell = grid.WorldToCell(targetUnit.Position);
        Vector3Int desiredAttackCell = targetCell;
        int resolvedRange = Mathf.Max(0, rangeInCells);

        if (!grid.TryFindWalkableCellInRange(targetCell, originCell, resolvedRange, movingUnit, out desiredAttackCell))
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.NoDesiredAttackCell, desiredAttackCell, originCell);

        if (!TryResolveBlockedDesiredAttackCell(
                grid,
                movingUnit,
                debugName,
                desiredAttackCell,
                targetCell,
                originCell,
                resolvedRange,
                targetUnit,
                debugLogs,
                out Vector3Int resolvedCell))
        {
            return UnitMovementDecision.NoMove(UnitMovementPlanReason.DesiredAttackCellBlocked, desiredAttackCell, originCell);
        }

        if (resolvedCell != desiredAttackCell)
        {
            desiredAttackCell = resolvedCell;
            LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellResolved: original={targetCell} resolved={desiredAttackCell}");
        }

        bool pathChanged = RefreshPathCache(
            grid,
            movingUnit,
            debugName,
            originCell,
            targetCell,
            desiredAttackCell,
            targetUnit,
            resolvedRange,
            debugLogs);

        StepSelectionResult nextStep = GetNextStepTowards(grid, movingUnit, debugName, originCell, debugLogs);
        if (nextStep.Step == originCell)
        {
            return UnitMovementDecision.NoMove(
                UnitMovementPlanReason.NoStep,
                desiredAttackCell,
                nextStep.Step,
                pathChanged,
                nextStep.UsedFallback);
        }

        return UnitMovementDecision.Move(
            desiredAttackCell,
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
        if (grid == null || movingUnit == null || !IsCombatAliveTarget(targetUnit))
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

    private bool TryResolveBlockedDesiredAttackCell(
        RoomGrid grid,
        Unit movingUnit,
        string debugName,
        Vector3Int desiredAttackCell,
        Vector3Int targetCell,
        Vector3Int originCell,
        int rangeInCells,
        Unit targetUnit,
        bool debugLogs,
        out Vector3Int resolvedCell)
    {
        resolvedCell = desiredAttackCell;

        if (grid.OccupancyService.IsCellBlockedFor(movingUnit, desiredAttackCell))
        {
            IGridOccupant blockingOccupant = grid.OccupancyService.GetBlockingOccupant(desiredAttackCell, movingUnit);
            IGridOccupant reservingOccupant = grid.OccupancyService.GetReservingOccupant(desiredAttackCell);

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
                LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByEnemy: {desiredAttackCell} blocker={blockerAsUnit?.name}");

                if (grid.TryFindAttackPositionFromBlockedDesiredCell(desiredAttackCell, targetCell, originCell, rangeInCells, movingUnit, targetUnit, out Vector3Int attackPosition))
                {
                    if (attackPosition == originCell)
                    {
                        LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByEnemy_AlreadyInRange: can attack from current position");
                        resolvedCell = originCell;
                        return true;
                    }

                    LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByEnemy_AttackFromNearestValid: {attackPosition}");
                    resolvedCell = attackPosition;
                    return true;
                }

                LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByEnemy_NoValidAttackPosition");
                return false;
            }

            if (isAllyBlocker || isReservedByAlly)
            {
                string blockType = isAllyBlocker ? "Occupied" : "Reserved";
                string blockerName = isAllyBlocker ? blockerAsUnit?.name : (reservingOccupant as Unit)?.name;
                LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByAlly_{blockType}: {desiredAttackCell} blocker={blockerName}");

                if (grid.TryFindNearbyAlternativeCell(desiredAttackCell, targetCell, originCell, rangeInCells, movingUnit, out Vector3Int alternativeCell))
                {
                    LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByAlly_RepositionNearby: {alternativeCell}");
                    resolvedCell = alternativeCell;
                    return true;
                }

                LogDebug(debugLogs, $"[UnitMovement] {debugName} - DesiredAttackCellBlockedByAlly_NoNearbyCellFound");
                return false;
            }
        }

        return true;
    }

    private StepSelectionResult GetNextStepTowards(RoomGrid grid, Unit movingUnit, string debugName, Vector3Int originCell, bool debugLogs)
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

            LogDebug(debugLogs, $"[UnitMovement] {debugName} - CachedStepInvalidated: {cachedNextStep}");
        }

        Vector3Int tacticalReference = _hasCachedDesiredAttackCell
            ? _cachedDesiredAttackCell
            : (_cachedPath.Count > 2 ? _cachedPath[_cachedPath.Count - 1] : originCell);

        Vector3Int targetReference = _hasCachedTargetCell
            ? _cachedTargetCell
            : (_cachedPath.Count > 2 ? _cachedPath[_cachedPath.Count - 1] : originCell);

        int originDistanceToTactical = GridNavigationUtility.GetCellDistance(originCell, tacticalReference);
        int originDistanceToTarget = GridNavigationUtility.GetCellDistance(originCell, targetReference);

        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistanceToTactical;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        List<Vector3Int> neighbors = grid.GetNeighbors(originCell, movingUnit);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];

            if (grid.OccupancyService.IsCellBlockedFor(movingUnit, candidate))
                continue;

            int candidateDistanceToTactical = GridNavigationUtility.GetCellDistance(candidate, tacticalReference);

            if (candidateDistanceToTactical < bestImprovingDistance)
            {
                bestImprovingStep = candidate;
                bestImprovingDistance = candidateDistanceToTactical;
            }

            if (candidateDistanceToTactical <= originDistanceToTactical && candidateDistanceToTactical < bestFallbackDistance)
            {
                bestFallbackStep = candidate;
                bestFallbackDistance = candidateDistanceToTactical;
            }
        }

        if (bestImprovingStep == originCell && bestFallbackStep == originCell && targetReference != tacticalReference)
        {
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Int candidate = neighbors[i];

                if (grid.OccupancyService.IsCellBlockedFor(movingUnit, candidate))
                    continue;

                int candidateDistanceToTarget = GridNavigationUtility.GetCellDistance(candidate, targetReference);

                if (candidateDistanceToTarget < originDistanceToTarget && candidateDistanceToTarget < bestFallbackDistance)
                {
                    bestFallbackStep = candidate;
                    bestFallbackDistance = candidateDistanceToTarget;
                }
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
        Vector3Int desiredAttackCell,
        Unit targetUnit,
        int rangeInCells,
        bool debugLogs)
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
            LogDebug(debugLogs, $"[UnitMovement] {debugName} - PathInvalidated: {invalidationReason}");

        _cachedTargetUnit = targetUnit;
        _cachedTargetCell = targetCell;
        _hasCachedTargetCell = true;
        _cachedTargetRange = rangeInCells;
        _cachedDesiredAttackCell = desiredAttackCell;
        _hasCachedDesiredAttackCell = true;
        _nextRepathTime = Time.time + _repathInterval;

        _cachedPath.Clear();
        _cachedPath.AddRange(FindPath(grid, movingUnit, originCell, desiredAttackCell));

        if (_cachedPath.Count > 0)
            LogDebug(debugLogs, $"[UnitMovement] {debugName} - PathRecalculated: {_cachedPath.Count} steps from {originCell} to {desiredAttackCell}");

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

    private static bool IsCombatAliveTarget(Unit targetUnit)
    {
        return targetUnit != null &&
               targetUnit.IsAlive &&
               targetUnit.LifecycleState == UnitLifecycleState.Alive;
    }

    private static void LogDebug(bool debugLogs, string message)
    {
        if (debugLogs)
            Debug.Log(message);
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
