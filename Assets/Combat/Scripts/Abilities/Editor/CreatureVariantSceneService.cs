using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreatureVariantSceneService
{
    public struct LabSpawnedUnitRecord
    {
        public GameObject instance;
        public UnitData unitData;
        public SkillData skillData;
        public UnitTeam team;
        public bool isDummy;
    }

    private CreatureCombatDebugTool _cachedDebugTool;
    private CreatureVariantLabConfig _config;
    private readonly List<LabSpawnedUnitRecord> _spawnedRecords = new List<LabSpawnedUnitRecord>();

    public CreatureVariantSceneService(CreatureVariantLabConfig config)
    {
        _config = config;
    }

    public CreatureCombatDebugTool DebugTool
    {
        get
        {
            if (_cachedDebugTool == null && EditorApplication.isPlaying)
                _cachedDebugTool = Object.FindFirstObjectByType<CreatureCombatDebugTool>();
            return _cachedDebugTool;
        }
    }

    public RoomGrid Grid
    {
        get
        {
            var tool = DebugTool;
            return tool != null ? tool.ContextGrid : null;
        }
    }

    public RoomContext Context
    {
        get
        {
            var tool = DebugTool;
            return tool != null ? tool.ContextRoom : null;
        }
    }

    public void InvalidateCache()
    {
        _cachedDebugTool = null;
    }

    public void ClearSpawnTracking()
    {
        _spawnedRecords.Clear();
    }

    public string GetRuntimeContextBlockReason(bool requirePlayMode = true)
    {
        if (requirePlayMode && !EditorApplication.isPlaying)
            return "Runtime actions require Play Mode.";
        if (SceneManager.GetActiveScene().path != _config.testScenePath)
            return "TestMapScene is not active.";

        var tool = DebugTool;
        if (tool == null)
            return "CreatureCombatDebugTool is missing.";
        if (tool.ContextRoom == null)
            return "RoomContext is missing.";
        if (tool.ContextGrid == null)
            return "RoomGrid is missing.";
        if (tool.ContextRoom.CombatController == null)
            return "CombatRoomController is missing.";
        return null;
    }

    public string GetSpawnBlockReason(CreatureVariantLabState state, CreatureVariantLabConfig config, SkillCompositionValidator.ValidationResult validation)
    {
        if (!EditorApplication.isPlaying)
            return "Spawn blocked: the Editor is not in Play Mode.";
        if (SceneManager.GetActiveScene().path != _config.testScenePath)
            return $"Spawn blocked: active scene is '{SceneManager.GetActiveScene().name}'. TestMapScene is required.";

        string contextReason = GetRuntimeContextBlockReason();
        if (!string.IsNullOrEmpty(contextReason))
            return $"Spawn blocked: {contextReason}";
        if (validation.HasErrors)
            return $"Spawn blocked: composition has {validation.ErrorMessages.Count} blocking error(s).";
        return null;
    }

    public string GetDummySpawnBlockReason()
    {
        if (!EditorApplication.isPlaying)
            return "Dummy spawn blocked: the Editor is not in Play Mode.";
        if (SceneManager.GetActiveScene().path != _config.testScenePath)
            return $"Dummy spawn blocked: active scene is '{SceneManager.GetActiveScene().name}'. TestMapScene is required.";

        string contextReason = GetRuntimeContextBlockReason();
        return string.IsNullOrEmpty(contextReason) ? null : $"Dummy spawn blocked: {contextReason}";
    }

    public string GetSpawnedUnitsBlockReason()
    {
        if (!EditorApplication.isPlaying)
            return "Action blocked: the Editor is not in Play Mode.";
        if (_spawnedRecords.Count == 0 && DebugTool == null)
            return "Action blocked: CreatureCombatDebugTool is missing and no units have been spawned by the Lab.";
        return null;
    }

    public string GetSceneReadinessReport(UnitTeam testTeam, UnitTeam dummyTeam)
    {
        if (!EditorApplication.isPlaying)
            return "Not in Play Mode.";
        if (SceneManager.GetActiveScene().path != _config.testScenePath)
            return $"Wrong scene: '{SceneManager.GetActiveScene().name}'. Open TestMapScene.";

        var tool = DebugTool;
        if (tool == null)
            return "Scene missing: CreatureCombatDebugTool not found.";
        if (tool.ContextRoom == null)
            return "Scene missing: RoomContext not found.";
        if (tool.ContextGrid == null)
            return "Scene missing: RoomGrid not found in RoomContext.";
        if (tool.ContextRoom.CombatController == null)
            return "Scene missing: CombatRoomController not found in RoomContext.";

        var grid = tool.ContextGrid;
        string attachDiag;
        if (!TryAttachToAnyValidCell(grid, testTeam, out _, out attachDiag))
            return $"Scene not ready: no valid cell for {testTeam} spawn. {attachDiag}";
        if (!TryAttachToAnyValidCell(grid, dummyTeam, out _, out attachDiag))
            return $"Scene not ready: no valid cell for {dummyTeam} (dummy) spawn. {attachDiag}";

        return null;
    }

    public bool TryAttachToAnyValidCell(RoomGrid grid, UnitTeam team, out Vector3Int attachedCell, out string diagnosis)
    {
        attachedCell = Vector3Int.zero;
        diagnosis = null;
        if (grid == null)
        {
            diagnosis = "RoomGrid is null.";
            return false;
        }

        var candidates = GetCandidateCells(grid, team);

        int evaluated = 0;
        int failedHasCell = 0;
        int failedWalkable = 0;
        int failedOccupied = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            evaluated++;

            if (!grid.HasCell(candidate))
            {
                failedHasCell++;
                continue;
            }
            if (!grid.IsCellWalkable(candidate, null))
            {
                failedWalkable++;
                continue;
            }
            if (grid.IsCellOccupied(candidate, null))
            {
                failedOccupied++;
                continue;
            }

            attachedCell = candidate;
            diagnosis = null;
            return true;
        }

        diagnosis = $"Evaluated {evaluated} cells: {failedHasCell} out of bounds, {failedWalkable} not walkable, {failedOccupied} occupied. No attachable cell found.";
        return false;
    }

    private List<Vector3Int> GetCandidateCells(RoomGrid grid, UnitTeam team)
    {
        var candidates = new List<Vector3Int>();

        int biasX = team == UnitTeam.Ally ? -3 : 3;

        for (int r = 0; r <= 8; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    candidates.Add(new Vector3Int(dx + biasX, dy, 0));
                }
            }
        }

        for (int r = 0; r <= 12; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (r > 0 && Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    Vector3Int cell = new Vector3Int(dx, dy, 0);
                    if (!candidates.Contains(cell))
                        candidates.Add(cell);
                }
            }
        }

        return candidates;
    }

    public string SpawnTestUnit(CreatureVariantLabState state, CreatureVariantLabConfig config, SkillData tempSkill, UnitData tempUnit, GameObject prefab, string unitLabel)
    {
        UnitTeam team = state.team;
        var debugTool = DebugTool;
        if (debugTool == null)
            return "CreatureCombatDebugTool not found.";

        RoomGrid grid = debugTool.ContextGrid;
        RoomContext ctx = debugTool.ContextRoom;
        if (grid == null)
            return "RoomGrid not found. Ensure TestMapScene has a RoomGrid.";
        if (ctx == null)
            return "RoomContext not found. Ensure TestMapScene has a RoomContext.";

        var candidates = GetCandidateCells(grid, team);
        int evaluated = 0, failedHasCell = 0, failedWalkable = 0, failedOccupied = 0, attachFailures = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            evaluated++;

            if (!grid.HasCell(candidate)) { failedHasCell++; continue; }
            if (!grid.IsCellWalkable(candidate, null)) { failedWalkable++; continue; }
            if (grid.IsCellOccupied(candidate, null)) { failedOccupied++; continue; }

            GameObject instance;
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ctx.transform);
                if (instance == null)
                    return $"Could not instantiate '{prefab.name}'.";
            }
            else
            {
                instance = CreatureVariantPlaceholderFactory.CreatePlaceholderUnit(unitLabel, ctx.transform, team == UnitTeam.Ally ? PlaceholderVisualKind.Ally : PlaceholderVisualKind.Enemy);
            }

            instance.name = unitLabel;

            Unit unit = instance.GetComponent<Unit>();
            if (unit == null)
            {
                Object.DestroyImmediate(instance);
                return "Spawned object missing Unit component.";
            }

            UnitMovement movement = instance.GetComponent<UnitMovement>();
            if (movement != null)
            {
                if (!movement.AttachToGridAtCell(grid, candidate))
                {
                    Object.DestroyImmediate(instance);
                    attachFailures++;
                    continue;
                }
                instance.transform.position = grid.CellToWorld(candidate);
            }
            else
            {
                instance.transform.position = grid.CellToWorld(candidate);
            }

            unit.ForceSetUnitDataForEditor(tempUnit);
            unit.SetAffiliation(team, unit.Faction);

            SkillCaster skillCaster = instance.GetComponent<SkillCaster>();
            if (skillCaster != null)
                skillCaster.ForceOverrideSkillForEditor(tempSkill);

            RecruitableUnitState lifeState = instance.GetComponent<RecruitableUnitState>();
            if (lifeState != null && lifeState.CurrentState != UnitLifecycleState.Alive)
                lifeState.SetState(UnitLifecycleState.Alive);

            DisableAutonomousBehavior(instance);

            ctx.RegisterUnit(unit);
            debugTool.RegisterSpawnedUnit(unit);
            debugTool.TryStartCombatIfReady();

            _spawnedRecords.Add(new LabSpawnedUnitRecord
            {
                instance = instance,
                unitData = tempUnit,
                skillData = tempSkill,
                team = team,
                isDummy = false
            });

            UnityEditor.Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);

            return null;
        }

        return $"No viable spawn cell for {team}. Evaluated {evaluated} cells: {failedHasCell} out of bounds, {failedWalkable} not walkable, {failedOccupied} occupied, {attachFailures} attach failures.";
    }

    public string SpawnDummy(CreatureVariantLabConfig config, UnitTeam opponentTeam)
    {
        UnitTeam dummyTeam = opponentTeam == UnitTeam.Ally ? UnitTeam.Enemy : UnitTeam.Ally;
        var debugTool = DebugTool;
        if (debugTool == null)
            return "CreatureCombatDebugTool not found.";

        RoomGrid grid = debugTool.ContextGrid;
        RoomContext ctx = debugTool.ContextRoom;
        if (grid == null)
            return "RoomGrid not found. Ensure TestMapScene has a RoomGrid.";
        if (ctx == null)
            return "RoomContext not found. Ensure TestMapScene has a RoomContext.";

        Unit casterReference = FindFirstNonDummyUnit(opponentTeam);
        List<Vector3Int> candidates;

        if (casterReference != null)
        {
            int preferredRange = ResolvePreferredSkillRange(casterReference);
            candidates = GetCandidateCellsNearCaster(grid, casterReference, preferredRange);
            var generalCandidates = GetCandidateCells(grid, dummyTeam);
            for (int i = 0; i < generalCandidates.Count; i++)
            {
                if (!candidates.Contains(generalCandidates[i]))
                    candidates.Add(generalCandidates[i]);
            }
        }
        else
        {
            candidates = GetCandidateCells(grid, dummyTeam);
        }

        int evaluated = 0, failedHasCell = 0, failedWalkable = 0, failedOccupied = 0, attachFailures = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            evaluated++;

            if (!grid.HasCell(candidate)) { failedHasCell++; continue; }
            if (!grid.IsCellWalkable(candidate, null)) { failedWalkable++; continue; }
            if (grid.IsCellOccupied(candidate, null)) { failedOccupied++; continue; }

            UnitData dummyData = null;
            GameObject instance;
            if (config.dummyPrefabOverride != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(config.dummyPrefabOverride, ctx.transform);
                if (instance == null)
                    return $"Could not instantiate dummy prefab '{config.dummyPrefabOverride.name}'.";
            }
            else
            {
                instance = CreatureVariantPlaceholderFactory.CreatePlaceholderDummy("LabDummy", ctx.transform);
                dummyData = CreatureVariantPlaceholderFactory.CreateDummyUnitData();
                dummyData.hideFlags = HideFlags.DontSave;
            }

            instance.name = "LabDummy";

            Unit unit = instance.GetComponent<Unit>();
            if (unit == null)
            {
                Object.DestroyImmediate(instance);
                if (dummyData != null) Object.DestroyImmediate(dummyData);
                return "Spawned dummy object missing Unit component.";
            }

            if (dummyData != null)
                unit.ForceSetUnitDataForEditor(dummyData);

            unit.SetAffiliation(dummyTeam, UnitFaction.None);

            RecruitableUnitState lifeState = instance.GetComponent<RecruitableUnitState>();
            if (lifeState != null && lifeState.CurrentState != UnitLifecycleState.Alive)
                lifeState.SetState(UnitLifecycleState.Alive);

            DisableAutonomousBehavior(instance);

            UnitMovement movement = instance.GetComponent<UnitMovement>();
            if (movement != null)
            {
                if (!movement.AttachToGridAtCell(grid, candidate))
                {
                    Object.DestroyImmediate(instance);
                    if (dummyData != null) Object.DestroyImmediate(dummyData);
                    attachFailures++;
                    continue;
                }
                instance.transform.position = grid.CellToWorld(candidate);
            }
            else
            {
                instance.transform.position = grid.CellToWorld(candidate);
            }

            ctx.RegisterUnit(unit);
            debugTool.RegisterSpawnedUnit(unit);
            debugTool.TryStartCombatIfReady();

            _spawnedRecords.Add(new LabSpawnedUnitRecord
            {
                instance = instance,
                unitData = dummyData,
                skillData = null,
                team = dummyTeam,
                isDummy = true
            });

            UnityEditor.Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);

            return null;
        }

        return $"No viable spawn cell for dummy ({dummyTeam}). Evaluated {evaluated} cells: {failedHasCell} out of bounds, {failedWalkable} not walkable, {failedOccupied} occupied, {attachFailures} attach failures.";
    }

    public void ClearSpawnedUnits()
    {
        for (int i = _spawnedRecords.Count - 1; i >= 0; i--)
        {
            var rec = _spawnedRecords[i];
            if (rec.instance != null)
                Object.DestroyImmediate(rec.instance);
            if (rec.unitData != null)
                Object.DestroyImmediate(rec.unitData);
            if (rec.skillData != null)
                Object.DestroyImmediate(rec.skillData);
        }
        _spawnedRecords.Clear();

        var debugTool = DebugTool;
        if (debugTool != null)
            debugTool.ClearSpawned();
    }

    public void GetSpawnedTeamCounts(out int allies, out int enemies, out int dummies)
    {
        allies = 0;
        enemies = 0;
        dummies = 0;

        for (int i = _spawnedRecords.Count - 1; i >= 0; i--)
        {
            var rec = _spawnedRecords[i];
            if (rec.instance == null)
            {
                if (rec.unitData != null) Object.DestroyImmediate(rec.unitData);
                if (rec.skillData != null) Object.DestroyImmediate(rec.skillData);
                _spawnedRecords.RemoveAt(i);
                continue;
            }

            if (rec.isDummy)
                dummies++;
            else if (rec.team == UnitTeam.Ally)
                allies++;
            else
                enemies++;
        }
    }

    public string GetSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public string GetScenePath()
    {
        return SceneManager.GetActiveScene().path;
    }

    public bool IsCorrectScene()
    {
        return SceneManager.GetActiveScene().path == _config.testScenePath;
    }

    private static void DisableAutonomousBehavior(GameObject instance)
    {
        var brain = instance.GetComponent<UnitBrain>();
        if (brain != null)
            brain.enabled = false;
    }

    private Unit FindFirstNonDummyUnit(UnitTeam team)
    {
        for (int i = 0; i < _spawnedRecords.Count; i++)
        {
            var rec = _spawnedRecords[i];
            if (rec.isDummy || rec.team != team || rec.instance == null)
                continue;
            Unit unit = rec.instance.GetComponent<Unit>();
            if (unit != null && unit.IsAlive)
                return unit;
        }
        return null;
    }

    private static int ResolvePreferredSkillRange(Unit casterUnit)
    {
        if (casterUnit == null) return 3;
        SkillCaster skillCaster = casterUnit.GetComponent<SkillCaster>();
        if (skillCaster == null) return 3;
        SkillData skill = skillCaster.Skill;
        if (skill == null) return 3;
        return Mathf.Max(1, skill.RangeInCells);
    }

    private static List<Vector3Int> GetCandidateCellsNearCaster(RoomGrid grid, Unit casterUnit, int preferredRange)
    {
        var candidates = new List<Vector3Int>();
        if (casterUnit == null) return candidates;

        UnitMovement movement = casterUnit.GetComponent<UnitMovement>();
        if (movement == null || !movement.TryGetLogicalCell(out Vector3Int casterCell)) return candidates;

        for (int r = 1; r <= preferredRange; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    candidates.Add(new Vector3Int(casterCell.x + dx, casterCell.y + dy, 0));
                }
            }
        }

        for (int r = preferredRange + 1; r <= preferredRange + 5; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                    Vector3Int cell = new Vector3Int(casterCell.x + dx, casterCell.y + dy, 0);
                    if (!candidates.Contains(cell))
                        candidates.Add(cell);
                }
            }
        }

        return candidates;
    }
}