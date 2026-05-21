using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SkillRuntimeCatalogV2BatchValidator
{
    private const string ScenePath = "Assets/Combat/Testing/Skills/Scenes/SkillRuntimeValidationScene.unity";
    private const string ReportPath = "Logs/skill_v2_validation_report.txt";
    private const string ValidatorPrefix = "[SkillRuntimeCatalogV2BatchValidator]";

    private static readonly string[] CasesToValidate =
    {
        "TestCase_DPS_DirectDamage",
        "TestCase_DPS_LineDamage",
        "TestCase_DPS_PiercingLineDamage",
        "TestCase_Tank_AreaTaunt",
        "TestCase_Tank_AreaDamage",
        "TestCase_Support_SelfHeal",
        "TestCase_Support_AllyHeal",
        "TestCase_Support_EnemyDebuffDefense",
        "TestCase_DPS_DirectSplashDamage",
        "TestCase_DPS_DirectExplosiveDamage",
        "TestCase_DPS_DirectBounceDamage",
        "TestCase_DPS_LinePiercingExplosiveDamage",
        "TestCase_Tank_SpawnMinions",
        "TestCase_Support_SpawnMinions"
    };

    private static readonly List<string> Failures = new();
    private static readonly List<string> Warnings = new();
    private static readonly List<string> Notes = new();
    private static readonly List<string> ImpactNotes = new();
    private static readonly List<string> ConsoleErrors = new();
    private static readonly List<string> ConsoleWarnings = new();

    private static bool _isRunning;
    private static bool _finished;
    private static bool _playModeValidationQueued;
    private static int _playModeFrameCount;
    private static int _updateCount;
    private static bool _enterPlayModeOptionsEnabledBeforeRun;
    private static EnterPlayModeOptions _enterPlayModeOptionsBeforeRun;

    [MenuItem("Tools/Validation/Run Skill Runtime V2 Batch Validation")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        if (_isRunning)
            return;

        _isRunning = true;
        _finished = false;
        _playModeValidationQueued = true;
        _playModeFrameCount = 0;
        _updateCount = 0;

        Failures.Clear();
        Warnings.Clear();
        Notes.Clear();
        ImpactNotes.Clear();
        ConsoleErrors.Clear();
        ConsoleWarnings.Clear();

        Directory.CreateDirectory("Logs");
        CacheEnterPlayModeOptions();
        EnablePlayModeWithoutDomainReload();

        Application.logMessageReceived -= HandleLogMessage;
        Application.logMessageReceived += HandleLogMessage;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EditorApplication.update -= HandleEditorUpdate;
        EditorApplication.update += HandleEditorUpdate;

        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneInEditMode();
        }
        catch (Exception exception)
        {
            RegisterFailure($"Failed to open scene '{ScenePath}': {exception}");
            FinishAndExit();
            return;
        }

        try
        {
            EditorApplication.EnterPlaymode();
        }
        catch (Exception exception)
        {
            RegisterFailure($"Failed to enter Play Mode: {exception}");
            FinishAndExit();
        }
    }

    private static void HandleEditorUpdate()
    {
        if (!_isRunning || _finished)
            return;

        _updateCount++;
        if (_updateCount > 4000)
        {
            RegisterFailure("Validation timed out before completion.");
            FinishAndExit();
            return;
        }

        if (!EditorApplication.isPlaying || !_playModeValidationQueued)
            return;

        _playModeFrameCount++;
        if (_playModeFrameCount < 10)
            return;

        _playModeValidationQueued = false;

        try
        {
            ValidateSceneInPlayMode();
        }
        catch (Exception exception)
        {
            RegisterFailure($"Unexpected validator exception: {exception}");
        }

        FinishAndExit();
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange change)
    {
        if (!_isRunning)
            return;

        if (change == PlayModeStateChange.EnteredPlayMode)
            Notes.Add("Entered Play Mode successfully.");
    }

    private static void ValidateSceneInEditMode()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            RegisterFailure("Active scene path does not match SkillRuntimeValidationScene.");
        else
            Notes.Add($"Opened scene '{ScenePath}'.");

        ValidateMissingScriptsInScene(EditorSceneManager.GetActiveScene(), "EditMode");
    }

    private static void ValidateSceneInPlayMode()
    {
        GameObject validationRoot = GameObject.Find("SkillRuntimeValidationRoot");
        if (validationRoot == null)
        {
            RegisterFailure("SkillRuntimeValidationRoot was not found in Play Mode.");
            return;
        }

        Notes.Add("SkillRuntimeValidationRoot exists.");

        SkillRuntimeValidationSceneController sceneController =
            UnityEngine.Object.FindFirstObjectByType<SkillRuntimeValidationSceneController>();
        RoomContext roomContext = UnityEngine.Object.FindFirstObjectByType<RoomContext>();
        CombatRoomController combatRoomController = UnityEngine.Object.FindFirstObjectByType<CombatRoomController>();
        SkillRuntimeTestCaseSelector selector = UnityEngine.Object.FindFirstObjectByType<SkillRuntimeTestCaseSelector>();

        if (sceneController == null)
            RegisterFailure("SkillRuntimeValidationSceneController was not found.");
        else
            Notes.Add("SkillRuntimeValidationSceneController initialized.");

        if (roomContext == null)
            RegisterFailure("RoomContext was not found.");
        else
            Notes.Add($"RoomContext found with {roomContext.Units.Count} registered unit(s).");

        if (combatRoomController == null)
        {
            RegisterFailure("CombatRoomController was not found.");
        }
        else if (!combatRoomController.IsDeploymentActive)
        {
            RegisterFailure($"CombatRoomController expected Deployment but was '{combatRoomController.State}'.");
        }
        else
        {
            Notes.Add("CombatRoomController stayed in Deployment.");
        }

        if (selector == null)
        {
            RegisterFailure("SkillRuntimeTestCaseSelector was not found.");
        }
        else
        {
            ValidateSelector(selector);
        }

        if (sceneController == null || roomContext == null)
            return;

        SkillCaster caster = sceneController.Caster;
        if (caster == null)
        {
            RegisterFailure("Scene controller did not resolve the caster.");
            return;
        }

        if (!ReferenceEquals(caster.GetComponent<Unit>().RoomContext, roomContext))
            RegisterFailure("Caster was not integrated into the room context.");

        if (roomContext.Units.Count < 10)
            RegisterWarning($"RoomContext registered only {roomContext.Units.Count} unit(s). Expected at least 10.");

        ValidateMissingScriptsInScene(SceneManager.GetActiveScene(), "PlayMode");

        for (int i = 0; i < CasesToValidate.Length; i++)
            ValidateCase(sceneController, CasesToValidate[i]);
    }

    private static void ValidateSelector(SkillRuntimeTestCaseSelector selector)
    {
        FieldInfo testCasesField = typeof(SkillRuntimeTestCaseSelector)
            .GetField("_testCases", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo getActiveHarnessMethod = typeof(SkillRuntimeTestCaseSelector)
            .GetMethod("GetActiveHarness", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo selectNextCaseMethod = typeof(SkillRuntimeTestCaseSelector)
            .GetMethod("SelectNextCase", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo castActiveCaseMethod = typeof(SkillRuntimeTestCaseSelector)
            .GetMethod("CastActiveCase", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo activeIndexField = typeof(SkillRuntimeTestCaseSelector)
            .GetField("_activeIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        if (testCasesField == null || getActiveHarnessMethod == null || selectNextCaseMethod == null ||
            castActiveCaseMethod == null || activeIndexField == null)
        {
            RegisterFailure("Could not reflect SkillRuntimeTestCaseSelector internals.");
            return;
        }

        List<SkillRuntimeTestHarness> testCases = testCasesField.GetValue(selector) as List<SkillRuntimeTestHarness>;
        if (testCases == null || testCases.Count == 0)
        {
            RegisterFailure("SkillRuntimeTestCaseSelector discovered no V2 test cases.");
            return;
        }

        Notes.Add($"SkillRuntimeTestCaseSelector discovered {testCases.Count} harness case(s).");

        SkillRuntimeTestHarness activeBefore =
            getActiveHarnessMethod.Invoke(selector, null) as SkillRuntimeTestHarness;
        selectNextCaseMethod.Invoke(selector, null);
        SkillRuntimeTestHarness activeAfter =
            getActiveHarnessMethod.Invoke(selector, null) as SkillRuntimeTestHarness;

        if (activeBefore == null || activeAfter == null || ReferenceEquals(activeBefore, activeAfter))
            RegisterFailure("Selector did not advance to a different active case.");
        else
            Notes.Add($"Selector advanced from '{activeBefore.name}' to '{activeAfter.name}'.");

        int directDamageIndex = FindHarnessIndex(testCases, "TestCase_DPS_DirectDamage");
        if (directDamageIndex < 0)
        {
            RegisterFailure("Selector list does not contain TestCase_DPS_DirectDamage.");
            return;
        }

        ResetCaseState();
        activeIndexField.SetValue(selector, directDamageIndex);
        EnsureHarnessBound(testCases[directDamageIndex]);
        castActiveCaseMethod.Invoke(selector, null);

        SkillCaster caster = UnityEngine.Object.FindFirstObjectByType<SkillRuntimeValidationSceneController>()?.Caster;
        if (caster == null)
        {
            RegisterFailure("Could not resolve caster for selector cast validation.");
            return;
        }

        List<SkillImpact> impacts = new();
        caster.CopyLastResolvedImpactsForDebug(impacts);
        if (impacts.Count <= 0)
            RegisterFailure("Selector cast path did not produce impacts.");
        else
            Notes.Add($"Selector cast path produced {impacts.Count} impact(s) for TestCase_DPS_DirectDamage.");
    }

    private static int FindHarnessIndex(List<SkillRuntimeTestHarness> harnesses, string harnessName)
    {
        for (int i = 0; i < harnesses.Count; i++)
        {
            SkillRuntimeTestHarness harness = harnesses[i];
            if (harness == null)
                continue;

            if (string.Equals(harness.name, harnessName, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static void ValidateCase(SkillRuntimeValidationSceneController sceneController, string harnessName)
    {
        ResetCaseState();

        SkillRuntimeTestHarness harness = FindHarness(harnessName);
        if (harness == null)
        {
            RegisterFailure($"Missing harness '{harnessName}' in scene.");
            return;
        }

        EnsureHarnessBound(harness);

        SkillCaster caster = sceneController.Caster;
        if (caster == null)
        {
            RegisterFailure($"Case '{harnessName}' could not resolve the caster.");
            return;
        }

        int summonedBefore = CountSummonedUnits();
        bool castAccepted = harness.TriggerCastForDebug();

        List<SkillImpact> impacts = new();
        caster.CopyLastResolvedImpactsForDebug(impacts);
        ImpactNotes.Add($"{harnessName}: accepted={castAccepted}, impacts={DescribeImpacts(impacts)}");

        if (!castAccepted)
        {
            RegisterFailure($"Case '{harnessName}' was rejected by SkillCaster.");
            return;
        }

        switch (harnessName)
        {
            case "TestCase_DPS_DirectDamage":
                ValidateDirectDamage(impacts, "Enemy_SingleTarget");
                break;

            case "TestCase_DPS_LineDamage":
                ValidateLineDamage(impacts);
                break;

            case "TestCase_DPS_PiercingLineDamage":
                ValidatePiercingLineDamage(impacts);
                break;

            case "TestCase_Tank_AreaTaunt":
                ValidateAreaTaunt();
                break;

            case "TestCase_Tank_AreaDamage":
                ValidateAreaDamage(impacts);
                break;

            case "TestCase_Tank_SpawnMinions":
            case "TestCase_Support_SpawnMinions":
                ValidateSpawnMinions(harnessName, summonedBefore);
                break;

            case "TestCase_Support_SelfHeal":
                ValidateHealResult("TestCase_Support_SelfHeal", "SkillCaster_Ally");
                break;

            case "TestCase_Support_AllyHeal":
                ValidateHealResult("TestCase_Support_AllyHeal", "Ally_HealTarget");
                break;

            case "TestCase_Support_EnemyDebuffDefense":
                ValidateDefenseDebuff();
                break;

            case "TestCase_DPS_DirectSplashDamage":
            case "TestCase_DPS_DirectExplosiveDamage":
                ValidateAreaModifierCase(harnessName, impacts);
                break;

            case "TestCase_DPS_DirectBounceDamage":
                ValidateBounceDamage(impacts);
                break;

            case "TestCase_DPS_LinePiercingExplosiveDamage":
                ValidateLinePiercingExplosive(impacts);
                break;
        }
    }

    private static void ValidateDirectDamage(List<SkillImpact> impacts, string expectedTargetName)
    {
        if (impacts.Count != 1)
        {
            RegisterFailure($"Direct damage expected 1 impact but resolved {impacts.Count}.");
            return;
        }

        Unit targetUnit = impacts[0].TargetUnit;
        if (targetUnit == null || !string.Equals(targetUnit.name, expectedTargetName, StringComparison.Ordinal))
            RegisterFailure($"Direct damage expected target '{expectedTargetName}' but got '{targetUnit?.name ?? "None"}'.");
    }

    private static void ValidateLineDamage(List<SkillImpact> impacts)
    {
        if (impacts.Count != 1)
            RegisterFailure($"LineDamage should not pierce. Expected 1 impact but got {impacts.Count}.");
    }

    private static void ValidatePiercingLineDamage(List<SkillImpact> impacts)
    {
        int uniqueTargets = CountUniqueTargetUnits(impacts);
        if (uniqueTargets < 2)
            RegisterFailure($"PiercingLineDamage expected multiple aligned targets but got {uniqueTargets}.");
    }

    private static void ValidateAreaTaunt()
    {
        int tauntedHostiles = CountHostilesWithStatus(StatusEffectType.Taunt);
        if (tauntedHostiles < 1)
            RegisterFailure("AreaTaunt did not apply Taunt to any hostile in range.");
        else
            Notes.Add($"AreaTaunt applied Taunt to {tauntedHostiles} hostile unit(s).");
    }

    private static void ValidateAreaDamage(List<SkillImpact> impacts)
    {
        if (CountUniqueTargetUnits(impacts) < 2)
            RegisterFailure($"AreaDamage expected multiple units in area but got {CountUniqueTargetUnits(impacts)}.");
    }

    private static void ValidateSpawnMinions(string harnessName, int summonedBefore)
    {
        int summonedAfter = CountSummonedUnits();
        if (summonedAfter <= summonedBefore)
            RegisterFailure($"{harnessName} did not summon any runtime minion.");
        else
            Notes.Add($"{harnessName} summoned {summonedAfter - summonedBefore} minion(s).");
    }

    private static void ValidateHealResult(string harnessName, string unitName)
    {
        LifeController lifeController = GameObject.Find(unitName)?.GetComponent<LifeController>();
        if (lifeController == null)
        {
            RegisterFailure($"{harnessName} could not resolve LifeController for '{unitName}'.");
            return;
        }

        if (lifeController.CurrentHealth <= 5)
            RegisterFailure($"{harnessName} did not heal '{unitName}'. CurrentHealth={lifeController.CurrentHealth}.");
        else
            Notes.Add($"{harnessName} healed '{unitName}' to {lifeController.CurrentHealth} HP.");
    }

    private static void ValidateDefenseDebuff()
    {
        Unit enemy = GameObject.Find("Enemy_SingleTarget")?.GetComponent<Unit>();
        StatusEffectController statusEffects = enemy != null ? enemy.StatusEffects : null;
        if (statusEffects == null)
        {
            RegisterFailure("Support_EnemyDebuffDefense could not resolve Enemy_SingleTarget status controller.");
            return;
        }

        bool foundDefenseDebuff = false;
        IReadOnlyList<ActiveStatusEffect> activeEffects = statusEffects.ActiveEffects;
        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveStatusEffect effect = activeEffects[i];
            if (effect?.Definition == null)
                continue;

            if (string.Equals(effect.Definition.EffectId, "Status_Debuff_Defense", StringComparison.Ordinal))
            {
                foundDefenseDebuff = true;
                break;
            }
        }

        if (!foundDefenseDebuff)
            RegisterFailure("Support_EnemyDebuffDefense did not apply Status_Debuff_Defense.");
    }

    private static void ValidateAreaModifierCase(string harnessName, List<SkillImpact> impacts)
    {
        int uniqueTargets = CountUniqueTargetUnits(impacts);
        if (uniqueTargets < 2)
            RegisterFailure($"{harnessName} expected multiple unique impacted units but got {uniqueTargets}.");

        if (uniqueTargets != impacts.Count)
            RegisterFailure($"{harnessName} duplicated impacted units. impacts={impacts.Count}, uniqueTargets={uniqueTargets}.");
    }

    private static void ValidateBounceDamage(List<SkillImpact> impacts)
    {
        int uniqueTargets = CountUniqueTargetUnits(impacts);
        if (uniqueTargets < 2)
            RegisterFailure($"DirectBounceDamage expected at least 2 bounced targets but got {uniqueTargets}.");

        if (uniqueTargets != impacts.Count)
            RegisterFailure($"DirectBounceDamage produced duplicated or looping impacts. impacts={impacts.Count}, uniqueTargets={uniqueTargets}.");
    }

    private static void ValidateLinePiercingExplosive(List<SkillImpact> impacts)
    {
        int uniqueTargets = CountUniqueTargetUnits(impacts);
        if (uniqueTargets < 3)
            RegisterFailure($"LinePiercingExplosiveDamage expected several unique targets but got {uniqueTargets}.");

        if (uniqueTargets != impacts.Count)
            RegisterFailure($"LinePiercingExplosiveDamage duplicated impacted units. impacts={impacts.Count}, uniqueTargets={uniqueTargets}.");
    }

    private static SkillRuntimeTestHarness FindHarness(string harnessName)
    {
        SkillRuntimeTestHarness[] harnesses =
            UnityEngine.Object.FindObjectsByType<SkillRuntimeTestHarness>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < harnesses.Length; i++)
        {
            SkillRuntimeTestHarness harness = harnesses[i];
            if (harness == null)
                continue;

            if (string.Equals(harness.name, harnessName, StringComparison.Ordinal))
                return harness;
        }

        return null;
    }

    private static void EnsureHarnessBound(SkillRuntimeTestHarness harness)
    {
        if (harness == null)
            return;

        SkillRuntimeTestCaseBinder binder = harness.GetComponent<SkillRuntimeTestCaseBinder>();
        if (binder != null)
            binder.Bind();
    }

    private static void ResetCaseState()
    {
        Unit[] units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit == null)
                continue;

            StatusEffectController statusEffects = unit.StatusEffects;
            if (statusEffects != null)
                statusEffects.ClearAllEffects();

            LifeController life = unit.GetComponent<LifeController>();
            if (life != null)
                life.SetCurrentHealth(life.MaxHealth);
        }

        SetHealth("SkillCaster_Ally", 5);
        SetHealth("Ally_HealTarget", 5);
    }

    private static void SetHealth(string unitName, int health)
    {
        LifeController life = GameObject.Find(unitName)?.GetComponent<LifeController>();
        if (life == null)
            return;

        life.SetCurrentHealth(Mathf.Clamp(health, 1, life.MaxHealth));
    }

    private static int CountSummonedUnits()
    {
        CombatSummonedUnitRuntimeMarker[] markers =
            UnityEngine.Object.FindObjectsByType<CombatSummonedUnitRuntimeMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return markers.Length;
    }

    private static int CountHostilesWithStatus(StatusEffectType effectType)
    {
        Unit[] units = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int count = 0;

        for (int i = 0; i < units.Length; i++)
        {
            Unit unit = units[i];
            if (unit == null || unit.Team != UnitTeam.Enemy || unit.StatusEffects == null)
                continue;

            if (unit.StatusEffects.HasEffect(effectType))
                count++;
        }

        return count;
    }

    private static int CountUniqueTargetUnits(List<SkillImpact> impacts)
    {
        var uniqueTargets = new HashSet<Unit>();
        for (int i = 0; i < impacts.Count; i++)
        {
            Unit targetUnit = impacts[i].TargetUnit;
            if (targetUnit == null)
                continue;

            uniqueTargets.Add(targetUnit);
        }

        return uniqueTargets.Count;
    }

    private static string DescribeImpacts(List<SkillImpact> impacts)
    {
        if (impacts.Count == 0)
            return "[]";

        var parts = new List<string>(impacts.Count);
        for (int i = 0; i < impacts.Count; i++)
        {
            SkillImpact impact = impacts[i];
            string unitName = impact.TargetUnit != null ? impact.TargetUnit.name : "None";
            parts.Add($"{impact.Kind}:{unitName}:primary={impact.IsPrimaryImpact}:chain={impact.ChainIndex}");
        }

        return string.Join(" | ", parts);
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!_isRunning)
            return;

        if (condition != null && condition.StartsWith(ValidatorPrefix, StringComparison.Ordinal))
            return;

        switch (type)
        {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                ConsoleErrors.Add($"{type}: {condition}");
                break;

            case LogType.Warning:
                if (IsRelevantWarning(condition))
                    ConsoleWarnings.Add(condition);
                break;
        }
    }

    private static bool IsRelevantWarning(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("guid", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("deserialize", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("legacy", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("nullreference", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void RegisterFailure(string message)
    {
        Failures.Add(message);
        Debug.LogError($"{ValidatorPrefix} {message}");
    }

    private static void RegisterWarning(string message)
    {
        Warnings.Add(message);
        Debug.LogWarning($"{ValidatorPrefix} {message}");
    }

    private static void CacheEnterPlayModeOptions()
    {
        _enterPlayModeOptionsEnabledBeforeRun = EditorSettings.enterPlayModeOptionsEnabled;
        _enterPlayModeOptionsBeforeRun = EditorSettings.enterPlayModeOptions;
    }

    private static void EnablePlayModeWithoutDomainReload()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
        Notes.Add("Temporarily enabled Enter Play Mode without domain reload for batch validation.");
    }

    private static void RestoreEnterPlayModeOptions()
    {
        EditorSettings.enterPlayModeOptionsEnabled = _enterPlayModeOptionsEnabledBeforeRun;
        EditorSettings.enterPlayModeOptions = _enterPlayModeOptionsBeforeRun;
    }

    private static void ValidateMissingScriptsInScene(Scene scene, string context)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            RegisterFailure($"Cannot inspect missing scripts for {context}: scene is not loaded.");
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            ValidateMissingScriptsRecursive(roots[i], context);
    }

    private static void ValidateMissingScriptsRecursive(GameObject gameObject, string context)
    {
        if (gameObject == null)
            return;

        int missingScripts = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
        if (missingScripts > 0)
            RegisterFailure($"{context} missing script on '{BuildGameObjectPath(gameObject.transform)}' (count={missingScripts}).");

        Transform transform = gameObject.transform;
        for (int i = 0; i < transform.childCount; i++)
            ValidateMissingScriptsRecursive(transform.GetChild(i).gameObject, context);
    }

    private static string BuildGameObjectPath(Transform transform)
    {
        if (transform == null)
            return "<null>";

        string path = transform.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && prefabStage.scene == transform.gameObject.scene)
            return prefabStage.assetPath + "::" + path;

        return path;
    }

    private static void FinishAndExit()
    {
        if (_finished)
            return;

        _finished = true;
        _isRunning = false;

        Application.logMessageReceived -= HandleLogMessage;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.update -= HandleEditorUpdate;
        RestoreEnterPlayModeOptions();

        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();

        string summary = BuildSummary();
        File.WriteAllText(ReportPath, summary);
        Debug.Log($"{ValidatorPrefix} Wrote report to '{ReportPath}'.");

        EditorApplication.Exit(Failures.Count > 0 || ConsoleErrors.Count > 0 ? 1 : 0);
    }

    private static string BuildSummary()
    {
        var lines = new List<string>();
        lines.Add("Skill Runtime V2 Unity Validation");
        lines.Add($"TimestampUtc: {DateTime.UtcNow:O}");
        lines.Add($"Scene: {ScenePath}");
        lines.Add(string.Empty);

        lines.Add("Notes:");
        AppendLines(lines, Notes);

        lines.Add(string.Empty);
        lines.Add("Impact Logs:");
        AppendLines(lines, ImpactNotes);

        lines.Add(string.Empty);
        lines.Add("Warnings:");
        AppendLines(lines, Warnings);

        lines.Add(string.Empty);
        lines.Add("ConsoleWarnings:");
        AppendLines(lines, ConsoleWarnings);

        lines.Add(string.Empty);
        lines.Add("Failures:");
        AppendLines(lines, Failures);

        lines.Add(string.Empty);
        lines.Add("ConsoleErrors:");
        AppendLines(lines, ConsoleErrors);

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendLines(List<string> destination, List<string> source)
    {
        if (source.Count == 0)
        {
            destination.Add("- none");
            return;
        }

        for (int i = 0; i < source.Count; i++)
            destination.Add($"- {source[i]}");
    }
}
