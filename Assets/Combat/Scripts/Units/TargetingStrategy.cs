using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class TargetingStrategy : MonoBehaviour
{
    // Keep the combat target selection story in one place so behavior changes do not
    // require hopping through multiple small helper files.
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

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);
        TargetingPolicy hostilePolicy = TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile);
        TargetingPolicy selectionPolicy = hostilePolicy;

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => SelectDynamicTarget(
                self,
                currentTarget,
                roomUnits,
                self.GetAliveAggressors(),
                candidate => IsTargetStillValid(self, candidate, selectionPolicy)),
            _ => SelectRolePriorityTarget(
                self,
                currentTarget,
                roomUnits,
                candidate => IsTargetStillValid(self, candidate, selectionPolicy))
        };
    }

    public Unit SelectAllyTarget(Unit self, in TargetingPolicy policy, Unit currentTarget)
    {
        if (self == null)
            return null;

        TargetingPolicy selectionPolicy = policy;

        if (IsTargetValidForSelection(self, currentTarget, policy))
            return currentTarget;

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);
        return SelectLowestHealthRatioTarget(
            self,
            roomUnits,
            candidate => IsTargetValidForSelection(self, candidate, selectionPolicy));
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

    public static IReadOnlyList<Unit> GetRoomCandidates(Unit self)
    {
        return self != null && self.RoomContext != null
            ? self.RoomContext.Units
            : Array.Empty<Unit>();
    }

    public static Unit SelectClosestTarget(Unit self, IReadOnlyList<Unit> candidates, Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        Unit closest = null;
        float bestSqrDistance = float.MaxValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !isValidCandidate(candidate))
                continue;

            float sqrDistance = (candidate.Position - self.Position).sqrMagnitude;
            if (sqrDistance > bestSqrDistance)
                continue;

            if (Mathf.Approximately(sqrDistance, bestSqrDistance) && candidate.CurrentHealth >= bestHealth)
                continue;

            closest = candidate;
            bestSqrDistance = sqrDistance;
            bestHealth = candidate.CurrentHealth;
        }

        return closest;
    }

    public static Unit SelectLowestHealthRatioTarget(Unit self, IReadOnlyList<Unit> candidates, Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        Unit bestTarget = null;
        float bestHealthRatio = float.MaxValue;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !isValidCandidate(candidate))
                continue;

            float healthRatio = candidate.MaxHealth > 0
                ? (float)candidate.CurrentHealth / candidate.MaxHealth
                : 1f;
            float sqrDistance = (self.Position - candidate.Position).sqrMagnitude;

            if (healthRatio > bestHealthRatio)
                continue;

            if (Mathf.Approximately(healthRatio, bestHealthRatio) && sqrDistance >= bestSqrDistance)
                continue;

            bestTarget = candidate;
            bestHealthRatio = healthRatio;
            bestSqrDistance = sqrDistance;
        }

        return bestTarget;
    }

    private static Unit SelectDynamicTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        IReadOnlyList<Unit> aggressors,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || isValidCandidate == null)
            return null;

        if (aggressors != null)
        {
            if (aggressors.Count == 1)
            {
                Unit loneAggressor = aggressors[0];
                if (isValidCandidate(loneAggressor))
                    return loneAggressor;
            }
            else if (aggressors.Count > 1)
            {
                Unit weakestAggressor = SelectLowestAbsoluteHealthTarget(
                    self,
                    aggressors,
                    candidate => candidate != null && candidate.IsAlive);
                if (isValidCandidate(weakestAggressor))
                    return weakestAggressor;
            }
        }

        if (isValidCandidate(currentTarget))
            return currentTarget;

        return SelectClosestTarget(self, candidates, isValidCandidate);
    }

    private static Unit SelectRolePriorityTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        UnitRole? highestPriorityRole = GetHighestPriorityAvailableRole(candidates, isValidCandidate);
        if (!highestPriorityRole.HasValue)
            return null;

        if (currentTarget != null &&
            currentTarget.Role == highestPriorityRole.Value &&
            isValidCandidate(currentTarget))
        {
            return currentTarget;
        }

        UnitRole requiredRole = highestPriorityRole.Value;
        return SelectClosestTarget(
            self,
            candidates,
            candidate => candidate != null &&
                         candidate.Role == requiredRole &&
                         isValidCandidate(candidate));
    }

    private static Unit SelectLowestAbsoluteHealthTarget(
        Unit self,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> shouldScoreCandidate)
    {
        if (self == null || candidates == null || shouldScoreCandidate == null)
            return null;

        Unit weakest = null;
        int lowestHealth = int.MaxValue;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !shouldScoreCandidate(candidate))
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

    private static UnitRole? GetHighestPriorityAvailableRole(
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (candidates == null || candidates.Count == 0 || isValidCandidate == null)
            return null;

        if (HasCandidateWithRole(candidates, UnitRole.Tank, isValidCandidate))
            return UnitRole.Tank;

        if (HasCandidateWithRole(candidates, UnitRole.DPS, isValidCandidate))
            return UnitRole.DPS;

        if (HasCandidateWithRole(candidates, UnitRole.Support, isValidCandidate))
            return UnitRole.Support;

        return null;
    }

    private static bool HasCandidateWithRole(
        IReadOnlyList<Unit> candidates,
        UnitRole role,
        Func<Unit, bool> isValidCandidate)
    {
        if (candidates == null || isValidCandidate == null)
            return false;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || candidate.Role != role)
                continue;

            if (isValidCandidate(candidate))
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
