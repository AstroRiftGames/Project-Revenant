using System.Collections.Generic;
using UnityEngine;

public static class SkillCompositionValidator
{
    public struct ValidationResult
    {
        public List<ValidationIssue> issues;

        public bool HasErrors
        {
            get
            {
                if (issues == null) return false;
                for (int i = 0; i < issues.Count; i++)
                    if (issues[i].severity == ValidationSeverity.Error) return true;
                return false;
            }
        }

        public bool HasWarnings
        {
            get
            {
                if (issues == null) return false;
                for (int i = 0; i < issues.Count; i++)
                    if (issues[i].severity == ValidationSeverity.Warning) return true;
                return false;
            }
        }

        public List<string> ErrorMessages
        {
            get
            {
                var result = new List<string>();
                if (issues == null) return result;
                for (int i = 0; i < issues.Count; i++)
                    if (issues[i].severity == ValidationSeverity.Error)
                        result.Add(issues[i].message);
                return result;
            }
        }

        public List<string> WarningMessages
        {
            get
            {
                var result = new List<string>();
                if (issues == null) return result;
                for (int i = 0; i < issues.Count; i++)
                    if (issues[i].severity == ValidationSeverity.Warning)
                        result.Add(issues[i].message);
                return result;
            }
        }

        public static ValidationResult Empty()
        {
            return new ValidationResult { issues = new List<ValidationIssue>() };
        }
    }

    public static ValidationResult ValidateComposition(CreatureVariantLabState state, GameObject resolvedPrefab)
    {
        var result = ValidationResult.Empty();

        SkillCompositionParameterValidator.ValidatePrefab(resolvedPrefab, result.issues);
        SkillCompositionParameterValidator.ValidateUnitStats(state, result.issues);
        SkillCompositionParameterValidator.ValidateRoleDelivery(state, result.issues);
        SkillCompositionParameterValidator.ValidateEffectParameters(state, result.issues);
        SkillCompositionParameterValidator.ValidateEffectTargetCompatibility(state, result.issues);
        SkillCompositionParameterValidator.ValidateModifierParameters(state, result.issues);

        return result;
    }

    public static void ValidateAgainstRuntimeReadiness(SkillData skill, List<ValidationIssue> issues, bool requireProductionSupport = false)
    {
        if (skill == null)
            return;

        if (!skill.TryValidateDeclarativeContract(out string contractError))
            issues.Add(ValidationIssue.Error(ValidationCategory.RuntimeReadiness, $"Declarative contract: {contractError}"));

        if (!SkillCompositionExecutor.CanExecuteComposition(skill, out string runtimeGap))
            issues.Add(ValidationIssue.Error(ValidationCategory.RuntimeReadiness, $"Runtime readiness: {runtimeGap}"));

        if (!requireProductionSupport)
            return;

        List<string> productionSupportIssues = SkillProductionSupportCatalog.GetProductionSupportIssues(skill);
        for (int i = 0; i < productionSupportIssues.Count; i++)
            issues.Add(ValidationIssue.Error(ValidationCategory.Production, productionSupportIssues[i]));
    }

    public static bool IsProductionCatalogCompatible(CreatureVariantLabState state)
    {
        return SkillCompositionRules.IsProductionCatalogCompatible(state);
    }

    public static bool IsRoleDeliveryCompatible(UnitRole role, ImpactPattern delivery)
    {
        return SkillCompositionRules.IsRoleDeliveryCompatible(role, delivery);
    }

    public static bool IsEffectTargetCompatible(LabEffectKind effect, PrimaryTargetRequirement primaryTarget)
    {
        return SkillCompositionRules.IsEffectTargetCompatible(effect, primaryTarget);
    }
}
