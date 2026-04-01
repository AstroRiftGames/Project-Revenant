using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class RoleBasedTargeting : TargetingStrategy
{
    public override Unit SelectTarget(Unit self, Unit currentTarget)
    {
        if (self == null || !self.IsAlive)
            return null;

        // Primero intenta seleccionar entre los hostiles visibles.
        List<IUnit> visibleHostiles = self.GetVisibleHostileUnitsInScene();
        Unit visibleTarget = SelectByRolePriority(visibleHostiles, self);
        if (visibleTarget != null)
            return visibleTarget;

        // Si no ve a nadie, entra en búsqueda usando todos los hostiles conocidos en escena.
        List<Unit> hostilesInScene = self.GetHostileUnitsInScene();
        return SelectByRolePriority(hostilesInScene, self);
    }

    private Unit SelectByRolePriority(List<IUnit> candidates, Unit self)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        Unit target = SelectNearestByRole(candidates, self, UnitRole.Tank);
        if (target != null)
            return target;

        target = SelectNearestByRole(candidates, self, UnitRole.DPS);
        if (target != null)
            return target;

        return SelectNearestByRole(candidates, self, UnitRole.Support);
    }

    private Unit SelectByRolePriority(List<Unit> candidates, Unit self)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        Unit target = SelectNearestByRole(candidates, self, UnitRole.Tank);
        if (target != null)
            return target;

        target = SelectNearestByRole(candidates, self, UnitRole.DPS);
        if (target != null)
            return target;

        return SelectNearestByRole(candidates, self, UnitRole.Support);
    }

    private Unit SelectNearestByRole(List<IUnit> candidates, Unit self, UnitRole role)
    {
        Unit bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] is not Unit candidate)
                continue;

            if (!candidate.IsAlive || !self.IsHostileTo(candidate) || candidate.Role != role)
                continue;

            float distance = Vector3.Distance(self.Position, candidate.Position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestTarget = candidate;
        }

        return bestTarget;
    }

    private Unit SelectNearestByRole(List<Unit> candidates, Unit self, UnitRole role)
    {
        Unit bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !candidate.IsAlive || !self.IsHostileTo(candidate) || candidate.Role != role)
                continue;

            float distance = Vector3.Distance(self.Position, candidate.Position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestTarget = candidate;
        }

        return bestTarget;
    }
}
