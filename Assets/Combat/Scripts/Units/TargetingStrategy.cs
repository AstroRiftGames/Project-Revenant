using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class TargetingStrategy : MonoBehaviour
{
    public Unit SelectTarget(Unit self, IBasicAction action, Unit currentTarget)
    {
        if (self == null)
            return null;

        if (self.StatusEffects != null && self.StatusEffects.TryGetForcedTarget(out Unit forcedTarget))
            return forcedTarget;

        RequiredTargetRelationship targetRelationship = action != null
            ? action.RequiredTargetRelationship
            : RequiredTargetRelationship.Hostile;

        if (targetRelationship == RequiredTargetRelationship.Ally)
            return SelectAllyTarget(self, currentTarget);

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => SelectDynamicTarget(self, currentTarget),
            _ => SelectRolePriorityTarget(self, currentTarget)
        };
    }

    public Unit SelectAllyTarget(Unit self, Unit currentTarget)
    {
        if (self == null)
            return null;

        if (IsTargetStillValid(self, currentTarget, RequiredTargetRelationship.Ally) && currentTarget.CurrentHealth < currentTarget.MaxHealth)
            return currentTarget;

        List<Unit> allies = GetAlliesInScene(self);
        if (allies.Count == 0)
            return null;

        return GetLowestHealthAliveAlly(allies);
    }

    private List<Unit> GetAlliesInScene(Unit self)
    {
        return self != null ? self.GetAlliedUnitsInScene() : new List<Unit>();
    }

    private Unit GetLowestHealthAliveAlly(List<Unit> allies)
    {
        Unit lowest = null;
        float lowestHealthRatio = float.MaxValue;

        for (int i = 0; i < allies.Count; i++)
        {
            Unit ally = allies[i];
            float healthRatio = (float)ally.CurrentHealth / ally.MaxHealth;

            if (healthRatio < lowestHealthRatio)
            {
                lowestHealthRatio = healthRatio;
                lowest = ally;
            }
        }

        return lowest;
    }

