using UnityEngine;

public static class SkillCompositionRules
{
    public static bool IsRoleDeliveryCompatible(UnitRole role, ImpactPattern delivery)
    {
        return role switch
        {
            UnitRole.DPS => delivery == ImpactPattern.Direct || delivery == ImpactPattern.Line,
            UnitRole.Tank => delivery == ImpactPattern.Area,
            UnitRole.Support => delivery == ImpactPattern.Direct || delivery == ImpactPattern.Area,
            _ => false
        };
    }

    public static bool IsEffectTargetCompatible(LabEffectKind effect, PrimaryTargetRequirement primaryTarget)
    {
        bool isBeneficial = SkillCompositionMapper.IsBeneficialEffect(effect);
        bool isOffensive = SkillCompositionMapper.IsOffensiveEffect(effect);

        if (isBeneficial && primaryTarget == PrimaryTargetRequirement.Hostile)
            return false;
        if (isOffensive && (primaryTarget == PrimaryTargetRequirement.Ally || primaryTarget == PrimaryTargetRequirement.Self))
            return false;
        return true;
    }

    public static bool IsProductionCatalogCompatible(CreatureVariantLabState state)
    {
        if (state == null)
            return false;

        return state.primaryTarget != PrimaryTargetRequirement.GroundCell &&
               SkillProductionSupportCatalog.IsProductionSupported(state.effect) &&
               SkillProductionSupportCatalog.IsProductionSupported(state.modifier);
    }
}
