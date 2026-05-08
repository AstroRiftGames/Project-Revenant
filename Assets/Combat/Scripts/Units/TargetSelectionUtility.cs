using System;
using System.Collections.Generic;
using UnityEngine;

public static class TargetSelectionUtility
{
    // This utility only ranks/selects from caller-provided candidates. It does not
    // own targeting rules, status checks, or action-specific validation.
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
}
