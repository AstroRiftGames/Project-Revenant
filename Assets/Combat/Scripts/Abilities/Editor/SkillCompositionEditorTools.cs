using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SkillCompositionEditorTools
{


    public struct ValidationViolation
    {
        public int RuleNumber;
        public string Message;
    }

    [MenuItem("Tools/Skills/Validate Composition Metadata")]
    public static void ValidateComposition()
    {
        string[] skillGuids = AssetDatabase.FindAssets("t:SkillData");
        var realSkills = new HashSet<SkillData>();
        var skillToCreatures = new Dictionary<SkillData, List<string>>();

        // Find skills used by real playable creatures
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Combat/Prefabs/Creatures/Playable" });
        foreach (var pGuid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(pGuid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null && go.name != "Summoned_MinorMinion_Debug")
            {
                var unit = go.GetComponent<Unit>();
                if (unit != null)
                {
                    var unitDataField = typeof(Unit).GetField("_unitData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var unitData = unitDataField?.GetValue(unit) as UnitData;
                    if (unitData != null && unitData.skill != null)
                    {
                        realSkills.Add(unitData.skill);
                        if (!skillToCreatures.ContainsKey(unitData.skill))
                            skillToCreatures[unitData.skill] = new List<string>();
                        skillToCreatures[unitData.skill].Add(go.name);
                    }
                }
            }
        }

        var report = new StringBuilder();
        report.AppendLine("=== SKILLS COMPOSITION METADATA VALIDATION REPORT ===");
        report.AppendLine();

        int[] rules = new int[11];

        foreach (string guid in skillGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null) continue;

            var violations = ValidateSkill(skill, realSkills, skillToCreatures);
            foreach (var violation in violations)
            {
                report.AppendLine(violation.Message);
                if (violation.RuleNumber >= 1 && violation.RuleNumber <= 10)
                {
                    rules[violation.RuleNumber]++;
                }
            }
        }

        report.AppendLine();
        report.AppendLine("=== SUMMARY ===");
        int criticalMetadataViolations = 0;
        for (int i = 1; i <= 10; i++)
        {
            if (i == 10 || rules[i] > 0 || i == 1 || i == 2 || i == 3 || i == 4 || i == 5 || i == 6 || i == 7 || i == 8 || i == 9)
            {
                report.AppendLine($"Rule {i}: {rules[i]} cases");
            }

            if (i != 7 && i != 8)
                criticalMetadataViolations += rules[i];
        }
        report.AppendLine($"Critical Metadata Violations: {criticalMetadataViolations}");

        string finalReport = report.ToString();
        Debug.Log(finalReport);
        
        string logPath = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "skills_validation_report.txt");
        try
        {
            System.IO.File.WriteAllText(logPath, finalReport);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Could not save validation report to {logPath}: {ex.Message}");
        }
        
        EditorUtility.DisplayDialog("Validation Complete", $"Validation finished. Details printed to Console and saved to: {logPath}", "OK");
    }

    public static List<ValidationViolation> ValidateSkill(SkillData skill, HashSet<SkillData> realSkills = null, Dictionary<SkillData, List<string>> skillToCreatures = null)
    {
        if (realSkills == null || skillToCreatures == null)
        {
            realSkills = new HashSet<SkillData>();
            skillToCreatures = new Dictionary<SkillData, List<string>>();

            // Find skills used by real playable creatures
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Combat/Prefabs/Creatures/Playable" });
            foreach (var pGuid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(pGuid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.name != "Summoned_MinorMinion_Debug")
                {
                    var unit = go.GetComponent<Unit>();
                    if (unit != null)
                    {
                        var unitDataField = typeof(Unit).GetField("_unitData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var unitData = unitDataField?.GetValue(unit) as UnitData;
                        if (unitData != null && unitData.skill != null)
                        {
                            realSkills.Add(unitData.skill);
                            if (!skillToCreatures.ContainsKey(unitData.skill))
                                skillToCreatures[unitData.skill] = new List<string>();
                            if (!skillToCreatures[unitData.skill].Contains(go.name))
                                skillToCreatures[unitData.skill].Add(go.name);
                        }
                    }
                }
            }
        }

        var errors = new List<ValidationViolation>();
        bool isReal = realSkills.Contains(skill);
        string creatureInfo = isReal ? $" (Used by: {string.Join(", ", skillToCreatures[skill])})" : "";

        // 1. Skills Official sin CompositionEffects
        if (skill.CompositionState == SkillCompositionState.Official && (skill.CompositionEffects == null || skill.CompositionEffects.Length == 0))
        {
            errors.Add(new ValidationViolation { RuleNumber = 1, Message = $"[Rule 1] '{skill.name}' is Official but has no CompositionEffects." });
        }

        // 2. Skills NonOfficial usadas por criaturas reales
        if (isReal && skill.CompositionState == SkillCompositionState.NonOfficial)
        {
            errors.Add(new ValidationViolation { RuleNumber = 2, Message = $"[Rule 2] '{skill.name}' is NonOfficial and used by real creatures{creatureInfo}." });
        }

        // 3. Skills RequiresDesignDecision usadas por criaturas reales
        if (isReal && skill.CompositionState == SkillCompositionState.RequiresDesignDecision)
        {
            errors.Add(new ValidationViolation { RuleNumber = 3, Message = $"[Rule 3] '{skill.name}' is RequiresDesignDecision and used by real creatures{creatureInfo}." });
        }

        // 10. Skills con CompositionState DebugOnly o NonOfficial dentro de datos reales de criaturas
        if (isReal && (skill.CompositionState == SkillCompositionState.DebugOnly || skill.CompositionState == SkillCompositionState.NonOfficial))
        {
            errors.Add(new ValidationViolation { RuleNumber = 10, Message = $"[Rule 10] '{skill.name}' has state {skill.CompositionState} but is used by real creatures{creatureInfo}." });
        }

        // Determine status properties from composition
        bool hasStatusWithDuration = false;
        bool hasPeriodicStatus = false;

        if (skill.CompositionEffects != null)
        {
            foreach (var compEff in skill.CompositionEffects)
            {
                if (compEff.EffectKind == SkillEffectKind.Slow || compEff.EffectKind == SkillEffectKind.Stun ||
                    compEff.EffectKind == SkillEffectKind.Haste || compEff.EffectKind == SkillEffectKind.StrengthBuff ||
                    compEff.EffectKind == SkillEffectKind.PoisonBurn || compEff.EffectKind == SkillEffectKind.Heal ||
                    compEff.EffectKind == SkillEffectKind.Shield || compEff.EffectKind == SkillEffectKind.Taunt ||
                    compEff.EffectKind == SkillEffectKind.Blind)
                {
                    if (compEff.Duration > 0f)
                        hasStatusWithDuration = true;
                    if (compEff.Interval > 0f)
                        hasPeriodicStatus = true;
                }
            }
        }

        // 7. Skills con StatusEffectDefinition persistente pero sin modifier Persistent
        if (hasStatusWithDuration)
        {
            var fullActualModifiers = new List<SkillModifierKind>();
            if (skill.CompositionModifierKinds != null)
                fullActualModifiers.AddRange(skill.CompositionModifierKinds);

            if (!fullActualModifiers.Contains(SkillModifierKind.Persistent))
            {
                if (skill.CompositionState == SkillCompositionState.Official)
                {
                    errors.Add(new ValidationViolation { RuleNumber = 7, Message = $"[Rule 7] '{skill.name}' has persistent status effect but is missing 'Persistent' modifier in composition." });
                }
            }
        }

        // 8. Skills con ticks periódicos pero sin modifier Periodic
        if (hasPeriodicStatus)
        {
            var fullActualModifiers = new List<SkillModifierKind>();
            if (skill.CompositionModifierKinds != null)
                fullActualModifiers.AddRange(skill.CompositionModifierKinds);

            if (!fullActualModifiers.Contains(SkillModifierKind.Periodic))
            {
                if (skill.CompositionState == SkillCompositionState.Official)
                {
                    errors.Add(new ValidationViolation { RuleNumber = 8, Message = $"[Rule 8] '{skill.name}' has periodic ticks status effect but is missing 'Periodic' modifier in composition." });
                }
            }
        }

        return errors;
    }
    // ============================================================
    // COMPOSITION RUNTIME READINESS VALIDATION
    // ============================================================

    [MenuItem("Tools/Skills/Validate Composition Runtime Readiness")]
    public static void ValidateCompositionRuntimeReadiness()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillData");
        var report = new StringBuilder();
        report.AppendLine("=== Composition Runtime Readiness Report ===");
        report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm}");
        report.AppendLine();

        int totalSkills = 0;
        int officialSkills = 0;
        int skillsWithGaps = 0;
        int totalEffectsComposition = 0;
        var effectKindCounts = new Dictionary<SkillEffectKind, int>();
        var modifierKindCounts = new Dictionary<SkillModifierKind, int>();
        var gapEffects = new HashSet<string>();
        var gapModifiers = new HashSet<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (skill == null) continue;

            totalSkills++;
            if (skill.CompositionState == SkillCompositionState.Official)
                officialSkills++;

            report.AppendLine($"--- {skill.name} (State: {skill.CompositionState}) ---");
            report.AppendLine($"  Path: {path}");

            // Count effects
            int compEffectCount = skill.CompositionEffects != null ? skill.CompositionEffects.Length : 0;
            totalEffectsComposition += compEffectCount;

            report.AppendLine($"  Composition Effects: {compEffectCount}");

            if (skill.CompositionState == SkillCompositionState.Official &&
                !SkillCompositionRuntimeExecutor.CanExecuteComposition(skill, out string runtimeGap))
            {
                skillsWithGaps++;
                report.AppendLine($"  GAP [Runtime]: {runtimeGap}");
            }

            // Build lookup for modifier data by kind
            var modifierDataByKind = new Dictionary<SkillModifierKind, SkillCompositionModifierData>();
            if (skill.CompositionModifierData != null)
            {
                foreach (var md in skill.CompositionModifierData)
                {
                    if (!modifierDataByKind.ContainsKey(md.ModifierKind))
                        modifierDataByKind[md.ModifierKind] = md;
                }
            }

            // Track composition effect kinds
            if (skill.CompositionEffects != null)
            {
                var compKinds = new HashSet<SkillEffectKind>();
                foreach (var compEff in skill.CompositionEffects)
                {
                    compKinds.Add(compEff.EffectKind);
                    if (!effectKindCounts.ContainsKey(compEff.EffectKind))
                        effectKindCounts[compEff.EffectKind] = 0;
                    effectKindCounts[compEff.EffectKind]++;
                }

                foreach (var compEff in skill.CompositionEffects)
                {
                    switch (compEff.EffectKind)
                    {
                        case SkillEffectKind.Damage:
                        case SkillEffectKind.Heal:
                            break;

                        case SkillEffectKind.Shield:
                            if (compEff.Duration <= 0f)
                                report.AppendLine($"  GAP [Shield]: Duration is 0, shield will not be applied");
                            break;

                        case SkillEffectKind.Knockback:
                            break;

                        case SkillEffectKind.Summon:
                            if (compEff.SummonedUnit == null)
                                report.AppendLine($"  GAP [Summon]: No SummonedUnit assigned in composition");
                            if (compEff.SummonAnchorMode == SummonAnchorMode.AroundImpactCenter)
                                report.AppendLine($"  OK [Summon]: Using SummonAnchorMode={compEff.SummonAnchorMode}");
                            else
                                report.AppendLine($"  OK [Summon]: SummonAnchorMode={compEff.SummonAnchorMode} preserved");
                            break;

                        case SkillEffectKind.Haste:
                        case SkillEffectKind.StrengthBuff:
                        case SkillEffectKind.Slow:
                        case SkillEffectKind.Stun:
                        case SkillEffectKind.PoisonBurn:
                        case SkillEffectKind.Taunt:
                        case SkillEffectKind.Blind:
                            if (compEff.StatusDefinition != null)
                                report.AppendLine($"  OK [{compEff.EffectKind}]: StatusDefinition '{compEff.StatusDefinition.name}' reference present");
                            else
                            {
                                gapEffects.Add($"Status:{compEff.EffectKind}");
                                report.AppendLine($"  GAP [{compEff.EffectKind}]: No StatusEffectDefinition reference in composition");
                            }
                            break;

                        default:
                            report.AppendLine($"  UNKNOWN effect kind: {compEff.EffectKind}");
                            break;
                    }
                }
            }

            // Track composition modifier kinds
            if (skill.CompositionModifierKinds != null)
            {
                foreach (var modKind in skill.CompositionModifierKinds)
                {
                    if (!modifierKindCounts.ContainsKey(modKind))
                        modifierKindCounts[modKind] = 0;
                    modifierKindCounts[modKind]++;

                    switch (modKind)
                    {
                        case SkillModifierKind.Splash:
                        case SkillModifierKind.Penetrating:
                            break;

                        case SkillModifierKind.Bounce:
                            if (modifierDataByKind.TryGetValue(SkillModifierKind.Bounce, out var bounceData) && bounceData.BounceMaxBounces > 0)
                                report.AppendLine($"  OK [Bounce]: MaxBounces={bounceData.BounceMaxBounces}, Range={bounceData.BounceRangeInCells}, CanBounceAgain={bounceData.BounceCanBounceToPrimaryTargetAgain}");
                            else
                            {
                                gapModifiers.Add("Bounce:MaxBounces,BounceRangeInCells,CanBounceToPrimaryTargetAgain");
                                report.AppendLine($"  GAP [Bounce]: Missing MaxBounces, BounceRangeInCells, CanBounceToPrimaryTargetAgain in composition");
                            }
                            break;

                        case SkillModifierKind.Explosive:
                            if (modifierDataByKind.TryGetValue(SkillModifierKind.Explosive, out var explosiveData) && explosiveData.ExplosiveRadiusInCells > 0)
                                report.AppendLine($"  OK [Explosive]: Radius={explosiveData.ExplosiveRadiusInCells}, IncludePrimary={explosiveData.ExplosiveIncludePrimaryImpactTarget}");
                            else
                            {
                                gapModifiers.Add("Explosive:ExplosionRadiusInCells,IncludePrimaryImpactTarget");
                                report.AppendLine($"  GAP [Explosive]: Missing ExplosionRadiusInCells, IncludePrimaryImpactTarget in composition");
                            }
                            break;

                        case SkillModifierKind.Persistent:
                        case SkillModifierKind.Cumulative:
                        case SkillModifierKind.Periodic:
                        case SkillModifierKind.Expandable:
                            gapModifiers.Add($"{modKind}:NoRuntimeImplementation");
                            report.AppendLine($"  GAP [{modKind}]: Declared in enum but no runtime implementation exists");
                            break;
                    }
                }
            }

            report.AppendLine();
        }

        // Summary
        report.AppendLine("=== SUMMARY ===");
        report.AppendLine($"Total skills examined: {totalSkills}");
        report.AppendLine($"Official skills: {officialSkills}");
        report.AppendLine($"Skills with gaps: {skillsWithGaps}");
        report.AppendLine($"Runtime Gaps: {skillsWithGaps}");
        report.AppendLine($"Total composition effects: {totalEffectsComposition}");
        report.AppendLine();
        report.AppendLine("--- Effect Kind Usage (across all skills) ---");
        foreach (var kvp in effectKindCounts)
            report.AppendLine($"  {kvp.Key}: {kvp.Value}");

        report.AppendLine();
        report.AppendLine("--- Modifier Kind Usage (across all skills) ---");
        foreach (var kvp in modifierKindCounts)
            report.AppendLine($"  {kvp.Key}: {kvp.Value}");

        report.AppendLine();
        report.AppendLine("--- GAPS: Composition data that is missing for full standalone execution ---");
        report.AppendLine("  Effects with gaps:");
        foreach (var gap in gapEffects)
            report.AppendLine($"    - {gap}");

        report.AppendLine("  Modifiers with gaps:");
        foreach (var gap in gapModifiers)
            report.AppendLine($"    - {gap}");

        string reportPath = "Temp/CompositionRuntimeReadinessReport.txt";
        System.IO.File.WriteAllText(reportPath, report.ToString());
        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Composition Runtime Readiness",
            $"Report generated with {skillsWithGaps} official runtime gap(s).\nSee Console and Temp/CompositionRuntimeReadinessReport.txt for details.", "OK");
    }
}
