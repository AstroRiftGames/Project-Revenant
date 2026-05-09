using System;
using System.Collections.Generic;

public static class TargetingScorer
{
    // This scorer only ranks caller-provided candidates that have already been sourced
    // and can be filtered by the caller through isValidCandidate.
    public static Unit SelectDynamicTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        IReadOnlyList<Unit> aggressors,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || isValidCandidate == null)
            return null;

        // Preserve current gameplay contract:
        // 1. A single alive aggressor is always preferred if it is still a valid target.
        // 2. With multiple aggressors, pick the alive aggressor with the lowest absolute
        //    health, breaking ties by proximity, and only then validate it as a final target.
        // 3. If no aggressor path succeeds, keep the current target when it is still valid.
        // 4. Otherwise fall back to the closest valid candidate.
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

        return TargetSelectionUtility.SelectClosestTarget(self, candidates, isValidCandidate);
    }

    public static Unit SelectRolePriorityTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        // Preserve current gameplay contract:
        // 1. Select the first available hostile role in Tank > DPS > Support order.
        // 2. Keep the current target if it already matches that highest-priority role.
        // 3. Otherwise fall back to the closest valid target within that role.
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
        return TargetSelectionUtility.SelectClosestTarget(
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
