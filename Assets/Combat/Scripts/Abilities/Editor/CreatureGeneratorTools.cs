using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreatureGeneratorTools
{
    private const string HumanDPSParentFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/DPS";
    private const string HumanTankParentFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Tank";
    private const string HumanSupportParentFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/Human/Support";

    private const string OrcDPSParentFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/DPS";
    private const string OrcTankParentFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Tank";
    private const string OrcSupportParentFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/Orc/Support";

    private const string HumanSlayerPath = "Assets/Core/Data/Scriptable Objects/Allies/HumanSlayerData.asset";
    private const string HumanBrutePath = "Assets/Core/Data/Scriptable Objects/Allies/HumanBruteData.asset";
    private const string HumanShamanPath = "Assets/Core/Data/Scriptable Objects/Allies/HumanShamanData.asset";

    private const string OrcSlayerPath = "Assets/Core/Data/Scriptable Objects/Allies/OrcSlayerData.asset";
    private const string OrcBrutePath = "Assets/Core/Data/Scriptable Objects/Allies/OrcBruteData.asset";
    private const string OrcShamanPath = "Assets/Core/Data/Scriptable Objects/Allies/OrcShamanData.asset";

    internal static void GenerateSkillVariants()
    {
        var report = new StringBuilder();
        report.AppendLine("=== HUMAN-ORC SKILL VARIANTS GENERATION REPORT ===");
        report.AppendLine();

        // 1. Load base templates
        UnitData humanSlayer = AssetDatabase.LoadAssetAtPath<UnitData>(HumanSlayerPath);
        UnitData humanBrute = AssetDatabase.LoadAssetAtPath<UnitData>(HumanBrutePath);
        UnitData humanShaman = AssetDatabase.LoadAssetAtPath<UnitData>(HumanShamanPath);

        UnitData orcSlayer = AssetDatabase.LoadAssetAtPath<UnitData>(OrcSlayerPath);
        UnitData orcBrute = AssetDatabase.LoadAssetAtPath<UnitData>(OrcBrutePath);
        UnitData orcShaman = AssetDatabase.LoadAssetAtPath<UnitData>(OrcShamanPath);

        if (humanSlayer == null || humanBrute == null || humanShaman == null ||
            orcSlayer == null || orcBrute == null || orcShaman == null)
        {
            EditorUtility.DisplayDialog("Generation Aborted", "Could not load one or more base UnitData templates. Check console.", "OK");
            Debug.LogError("[Generation] Base UnitData templates missing!");
            return;
        }

        // Ensure folders exist
        EnsureFolderExists(HumanDPSParentFolder);
        EnsureFolderExists(HumanTankParentFolder);
        EnsureFolderExists(HumanSupportParentFolder);
        EnsureFolderExists(OrcDPSParentFolder);
        EnsureFolderExists(OrcTankParentFolder);
        EnsureFolderExists(OrcSupportParentFolder);

        // Scan SkillData by folders
        string[] dpsGuids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Core/Data/Scriptable Objects/Combat/Skills/Role_DPS" });
        string[] tankGuids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Core/Data/Scriptable Objects/Combat/Skills/Role_Tank" });
        string[] supportGuids = AssetDatabase.FindAssets("t:SkillData", new[] { "Assets/Core/Data/Scriptable Objects/Combat/Skills/Role_Support" });

        var dpsSkills = ProcessSkills(dpsGuids, UnitRole.DPS, report);
        var tankSkills = ProcessSkills(tankGuids, UnitRole.Tank, report);
        var supportSkills = ProcessSkills(supportGuids, UnitRole.Support, report);

        int createdCount = 0;
        int reusedCount = 0;
        int errorCount = 0;
        int excludedCount = 0;

        report.AppendLine("=== UNIT DATA GENERATION ===");
        report.AppendLine();

        // Safe Clean Step: Move Obsolete variants (e.g. skills now excluded under new criteria) to Generated/_Excluded folder
        ExcludeObsoleteVariants(dpsSkills, HumanDPSParentFolder, "Human", "DPS", report, ref excludedCount);
        ExcludeObsoleteVariants(tankSkills, HumanTankParentFolder, "Human", "Tank", report, ref excludedCount);
        ExcludeObsoleteVariants(supportSkills, HumanSupportParentFolder, "Human", "Support", report, ref excludedCount);

        ExcludeObsoleteVariants(dpsSkills, OrcDPSParentFolder, "Orc", "DPS", report, ref excludedCount);
        ExcludeObsoleteVariants(tankSkills, OrcTankParentFolder, "Orc", "Tank", report, ref excludedCount);
        ExcludeObsoleteVariants(supportSkills, OrcSupportParentFolder, "Orc", "Support", report, ref excludedCount);

        // Generate Humans
        GenerateFactionVariants(dpsSkills, humanSlayer, HumanDPSParentFolder, "Human", "DPS", ref createdCount, ref reusedCount, ref errorCount, report);
        GenerateFactionVariants(tankSkills, humanBrute, HumanTankParentFolder, "Human", "Tank", ref createdCount, ref reusedCount, ref errorCount, report);
        GenerateFactionVariants(supportSkills, humanShaman, HumanSupportParentFolder, "Human", "Support", ref createdCount, ref reusedCount, ref errorCount, report);

        // Generate Orcs
        GenerateFactionVariants(dpsSkills, orcSlayer, OrcDPSParentFolder, "Orc", "DPS", ref createdCount, ref reusedCount, ref errorCount, report);
        GenerateFactionVariants(tankSkills, orcBrute, OrcTankParentFolder, "Orc", "Tank", ref createdCount, ref reusedCount, ref errorCount, report);
        GenerateFactionVariants(supportSkills, orcShaman, OrcSupportParentFolder, "Orc", "Support", ref createdCount, ref reusedCount, ref errorCount, report);

        report.AppendLine();
        report.AppendLine("=== SUMMARY ===");
        report.AppendLine($"UnitData Created: {createdCount}");
        report.AppendLine($"UnitData Reused: {reusedCount}");
        report.AppendLine($"UnitData Excluded/Moved: {excludedCount}");
        report.AppendLine($"UnitData Errors: {errorCount}");

        string finalReport = report.ToString();
        Debug.Log(finalReport);

        string logPath = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "creatures_generation_report.txt");
        try
        {
            System.IO.File.WriteAllText(logPath, finalReport);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Could not save generation report to {logPath}: {ex.Message}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Generation Complete", $"Generated variants successfully.\nCreated: {createdCount}\nReused: {reusedCount}\nExcluded/Moved: {excludedCount}\nErrors: {errorCount}\nReport saved to {logPath}", "OK");
    }

    internal static void ValidateSkillVariants()
    {
        var report = new StringBuilder();
        report.AppendLine("=== HUMAN-ORC SKILL VARIANTS VALIDATION REPORT ===");
        report.AppendLine();

        // Check if original templates were modified
        UnitData humanSlayer = AssetDatabase.LoadAssetAtPath<UnitData>(HumanSlayerPath);
        UnitData humanBrute = AssetDatabase.LoadAssetAtPath<UnitData>(HumanBrutePath);
        UnitData humanShaman = AssetDatabase.LoadAssetAtPath<UnitData>(HumanShamanPath);
        UnitData orcSlayer = AssetDatabase.LoadAssetAtPath<UnitData>(OrcSlayerPath);
        UnitData orcBrute = AssetDatabase.LoadAssetAtPath<UnitData>(OrcBrutePath);
        UnitData orcShaman = AssetDatabase.LoadAssetAtPath<UnitData>(OrcShamanPath);

        bool baseModified = false;
        if (humanSlayer != null && humanSlayer.skill != null) { report.AppendLine($"[Warning] Base HumanSlayerData has skill assigned: {humanSlayer.skill.name}"); baseModified = true; }
        if (humanBrute != null && humanBrute.skill != null) { report.AppendLine($"[Warning] Base HumanBruteData has skill assigned: {humanBrute.skill.name}"); baseModified = true; }
        if (humanShaman != null && humanShaman.skill != null) { report.AppendLine($"[Warning] Base HumanShamanData has skill assigned: {humanShaman.skill.name}"); baseModified = true; }
        if (orcSlayer != null && orcSlayer.skill != null) { report.AppendLine($"[Warning] Base OrcSlayerData has skill assigned: {orcSlayer.skill.name}"); baseModified = true; }
        if (orcBrute != null && orcBrute.skill != null) { report.AppendLine($"[Warning] Base OrcBruteData has skill assigned: {orcBrute.skill.name}"); baseModified = true; }
        if (orcShaman != null && orcShaman.skill != null) { report.AppendLine($"[Warning] Base OrcShamanData has skill assigned: {orcShaman.skill.name}"); baseModified = true; }

        if (!baseModified)
        {
            report.AppendLine("Base UnitData templates are unmodified (skills are empty). OK.");
        }

        string[] generatedGuids = AssetDatabase.FindAssets("t:UnitData", new[] {
            "Assets/Core/Data/Scriptable Objects/Creatures/Generated"
        });

        int validCount = 0;
        int invalidCount = 0;
        var factionRoleCounts = new Dictionary<string, int>();

        foreach (var guid in generatedGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Ignore variants in the _Excluded subfolder
            if (path.Contains("/_Excluded/")) continue;

            UnitData ud = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (ud == null) continue;

            bool isValid = true;
            var errors = new List<string>();

            // 1. Points to correct prefab
            if (ud.unitPrefab == null)
            {
                errors.Add("unitPrefab is null");
                isValid = false;
            }
            else
            {
                string prefabName = ud.unitPrefab.name;
                string expectedPrefabName = $"{ud.faction}_{ud.role}";
                if (prefabName != expectedPrefabName)
                {
                    errors.Add($"unitPrefab name '{prefabName}' does not match expected '{expectedPrefabName}'");
                    isValid = false;
                }
            }

            // 2. Skill matches role and is valid
            if (ud.skill == null)
            {
                errors.Add("skill is null");
                isValid = false;
            }
            else
            {
                // Verify skill role
                string skillPath = AssetDatabase.GetAssetPath(ud.skill);
                string expectedFolder = $"Role_{ud.role}";
                if (!skillPath.Contains(expectedFolder))
                {
                    errors.Add($"skill folder '{skillPath}' does not match expected role '{expectedFolder}'");
                    isValid = false;
                }

                // Verify composition state
                if (ud.skill.CompositionState == SkillCompositionState.DebugOnly ||
                    ud.skill.CompositionState == SkillCompositionState.NonOfficial ||
                    ud.skill.CompositionState == SkillCompositionState.RequiresDesignDecision)
                {
                    errors.Add($"skill '{ud.skill.name}' has non-production state '{ud.skill.CompositionState}'");
                    isValid = false;
                }

                // Verify no summon
                if (ud.skill.CompositionEffects != null)
                {
                    foreach (var eff in ud.skill.CompositionEffects)
                    {
                        if (eff.EffectKind == SkillEffectKind.Summon)
                        {
                            errors.Add($"skill '{ud.skill.name}' contains Summon effect (SummonNotProductionReady)");
                            isValid = false;
                        }
                    }
                }

                // Verify specific forbidden skills
                if (ud.skill.name == "Support_EnemyDebuffDefense")
                {
                    errors.Add("skill 'Support_EnemyDebuffDefense' is forbidden in real variants");
                    isValid = false;
                }

                // Verify advanced/debug criteria
                if (ud.skill.ImpactPattern == ImpactPattern.MultiTarget)
                {
                    errors.Add($"skill '{ud.skill.name}' is MultiTarget (MultiTargetNotProductionReady)");
                    isValid = false;
                }

                bool hasBounce = false;
                bool hasExplosive = false;
                int physicalModCount = 0;
                if (ud.skill.CompositionModifierKinds != null)
                {
                    foreach (var mod in ud.skill.CompositionModifierKinds)
                    {
                        if (mod == SkillModifierKind.Bounce) hasBounce = true;
                        if (mod == SkillModifierKind.Explosive) hasExplosive = true;

                        if (mod == SkillModifierKind.Penetrating ||
                            mod == SkillModifierKind.Bounce ||
                            mod == SkillModifierKind.Explosive ||
                            mod == SkillModifierKind.Splash ||
                            mod == SkillModifierKind.Expandable)
                        {
                            physicalModCount++;
                        }
                    }
                }

                if (hasBounce || hasExplosive)
                {
                    errors.Add($"skill '{ud.skill.name}' has Bounce/Explosive modifier (ModifierNotProductionReady)");
                    isValid = false;
                }

                if (physicalModCount > 1)
                {
                    errors.Add($"skill '{ud.skill.name}' has multiple physical modifiers (MultiModifierNotProductionReady)");
                    isValid = false;
                }
            }

            string key = $"{ud.faction}_{ud.role}";
            if (!factionRoleCounts.ContainsKey(key))
                factionRoleCounts[key] = 0;

            if (isValid)
            {
                validCount++;
                factionRoleCounts[key]++;
            }
            else
            {
                invalidCount++;
                report.AppendLine($"[ERROR] Variant '{ud.name}' is invalid:\n  - {string.Join("\n  - ", errors)}");
            }
        }

        report.AppendLine();
        report.AppendLine("=== COMBINATION CHECKS ===");
        string[] combinations = new[] { "Human_DPS", "Human_Tank", "Human_Support", "Orc_DPS", "Orc_Tank", "Orc_Support" };
        foreach (var c in combinations)
        {
            int count = factionRoleCounts.ContainsKey(c) ? factionRoleCounts[c] : 0;
            report.AppendLine($"Combination '{c}': {count} valid variants.");
            if (count == 0)
            {
                report.AppendLine($"[ERROR] Combination '{c}' has 0 valid variants!");
            }
        }

        report.AppendLine();
        report.AppendLine("=== VALIDATION SUMMARY ===");
        report.AppendLine($"Valid Variants: {validCount}");
        report.AppendLine($"Invalid Variants: {invalidCount}");

        string finalReport = report.ToString();
        Debug.Log(finalReport);

        string logPath = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "creatures_validation_report.txt");
        try
        {
            System.IO.File.WriteAllText(logPath, finalReport);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Could not save validation report to {logPath}: {ex.Message}");
        }

        EditorUtility.DisplayDialog("Validation Complete", $"Validation finished.\nValid: {validCount}\nInvalid: {invalidCount}\nReport saved to {logPath}", "OK");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static List<SkillData> ProcessSkills(string[] guids, UnitRole role, StringBuilder report)
    {
        var result = new List<SkillData>();
        report.AppendLine($"--- Filtering skills for {role} ---");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null) continue;

            // 1. Verify state
            if (skill.CompositionState != SkillCompositionState.Official)
            {
                report.AppendLine($"  [Excluded] '{skill.name}': State is {skill.CompositionState} (only Official allowed)");
                continue;
            }

            // 2. Validate skill (reusing official validation logic, ignoring non-critical warnings Rule 7 and Rule 8)
            var violations = SkillCompositionEditorTools.ValidateSkill(skill);
            bool hasCriticalViolation = false;
            foreach (var violation in violations)
            {
                if (violation.RuleNumber != 7 && violation.RuleNumber != 8)
                {
                    hasCriticalViolation = true;
                    report.AppendLine($"  [Excluded] '{skill.name}': Critical violation Rule {violation.RuleNumber} - {violation.Message}");
                }
            }
            if (hasCriticalViolation)
            {
                continue;
            }

            // 3. Verify runtime readiness (Shield duration check and Status definition check)
            bool isRuntimeReady = true;
            if (skill.CompositionEffects != null)
            {
                foreach (var compEff in skill.CompositionEffects)
                {
                    if (compEff.EffectKind == SkillEffectKind.Shield)
                    {
                        if (compEff.Duration <= 0f)
                        {
                            report.AppendLine($"  [Excluded] '{skill.name}': Shield effect has Duration <= 0 (Not Runtime Ready)");
                            isRuntimeReady = false;
                            break;
                        }
                    }
                    else if (compEff.EffectKind == SkillEffectKind.Slow ||
                             compEff.EffectKind == SkillEffectKind.Stun ||
                             compEff.EffectKind == SkillEffectKind.Haste ||
                             compEff.EffectKind == SkillEffectKind.StrengthBuff ||
                             compEff.EffectKind == SkillEffectKind.PoisonBurn)
                    {
                        if (compEff.StatusDefinition == null)
                        {
                            report.AppendLine($"  [Excluded] '{skill.name}': Status effect is missing StatusEffectDefinition (Not Runtime Ready)");
                            isRuntimeReady = false;
                            break;
                        }
                    }
                }
            }
            if (!isRuntimeReady)
            {
                continue;
            }

            // 4. Exclude Summon effect
            bool hasSummon = false;
            if (skill.CompositionEffects != null)
            {
                foreach (var eff in skill.CompositionEffects)
                {
                    if (eff.EffectKind == SkillEffectKind.Summon)
                    {
                        hasSummon = true;
                        break;
                    }
                }
            }
            if (hasSummon)
            {
                report.AppendLine($"  [Excluded] '{skill.name}': SummonNotProductionReady");
                continue;
            }

            // 5. Verify specific forbidden names
            if (skill.name == "Support_EnemyDebuffDefense")
            {
                report.AppendLine($"  [Excluded] '{skill.name}': Explicitly forbidden debuff skill");
                continue;
            }

            // 6. Exclude MultiTarget patterns
            if (skill.ImpactPattern == ImpactPattern.MultiTarget)
            {
                report.AppendLine($"  [Excluded] '{skill.name}': MultiTargetNotProductionReady");
                continue;
            }

            // 7. Exclude Bounce / Explosive modifiers and multi-modifiers
            bool hasBounce = false;
            bool hasExplosive = false;
            int physicalModCount = 0;
            if (skill.CompositionModifierKinds != null)
            {
                foreach (var mod in skill.CompositionModifierKinds)
                {
                    if (mod == SkillModifierKind.Bounce) hasBounce = true;
                    if (mod == SkillModifierKind.Explosive) hasExplosive = true;

                    if (mod == SkillModifierKind.Penetrating ||
                        mod == SkillModifierKind.Bounce ||
                        mod == SkillModifierKind.Explosive ||
                        mod == SkillModifierKind.Splash ||
                        mod == SkillModifierKind.Expandable)
                    {
                        physicalModCount++;
                    }
                }
            }

            if (hasBounce || hasExplosive)
            {
                report.AppendLine($"  [Excluded] '{skill.name}': ModifierNotProductionReady");
                continue;
            }

            if (physicalModCount > 1)
            {
                report.AppendLine($"  [Excluded] '{skill.name}': MultiModifierNotProductionReady");
                continue;
            }

            // 8. Verify impact pattern vs role compatibility
            bool patternValid = false;
            switch (role)
            {
                case UnitRole.DPS:
                    // DPS: Direct (0) / Line (2) (MultiTarget is already excluded)
                    patternValid = (skill.ImpactPattern == ImpactPattern.Direct ||
                                    skill.ImpactPattern == ImpactPattern.Line);
                    break;
                case UnitRole.Tank:
                    // Tank: Area (1) (MultiTarget is already excluded)
                    patternValid = (skill.ImpactPattern == ImpactPattern.Area);
                    break;
                case UnitRole.Support:
                    // Support: Direct (0) / Area (1)
                    patternValid = (skill.ImpactPattern == ImpactPattern.Direct ||
                                    skill.ImpactPattern == ImpactPattern.Area);
                    break;
            }

            if (!patternValid)
            {
                report.AppendLine($"  [Excluded] '{skill.name}': Impact pattern {skill.ImpactPattern} is not compatible with role {role} (AdvancedPatternNotProductionReady)");
                continue;
            }

            report.AppendLine($"  [Included] '{skill.name}' (ImpactPattern: {skill.ImpactPattern})");
            result.Add(skill);
        }

        report.AppendLine();
        return result;
    }

    private static void ExcludeObsoleteVariants(
        List<SkillData> validSkills,
        string folderPath,
        string faction,
        string role,
        StringBuilder report,
        ref int excludedCount)
    {
        if (!AssetDatabase.IsValidFolder(folderPath)) return;

        string[] guids = AssetDatabase.FindAssets("t:UnitData", new[] { folderPath });
        var validSkillGuids = new HashSet<string>();
        foreach (var s in validSkills)
        {
            if (s != null)
            {
                string sPath = AssetDatabase.GetAssetPath(s);
                string sGuid = AssetDatabase.AssetPathToGUID(sPath);
                validSkillGuids.Add(sGuid);
            }
        }

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitData ud = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (ud == null) continue;

            // Make sure the path matches the folder exactly (not subfolders)
            string directory = Path.GetDirectoryName(path).Replace('\\', '/');
            if (directory != folderPath) continue;

            bool isObsolete = false;
            if (ud.skill == null)
            {
                isObsolete = true;
            }
            else
            {
                string skillPath = AssetDatabase.GetAssetPath(ud.skill);
                string skillGuid = AssetDatabase.AssetPathToGUID(skillPath);
                if (!validSkillGuids.Contains(skillGuid))
                {
                    isObsolete = true;
                }
            }

            if (isObsolete)
            {
                string assetName = Path.GetFileName(path);
                string destFolder = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/_Excluded";
                EnsureFolderExists(destFolder);
                string destPath = $"{destFolder}/{assetName}";

                if (AssetDatabase.LoadAssetAtPath<UnitData>(destPath) != null)
                {
                    AssetDatabase.DeleteAsset(destPath);
                }

                string error = AssetDatabase.MoveAsset(path, destPath);
                if (string.IsNullOrEmpty(error))
                {
                    report.AppendLine($"  [Excluded Obsolete] Moved '{assetName}' to {destFolder} (Skill was excluded or null)");
                    excludedCount++;
                }
                else
                {
                    Debug.LogError($"[Generation] Failed to move obsolete variant '{path}': {error}");
                    report.AppendLine($"  [Error] Failed to move obsolete variant '{assetName}': {error}");
                }
            }
        }
    }

    private static void GenerateFactionVariants(
        List<SkillData> skills,
        UnitData template,
        string folderPath,
        string faction,
        string role,
        ref int created,
        ref int reused,
        ref int errors,
        StringBuilder report)
    {
        foreach (var skill in skills)
        {
            string skillName = skill.name;
            string rolePrefix = $"{role}_";
            if (skillName.StartsWith(rolePrefix))
            {
                skillName = skillName.Substring(rolePrefix.Length);
            }
            string assetName = $"{faction}_{role}_{skillName}.asset";
            string fullPath = $"{folderPath}/{assetName}";

            // Check if already exists
            UnitData existing = AssetDatabase.LoadAssetAtPath<UnitData>(fullPath);
            if (existing != null)
            {
                // Check if it already matches
                if (existing.skill == skill &&
                    existing.unitPrefab == template.unitPrefab &&
                    existing.team == template.team &&
                    existing.role == template.role &&
                    existing.combatStyle == template.combatStyle &&
                    existing.targetingMode == template.targetingMode &&
                    existing.faction == template.faction &&
                    existing.sprite == template.sprite &&
                    existing.tileSize == template.tileSize &&
                    existing.isFusion == template.isFusion &&
                    existing.unitId == $"{template.unitId}_{skill.name}" &&
                    existing.displayName == $"{template.displayName} ({skill.DisplayName})" &&
                    CompareStats(existing.stats, template.stats))
                {
                    report.AppendLine($"  [Reused] '{assetName}' (already exists and matches)");
                    reused++;
                    continue;
                }

                // If differs, overwrite it (safe refresh in batchmode)
                bool overwrite = true;
                if (!Application.isBatchMode)
                {
                    overwrite = EditorUtility.DisplayDialog("Overwrite Variant?",
                        $"Variant '{assetName}' already exists but differs from base template.\n\nDo you want to overwrite it?",
                        "Yes, Overwrite", "No, Skip");
                }
                if (!overwrite)
                {
                    report.AppendLine($"  [Skipped] '{assetName}' (user declined overwrite)");
                    continue;
                }
            }

            // Create new instance
            UnitData newAsset = ScriptableObject.CreateInstance<UnitData>();
            newAsset.unitPrefab = template.unitPrefab;
            newAsset.team = template.team;
            newAsset.role = template.role;
            newAsset.combatStyle = template.combatStyle;
            newAsset.targetingMode = template.targetingMode;
            newAsset.faction = template.faction;
            newAsset.sprite = template.sprite;
            newAsset.tileSize = template.tileSize;
            newAsset.softCurrencyRewardOnSoulAbsorb = template.softCurrencyRewardOnSoulAbsorb;
            newAsset.manaCostToRecruit = template.manaCostToRecruit;
            newAsset.manaCostToAbsorbSoul = template.manaCostToAbsorbSoul;
            newAsset.isFusion = template.isFusion;

            newAsset.stats = new UnitStatsData
            {
                maxHealth = template.stats.maxHealth,
                attackDamage = template.stats.attackDamage,
                attackCooldown = template.stats.attackCooldown,
                attackRangeInCells = template.stats.attackRangeInCells,
                preferredDistanceInCells = template.stats.preferredDistanceInCells,
                accuracy = template.stats.accuracy,
                evasion = template.stats.evasion,
                defense = template.stats.defense,
                moveSpeed = template.stats.moveSpeed,
                visionRange = template.stats.visionRange
            };

            newAsset.unitId = $"{template.unitId}_{skill.name}";
            newAsset.displayName = $"{template.displayName} ({skill.DisplayName})";
            newAsset.skill = skill;

            try
            {
                if (existing != null)
                {
                    // Overwrite
                    EditorUtility.CopySerializedManagedFieldsOnly(newAsset, existing);
                    EditorUtility.SetDirty(existing);
                    report.AppendLine($"  [Overwritten] '{assetName}'");
                }
                else
                {
                    // Create
                    AssetDatabase.CreateAsset(newAsset, fullPath);
                    report.AppendLine($"  [Created] '{assetName}'");
                }
                created++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Generation] Failed to save {assetName}: {ex.Message}");
                report.AppendLine($"  [Error] Failed to save '{assetName}': {ex.Message}");
                errors++;
            }
        }
    }

    private static bool CompareStats(UnitStatsData a, UnitStatsData b)
    {
        if (a == null || b == null) return false;
        return (a.maxHealth == b.maxHealth &&
                a.attackDamage == b.attackDamage &&
                Mathf.Approximately(a.attackCooldown, b.attackCooldown) &&
                a.attackRangeInCells == b.attackRangeInCells &&
                a.preferredDistanceInCells == b.preferredDistanceInCells &&
                Mathf.Approximately(a.accuracy, b.accuracy) &&
                Mathf.Approximately(a.evasion, b.evasion) &&
                a.defense == b.defense &&
                Mathf.Approximately(a.moveSpeed, b.moveSpeed) &&
                Mathf.Approximately(a.visionRange, b.visionRange));
    }
}


