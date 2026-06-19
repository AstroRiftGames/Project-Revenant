using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CreatureVariantAssetService
{
    public struct ConfigFindResult
    {
        public CreatureVariantLabConfigValidator.ConfigStatus status;
        public CreatureVariantLabConfig config;
        public string message;
    }

    private static readonly string DefaultConfigAssetPath = "Assets/Combat/Data/Editor/CreatureVariantLabConfig.asset";

    public static ConfigFindResult FindOrCreateConfig()
    {
        string[] paths = FindConfigAssetPaths();

        if (paths.Length > 1)
        {
            string list = string.Join("\n", paths);
            return new ConfigFindResult
            {
                status = CreatureVariantLabConfigValidator.ConfigStatus.Multiple,
                config = null,
                message = list
            };
        }

        if (paths.Length == 1)
        {
            var config = AssetDatabase.LoadAssetAtPath<CreatureVariantLabConfig>(paths[0]);
            return new ConfigFindResult
            {
                status = config != null ? CreatureVariantLabConfigValidator.GetConfigStatus(config) : CreatureVariantLabConfigValidator.ConfigStatus.Missing,
                config = config,
                message = null
            };
        }

        return new ConfigFindResult
        {
            status = CreatureVariantLabConfigValidator.ConfigStatus.Missing,
            config = null,
            message = null
        };
    }

    public static CreatureVariantLabConfig CreateOrFindConfig()
    {
        string[] paths = FindConfigAssetPaths();
        if (paths.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<CreatureVariantLabConfig>(paths[0]);
        }

        EnsureFolderExists(DefaultConfigAssetPath);

        var config = ScriptableObject.CreateInstance<CreatureVariantLabConfig>();
        config.presets = new CreatureVariantLabConfig.FactionRolePreset[]
        {
            new() { faction = UnitFaction.Human, role = UnitRole.DPS, defaultCombatStyle = UnitCombatStyle.Default, defaultMaxHealth = 80, defaultAttackDamage = 12, defaultAttackCooldown = 1f, defaultAttackRangeInCells = 1, defaultPreferredDistanceInCells = 1, defaultAccuracy = 0.85f, defaultEvasion = 0.1f, defaultDefense = 0, defaultMoveSpeed = 3.5f, defaultVisionRange = 8f, defaultManaCostToRecruit = 1, defaultManaCostToAbsorbSoul = 1 },
            new() { faction = UnitFaction.Human, role = UnitRole.Tank, defaultCombatStyle = UnitCombatStyle.Melee, defaultMaxHealth = 150, defaultAttackDamage = 6, defaultAttackCooldown = 1.2f, defaultAttackRangeInCells = 1, defaultPreferredDistanceInCells = 1, defaultAccuracy = 0.8f, defaultEvasion = 0.05f, defaultDefense = 8, defaultMoveSpeed = 2.5f, defaultVisionRange = 8f, defaultManaCostToRecruit = 2, defaultManaCostToAbsorbSoul = 2 },
            new() { faction = UnitFaction.Human, role = UnitRole.Support, defaultCombatStyle = UnitCombatStyle.Ranged, defaultMaxHealth = 90, defaultAttackDamage = 4, defaultAttackCooldown = 1.4f, defaultAttackRangeInCells = 3, defaultPreferredDistanceInCells = 3, defaultAccuracy = 0.85f, defaultEvasion = 0.1f, defaultDefense = 2, defaultMoveSpeed = 3f, defaultVisionRange = 8f, defaultManaCostToRecruit = 2, defaultManaCostToAbsorbSoul = 2 },
            new() { faction = UnitFaction.Orc, role = UnitRole.DPS, defaultCombatStyle = UnitCombatStyle.Melee, defaultMaxHealth = 80, defaultAttackDamage = 12, defaultAttackCooldown = 1f, defaultAttackRangeInCells = 1, defaultPreferredDistanceInCells = 1, defaultAccuracy = 0.85f, defaultEvasion = 0.1f, defaultDefense = 0, defaultMoveSpeed = 3.5f, defaultVisionRange = 8f, defaultManaCostToRecruit = 1, defaultManaCostToAbsorbSoul = 1 },
            new() { faction = UnitFaction.Orc, role = UnitRole.Tank, defaultCombatStyle = UnitCombatStyle.Melee, defaultMaxHealth = 150, defaultAttackDamage = 6, defaultAttackCooldown = 1.2f, defaultAttackRangeInCells = 1, defaultPreferredDistanceInCells = 1, defaultAccuracy = 0.8f, defaultEvasion = 0.05f, defaultDefense = 8, defaultMoveSpeed = 2.5f, defaultVisionRange = 8f, defaultManaCostToRecruit = 2, defaultManaCostToAbsorbSoul = 2 },
            new() { faction = UnitFaction.Orc, role = UnitRole.Support, defaultCombatStyle = UnitCombatStyle.Ranged, defaultMaxHealth = 90, defaultAttackDamage = 4, defaultAttackCooldown = 1.4f, defaultAttackRangeInCells = 3, defaultPreferredDistanceInCells = 3, defaultAccuracy = 0.85f, defaultEvasion = 0.1f, defaultDefense = 2, defaultMoveSpeed = 3f, defaultVisionRange = 8f, defaultManaCostToRecruit = 2, defaultManaCostToAbsorbSoul = 2 },
        };

        AssetDatabase.CreateAsset(config, DefaultConfigAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Creature Variant Lab] Created default config at: {DefaultConfigAssetPath}");
        return config;
    }

    public static string[] FindConfigAssetPaths()
    {
        string[] guids = AssetDatabase.FindAssets("t:CreatureVariantLabConfig");
        var paths = new List<string>();
        foreach (var guid in guids)
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        return paths.ToArray();
    }

    public static SkillData CreateTempSkill(CreatureVariantLabState state, CreatureVariantLabConfig config)
    {
        SkillData skill = ScriptableObject.CreateInstance<SkillData>();
        CreatureVariantBuilder.ConfigureSkillData(skill, state, "LabTemp_Skill", $"LabTemp_{state.role}_{state.delivery}", SkillCompositionState.Provisional, config);
        return skill;
    }

    public static UnitData CreateTempUnit(CreatureVariantLabState state, CreatureVariantLabConfig config, SkillData skill, GameObject prefab)
    {
        UnitData unit = ScriptableObject.CreateInstance<UnitData>();
        CreatureVariantBuilder.ConfigureUnitData(unit, state, skill, prefab);
        return unit;
    }

    public static void WriteTempAssets(SkillData skill, UnitData unit, CreatureVariantLabConfig config, out string skillPath, out string unitPath)
    {
        string tempFolder = config.tempAssetFolder;
        EnsureFolderExists(tempFolder + "/dummy.asset");

        skillPath = $"{tempFolder}/LabTemp_Skill.asset";
        unitPath = $"{tempFolder}/LabTemp_UnitData.asset";

        if (AssetDatabase.LoadAssetAtPath<SkillData>(skillPath) != null)
            AssetDatabase.DeleteAsset(skillPath);
        if (AssetDatabase.LoadAssetAtPath<UnitData>(unitPath) != null)
            AssetDatabase.DeleteAsset(unitPath);

        AssetDatabase.CreateAsset(skill, skillPath);
        AssetDatabase.CreateAsset(unit, unitPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void SaveProductionVariant(CreatureVariantLabState state, CreatureVariantLabConfig config, SkillCompositionValidator.ValidationResult validation)
    {
        if (validation.HasErrors)
            return;

        if (!SkillCompositionValidator.IsProductionCatalogCompatible(state))
        {
            string blockReason = SkillProductionSupportCatalog.GetProductionBlockReason(state) ??
                "Production save blocked: composition is outside the production-supported catalog.";
            EditorUtility.DisplayDialog("Save Blocked", blockReason, "OK");
            return;
        }

        string skillName = state.saveSkillName.Trim();
        if (string.IsNullOrEmpty(skillName))
            return;

        GameObject prefab = state.prefabOverride != null ? state.prefabOverride : config.GetPrefab(state.faction, state.role);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Save Blocked", "Production save requires a prefab override. Runtime placeholders are only for testing.", "OK");
            return;
        }

        SkillData preSaveSkill = ScriptableObject.CreateInstance<SkillData>();
        try
        {
            CreatureVariantBuilder.ConfigureSkillData(preSaveSkill, state, "SaveValidateTemp", "SaveValidateTemp", SkillCompositionState.Official, config);
            var runtimeIssues = new List<ValidationIssue>();
            SkillCompositionValidator.ValidateAgainstRuntimeReadiness(preSaveSkill, runtimeIssues, true);
            bool hasRuntimeErrors = false;
            var errorMessages = new List<string>();
            foreach (var issue in runtimeIssues)
            {
                if (issue.severity == ValidationSeverity.Error)
                {
                    hasRuntimeErrors = true;
                    errorMessages.Add(issue.message);
                }
            }
            if (hasRuntimeErrors)
            {
                string combined = string.Join("\n", errorMessages);
                EditorUtility.DisplayDialog("Save Blocked", $"Production validation errors:\n\n{combined}", "OK");
                return;
            }
        }
        finally
        {
            Object.DestroyImmediate(preSaveSkill);
        }

        string skillAssetName = $"{state.role}_{skillName}";
        string variantAssetName = $"{state.faction}_{state.role}_{skillName}";

        string skillFolder = config.GetProductionSkillFolder(state.role);
        string variantFolder = config.GetProductionVariantFolder(state.faction, state.role);

        string skillPath = $"{skillFolder}/{skillAssetName}.asset";
        string variantPath = $"{variantFolder}/{variantAssetName}.asset";

        bool skillExists = AssetDatabase.LoadAssetAtPath<SkillData>(skillPath) != null;
        bool variantExists = AssetDatabase.LoadAssetAtPath<UnitData>(variantPath) != null;

        if (skillExists || variantExists)
        {
            string existingLabel = skillExists && variantExists
                ? "the skill and variant"
                : skillExists ? "the skill" : "the variant";
            if (!EditorUtility.DisplayDialog("Overwrite Assets",
                $"Already exists: {existingLabel}.\n\nOverwrite?", "Yes, Overwrite", "Cancel"))
                return;
        }

        EnsureFolderExists(skillPath);
        EnsureFolderExists(variantPath);

        SkillData finalSkill = AssetDatabase.LoadAssetAtPath<SkillData>(skillPath);
        bool isNewSkill = finalSkill == null;
        if (isNewSkill)
            finalSkill = ScriptableObject.CreateInstance<SkillData>();

        CreatureVariantBuilder.ConfigureSkillData(finalSkill, state, skillAssetName, skillName, SkillCompositionState.Official, config);

        if (isNewSkill)
            AssetDatabase.CreateAsset(finalSkill, skillPath);
        else
            EditorUtility.SetDirty(finalSkill);

        UnitData finalUnit = AssetDatabase.LoadAssetAtPath<UnitData>(variantPath);
        bool isNewUnit = finalUnit == null;
        if (isNewUnit)
            finalUnit = ScriptableObject.CreateInstance<UnitData>();

        CreatureVariantBuilder.ConfigureUnitData(finalUnit, state, finalSkill, prefab);

        if (isNewUnit)
            AssetDatabase.CreateAsset(finalUnit, variantPath);
        else
            EditorUtility.SetDirty(finalUnit);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Creature Variant Lab] Saved variant: '{variantAssetName}' and skill: '{skillAssetName}'.");
        EditorUtility.DisplayDialog("Save Complete",
            $"Variant saved:\n\nUnit: {variantPath}\nSkill: {skillPath}", "OK");
    }

    public static UnitData FindMatchingVariant(CreatureVariantLabState state, CreatureVariantLabConfig config)
    {
        string[] guids = AssetDatabase.FindAssets("t:UnitData", new[] { config.productionVariantFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/_LabTemp/") || path.Contains("/_Excluded/"))
                continue;

            UnitData ud = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (ud == null || ud.faction != state.faction || ud.role != state.role)
                continue;
            if (ud.skill == null)
                continue;

            if (!CreatureVariantBuilder.VariantMatchesComposition(ud, state, config))
                continue;

            return ud;
        }
        return null;
    }

    public static int ClearLabTempAssets(CreatureVariantLabConfig config)
    {
        string tempFolder = config.tempAssetFolder;
        if (!AssetDatabase.IsValidFolder(tempFolder))
            return 0;

        int deletedCount = 0;
        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { tempFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && path != tempFolder)
            {
                AssetDatabase.DeleteAsset(path);
                deletedCount++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return deletedCount;
    }

    public static int GetLabTempAssetCount(CreatureVariantLabConfig config)
    {
        string tempFolder = config.tempAssetFolder;
        if (!AssetDatabase.IsValidFolder(tempFolder))
            return 0;

        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { tempFolder });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                count++;
        }
        return count;
    }

    public static string GetSkillNameValidationError(string name)
    {
        return CreatureVariantNameSanitizer.GetAssetNameValidationError(name);
    }

    public static void LoadStatusDefinitions(List<StatusEffectDefinition> definitions, List<string> names)
    {
        definitions.Clear();
        names.Clear();
        names.Add("None");

        string[] guids = AssetDatabase.FindAssets("t:StatusEffectDefinition");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sd = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
            if (sd != null)
            {
                definitions.Add(sd);
                names.Add(sd.name);
            }
        }
    }

    public static void EnsureFolderExists(string path)
    {
        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        if (string.IsNullOrEmpty(dir) || Directory.Exists(dir))
            return;

        string[] parts = dir.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!Directory.Exists(next) && !AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    public static void EnsureDefaultPresets(CreatureVariantLabConfig config)
    {
        if (config == null) return;

        var missing = CreatureVariantLabConfigValidator.GetMissingPresets(config);
        if (missing.Count == 0) return;

        var list = new List<CreatureVariantLabConfig.FactionRolePreset>();
        if (config.presets != null)
        {
            foreach (var p in config.presets)
                if (p != null) list.Add(p);
        }

        foreach (var key in missing)
            list.Add(CreatureVariantLabConfig.GetDefaultPreset(key.faction, key.role));

        config.presets = list.ToArray();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Creature Variant Lab] Added {missing.Count} missing default preset(s).");
    }

    public static void RepairInvalidPresetDefaults(CreatureVariantLabConfig config)
    {
        if (config == null || config.presets == null) return;

        int repaired = 0;
        for (int i = 0; i < config.presets.Length; i++)
        {
            var p = config.presets[i];
            if (p == null) continue;

            var defaults = CreatureVariantLabConfig.GetDefaultPreset(p.faction, p.role);

            if (p.defaultMaxHealth <= 0) { p.defaultMaxHealth = defaults.defaultMaxHealth; repaired++; }
            if (p.defaultAttackCooldown <= 0f) { p.defaultAttackCooldown = defaults.defaultAttackCooldown; repaired++; }
            if (p.defaultAttackRangeInCells <= 0) { p.defaultAttackRangeInCells = defaults.defaultAttackRangeInCells; repaired++; }
            if (p.defaultPreferredDistanceInCells <= 0) { p.defaultPreferredDistanceInCells = defaults.defaultPreferredDistanceInCells; repaired++; }
            if (p.defaultMoveSpeed <= 0f) { p.defaultMoveSpeed = defaults.defaultMoveSpeed; repaired++; }
            if (p.defaultVisionRange <= 0f) { p.defaultVisionRange = defaults.defaultVisionRange; repaired++; }
            if (p.defaultAccuracy < 0f || p.defaultAccuracy > 1f) { p.defaultAccuracy = defaults.defaultAccuracy; repaired++; }
            if (p.defaultEvasion < 0f || p.defaultEvasion > 1f) { p.defaultEvasion = defaults.defaultEvasion; repaired++; }
            if (p.defaultAttackDamage < 0) { p.defaultAttackDamage = defaults.defaultAttackDamage; repaired++; }
            if (p.defaultDefense < 0) { p.defaultDefense = defaults.defaultDefense; repaired++; }
        }

        if (repaired > 0)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Creature Variant Lab] Repaired {repaired} invalid preset value(s).");
        }
    }

    public static void RegenerateAllDefaultPresets(CreatureVariantLabConfig config)
    {
        if (config == null) return;

        var list = new List<CreatureVariantLabConfig.FactionRolePreset>();
        foreach (var key in CreatureVariantLabConfig.RequiredPresets)
        {
            var existing = config.GetPreset(key.faction, key.role);
            var preset = existing ?? CreatureVariantLabConfig.GetDefaultPreset(key.faction, key.role);

            var defaults = CreatureVariantLabConfig.GetDefaultPreset(key.faction, key.role);
            preset.defaultMaxHealth = defaults.defaultMaxHealth;
            preset.defaultAttackDamage = defaults.defaultAttackDamage;
            preset.defaultAttackCooldown = defaults.defaultAttackCooldown;
            preset.defaultAttackRangeInCells = defaults.defaultAttackRangeInCells;
            preset.defaultPreferredDistanceInCells = defaults.defaultPreferredDistanceInCells;
            preset.defaultAccuracy = defaults.defaultAccuracy;
            preset.defaultEvasion = defaults.defaultEvasion;
            preset.defaultDefense = defaults.defaultDefense;
            preset.defaultMoveSpeed = defaults.defaultMoveSpeed;
            preset.defaultVisionRange = defaults.defaultVisionRange;
            preset.defaultCombatStyle = defaults.defaultCombatStyle;
            preset.defaultTargetingMode = defaults.defaultTargetingMode;
            preset.defaultManaCostToRecruit = defaults.defaultManaCostToRecruit;
            preset.defaultManaCostToAbsorbSoul = defaults.defaultManaCostToAbsorbSoul;

            list.Add(preset);
        }

        config.presets = list.ToArray();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("[Creature Variant Lab] Regenerated all default presets.");
    }
}
