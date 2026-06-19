public enum SkillUseFailureReason
{
    None,
    NoSkillAssigned,
    TemporaryCombatUnit,
    OwnerNotAlive,
    OwnerNotReady,
    SkillNotReady,
    AlreadyCasting,
    BlockedByStatus,
    InvalidTarget,
    InvalidTargetRelation,
    TargetDead,
    TargetOutOfRoom,
    OutOfRange,
    InvalidGroundCell,
    GroundCellBlocked,
    MissingRoomContext,
    MissingRoomGrid,
    ContextInvalid,
    NoImpacts,
    EffectApplicationFailed,
    Interrupted,
    CombatEnded,
    Unknown
}

public struct SkillUseResult
{
    public bool Success { get; }
    public SkillUseFailureReason FailureReason { get; }
    public string Detail { get; }

    public SkillUseResult(bool success, SkillUseFailureReason failureReason, string detail)
    {
        Success = success;
        FailureReason = failureReason;
        Detail = detail;
    }
}
