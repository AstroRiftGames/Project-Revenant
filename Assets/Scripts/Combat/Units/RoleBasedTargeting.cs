using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Unit))]
public class RoleBasedTargeting : TargetingStrategy
{
    public override Unit SelectTarget(Unit self, Unit currentTarget)
    {
        if (self == null)
            return null;

        return self.Role switch
        {
            UnitRole.Tank => SelectTankTarget(self, currentTarget),
            UnitRole.DPS => SelectDpsTarget(self, currentTarget),
            UnitRole.Support => SelectSupportTarget(self, currentTarget),
            _ => null
        };
    }

    private Unit SelectTankTarget(Unit self, Unit currentTarget)
    {
        if (IsTargetStillValid(self, currentTarget, TargetRelationship.Hostile))
            return currentTarget;

        List<Unit> aggressors = self.GetAliveAggressors();
        Unit bestAggressor = GetClosestUnit(self, aggressors);
        if (bestAggressor != null)
            return bestAggressor;

        return self.GetNearestHostileUnitInScene();
    }

    private Unit SelectDpsTarget(Unit self, Unit currentTarget)
    {
        List<Unit> hostiles = self.GetHostileUnitsInScene();
        if (hostiles.Count == 0)
            return null;

        Unit bestTarget = GetBestDpsTarget(self, hostiles);
        if (bestTarget == null)
            return null;

        if (IsTargetStillValid(self, currentTarget, TargetRelationship.Hostile) &&
            currentTarget != null &&
            CompareDpsTargets(self, currentTarget, bestTarget) <= 0)
        {
            return currentTarget;
        }

        return bestTarget;
    }

    private Unit SelectSupportTarget(Unit self, Unit currentTarget)
    {
        if (IsTargetStillValid(self, currentTarget, TargetRelationship.Ally) &&
            currentTarget.CurrentHealth < currentTarget.MaxHealth)
        {
            return currentTarget;
        }

        Unit injuredAlly = self.GetLowestHealthAllyNeedingHelpInScene();
        if (injuredAlly != null)
            return injuredAlly;

        Unit frontliner = self.GetNearestAllyByRoleInScene(UnitRole.Tank);
        if (frontliner != null)
            return frontliner;

        return self.GetNearestAllyInScene();
    }

    private static Unit GetBestDpsTarget(Unit self, List<Unit> hostiles)
    {
        Unit bestTarget = null;

        for (int i = 0; i < hostiles.Count; i++)
        {
            Unit candidate = hostiles[i];
            if (bestTarget == null || CompareDpsTargets(self, candidate, bestTarget) < 0)
                bestTarget = candidate;
        }

        return bestTarget;
    }

    private static int CompareDpsTargets(Unit self, Unit candidate, Unit incumbent)
    {
        if (candidate == null)
            return 1;

        if (incumbent == null)
            return -1;

        int candidatePriority = GetDpsPriority(candidate);
        int incumbentPriority = GetDpsPriority(incumbent);
        if (candidatePriority != incumbentPriority)
            return candidatePriority.CompareTo(incumbentPriority);

        if (candidate.CurrentHealth != incumbent.CurrentHealth)
            return candidate.CurrentHealth.CompareTo(incumbent.CurrentHealth);

        float candidateDistance = (candidate.Position - self.Position).sqrMagnitude;
        float incumbentDistance = (incumbent.Position - self.Position).sqrMagnitude;
        return candidateDistance.CompareTo(incumbentDistance);
    }

    private static int GetDpsPriority(Unit candidate)
    {
        return candidate.Role switch
        {
            UnitRole.Support => 0,
            UnitRole.DPS when candidate.IsDpsRanged => 1,
            UnitRole.DPS => 2,
            UnitRole.Tank => 3,
            _ => 4
        };
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
}
