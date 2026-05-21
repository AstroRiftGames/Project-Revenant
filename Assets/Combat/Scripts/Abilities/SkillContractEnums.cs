public enum PrimaryTargetRequirement
{
    None,
    Hostile,
    Ally,
    Self,
    GroundCell
}

public enum ImpactTargetRequirement
{
    Hostile,
    Ally,
    Self,
    Any
}

public enum TargetSelectionMode
{
    None,
    Closest,
    LowestHealth,
    HighestBasicDamage,
    AllyLowestHealth,
    AllyRolePriority,
    RoleBasedOffensive
}

public enum TargetFallbackMode
{
    Cancel,
    Retarget,
    ContinueFromCurrentContext
}

public enum SkillTrajectory
{
    Hitscan,
    Projectile
}

public enum ImpactPattern
{
    Direct,
    Area,
    Line,
    MultiTarget
}

public enum SkillExecutionMode
{
    Instant,
    CastTime,
    Channel
}
