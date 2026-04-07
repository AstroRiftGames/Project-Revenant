using UnityEngine;

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

        return self.GetNearestHostileUnitInScene();
    }

    private Unit SelectDpsTarget(Unit self, Unit currentTarget)
    {
        System.Collections.Generic.List<Unit> hostiles = self.GetHostileUnitsInScene();
        if (hostiles.Count == 0)
            return null;

        Unit bestTarget = null;
        int lowestHealth = int.MaxValue;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < hostiles.Count; i++)
        {
            Unit candidate = hostiles[i];
            float sqrDistance = (candidate.Position - self.Position).sqrMagnitude;

            if (candidate.CurrentHealth > lowestHealth)
                continue;

            if (candidate.CurrentHealth == lowestHealth && sqrDistance >= bestSqrDistance)
                continue;

            bestTarget = candidate;
            lowestHealth = candidate.CurrentHealth;
            bestSqrDistance = sqrDistance;
        }

        if (IsTargetStillValid(self, currentTarget, TargetRelationship.Hostile) &&
            currentTarget != null &&
            currentTarget.CurrentHealth <= lowestHealth)
        {
            return currentTarget;
        }

        return bestTarget;
    }

    private Unit SelectSupportTarget(Unit self, Unit currentTarget)
    {
        if (IsTargetStillValid(self, currentTarget, TargetRelationship.Ally))
            return currentTarget;

        return self.GetLowestHealthAllyInScene();
    }
}
