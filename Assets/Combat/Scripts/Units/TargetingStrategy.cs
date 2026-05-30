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
            return SelectAllyTarget(self, basicAction, currentTarget, canPickTarget);

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => SelectDynamicTarget(
                self,
                currentTarget,
                roomUnits,
                self.GetAliveAggressors(),
                canPickTarget),
            _ => SelectBestOffensiveTarget(
                self,
                currentTarget,
                roomUnits,
                canPickTarget)
        };
    }

    public Unit SelectAlternativeBasicActionTarget(Unit self, IBasicAction basicAction, Unit excludedTarget)
    {
        if (self == null)
            return null;

        Func<Unit, bool> canPickTarget = candidate =>
            candidate != null &&
            !ReferenceEquals(candidate, excludedTarget) &&
            CanPickBasicActionTarget(self, basicAction, candidate);

        if (TrySelectForcedTarget(self, canPickTarget, out Unit forcedTarget))
            return forcedTarget;

        if (basicAction != null && basicAction.TargetRelation == TargetRelation.Ally)
            return SelectAllyTarget(self, basicAction, null, canPickTarget);

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);

        return self.TargetingMode switch
        {
            UnitTargetingMode.Dynamic => SelectDynamicTarget(
                self,
                null,
                roomUnits,
                self.GetAliveAggressors(),
                canPickTarget),
            _ => SelectBestOffensiveTarget(
                self,
                null,
                roomUnits,
                canPickTarget)
        };
    }

    public Unit SelectAllyTarget(Unit self, IBasicAction basicAction, Unit currentTarget, Func<Unit, bool> canChooseTarget)
    {
        if (self == null || canChooseTarget == null)
            return null;

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);
        if (basicAction != null && basicAction.RequiresInjuredTarget)
            return SelectBestHealingAllyTarget(self, currentTarget, roomUnits, canChooseTarget);

        return SelectBestBuffAllyTarget(self, currentTarget, roomUnits, canChooseTarget);
    }

    public Unit SelectAllyTarget(Unit self, Unit currentTarget, Func<Unit, bool> canChooseTarget)
    {
        if (self == null || canChooseTarget == null)
            return null;

        IReadOnlyList<Unit> roomUnits = GetRoomCandidates(self);
        return SelectBestHealingAllyTarget(self, currentTarget, roomUnits, canChooseTarget);
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
        if (self == null)
            return false;

        TargetingPolicy policy = new(
            TargetRelation.Hostile,
            requiresTarget: true,
            allowSelf: false,
            requireSelf: false,
            allowInvisible: false,
            requireInjured: false,
            requireSameRoom: false,
            requireActive: true,
            requireDetectable: true);

        return UnitTargetValidator.IsTargetSelectable(self, candidate, policy);
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

    public static Unit SelectHighestBasicDamageTarget(Unit self, IReadOnlyList<Unit> candidates, Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        Unit bestTarget = null;
        int bestBaseDamage = int.MinValue;
        float bestSqrDistance = float.MaxValue;
        int bestInstanceId = int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !isValidCandidate(candidate))
                continue;

            int candidateBaseDamage = ResolveBaseDamage(candidate);
            float candidateSqrDistance = ResolveSqrDistance(self, candidate);
            int candidateInstanceId = candidate.GetInstanceID();

            if (candidateBaseDamage > bestBaseDamage)
            {
                bestTarget = candidate;
                bestBaseDamage = candidateBaseDamage;
                bestSqrDistance = candidateSqrDistance;
                bestInstanceId = candidateInstanceId;
                continue;
            }

            if (candidateBaseDamage < bestBaseDamage)
                continue;

            if (candidateSqrDistance < bestSqrDistance)
            {
                bestTarget = candidate;
                bestSqrDistance = candidateSqrDistance;
                bestInstanceId = candidateInstanceId;
                continue;
            }

            if (candidateSqrDistance > bestSqrDistance)
                continue;

            if (candidateInstanceId < bestInstanceId)
            {
                bestTarget = candidate;
                bestInstanceId = candidateInstanceId;
            }
        }

        return bestTarget;
    }

    public static Unit SelectBestOffensiveTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        Unit bestTarget = isValidCandidate(currentTarget) ? currentTarget : null;
        float bestHealthRatio = bestTarget != null ? ResolveHealthRatio(bestTarget) : float.MaxValue;
        int bestBaseDamage = bestTarget != null ? ResolveBaseDamage(bestTarget) : int.MinValue;
        int bestRolePriority = bestTarget != null ? ResolveOffensiveTargetRolePriority(self.Role, bestTarget.Role) : int.MaxValue;
        float bestSqrDistance = bestTarget != null ? ResolveSqrDistance(self, bestTarget) : float.MaxValue;
        int bestInstanceId = bestTarget != null ? bestTarget.GetInstanceID() : int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !isValidCandidate(candidate))
                continue;

            float candidateHealthRatio = ResolveHealthRatio(candidate);
            int candidateBaseDamage = ResolveBaseDamage(candidate);
            int candidateRolePriority = ResolveOffensiveTargetRolePriority(self.Role, candidate.Role);
            float candidateSqrDistance = ResolveSqrDistance(self, candidate);
            int candidateInstanceId = candidate.GetInstanceID();

            if (!IsBetterOffensiveCandidate(
                    self.Role,
                    currentTarget,
                    candidate,
                    candidateHealthRatio,
                    candidateBaseDamage,
                    candidateRolePriority,
                    candidateSqrDistance,
                    candidateInstanceId,
                    bestTarget,
                    bestHealthRatio,
                    bestBaseDamage,
                    bestRolePriority,
                    bestSqrDistance,
                    bestInstanceId))
            {
                continue;
            }

            bestTarget = candidate;
            bestHealthRatio = candidateHealthRatio;
            bestBaseDamage = candidateBaseDamage;
            bestRolePriority = candidateRolePriority;
            bestSqrDistance = candidateSqrDistance;
            bestInstanceId = candidateInstanceId;
        }

        return bestTarget;
    }

    public static Unit SelectBestHealingAllyTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        Unit bestTarget = isValidCandidate(currentTarget) ? currentTarget : null;
        float bestHealthRatio = bestTarget != null ? ResolveHealthRatio(bestTarget) : float.MaxValue;
        float bestSqrDistance = bestTarget != null ? ResolveSqrDistance(self, bestTarget) : float.MaxValue;
        int bestInstanceId = bestTarget != null ? bestTarget.GetInstanceID() : int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !isValidCandidate(candidate))
                continue;

            float candidateHealthRatio = ResolveHealthRatio(candidate);
            float candidateSqrDistance = ResolveSqrDistance(self, candidate);
            int candidateInstanceId = candidate.GetInstanceID();

            if (!IsBetterHealingCandidate(
                    currentTarget,
                    candidate,
                    candidateHealthRatio,
                    candidateSqrDistance,
                    candidateInstanceId,
                    bestTarget,
                    bestHealthRatio,
                    bestSqrDistance,
                    bestInstanceId))
            {
                continue;
            }

            bestTarget = candidate;
            bestHealthRatio = candidateHealthRatio;
            bestSqrDistance = candidateSqrDistance;
            bestInstanceId = candidateInstanceId;
        }

        return bestTarget;
    }

    public static Unit SelectBestBuffAllyTarget(
        Unit self,
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (self == null || candidates == null || isValidCandidate == null)
            return null;

        Unit bestTarget = isValidCandidate(currentTarget) ? currentTarget : null;
        int bestRolePriority = bestTarget != null ? ResolveBuffRolePriority(bestTarget.Role) : int.MaxValue;
        float bestSqrDistance = bestTarget != null ? ResolveSqrDistance(self, bestTarget) : float.MaxValue;
        int bestInstanceId = bestTarget != null ? bestTarget.GetInstanceID() : int.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            Unit candidate = candidates[i];
            if (candidate == null || !isValidCandidate(candidate))
                continue;

            int candidateRolePriority = ResolveBuffRolePriority(candidate.Role);
            float candidateSqrDistance = ResolveSqrDistance(self, candidate);
            int candidateInstanceId = candidate.GetInstanceID();

            if (!IsBetterBuffCandidate(
                    currentTarget,
                    candidate,
                    candidateRolePriority,
                    candidateSqrDistance,
                    candidateInstanceId,
                    bestTarget,
                    bestRolePriority,
                    bestSqrDistance,
                    bestInstanceId))
            {
                continue;
            }

            bestTarget = candidate;
            bestRolePriority = candidateRolePriority;
            bestSqrDistance = candidateSqrDistance;
            bestInstanceId = candidateInstanceId;
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
                Unit bestAggressor = SelectBestOffensiveTarget(
                    self,
                    ResolveScopedCurrentTarget(currentTarget, aggressors, isValidCandidate),
                    aggressors,
                    isValidCandidate);
                if (bestAggressor != null)
                    return bestAggressor;
            }
        }

        return SelectBestOffensiveTarget(self, currentTarget, candidates, isValidCandidate);
    }

    private static bool IsBetterHealingCandidate(
        Unit currentTarget,
        Unit candidate,
        float candidateHealthRatio,
        float candidateSqrDistance,
        int candidateInstanceId,
        Unit bestTarget,
        float bestHealthRatio,
        float bestSqrDistance,
        int bestInstanceId)
    {
        if (bestTarget == null)
            return true;

        if (candidateHealthRatio < bestHealthRatio)
            return true;

        if (candidateHealthRatio > bestHealthRatio)
            return false;

        if (candidateSqrDistance < bestSqrDistance)
            return true;

        if (candidateSqrDistance > bestSqrDistance)
            return false;

        if (ReferenceEquals(bestTarget, currentTarget))
            return false;

        return candidateInstanceId < bestInstanceId;
    }

    private static bool IsBetterBuffCandidate(
        Unit currentTarget,
        Unit candidate,
        int candidateRolePriority,
        float candidateSqrDistance,
        int candidateInstanceId,
        Unit bestTarget,
        int bestRolePriority,
        float bestSqrDistance,
        int bestInstanceId)
    {
        if (bestTarget == null)
            return true;

        if (candidateRolePriority < bestRolePriority)
            return true;

        if (candidateRolePriority > bestRolePriority)
            return false;

        if (candidateSqrDistance < bestSqrDistance)
            return true;

        if (candidateSqrDistance > bestSqrDistance)
            return false;

        if (ReferenceEquals(bestTarget, currentTarget))
            return false;

        return candidateInstanceId < bestInstanceId;
    }

    private static bool IsBetterOffensiveCandidate(
        UnitRole attackerRole,
        Unit currentTarget,
        Unit candidate,
        float candidateHealthRatio,
        int candidateBaseDamage,
        int candidateRolePriority,
        float candidateSqrDistance,
        int candidateInstanceId,
        Unit bestTarget,
        float bestHealthRatio,
        int bestBaseDamage,
        int bestRolePriority,
        float bestSqrDistance,
        int bestInstanceId)
    {
        if (bestTarget == null)
            return true;

        if (attackerRole == UnitRole.Tank)
        {
            if (candidateBaseDamage > bestBaseDamage)
                return true;

            if (candidateBaseDamage < bestBaseDamage)
                return false;
        }
        else
        {
            if (candidateHealthRatio < bestHealthRatio)
                return true;

            if (candidateHealthRatio > bestHealthRatio)
                return false;
        }

        if (candidateRolePriority < bestRolePriority)
            return true;

        if (candidateRolePriority > bestRolePriority)
            return false;

        if (candidateSqrDistance < bestSqrDistance)
            return true;

        if (candidateSqrDistance > bestSqrDistance)
            return false;

        if (ReferenceEquals(bestTarget, currentTarget))
            return false;

        return candidateInstanceId < bestInstanceId;
    }

    private static float ResolveHealthRatio(Unit target)
    {
        if (target == null || target.MaxHealth <= 0)
            return 1f;

        return (float)target.CurrentHealth / target.MaxHealth;
    }

    private static float ResolveSqrDistance(Unit self, Unit target)
    {
        if (self == null || target == null)
            return float.MaxValue;

        return (self.Position - target.Position).sqrMagnitude;
    }

    private static int ResolveBaseDamage(Unit target)
    {
        if (target == null)
            return 0;

        UnitStatsData coreStats = target.CoreStats;
        if (coreStats != null)
            return coreStats.attackDamage;

        return target.AttackDamage;
    }

    private static int ResolveBuffRolePriority(UnitRole role)
    {
        return role switch
        {
            UnitRole.DPS => 0,
            UnitRole.Tank => 1,
            UnitRole.Support => 2,
            _ => int.MaxValue
        };
    }

    private static int ResolveOffensiveTargetRolePriority(UnitRole attackerRole, UnitRole targetRole)
    {
        return attackerRole switch
        {
            UnitRole.Tank => ResolveTankTargetRolePriority(targetRole),
            UnitRole.Support => ResolveSupportOffensiveTargetRolePriority(targetRole),
            _ => ResolveDpsTargetRolePriority(targetRole)
        };
    }

    private static int ResolveDpsTargetRolePriority(UnitRole role)
    {
        return role switch
        {
            UnitRole.DPS => 0,
            UnitRole.Support => 1,
            UnitRole.Tank => 2,
            _ => int.MaxValue
        };
    }

    private static int ResolveTankTargetRolePriority(UnitRole role)
    {
        return role switch
        {
            UnitRole.DPS => 0,
            UnitRole.Tank => 1,
            UnitRole.Support => 2,
            _ => int.MaxValue
        };
    }

    private static int ResolveSupportOffensiveTargetRolePriority(UnitRole role)
    {
        return role switch
        {
            UnitRole.Support => 0,
            UnitRole.DPS => 1,
            UnitRole.Tank => 2,
            _ => int.MaxValue
        };
    }

    private static Unit ResolveScopedCurrentTarget(
        Unit currentTarget,
        IReadOnlyList<Unit> candidates,
        Func<Unit, bool> isValidCandidate)
    {
        if (currentTarget == null || candidates == null || isValidCandidate == null || !isValidCandidate(currentTarget))
            return null;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (ReferenceEquals(candidates[i], currentTarget))
                return currentTarget;
        }

        return null;
    }
}

public enum TargetRelation
{
    Any,
    Hostile,
    Ally
}
