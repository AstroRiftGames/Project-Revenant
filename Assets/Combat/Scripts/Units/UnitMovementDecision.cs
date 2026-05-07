using UnityEngine;

public enum UnitMovementPlanReason
{
    None,
    InvalidRequest,
    NoDesiredCell,
    DesiredCellBlocked,
    NoStep,
    MoveTowardsTarget,
    MoveAwayFromTarget
}

public readonly struct UnitMovementDecision
{
    public UnitMovementDecision(
        bool hasMove,
        Vector3Int desiredCell,
        Vector3Int nextStepCell,
        UnitMovementPlanReason reason,
        bool pathChanged = false,
        bool usedFallback = false)
    {
        HasMove = hasMove;
        DesiredCell = desiredCell;
        NextStepCell = nextStepCell;
        Reason = reason;
        PathChanged = pathChanged;
        UsedFallback = usedFallback;
    }

    public bool HasMove { get; }
    public Vector3Int DesiredCell { get; }
    public Vector3Int NextStepCell { get; }
    public UnitMovementPlanReason Reason { get; }
    public bool PathChanged { get; }
    public bool UsedFallback { get; }

    public static UnitMovementDecision NoMove(
        UnitMovementPlanReason reason,
        Vector3Int desiredCell,
        Vector3Int nextStepCell,
        bool pathChanged = false,
        bool usedFallback = false)
    {
        return new UnitMovementDecision(
            hasMove: false,
            desiredCell: desiredCell,
            nextStepCell: nextStepCell,
            reason: reason,
            pathChanged: pathChanged,
            usedFallback: usedFallback);
    }

    public static UnitMovementDecision Move(
        Vector3Int desiredCell,
        Vector3Int nextStepCell,
        UnitMovementPlanReason reason,
        bool pathChanged = false,
        bool usedFallback = false)
    {
        return new UnitMovementDecision(
            hasMove: true,
            desiredCell: desiredCell,
            nextStepCell: nextStepCell,
            reason: reason,
            pathChanged: pathChanged,
            usedFallback: usedFallback);
    }
}
