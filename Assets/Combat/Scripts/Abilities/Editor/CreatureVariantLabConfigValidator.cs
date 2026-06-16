using System.Collections.Generic;
using UnityEngine;

public static class CreatureVariantLabConfigValidator
{
    public enum ConfigStatus
    {
        Missing,
        Multiple,
        Incomplete,
        Valid
    }

    public static bool IsPresetStatInvalid(CreatureVariantLabConfig.FactionRolePreset p)
    {
        if (p == null) return false;
        if (p.defaultMaxHealth <= 0) return true;
        if (p.defaultAttackCooldown <= 0f) return true;
        if (p.defaultAttackRangeInCells <= 0) return true;
        if (p.defaultPreferredDistanceInCells <= 0) return true;
        if (p.defaultMoveSpeed <= 0f) return true;
        if (p.defaultVisionRange <= 0f) return true;
        if (p.defaultAccuracy < 0f || p.defaultAccuracy > 1f) return true;
        if (p.defaultEvasion < 0f || p.defaultEvasion > 1f) return true;
        if (p.defaultAttackDamage < 0) return true;
        if (p.defaultDefense < 0) return true;
        if (p.defaultManaCostToRecruit < 0) return true;
        if (p.defaultManaCostToAbsorbSoul < 0) return true;
        return false;
    }

    public static ConfigStatus ValidateConfig(CreatureVariantLabConfig config, List<ValidationIssue> issues)
    {
        if (config == null)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Config, "No CreatureVariantLabConfig asset found. Create one via Assets > Create > Combat > Editor > Creature Variant Lab Config."));
            return ConfigStatus.Missing;
        }

        if (config.dummyPrefabOverride != null)
        {
            if (config.dummyPrefabOverride.GetComponent<Unit>() == null)
                issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: dummy prefab override missing Unit component."));
        }

        if (string.IsNullOrWhiteSpace(config.testScenePath))
            issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: testScenePath is empty."));

        if (string.IsNullOrWhiteSpace(config.tempAssetFolder))
            issues.Add(ValidationIssue.Info(ValidationCategory.Config, "Config: tempAssetFolder is empty (only needed for legacy cleanup)."));
        if (string.IsNullOrWhiteSpace(config.productionSkillFolder))
            issues.Add(ValidationIssue.Info(ValidationCategory.Production, "Config: productionSkillFolder is empty (production save not available)."));
        if (string.IsNullOrWhiteSpace(config.productionVariantFolder))
            issues.Add(ValidationIssue.Info(ValidationCategory.Production, "Config: productionVariantFolder is empty (production save not available)."));

        var seenKeys = new HashSet<string>();
        if (config.presets == null || config.presets.Length == 0)
        {
            issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: no faction/role presets defined."));
        }
        else
        {
            for (int i = 0; i < config.presets.Length; i++)
            {
                var preset = config.presets[i];
                if (preset == null)
                {
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: preset {i} is null."));
                    continue;
                }

                string key = $"{preset.faction}_{preset.role}";
                if (!seenKeys.Add(key))
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: duplicate preset for {preset.faction}/{preset.role}."));

                if (preset.prefabOverride != null)
                {
                    if (preset.prefabOverride.GetComponent<Unit>() == null)
                        issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} prefab override '{preset.prefabOverride.name}' missing Unit component."));
                    if (preset.prefabOverride.GetComponent<UnitMovement>() == null)
                        issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} prefab override '{preset.prefabOverride.name}' missing UnitMovement component."));
                    if (preset.prefabOverride.GetComponent<SkillCaster>() == null)
                        issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} prefab override '{preset.prefabOverride.name}' missing SkillCaster component."));
                    if (preset.prefabOverride.GetComponent<LifeController>() == null)
                        issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} prefab override '{preset.prefabOverride.name}' missing LifeController component."));
                }

                if (preset.defaultMaxHealth <= 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultMaxHealth must be greater than 0."));
                if (preset.defaultAttackCooldown <= 0f)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultAttackCooldown must be greater than 0."));
                if (preset.defaultAttackRangeInCells <= 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultAttackRangeInCells must be greater than 0."));
                if (preset.defaultPreferredDistanceInCells <= 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultPreferredDistanceInCells must be greater than 0."));
                if (preset.defaultMoveSpeed <= 0f)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultMoveSpeed must be greater than 0."));
                if (preset.defaultVisionRange <= 0f)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultVisionRange must be greater than 0."));
                if (preset.defaultAccuracy < 0f || preset.defaultAccuracy > 1f)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultAccuracy must be between 0 and 1."));
                if (preset.defaultEvasion < 0f || preset.defaultEvasion > 1f)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultEvasion must be between 0 and 1."));
                if (preset.defaultAttackDamage < 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultAttackDamage cannot be negative."));
                if (preset.defaultDefense < 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultDefense cannot be negative."));
                if (preset.defaultManaCostToRecruit < 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultManaCostToRecruit cannot be negative."));
                if (preset.defaultManaCostToAbsorbSoul < 0)
                    issues.Add(ValidationIssue.Error(ValidationCategory.Config, $"Config: {preset.faction}/{preset.role} preset defaultManaCostToAbsorbSoul cannot be negative."));
            }
        }

        if (config.maxTargets == null)
        {
            issues.Add(ValidationIssue.Info(ValidationCategory.Config, "Config: maxTargets is null, using hardcoded defaults."));
        }
        else
        {
            if (config.maxTargets.direct < 1)
                issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: maxTargets.direct must be at least 1."));
            if (config.maxTargets.area < 1)
                issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: maxTargets.area must be at least 1."));
            if (config.maxTargets.line < 1)
                issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: maxTargets.line must be at least 1."));
            if (config.maxTargets.fallback < 1)
                issues.Add(ValidationIssue.Error(ValidationCategory.Config, "Config: maxTargets.fallback must be at least 1."));
        }

        bool currentHasErrors = false;
        for (int i = 0; i < issues.Count; i++)
        {
            if (issues[i].severity == ValidationSeverity.Error)
            {
                currentHasErrors = true;
                break;
            }
        }

        return currentHasErrors ? ConfigStatus.Incomplete : ConfigStatus.Valid;
    }

    public static ConfigStatus GetConfigStatus(CreatureVariantLabConfig config)
    {
        if (config == null)
            return ConfigStatus.Missing;

        var issues = new List<ValidationIssue>();
        ValidateConfig(config, issues);

        foreach (var issue in issues)
        {
            if (issue.severity == ValidationSeverity.Error)
                return ConfigStatus.Incomplete;
        }

        return ConfigStatus.Valid;
    }

    public static List<(UnitFaction faction, UnitRole role)> GetMissingPresets(CreatureVariantLabConfig config)
    {
        var missing = new List<(UnitFaction, UnitRole)>();
        if (config == null || config.presets == null)
            return new List<(UnitFaction, UnitRole)>(CreatureVariantLabConfig.RequiredPresets);

        var existing = new HashSet<string>();
        for (int i = 0; i < config.presets.Length; i++)
        {
            if (config.presets[i] != null)
                existing.Add($"{config.presets[i].faction}_{config.presets[i].role}");
        }

        foreach (var key in CreatureVariantLabConfig.RequiredPresets)
        {
            if (!existing.Contains($"{key.faction}_{key.role}"))
                missing.Add(key);
        }

        return missing;
    }

    public static bool HasInvalidPresetStats(CreatureVariantLabConfig config)
    {
        if (config == null || config.presets == null)
            return false;

        for (int i = 0; i < config.presets.Length; i++)
        {
            if (IsPresetStatInvalid(config.presets[i]))
                return true;
        }

        return false;
    }
}