using System.Collections.Generic;
using UnityEngine;

public static class SkillCompositionParameterValidator
{
    public static void ValidatePrefab(GameObject prefab, List<ValidationIssue> issues)
    {
        if (prefab == null)
            return;

        if (prefab.GetComponent<Unit>() == null)
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Prefab '{prefab.name}' missing Unit component."));
        if (prefab.GetComponent<UnitMovement>() == null)
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Prefab '{prefab.name}' missing UnitMovement component."));
        if (prefab.GetComponent<SkillCaster>() == null)
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Prefab '{prefab.name}' missing SkillCaster component."));
        if (prefab.GetComponent<LifeController>() == null)
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Prefab '{prefab.name}' missing LifeController component."));
    }

    public static void ValidateUnitStats(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(state.unitName))
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, "Unit name is empty."));

        string nameError = CreatureVariantNameSanitizer.GetAssetNameValidationError(state.unitName);
        if (!string.IsNullOrEmpty(nameError))
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Unit name {nameError}"));

        if (state.maxHealth <= 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Max HP must be greater than 0."));

        if (state.attackDamage < 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Attack damage cannot be negative."));

        if (state.attackCooldown <= 0f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Attack cooldown must be greater than 0."));

        if (state.defense < 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Defense cannot be negative."));

        if (state.moveSpeed <= 0f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Move speed must be greater than 0."));

        if (state.attackRangeInCells <= 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Attack range must be greater than 0."));

        if (state.accuracy < 0f || state.accuracy > 1f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Accuracy must be between 0 and 1."));

        if (state.evasion < 0f || state.evasion > 1f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Evasion must be between 0 and 1."));

        if (state.visionRange <= 0f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Vision range must be greater than 0."));

        if (state.manaCostToRecruit < 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Mana cost to recruit cannot be negative."));

        if (state.manaCostToAbsorbSoul < 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Mana cost to absorb soul cannot be negative."));
    }

    public static void ValidateRoleDelivery(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (!SkillCompositionRules.IsRoleDeliveryCompatible(state.role, state.delivery))
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Impact pattern {state.delivery} is not compatible with role {state.role} (AdvancedPatternNotProductionReady)."));
    }

    public static void ValidateEffectParameters(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (SkillCompositionMapper.HasNumericValue(state.effect) && state.value <= 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, $"Effect {state.effect} requires a value greater than 0."));

        if (state.effect == LabEffectKind.Shield && state.duration <= 0f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Shield effect requires duration greater than 0."));

        bool isStatus = SkillCompositionMapper.IsStatusEffect(state.effect);
        if (isStatus)
            ValidateStatusDefinition(state, issues);

        if (state.effect == LabEffectKind.Summon)
            ValidateSummon(state, issues);

        if (state.effect == LabEffectKind.Knockback && state.knockbackCells <= 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Knockback requires distance greater than 0."));

        ValidateDeliveryParameters(state, issues);
        ValidateTargetParameters(state, issues);
    }

    public static void ValidateEffectTargetCompatibility(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (!SkillCompositionRules.IsEffectTargetCompatible(state.effect, state.primaryTarget))
        {
            bool isBeneficial = SkillCompositionMapper.IsBeneficialEffect(state.effect);
            if (isBeneficial)
                issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Effect {state.effect} cannot use Hostile as primary target."));
            else
                issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Effect {state.effect} cannot use {state.primaryTarget} as primary target."));
        }
    }

    public static void ValidateModifierParameters(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        ValidateProductionSupport(state, issues);

        if (state.modifier == LabModifierKind.Bounce)
        {
            if (state.bounceMaxBounces <= 0 || state.bounceRangeInCells <= 0)
                issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Bounce requires count and range greater than 0."));
        }

        if (state.modifier == LabModifierKind.Explosive)
        {
            if (state.explosiveRadiusInCells <= 0)
                issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Explosive requires radius greater than 0."));
        }

        if (state.modifier == LabModifierKind.Persistent || state.modifier == LabModifierKind.Periodic)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, $"Modifier {state.modifier} has no runtime implementation in SkillCompositionRuntimeExecutor and cannot be used in production assets."));
        }

        bool isStatus = SkillCompositionMapper.IsStatusEffect(state.effect);
        if ((isStatus || state.effect == LabEffectKind.Shield) &&
            state.modifier != LabModifierKind.Persistent &&
            state.duration > 0f)
        {
            issues.Add(ValidationIssue.Warning(ValidationCategory.Parameters, "Duration belongs to the effect/status definition. Do not add Persistent unless runtime support is implemented."));
        }

        if ((state.effect == LabEffectKind.PoisonBurn || state.effect == LabEffectKind.HealOverTime) &&
            state.interval > 0f &&
            state.modifier != LabModifierKind.Periodic)
        {
            issues.Add(ValidationIssue.Warning(ValidationCategory.Parameters, "Tick behavior belongs to the StatusEffectDefinition. Do not add Periodic unless runtime support is implemented."));
        }
    }

    private static void ValidateProductionSupport(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        SkillProductionSupportStatus effectStatus = SkillProductionSupportCatalog.GetEffectStatus(state.effect);
        if (effectStatus != SkillProductionSupportStatus.ProductionSupported)
        {
            issues.Add(ValidationIssue.Warning(
                ValidationCategory.Production,
                $"Effect {state.effect} is {effectStatus}. {SkillProductionSupportCatalog.GetEffectStatusExplanation(state.effect)} Save as Production Variant will be blocked."));
        }

        if (state.modifier == LabModifierKind.None)
            return;

        SkillProductionSupportStatus modifierStatus = SkillProductionSupportCatalog.GetModifierStatus(state.modifier);
        if (modifierStatus == SkillProductionSupportStatus.MissingRuntime)
        {
            issues.Add(ValidationIssue.Error(
                ValidationCategory.Production,
                $"Modifier {state.modifier} is {modifierStatus}. {SkillProductionSupportCatalog.GetModifierStatusExplanation(state.modifier)}"));
        }
        else if (modifierStatus != SkillProductionSupportStatus.ProductionSupported)
        {
            issues.Add(ValidationIssue.Warning(
                ValidationCategory.Production,
                $"Modifier {state.modifier} is {modifierStatus}. {SkillProductionSupportCatalog.GetModifierStatusExplanation(state.modifier)} Save as Production Variant will be blocked."));
        }
    }

    private static void ValidateStatusDefinition(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (state.statusDefinition == null)
        {
            if (state.effect == LabEffectKind.Taunt)
            {
                issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "No StatusEffectDefinition found for Taunt."));
            }
            else if (state.effect == LabEffectKind.Blind)
            {
                issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "No StatusEffectDefinition found for Blind."));
            }
            else
            {
                issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, $"No StatusEffectDefinition assigned for {state.effect}."));
            }
            return;
        }

        SkillEffectKind expectedType = SkillCompositionMapper.ToSkillEffectKind(state.effect);
        if (state.effect == LabEffectKind.HealOverTime)
            expectedType = SkillEffectKind.Heal;

        if (state.statusDefinition.EffectType != expectedType)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, $"StatusEffectDefinition '{state.statusDefinition.name}' is type '{state.statusDefinition.EffectType}' and does not match '{state.effect}'."));

        if (state.statusDefinition.HasTimedDuration && state.statusDefinition.DurationSeconds <= 0f)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, $"StatusEffectDefinition '{state.statusDefinition.name}' requires duration greater than 0."));

        if ((state.effect == LabEffectKind.PoisonBurn || state.effect == LabEffectKind.HealOverTime) &&
            state.statusDefinition.TickIntervalSeconds <= 0f)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, $"StatusEffectDefinition '{state.statusDefinition.name}' requires tick interval greater than 0."));
        }

        issues.Add(ValidationIssue.Warning(ValidationCategory.Parameters, "Duration, ticks and stacks at runtime come from the selected StatusEffectDefinition."));
    }

    private static void ValidateSummon(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (state.summonedUnit == null)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Summon effect requires a SummonedUnit assignment."));
        }
        else if (state.summonedUnit.unitPrefab == null || state.summonedUnit.unitPrefab.GetComponent<Unit>() == null)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, $"SummonedUnit '{state.summonedUnit.name}' needs a valid prefab with Unit component."));
        }

        issues.Add(ValidationIssue.Warning(ValidationCategory.Production, "Summon is experimental. Save as Production Variant will be blocked."));
    }

    private static void ValidateDeliveryParameters(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (state.delivery == ImpactPattern.Area && state.radiusInCells <= 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Area delivery requires radius greater than 0."));
        if (state.delivery == ImpactPattern.Line && state.lineLengthInCells <= 0)
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Line delivery requires length greater than 0."));
    }

    private static void ValidateTargetParameters(CreatureVariantLabState state, List<ValidationIssue> issues)
    {
        if (state.primaryTarget != PrimaryTargetRequirement.Self &&
            state.primaryTarget != PrimaryTargetRequirement.None &&
            state.rangeInCells <= 0)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Parameters, "Composition requires range greater than 0."));
        }

        if (state.delivery == ImpactPattern.Direct && state.primaryTarget == PrimaryTargetRequirement.None)
            issues.Add(ValidationIssue.Error(ValidationCategory.Composition, "Direct delivery requires Hostile, Ally or Self as primary target."));

        if (state.primaryTarget == PrimaryTargetRequirement.GroundCell)
            issues.Add(ValidationIssue.Warning(ValidationCategory.Production, "GroundCell targeting remains experimental. Save as Production Variant will be blocked."));
    }
}
