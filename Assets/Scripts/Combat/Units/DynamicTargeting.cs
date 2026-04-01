using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class DynamicTargeting : TargetingStrategy
{
    public override Unit SelectTarget(Unit self, Unit currentTarget)
    {
        if (self == null || !self.IsAlive)
            return null;

        // Si la unidad esta siendo atacada, eso tiene prioridad sobre la seleccion normal.
        List<Unit> aggressors = self.GetAliveAggressors();
        Unit aggressorTarget = SelectLowestHealthTarget(aggressors, self);
        if (aggressorTarget != null)
            return aggressorTarget;

        // Si no hay agresores, el targeting base prioriza al hostil visible mas cercano.
        List<IUnit> visibleHostiles = self.GetVisibleHostileUnitsInScene();
        Unit visibleTarget = SelectNearestTarget(visibleHostiles, self);
        if (visibleTarget != null)
            return visibleTarget;

        // Si no hay hostiles visibles, entra en busqueda y avanza hacia el hostil conocido mas cercano.
        return self.GetNearestHostileUnitInScene();
    }

    private Unit SelectLowestHealthTarget(List<Unit> candidates, Unit self)
    {
        Unit bestTarget = null;
        int bestHealth = int.MaxValue;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !candidate.IsAlive || !self.IsHostileTo(candidate))
                continue;

            float distance = Vector3.Distance(self.Position, candidate.Position);
            if (candidate.CurrentHealth < bestHealth)
            {
                bestTarget = candidate;
                bestHealth = candidate.CurrentHealth;
                bestDistance = distance;
                continue;
            }

            // Si hay varios agresores vivos, prioriza al mas debil y desempata por distancia.
            if (candidate.CurrentHealth == bestHealth && distance < bestDistance)
            {
                bestTarget = candidate;
                bestDistance = distance;
            }
        }

        return bestTarget;
    }

    private Unit SelectNearestTarget(List<IUnit> candidates, Unit self)
    {
        Unit bestTarget = null;
        float bestDistance = float.MaxValue;
        int bestHealth = int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i] is not Unit candidate || !candidate.IsAlive)
                continue;

            float distance = Vector3.Distance(self.Position, candidate.Position);

            // La seleccion inicial del objetivo se basa en proximidad.
            if (distance < bestDistance)
            {
                bestTarget = candidate;
                bestDistance = distance;
                bestHealth = candidate.CurrentHealth;
                continue;
            }

            // Si dos hostiles estan a la misma distancia, prioriza al que tenga menos vida.
            if (Mathf.Approximately(distance, bestDistance) && candidate.CurrentHealth < bestHealth)
            {
                bestTarget = candidate;
                bestHealth = candidate.CurrentHealth;
            }
        }

        return bestTarget;
    }
}
