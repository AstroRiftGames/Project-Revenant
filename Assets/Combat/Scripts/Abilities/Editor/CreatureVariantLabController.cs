using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreatureVariantLabController
{
    private CreatureVariantLabState _state;
    private CreatureVariantLabConfig _config;
    private CreatureVariantSceneService _sceneService;
    private List<ValidationIssue> _validationIssues = new List<ValidationIssue>();
    private UnitData _matchingVariant;
    private int _spawnSequenceId;

    public CreatureVariantLabState State => _state;
    public CreatureVariantLabConfig Config => _config;
    public CreatureVariantSceneService SceneService => _sceneService;
    public IReadOnlyList<ValidationIssue> ValidationIssues => _validationIssues;
    public UnitData MatchingVariant => _matchingVariant;

    public CreatureVariantLabController(CreatureVariantLabState state, CreatureVariantLabConfig config)
    {
        _state = state;
        _config = config;
        _sceneService = new CreatureVariantSceneService(config);
    }

    public void Validate()
    {
        _validationIssues.Clear();
        _matchingVariant = null;

        CreatureVariantLabConfigValidator.ValidateConfig(_config, _validationIssues);

        if (_validationIssues.Count > 0 && HasConfigErrors())
            return;

        GameObject prefab = ResolvePrefab();

        var result = SkillCompositionValidator.ValidateComposition(_state, prefab);
        _validationIssues.AddRange(result.issues);

        if (!ValidationIssue.ListHasErrors(_validationIssues))
        {
            SkillData tempSkill = ScriptableObject.CreateInstance<SkillData>();
            try
            {
                CreatureVariantBuilder.ConfigureSkillData(tempSkill, _state, "ValidateTemp", "ValidateTemp", SkillCompositionState.Provisional, _config);
                SkillCompositionValidator.ValidateAgainstRuntimeReadiness(tempSkill, _validationIssues);
            }
            finally
            {
                Object.DestroyImmediate(tempSkill);
            }
        }
    }

    public CreatureVariantLabConfigValidator.ConfigStatus GetConfigStatus()
    {
        return CreatureVariantLabConfigValidator.GetConfigStatus(_config);
    }

    public bool HasConfigErrors()
    {
        for (int i = 0; i < _validationIssues.Count; i++)
        {
            if (_validationIssues[i].severity == ValidationSeverity.Error &&
                _validationIssues[i].category == ValidationCategory.Config)
                return true;
        }
        return false;
    }

    public List<string> GetValidationErrorMessages()
    {
        var result = new List<string>();
        for (int i = 0; i < _validationIssues.Count; i++)
            if (_validationIssues[i].severity == ValidationSeverity.Error)
                result.Add(_validationIssues[i].message);
        return result;
    }

    public List<string> GetValidationWarningMessages()
    {
        var result = new List<string>();
        for (int i = 0; i < _validationIssues.Count; i++)
            if (_validationIssues[i].severity == ValidationSeverity.Warning)
                result.Add(_validationIssues[i].message);
        return result;
    }

    public GameObject ResolvePrefab()
    {
        return _state.prefabOverride != null ? _state.prefabOverride : _config.GetPrefab(_state.faction, _state.role);
    }

    public bool IsUsingPlaceholderPrefab()
    {
        return ResolvePrefab() == null;
    }

    public bool IsUsingPlaceholderDummy()
    {
        return _config == null || _config.dummyPrefabOverride == null;
    }

    public string GetSceneReadinessReport()
    {
        UnitTeam oppositeTeam = _state.team == UnitTeam.Ally ? UnitTeam.Enemy : UnitTeam.Ally;
        return _sceneService.GetSceneReadinessReport(_state.team, oppositeTeam);
    }

    public void LoadDefaultsFromPreset()
    {
        var preset = _config?.GetPreset(_state.faction, _state.role);
        if (preset != null)
            CreatureVariantBuilder.ApplyDefaultsFromPreset(_state, preset);
    }

    public void ImportFromExistingUnitData(UnitData source)
    {
        CreatureVariantBuilder.ImportFromExistingUnitData(_state, source);
        Validate();
    }

    public string GetSpawnBlockReason()
    {
        return _sceneService.GetSpawnBlockReason(_state, _config, GetCurrentValidation());
    }

    public string GetDummySpawnBlockReason()
    {
        return _sceneService.GetDummySpawnBlockReason();
    }

    public string GetSpawnedUnitsBlockReason()
    {
        return _sceneService.GetSpawnedUnitsBlockReason();
    }

    public string GetSaveBlockReason()
    {
        if (HasConfigErrors())
            return "Save blocked: config has errors.";
        if (ValidationIssue.ListHasErrors(_validationIssues))
            return $"Save blocked: composition has {GetValidationErrorMessages().Count} blocking error(s).";
        if (!SkillCompositionValidator.IsProductionCatalogCompatible(_state))
            return "Save blocked: this composition is experimental or outside the active production catalog.";
        if (IsUsingPlaceholderPrefab())
            return "Production save requires a prefab override. Runtime placeholders are test-only.";

        string nameError = CreatureVariantAssetService.GetSkillNameValidationError(_state.saveSkillName);
        if (!string.IsNullOrEmpty(nameError))
            return $"Save blocked: {nameError}";

        return null;
    }

    public string GetCompactStatus()
    {
        if (HasConfigErrors())
            return "Status: Blocked - Config incomplete or missing.";
        if (!_sceneService.IsCorrectScene())
            return $"Status: Blocked - Open TestMapScene (current: {_sceneService.GetSceneName()}).";
        if (!EditorApplication.isPlaying)
            return "Status: Edit Mode - Enter Play Mode to test runtime spawn.";

        string contextReason = _sceneService.GetRuntimeContextBlockReason(false);
        if (!string.IsNullOrEmpty(contextReason))
            return $"Status: Blocked - {contextReason}";
        return !ValidationIssue.ListHasErrors(_validationIssues)
            ? "Status: Runtime Ready"
            : $"Status: Blocked - Fix {GetValidationErrorMessages().Count} validation error(s).";
    }

    public string SpawnTestUnit()
    {
        _spawnSequenceId++;
        GameObject prefab = ResolvePrefab();
        string idSuffix = _spawnSequenceId.ToString("D3");
        string unitLabel = $"{_state.faction}_{_state.role}_{_state.team}_{idSuffix}";

        SkillData tempSkill = ScriptableObject.CreateInstance<SkillData>();
        tempSkill.hideFlags = HideFlags.DontSave;
        CreatureVariantBuilder.ConfigureSkillData(tempSkill, _state, $"LabTemp_{unitLabel}_Skill", $"LabTemp_{unitLabel}", SkillCompositionState.Provisional, _config);

        UnitData tempUnit = ScriptableObject.CreateInstance<UnitData>();
        tempUnit.hideFlags = HideFlags.DontSave;
        CreatureVariantBuilder.ConfigureUnitData(tempUnit, _state, tempSkill, prefab);
        tempUnit.name = $"LabTemp_{unitLabel}_UnitData";

        return _sceneService.SpawnTestUnit(_state, _config, tempSkill, tempUnit, prefab, unitLabel);
    }

    public string SpawnDummy()
    {
        return _sceneService.SpawnDummy(_config, _state.team);
    }

    public void ClearSpawnedUnits()
    {
        _sceneService.ClearSpawnedUnits();
    }

    public int ClearLabTempAssets()
    {
        return CreatureVariantAssetService.ClearLabTempAssets(_config);
    }

    public int GetLabTempAssetCount()
    {
        return CreatureVariantAssetService.GetLabTempAssetCount(_config);
    }

    public void SaveProductionVariant()
    {
        CreatureVariantAssetService.SaveProductionVariant(_state, _config, GetCurrentValidation());
    }

    public void LoadStatusDefinitions(List<StatusEffectDefinition> definitions, List<string> names)
    {
        CreatureVariantAssetService.LoadStatusDefinitions(definitions, names);
    }

    public int ResolveSelectedStatusIndex(StatusEffectDefinition current, List<StatusEffectDefinition> definitions)
    {
        if (current != null)
        {
            int index = definitions.IndexOf(current);
            return index >= 0 ? index + 1 : 0;
        }
        return 0;
    }

    public StatusEffectDefinition ResolveStatusDefinition(int selectedIndex, List<StatusEffectDefinition> definitions)
    {
        return selectedIndex > 0 && selectedIndex <= definitions.Count
            ? definitions[selectedIndex - 1]
            : null;
    }

    public void OnPlayModeStateChanged()
    {
        _sceneService.InvalidateCache();
        _sceneService.ClearSpawnTracking();
    }

    public void OnHierarchyOrProjectChanged()
    {
    }

    private SkillCompositionValidator.ValidationResult GetCurrentValidation()
    {
        var result = SkillCompositionValidator.ValidationResult.Empty();
        result.issues.AddRange(_validationIssues);
        return result;
    }
}