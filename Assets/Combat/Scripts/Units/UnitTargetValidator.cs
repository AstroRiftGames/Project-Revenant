using UnityEngine;

public static class UnitTargetValidator
{
    #region Policy Entry Points

    public static bool IsBasicActionTargetSelectable(Unit source, Unit target, TargetRelation relationship, bool requiresInjuredTarget = false)
    {
        return IsTargetSelectable(
            source,
            target,
            TargetingPolicy.ForBasicAction(relationship, requiresInjuredTarget));
    }

    // Validates the primary unit target selected for a skill.
    public static bool IsSkillTargetSelectable(Unit source, Unit target, SkillData skill)
    {
        if (!TargetingPolicy.TryCreateForSkill(skill, out TargetingPolicy policy))
            return false;

        return IsTargetSelectable(source, target, policy);
    }

    #endregion

    #region Policy Application

    public static bool IsTargetSelectable(Unit source, Unit target, in TargetingPolicy policy)
    {
        if (!IsValidSelectingUnit(source))
            return false;

        if (target == null)
            return !policy.RequiresTarget;

        if (!IsValidTargetLifecycle(target))
            return false;

        bool isSelfTarget = ReferenceEquals(source, target);
        if (policy.RequireSelf && !isSelfTarget)
            return false;

        if (!policy.AllowSelf && isSelfTarget)
            return false;

        if (policy.RequireActive && !target.gameObject.activeInHierarchy)
            return false;

        if (!target.IsAlive)
            return false;

        if (policy.RequireSameRoom && !AreUnitsInSameResolvedRoom(source, target))
            return false;

        if (policy.RequireDetectable && !isSelfTarget && !source.CanDetect(target))
            return false;



        if (policy.RequireInjured && target.CurrentHealth >= target.MaxHealth)
            return false;

        return policy.Relationship switch
        {
            TargetRelation.Hostile => source.IsHostileTo(target),
            TargetRelation.Ally => !source.IsHostileTo(target),
            _ => true
        };
    }

    #endregion

    #region Range Validation

    public static bool IsSkillTargetInRange(Unit source, Unit target, SkillData skill)
    {
        if (source == null || skill == null)
            return false;

        PrimaryTargetRequirement primaryTargetRequirement = skill.PrimaryTargetRequirement;
        if (target == null)
        {
            return primaryTargetRequirement == PrimaryTargetRequirement.None ||
                   primaryTargetRequirement == PrimaryTargetRequirement.GroundCell;
        }

        if (primaryTargetRequirement == PrimaryTargetRequirement.Self)
            return true;

        return IsTargetInRange(source, target, skill.RangeInCells);
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

    #endregion

    #region Private Helpers

    private static bool IsValidSelectingUnit(Unit source)
    {
        if (source == null)
            return false;

        if (!source.IsAlive)
            return false;

        if (!TryGetLifecycleState(source, out UnitLifecycleState lifecycleState))
            return false;

        return lifecycleState == UnitLifecycleState.Alive;
    }

    private static bool IsValidTargetLifecycle(Unit target)
    {
        if (!TryGetLifecycleState(target, out UnitLifecycleState lifecycleState))
            return false;

        return lifecycleState == UnitLifecycleState.Alive;
    }

    private static bool TryGetLifecycleState(Unit unit, out UnitLifecycleState lifecycleState)
    {
        lifecycleState = UnitLifecycleState.Dead;
        if (unit == null)
            return false;

        RecruitableUnitState recruitableState = unit.GetComponent<RecruitableUnitState>();
        if (recruitableState == null)
            return false;

        lifecycleState = recruitableState.CurrentState;
        return true;
    }

    private static bool AreUnitsInSameResolvedRoom(Unit source, Unit target)
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

    #endregion
}
