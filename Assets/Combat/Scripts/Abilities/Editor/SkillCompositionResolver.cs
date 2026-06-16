using UnityEngine;

public static class SkillCompositionResolver
{
    public static ImpactTargetRequirement ResolveImpactTargetRequirement(CreatureVariantLabState state)
    {
        if (state.primaryTarget == PrimaryTargetRequirement.Self)
            return ImpactTargetRequirement.Self;

        LabEffectKind effect = state.effect;
        if (state.primaryTarget == PrimaryTargetRequirement.Ally ||
            effect == LabEffectKind.Heal ||
            effect == LabEffectKind.HealOverTime ||
            effect == LabEffectKind.Shield ||
            effect == LabEffectKind.Haste ||
            effect == LabEffectKind.StrengthBuff)
        {
            return ImpactTargetRequirement.Ally;
        }

        return ImpactTargetRequirement.Hostile;
    }

    public static TargetSelectionMode ResolveTargetSelectionMode(CreatureVariantLabState state)
    {
        return state.primaryTarget switch
        {
            PrimaryTargetRequirement.None => TargetSelectionMode.None,
            PrimaryTargetRequirement.GroundCell => TargetSelectionMode.None,
            PrimaryTargetRequirement.Self => TargetSelectionMode.None,
            PrimaryTargetRequirement.Ally => TargetSelectionMode.AllyLowestHealth,
            _ => TargetSelectionMode.RoleBasedOffensive
        };
    }

    public static ImpactCenterMode ResolveImpactCenterMode(CreatureVariantLabState state)
    {
        if (state.primaryTarget == PrimaryTargetRequirement.GroundCell)
            return ImpactCenterMode.TargetCell;
        if (state.primaryTarget == PrimaryTargetRequirement.Self ||
            state.primaryTarget == PrimaryTargetRequirement.None)
            return ImpactCenterMode.Caster;
        return ImpactCenterMode.PrimaryTarget;
    }

    public static int ResolveRangeInCells(CreatureVariantLabState state)
    {
        return state.primaryTarget == PrimaryTargetRequirement.Self ||
               state.primaryTarget == PrimaryTargetRequirement.None
            ? 0
            : Mathf.Max(0, state.rangeInCells);
    }

    public static int ResolveMaxTargets(CreatureVariantLabState state, CreatureVariantLabConfig config)
    {
        if (config == null || config.maxTargets == null)
            return ResolveMaxTargetsFallback(state);

        return state.delivery switch
        {
            ImpactPattern.Direct => config.maxTargets.direct,
            ImpactPattern.Line => config.maxTargets.line,
            ImpactPattern.Area => config.maxTargets.area,
            _ => config.maxTargets.fallback
        };
    }

    public static int ResolveMaxTargetsFallback(CreatureVariantLabState state)
    {
        return state.delivery switch
        {
            ImpactPattern.Direct => 1,
            _ => 10
        };
    }

    public static SkillTrajectory ResolveTrajectory(CreatureVariantLabState state)
    {
        return SkillTrajectory.Hitscan;
    }

    public static SkillExecutionMode ResolveExecutionMode(CreatureVariantLabState state)
    {
        return SkillExecutionMode.Instant;
    }

    public static TargetFallbackMode ResolveTargetFallbackMode(CreatureVariantLabState state)
    {
        return TargetFallbackMode.Retarget;
    }
}