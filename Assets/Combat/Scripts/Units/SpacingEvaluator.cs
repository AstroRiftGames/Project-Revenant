using System.Collections.Generic;

public static class SpacingEvaluator
{
    public static Unit GetSpacingThreat(Unit self, Unit currentTarget)
    {
        if (self == null || !self.WantsToHoldSpacing)
            return null;

        // Preserve current gameplay contract: if taunt already defines the shared
        // combat target, do not add an additional spacing threat override.
        if (self.StatusEffects != null &&
            self.StatusEffects.TryGetForcedTarget(out Unit tauntTarget) &&
            ReferenceEquals(currentTarget, tauntTarget))
        {
            return null;
        }

        if (self.Role == UnitRole.Support)
            return GetNearestVisibleHostile(self);

        TargetingPolicy hostilePolicy = TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile);
        if (UnitTargetValidator.IsTargetSelectable(self, currentTarget, hostilePolicy))
            return currentTarget;

        return GetNearestVisibleHostile(self);
    }

    public static Unit GetNearestVisibleHostile(Unit self)
    {
        if (self == null)
            return null;

        TargetingPolicy hostilePolicy = TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile);
        IReadOnlyList<Unit> roomUnits = TargetingCandidateProvider.GetRoomCandidates(self);
        return TargetSelectionUtility.SelectClosestTarget(
            self,
            roomUnits,
            candidate => UnitTargetValidator.IsTargetSelectable(self, candidate, hostilePolicy));
    }
}
