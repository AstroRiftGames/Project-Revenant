using UnityEngine;

public static class UnitTargetValidator
{
    public static bool IsTargetSelectableForRelationship(Unit source, Unit target, RequiredTargetRelationship relationship)
    {
        return IsTargetSelectable(source, target, relationship, allowInvisible: false, excludeSelf: relationship != RequiredTargetRelationship.Any);
    }

    public static bool IsTargetSelectableForBasicAction(Unit source, Unit target, RequiredTargetRelationship relationship)
    {
        return IsTargetSelectableForRelationship(source, target, relationship);
    }

    public static bool IsTargetSelectableForSkill(Unit source, Unit target, SkillRequirements requirements)
    {
        if (source == null)
            return false;

        if (requirements != null && !requirements.requiresTarget && target == null)
            return true;

        SkillTargetRequirement targetRequirement = ResolveSkillTargetRequirement(requirements);
        if (targetRequirement == SkillTargetRequirement.GroundCell || targetRequirement == SkillTargetRequirement.NoTarget)
            return false;

        RequiredTargetRelationship relationship = ResolveSkillRelationship(requirements);
        bool excludeSelf = targetRequirement != SkillTargetRequirement.Any && targetRequirement != SkillTargetRequirement.Self;
        if (!IsTargetSelectable(source, target, relationship, allowInvisible: false, excludeSelf: excludeSelf))
            return false;

        return requirements == null || requirements.AreMet(source, target);
    }

    public static bool IsTargetSelectable(Unit source, Unit target, RequiredTargetRelationship relationship, bool allowInvisible, bool excludeSelf = true)
    {
        if (source == null || target == null)
            return false;

        if (excludeSelf && ReferenceEquals(source, target))
            return false;

        if (!target.gameObject.activeInHierarchy || !target.IsAlive)
            return false;

        if (!IsInSameResolvedRoom(source, target))
            return false;

        if (!source.CanDetect(target))
            return false;

        if (!allowInvisible && target.StatusEffects != null && target.StatusEffects.HasInvisibility)
            return false;

        return relationship switch
        {
            RequiredTargetRelationship.Hostile => source.IsHostileTo(target),
            RequiredTargetRelationship.Ally => !source.IsHostileTo(target),
            _ => true
        };
    }

    public static bool IsTargetInRange(Unit source, Unit target, int rangeInCells)
    {
        if (source == null || target == null)
            return false;

        RoomGrid grid = source.RoomContext != null ? source.RoomContext.RoomGrid : null;
        if (grid == null)
        {
            float distance = Vector3.Distance(source.Position, target.Position);
            return distance <= Mathf.Max(0f, rangeInCells);
        }

        Vector3Int selfCell = ResolveUnitCell(grid, source);
        Vector3Int targetCell = ResolveUnitCell(grid, target);
        return GridNavigationUtility.IsWithinCellRange(selfCell, targetCell, rangeInCells);
    }

    public static RequiredTargetRelationship ResolveSkillRelationship(SkillRequirements requirements)
    {
        if (requirements == null || !requirements.requiresTarget)
            return RequiredTargetRelationship.Any;

        return ResolveSkillTargetRequirement(requirements) switch
        {
            SkillTargetRequirement.Hostile => RequiredTargetRelationship.Hostile,
            SkillTargetRequirement.Ally => RequiredTargetRelationship.Ally,
            SkillTargetRequirement.Self => RequiredTargetRelationship.Any,
            SkillTargetRequirement.Any => RequiredTargetRelationship.Any,
            _ => RequiredTargetRelationship.Any
        };
    }

    public static SkillTargetRequirement ResolveSkillTargetRequirement(SkillRequirements requirements)
    {
        return requirements != null
            ? requirements.ResolveTargetRequirement()
            : SkillTargetRequirement.Any;
    }

    private static bool IsInSameResolvedRoom(Unit source, Unit target)
    {
        RoomContext sourceRoom = source != null ? source.RoomContext : null;
        RoomContext targetRoom = target != null ? target.RoomContext : null;

        if (sourceRoom == null && targetRoom == null)
            return true;

        return ReferenceEquals(sourceRoom, targetRoom);
    }

    private static Vector3Int ResolveUnitCell(RoomGrid grid, Unit unit)
    {
        return GridUnitCellUtility.ResolveUnitCell(grid, unit);
    }
}