public Unit GetSpacingThreat(Unit self, Unit currentTarget)
    {
        if (self == null || !self.WantsToHoldSpacing)
            return null;

        if (self.StatusEffects != null && self.StatusEffects.TryGetForcedTarget(out Unit tauntTarget))
        {
            if (ReferenceEquals(currentTarget, tauntTarget))
                return null;
        }

        if (self.Role == UnitRole.Support)
            return GetNearestVisibleHostileInternal(self);

        if (IsTargetStillValid(self, currentTarget, RequiredTargetRelationship.Hostile))
            return currentTarget;

        return GetNearestVisibleHostileInternal(self);
    }

    public Unit GetNearestVisibleHostile(Unit self)
    {
        return GetNearestVisibleHostileInternal(self);
    }

    private Unit SelectDynamicTarget(Unit self, Unit currentTarget)
    {
        List<Unit> aggressors = self.GetAliveAggressors();
        if (aggressors.Count == 1)
        {
            Unit loneAggressor = aggressors[0];
            if (IsTargetStillValid(self, loneAggressor, RequiredTargetRelationship.Hostile))
                return loneAggressor;
        }
        else if (aggressors.Count > 1)
        {
            Unit weakestAggressor = GetLowestHealthUnit(self, aggressors);
            if (IsTargetStillValid(self, weakestAggressor, RequiredTargetRelationship.Hostile))
                return weakestAggressor;
        }

        if (IsTargetStillValid(self, currentTarget, RequiredTargetRelationship.Hostile))
            return currentTarget;

        return GetNearestVisibleHostileInternal(self);
    }

    private Unit SelectRolePriorityTarget(Unit self, Unit currentTarget)
    {
        List<Unit> hostiles = self.GetHostileUnitsInScene();
        if (hostiles == null || hostiles.Count == 0)
            return null;

        UnitRole? highestPriorityRole = GetHighestPriorityAvailableRole(hostiles);
        if (!highestPriorityRole.HasValue)
            return null;

        if (IsTargetStillValid(self, currentTarget, RequiredTargetRelationship.Hostile) && currentTarget.Role == highestPriorityRole.Value)
            return currentTarget;

        return GetClosestUnitByRole(self, hostiles, highestPriorityRole.Value);
    }

    private bool IsTargetStillValid(Unit self, Unit target)
    {
        return IsTargetStillValid(self, target, RequiredTargetRelationship.Hostile);
    }

    private bool IsTargetStillValid(Unit self, Unit target, RequiredTargetRelationship relationship)
    {
        return UnitTargetValidator.IsTargetSelectableForRelationship(self, target, relationship);
    }

    private static Unit GetClosestUnit(Unit self, List<Unit> units)
    {
        if (self == null || units == null || units.Count == 0)
            return null;

        Unit closest = null;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            Unit candidate = units[i];
            if (candidate == null || !candidate.IsAlive)
                continue;

            float sqrDistance = (candidate.Position - self.Position).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance)
                continue;

            closest = candidate;
            bestSqrDistance = sqrDistance;
        }

        return closest;
    }

    private static Unit GetNearestVisibleHostileInternal(Unit self)
    {
        if (self == null)
            return null;

        List<Unit> hostiles = self.GetHostileUnitsInScene();
        if (hostiles == null || hostiles.Count == 0)
            return null;

        Unit nearest = null;
        float bestSqrDistance = float.MaxValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < hostiles.Count; i++)
        {
            Unit candidate = hostiles[i];
            if (candidate == null || !candidate.IsAlive)
                continue;

            float sqrDistance = (candidate.Position - self.Position).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            if (Mathf.Approximately(sqrDistance, bestSqrDistance) && candidate.CurrentHealth >= bestHealth)
                continue;

            nearest = candidate;
            bestSqrDistance = sqrDistance;
            bestHealth = candidate.CurrentHealth;
        }

        return nearest;
    }

    private static Unit GetLowestHealthUnit(Unit self, List<Unit> units)
    {
        if (self == null || units == null || units.Count == 0)
            return null;

        Unit weakest = null;
        int lowestHealth = int.MaxValue;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < units.Count; i++)
        {
            Unit candidate = units[i];
            if (candidate == null || !candidate.IsAlive)
                continue;

            if (candidate.CurrentHealth > lowestHealth)
                continue;

            float sqrDistance = (candidate.Position - self.Position).sqrMagnitude;
            if (candidate.CurrentHealth == lowestHealth && sqrDistance >= bestSqrDistance)
                continue;

            weakest = candidate;
            lowestHealth = candidate.CurrentHealth;
            bestSqrDistance = sqrDistance;
        }

        return weakest;
    }

    private static Unit GetClosestUnitByRole(Unit self, List<Unit> units, UnitRole role)
    {
        if (self == null || units == null || units.Count == 0)
            return null;

        List<Unit> candidates = new();
        for (int i = 0; i < units.Count; i++)
        {
            Unit candidate = units[i];
            if (candidate == null || !candidate.IsAlive || candidate.Role != role)
                continue;

            candidates.Add(candidate);
        }

        return GetClosestUnit(self, candidates);
    }

    private static UnitRole? GetHighestPriorityAvailableRole(List<Unit> units)
    {
        if (units == null || units.Count == 0)
            return null;

        if (HasAliveUnitWithRole(units, UnitRole.Tank))
            return UnitRole.Tank;

        if (HasAliveUnitWithRole(units, UnitRole.DPS))
            return UnitRole.DPS;

        if (HasAliveUnitWithRole(units, UnitRole.Support))
            return UnitRole.Support;

        return null;
    }

    private static bool HasAliveUnitWithRole(List<Unit> units, UnitRole role)
    {
        if (units == null)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            Unit candidate = units[i];
            if (candidate == null || !candidate.IsAlive || candidate.Role != role)
                continue;

            return true;
        }

        return false;
    }
}

public enum RequiredTargetRelationship
{
    Any,
    Hostile,
    Ally
}
