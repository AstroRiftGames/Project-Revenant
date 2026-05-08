using UnityEngine;

public static class UnitTargetValidator
{
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
