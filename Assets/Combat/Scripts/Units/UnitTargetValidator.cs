using UnityEngine;

public static class UnitTargetValidator
{
    // Legacy convenience wrapper. Prefer IsTargetSelectable(source, target, policy)
    // in new code so relationship/self/invisibility rules stay centralized in TargetingPolicy.
    public static bool IsTargetSelectableForRelationship(Unit source, Unit target, RequiredTargetRelationship relationship)
    {
        return IsTargetSelectable(source, target, TargetingPolicy.ForRelationship(relationship, allowSelf: relationship == RequiredTargetRelationship.Any));
    }

    // Legacy convenience wrapper for basic actions. Prefer TargetingPolicy.ForBasicAction(...)
    // plus IsTargetSelectable(source, target, policy) in new code.
    public static bool IsTargetSelectableForBasicAction(Unit source, Unit target, RequiredTargetRelationship relationship)
    {
        return IsTargetSelectable(source, target, TargetingPolicy.ForBasicAction(relationship));
    }

    // Legacy compatibility wrapper that adapts SkillRequirements into the new policy-based path.
    // New skill targeting flows should build a TargetingPolicy explicitly and only query
    // AreSkillSpecificRequirementsMet for requirements beyond general target eligibility.
    public static bool IsTargetSelectableForSkill(Unit source, Unit target, SkillRequirements requirements)
    {
        if (source == null)
            return false;

        if (!TargetingPolicy.TryCreateForSkill(requirements, out TargetingPolicy policy))
            return false;

        if (!IsTargetSelectable(source, target, policy))
            return false;

        return requirements == null || requirements.AreSkillSpecificRequirementsMet(source, target);
    }

    // Legacy overload kept for existing callers that still pass relationship/self/invisibility
    // as loose parameters. Prefer the policy-based overload in new code.
    public static bool IsTargetSelectable(Unit source, Unit target, RequiredTargetRelationship relationship, bool allowInvisible, bool excludeSelf = true)
    {
        return IsTargetSelectable(
            source,
            target,
            new TargetingPolicy(
                relationship,
                allowSelf: !excludeSelf,
                allowInvisible: allowInvisible));
    }

    public static bool IsTargetSelectable(Unit source, Unit target, in TargetingPolicy policy)
    {
        if (source == null)
            return false;

        if (target == null)
            return !policy.RequiresTarget;

        bool isSelfTarget = ReferenceEquals(source, target);
        if (policy.RequireSelf && !isSelfTarget)
            return false;

        if (!policy.AllowSelf && isSelfTarget)
            return false;

        if (policy.RequireActive && !target.gameObject.activeInHierarchy)
            return false;

        if (!policy.AllowDead && !target.IsAlive)
            return false;

        if (policy.RequireSameRoom && !IsInSameResolvedRoom(source, target))
            return false;

        if (policy.RequireDetectable && !isSelfTarget && !source.CanDetect(target))
            return false;

        if (!policy.AllowInvisible && target.StatusEffects != null && target.StatusEffects.HasInvisibility)
            return false;

        if (policy.RequireInjured && target.CurrentHealth >= target.MaxHealth)
            return false;

        return policy.Relationship switch
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

    // Legacy helper kept for compatibility with older code that still maps SkillRequirements
    // through RequiredTargetRelationship. The main runtime targeting flow should resolve
    // TargetingPolicy directly instead of using this mapping.
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

    // Legacy compatibility helper. The main runtime targeting flow should consume
    // SkillRequirements through TargetingPolicy factories or SkillRequirements.TargetRequirement.
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
