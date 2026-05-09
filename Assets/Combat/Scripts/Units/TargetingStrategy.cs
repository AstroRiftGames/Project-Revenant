using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class TargetingStrategy : MonoBehaviour
{
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

        IReadOnlyList<Unit> roomUnits = TargetingCandidateProvider.GetRoomCandidates(self);
        TargetingPolicy hostilePolicy = TargetingPolicy.ForRelationship(RequiredTargetRelationship.Hostile);
        TargetingPolicy selectionPolicy = hostilePolicy;

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => TargetingScorer.SelectDynamicTarget(
                self,
                currentTarget,
                roomUnits,
                self.GetAliveAggressors(),
                candidate => IsTargetStillValid(self, candidate, selectionPolicy)),
            _ => TargetingScorer.SelectRolePriorityTarget(
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

        IReadOnlyList<Unit> roomUnits = TargetingCandidateProvider.GetRoomCandidates(self);
        return TargetSelectionUtility.SelectLowestHealthRatioTarget(
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
}

public enum RequiredTargetRelationship
{
    Any,
    Hostile,
    Ally
}
