using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class TargetingStrategy : MonoBehaviour
{
    public Unit SelectTarget(Unit self, IBasicAction action, Unit currentTarget)
    {
        if (self == null)
            return null;

        TargetingPolicy policy = action != null
            ? TargetingPolicy.ForBasicAction(action)
            : TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile);

        // Taunt only overrides the shared combat target after the current basic-action contract accepts it.
        if (TrySelectForcedTarget(self, policy, out Unit forcedTarget))
            return forcedTarget;

        if (policy.Relationship == RequiredTargetRelationship.Ally)
            return SelectAllyTarget(self, policy, currentTarget);

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => SelectDynamicTarget(self, currentTarget),
            _ => SelectRolePriorityTarget(self, currentTarget)
        };
    }

    public Unit SelectAllyTarget(Unit self, in TargetingPolicy policy, Unit currentTarget)
    {
        if (self == null)
            return null;

        TargetingPolicy selectionPolicy = policy;

        if (IsTargetValidForSelection(self, currentTarget, policy))
            return currentTarget;

        IReadOnlyList<Unit> roomUnits = TargetingCandidateProvider.GetRoomCandidates(self);
        return TargetSelectionUtility.SelectLowestHealthRatioTarget(
            self,
            roomUnits,
            candidate => IsTargetValidForSelection(self, candidate, selectionPolicy));
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
        IReadOnlyList<Unit> roomUnits = TargetingCandidateProvider.GetRoomCandidates(self);
        UnitRole? highestPriorityRole = GetHighestPriorityAvailableRole(self, roomUnits);
        if (!highestPriorityRole.HasValue)
            return null;

        if (IsTargetStillValid(self, currentTarget, RequiredTargetRelationship.Hostile) && currentTarget.Role == highestPriorityRole.Value)
            return currentTarget;

        return TargetSelectionUtility.SelectClosestTarget(
            self,
            roomUnits,
            candidate => candidate != null &&
                         candidate.Role == highestPriorityRole.Value &&
                         IsTargetStillValid(self, candidate, RequiredTargetRelationship.Hostile));
    }

    private bool IsTargetStillValid(Unit self, Unit target)
    {
        return IsTargetStillValid(self, target, TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile));
    }

    private bool IsTargetStillValid(Unit self, Unit target, RequiredTargetRelationship relationship)
    {
        return IsTargetStillValid(self, target, TargetingPolicy.ForRelationship(relationship));
    }

    private bool IsTargetStillValid(Unit self, Unit target, in TargetingPolicy policy)
    {
        return UnitTargetValidator.IsTargetSelectable(self, target, policy);
    }

    private bool IsTargetValidForSelection(Unit self, Unit target, in TargetingPolicy policy)
    {
        return UnitTargetValidator.IsTargetSelectable(self, target, policy);
    }

    private bool TrySelectForcedTarget(Unit self, in TargetingPolicy policy, out Unit forcedTarget)
    {
        forcedTarget = null;

        if (self == null || self.StatusEffects == null || !self.StatusEffects.TryGetForcedTarget(out Unit candidate))
            return false;

        if (!IsTargetValidForSelection(self, candidate, policy))
            return false;

        forcedTarget = candidate;
        return true;
    }

    private static Unit GetNearestVisibleHostileInternal(Unit self)
    {
        if (self == null)
            return null;

        return TargetSelectionUtility.SelectClosestTarget(
            self,
            TargetingCandidateProvider.GetRoomCandidates(self),
            candidate => UnitTargetValidator.IsTargetSelectable(
                self,
                candidate,
                TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile)));
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

    private static UnitRole? GetHighestPriorityAvailableRole(Unit self, IReadOnlyList<Unit> units)
    {
        if (self == null || units == null || units.Count == 0)
            return null;

        if (HasAliveUnitWithRole(self, units, UnitRole.Tank))
            return UnitRole.Tank;

        if (HasAliveUnitWithRole(self, units, UnitRole.DPS))
            return UnitRole.DPS;

        if (HasAliveUnitWithRole(self, units, UnitRole.Support))
            return UnitRole.Support;

        return null;
    }

    private static bool HasAliveUnitWithRole(Unit self, IReadOnlyList<Unit> units, UnitRole role)
    {
        if (self == null || units == null)
            return false;

        for (int i = 0; i < units.Count; i++)
        {
            Unit candidate = units[i];
            if (candidate == null || candidate.Role != role)
                continue;

            if (UnitTargetValidator.IsTargetSelectable(self, candidate, TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile)))
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
