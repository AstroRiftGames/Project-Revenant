using UnityEngine;

public sealed class SkillContext
{
    public SkillContext(
        Unit caster,
        SkillData skill,
        Unit primaryTarget,
        Vector2Int targetCell,
        bool hasTargetCell,
        Vector3 impactCenterWorld,
        Unit impactCenterUnit,
        RoomContext roomContext,
        RoomGrid roomGrid)
    {
        Caster = caster;
        Skill = skill;
        PrimaryTarget = primaryTarget;
        TargetCell = targetCell;
        HasTargetCell = hasTargetCell;
        ImpactCenterWorld = impactCenterWorld;
        ImpactCenterUnit = impactCenterUnit;
        RoomContext = roomContext;
        RoomGrid = roomGrid;
    }

    public Unit Caster { get; }
    public SkillData Skill { get; }
    public Unit PrimaryTarget { get; }
    public bool HasPrimaryTarget => PrimaryTarget != null;
    public Vector2Int TargetCell { get; }
    public bool HasTargetCell { get; }
    public Vector3 ImpactCenterWorld { get; }
    public Unit ImpactCenterUnit { get; }
    public bool HasImpactCenterUnit => ImpactCenterUnit != null;
    public RoomContext RoomContext { get; }
    public RoomGrid RoomGrid { get; }
}
