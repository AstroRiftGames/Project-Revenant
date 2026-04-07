using UnityEngine;

[RequireComponent(typeof(Unit))]
public class DynamicTargeting : TargetingStrategy
{
    public override Unit SelectTarget(Unit self, Unit currentTarget)
    {
        if (self == null)
            return null;

        if (IsTargetStillValid(self, currentTarget))
            return currentTarget;

        return self.GetNearestHostileUnitInScene();
    }
}
