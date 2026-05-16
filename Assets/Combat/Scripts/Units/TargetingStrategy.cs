using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class TargetingStrategy : MonoBehaviour
{
    public Unit SelectBasicActionTarget(Unit self, IBasicAction basicAction, Unit currentTarget)
    {
        if (self == null)
            return null;

        Func<Unit, bool> canPickTarget = candidate => CanPickBasicActionTarget(self, basicAction, candidate);

        if (TrySelectForcedTarget(self, canPickTarget, out Unit forcedTarget))
            return forcedTarget;

        if (basicAction != null && basicAction.TargetRelation == TargetRelation.Ally)
            return SelectAllyTarget(self, currentTarget, canPickTarget);

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => SelectDynamicTarget(
                self,
                currentTarget,
                roomUnits,
                self.GetAliveAggressors(),
                canPickTarget),
            _ => SelectRolePriorityTarget(
                self,
                currentTarget,
                roomUnits,
                canPickTarget)
        };
    }

    public Unit SelectAllyTarget(Unit self, Unit currentTarget, Func<Unit, bool> canChooseTarget)
    {
        if (self == null || canChooseTarget == null)
            return null;

        if (canChooseTarget(currentTarget))
            return currentTarget;

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);
        return SelectLowestHealthRatioTarget(
            self,
            roomUnits,
            canChooseTarget);
    }

    private bool TrySelectForcedTarget(Unit self, Func<Unit, bool> canChooseTarget, out Unit forcedTarget)
    {
        forcedTarget = null;

        if (self == null || self.StatusEffects == null || !self.StatusEffects.TryGetForcedTarget(out Unit candidate))
            return false;

        if (canChooseTarget == null || !canChooseTarget(candidate))
            return false;

        forcedTarget = candidate;
        return true;
    }

    private bool CanPickBasicActionTarget(Unit self, IBasicAction basicAction, Unit candidate)
    {
        if (self == null)
            return false;

        if (basicAction != null)
            return basicAction.IsValidTarget(self, candidate);

        return CanPickVisibleHostile(self, candidate);
    }

    public static bool CanPickVisibleHostile(Unit self, Unit candidate)
    {
        return self != null &&
               candidate != null &&
               candidate.gameObject.activeInHierarchy &&
               candidate.IsAlive &&
               self.IsHostileTo(candidate) &&
               self.CanDetect(candidate) &&
               (candidate.StatusEffects == null || !candidate.StatusEffects.HasInvisibility);
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

public enum TargetRelation
{
    Any,
    Hostile,
    Ally
}
