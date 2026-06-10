#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class CreatureSpawnEntry
{
    public GameObject prefab;
    public UnitTeam team = UnitTeam.Enemy;
    public Transform spawnCellMarker;
    public Vector2Int cell;
}

public class CreatureCombatDebugTool : MonoBehaviour
{
    private static string EntryLabel(CreatureSpawnEntry entry, int index)
    {
        return entry.prefab != null ? entry.prefab.name : $"Entry {index}";
    }
    [SerializeField] private RoomContext _roomContext;
    [SerializeField] private RoomGrid _roomGrid;
    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;
    [SerializeField] private bool _configureGridOnEnable;
    [SerializeField] private bool _startCombatOnEnable;
    [SerializeField] private List<CreatureSpawnEntry> _spawnEntries = new();

    [Header("Runtime State")]
    [SerializeField] private List<Unit> _spawnedUnits = new();
    [SerializeField] private Unit _selectedCaster;
    [SerializeField] private Unit _selectedTarget;
    [SerializeField] private Vector2Int _selectedTargetCell;

    public RoomContext ContextRoom => _roomContext;
    public RoomGrid ContextGrid => _roomGrid;
    public IReadOnlyList<Unit> SpawnedUnits => _spawnedUnits;
    public Unit SelectedCaster => _selectedCaster;
    public Unit SelectedTarget => _selectedTarget;
    public Vector2Int SelectedTargetCell => _selectedTargetCell;
    public IReadOnlyList<CreatureSpawnEntry> SpawnEntries => _spawnEntries;

    private void OnEnable()
    {
        ResolveReferences();
        ConfigureDebugRoom();
    }

    public void ResolveReferences()
    {
        if (_roomContext == null)
            _roomContext = GetComponent<RoomContext>() ?? GetComponentInParent<RoomContext>(includeInactive: true);
        if (_roomGrid == null)
            _roomGrid = GetComponent<RoomGrid>() ?? GetComponentInParent<RoomGrid>(includeInactive: true);
    }

    public void AutoFindContext()
    {
        _roomContext = FindFirstObjectByType<RoomContext>();
        if (_roomContext != null)
        {
            _roomGrid = _roomContext.RoomGrid;
            if (_roomGrid == null)
                _roomGrid = _roomContext.GetComponentInChildren<RoomGrid>(includeInactive: true);
        }
        Debug.Log($"[CreatureCombatDebugTool] AutoFind: RoomContext={(_roomContext != null ? _roomContext.name : "null")}, RoomGrid={(_roomGrid != null ? _roomGrid.name : "null")}.");
    }

    public void ValidateSetup()
    {
        bool valid = true;
        if (_roomContext == null) { Debug.LogWarning("[CreatureCombatDebugTool] VALIDATE: RoomContext is null."); valid = false; }
        if (_roomGrid == null) { Debug.LogWarning("[CreatureCombatDebugTool] VALIDATE: RoomGrid is null."); valid = false; }
        if (_configureGridOnEnable && _walkableTilemap == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] VALIDATE: Grid auto-configuration is enabled but the walkable Tilemap is null.");
            valid = false;
        }
        if (_startCombatOnEnable && (_roomContext == null || _roomContext.CombatController == null))
        {
            Debug.LogWarning("[CreatureCombatDebugTool] VALIDATE: Combat auto-start is enabled but no CombatRoomController is resolved.");
            valid = false;
        }
        for (int i = 0; i < _spawnEntries.Count; i++)
        {
            CreatureSpawnEntry e = _spawnEntries[i];
            string el = EntryLabel(e, i);
            if (e.prefab == null)
            {
                Debug.LogWarning($"[CreatureCombatDebugTool] VALIDATE: Entry [{i}] '{el}' has no prefab.");
                valid = false;
            }
            else if (e.prefab.GetComponent<Unit>() == null)
            {
                Debug.LogWarning($"[CreatureCombatDebugTool] VALIDATE: Entry [{i}] '{el}' prefab has no Unit component.");
                valid = false;
            }
            if (e.spawnCellMarker != null)
                Debug.Log($"[CreatureCombatDebugTool] VALIDATE: Entry [{i}] '{el}' uses spawnCellMarker '{e.spawnCellMarker.name}'.");
            else
                Debug.Log($"[CreatureCombatDebugTool] VALIDATE: Entry [{i}] '{el}' uses manual cell ({e.cell.x}, {e.cell.y}).");
        }
        if (valid) Debug.Log("[CreatureCombatDebugTool] VALIDATE: Setup looks valid.");
    }

    private void ConfigureDebugRoom()
    {
        if (_configureGridOnEnable)
        {
            if (_roomGrid == null || _walkableTilemap == null)
            {
                Debug.LogWarning("[CreatureCombatDebugTool] Cannot configure debug grid without RoomGrid and walkable Tilemap.", this);
            }
            else
            {
                _roomGrid.Configure(_walkableTilemap, _blockedTilemap);
            }
        }

        if (!_startCombatOnEnable)
            return;

        CombatRoomController combatController = _roomContext != null ? _roomContext.CombatController : null;
        if (combatController == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Cannot start debug combat because no CombatRoomController is resolved.", this);
            return;
        }

        if (combatController.IsDeploymentActive)
            combatController.TryStartCombat();
    }

    private Vector3Int ResolveSpawnCell(CreatureSpawnEntry entry)
    {
        if (entry.spawnCellMarker != null && _roomGrid != null)
            return _roomGrid.WorldToCell(entry.spawnCellMarker.position);

        return new Vector3Int(entry.cell.x, entry.cell.y, 0);
    }

    [ContextMenu("Spawn All")]
    public void SpawnAll()
    {
        if (_roomGrid == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] RoomGrid not assigned.");
            return;
        }

        for (int i = 0; i < _spawnEntries.Count; i++)
            SpawnSingle(i);
    }

    public Unit SpawnSingle(int index)
    {
        return SpawnSingle(index, null);
    }

    public Unit SpawnSingle(int index, UnitTeam? overrideTeam)
    {
        if (index < 0 || index >= _spawnEntries.Count)
        {
            Debug.LogWarning($"[CreatureCombatDebugTool] Index {index} out of range.");
            return null;
        }

        CreatureSpawnEntry entry = _spawnEntries[index];
        if (entry.prefab == null)
        {
            Debug.LogWarning($"[CreatureCombatDebugTool] Entry '{EntryLabel(entry, index)}' has no prefab assigned.");
            return null;
        }

        if (_roomGrid == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] RoomGrid not assigned.");
            return null;
        }

        if (_roomContext == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] RoomContext not assigned. Spawn aborted.");
            return null;
        }

        UnitTeam actualTeam = overrideTeam ?? entry.team;

        Vector3Int cell = ResolveSpawnCell(entry);
        Vector3 worldPos = _roomGrid.CellToWorld(cell);
        GameObject instance = Instantiate(entry.prefab, worldPos, Quaternion.identity, _roomContext.transform);
        instance.name = $"{EntryLabel(entry, index)}_Spawned";

        Unit unit = instance.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogWarning($"[CreatureCombatDebugTool] Prefab '{entry.prefab.name}' has no Unit component.");
            Destroy(instance);
            return null;
        }

        unit.SetAffiliation(actualTeam, unit.Faction);

        RecruitableUnitState lifeState = instance.GetComponent<RecruitableUnitState>();
        if (lifeState != null && lifeState.CurrentState != UnitLifecycleState.Alive)
            lifeState.SetState(UnitLifecycleState.Alive);

        UnitMovement movement = instance.GetComponent<UnitMovement>();
        if (movement != null)
        {
            if (!movement.AttachToGridAtCell(_roomGrid, cell))
            {
                Debug.LogWarning($"[CreatureCombatDebugTool] Failed to attach '{EntryLabel(entry, index)}' to cell {cell}. Spawn aborted.");
                Destroy(instance);
                return null;
            }
        }

        _spawnedUnits.Add(unit);
        string cellSource = entry.spawnCellMarker != null ? $"spawnCellMarker '{entry.spawnCellMarker.name}'" : $"cell ({entry.cell.x},{entry.cell.y})";
        Debug.Log($"[CreatureCombatDebugTool] Spawned '{EntryLabel(entry, index)}' ({actualTeam}) at grid cell ({cell.x},{cell.y}) from {cellSource}.");
        return unit;
    }

    [ContextMenu("Clear Spawned")]
    public void ClearSpawned()
    {
        for (int i = _spawnedUnits.Count - 1; i >= 0; i--)
        {
            Unit unit = _spawnedUnits[i];
            if (unit == null)
                continue;

            if (_roomContext != null)
                _roomContext.UnregisterUnit(unit);

            Destroy(unit.gameObject);
        }

        _spawnedUnits.Clear();
        ClearDeadSelection();
        Debug.Log("[CreatureCombatDebugTool] Cleared all spawned units.");
    }

    public void DestroySpawnedUnit(Unit unit)
    {
        if (unit == null)
            return;

        if (_roomContext != null)
            _roomContext.UnregisterUnit(unit);

        _spawnedUnits.Remove(unit);
        if (ReferenceEquals(_selectedCaster, unit)) _selectedCaster = null;
        if (ReferenceEquals(_selectedTarget, unit)) _selectedTarget = null;
        Destroy(unit.gameObject);
    }

    public void AssignSelectedCaster(Unit unit)
    {
        _selectedCaster = unit;
    }

    public void AssignSelectedTarget(Unit unit)
    {
        _selectedTarget = unit;
    }

    public void ClearSelection()
    {
        _selectedCaster = null;
        _selectedTarget = null;
    }

    public void UseFirstAllyAsCaster()
    {
        for (int i = 0; i < _spawnedUnits.Count; i++)
        {
            Unit u = _spawnedUnits[i];
            if (u != null && u.Team == UnitTeam.Ally && u.IsAlive)
            {
                _selectedCaster = u;
                Debug.Log($"[CreatureCombatDebugTool] Set caster to first ally: '{u.name}'.");
                return;
            }
        }
        Debug.LogWarning("[CreatureCombatDebugTool] No alive ally unit found.");
    }

    public void UseFirstEnemyAsTarget()
    {
        for (int i = 0; i < _spawnedUnits.Count; i++)
        {
            Unit u = _spawnedUnits[i];
            if (u != null && u.Team == UnitTeam.Enemy && u.IsAlive)
            {
                _selectedTarget = u;
                Debug.Log($"[CreatureCombatDebugTool] Set target to first enemy: '{u.name}'.");
                return;
            }
        }
        Debug.LogWarning("[CreatureCombatDebugTool] No alive enemy unit found.");
    }

    [ContextMenu("Force Full Skill Charge On Caster")]
    public void ForceFullSkillChargeOnCaster()
    {
        if (!AssertSelectedCaster(out SkillCaster caster))
            return;

        caster.AddAbilityCharge(caster.MaxCharge);
        Debug.Log($"[CreatureCombatDebugTool] Force-filled charge for '{_selectedCaster.name}': {caster.CurrentCharge}/{caster.MaxCharge}.");
    }

    [ContextMenu("Cast Selected Skill On Target")]
    public void CastSelectedSkillOnTarget()
    {
        if (_selectedCaster == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] No caster selected.");
            return;
        }
        if (_selectedTarget == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] No target selected.");
            return;
        }
        if (!AssertSelectedCaster(out SkillCaster caster))
            return;

        caster.AddAbilityCharge(caster.MaxCharge);
        string skillName = caster.Skill != null ? caster.Skill.DisplayName : "None";
        bool result = caster.TryUse(_selectedTarget);
        Debug.Log($"[CreatureCombatDebugTool] Cast skill '{skillName}' from '{_selectedCaster.name}' on '{_selectedTarget.name}': {(result ? "started" : "rejected")}.");
    }

    [ContextMenu("Cast Selected Skill On Cell")]
    public void CastSelectedSkillOnCell()
    {
        if (!AssertSelectedCaster(out SkillCaster caster))
            return;

        caster.AddAbilityCharge(caster.MaxCharge);
        string skillName = caster.Skill != null ? caster.Skill.DisplayName : "None";
        bool result = caster.TryUseGroundCell(_selectedTargetCell);
        Debug.Log($"[CreatureCombatDebugTool] Cast skill '{skillName}' from '{_selectedCaster.name}' on cell {_selectedTargetCell}: {(result ? "started" : "rejected")}.");
    }

    public void BasicAttackOnTarget()
    {
        if (_selectedCaster == null || _selectedTarget == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Need both caster and target selected for basic attack.");
            return;
        }
        bool result = _selectedCaster.TryBasicAttackForDebug(_selectedTarget);
        Debug.Log($"[CreatureCombatDebugTool] Basic attack from '{_selectedCaster.name}' on '{_selectedTarget.name}': {(result ? "executed" : "rejected")}.");
    }

    public void KillTarget(Unit target)
    {
        if (target == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] No target to kill.");
            return;
        }
        LifeController life = target.GetComponent<LifeController>();
        if (life == null)
        {
            Debug.LogWarning($"[CreatureCombatDebugTool] '{target.name}' has no LifeController.");
            return;
        }
        life.TakeDamage(int.MaxValue);
        Debug.Log($"[CreatureCombatDebugTool] Killed '{target.name}'.");
    }

    [ContextMenu("Log Current State")]
    public void LogCurrentState()
    {
        Debug.Log("[CreatureCombatDebugTool] == State ==");
        Debug.Log($"RoomContext: {(_roomContext != null ? _roomContext.name : "null")}");
        Debug.Log($"RoomGrid: {(_roomGrid != null ? _roomGrid.name : "null")}");
        Debug.Log($"Spawn entries: {_spawnEntries.Count}");
        Debug.Log($"Spawned units: {_spawnedUnits.Count}");
        for (int i = 0; i < _spawnedUnits.Count; i++)
        {
            Unit u = _spawnedUnits[i];
            if (u == null)
                Debug.Log($"  [{i}] (destroyed)");
            else
                Debug.Log($"  [{i}] {u.name} | {u.Team} | {u.Faction} | HP={u.CurrentHealth}/{u.MaxHealth} | Alive={u.IsAlive} | Lifecycle={u.LifecycleState}");
        }
        Debug.Log($"Selected caster: {(_selectedCaster != null ? _selectedCaster.name : "null")}");
        SkillCaster sc = _selectedCaster != null ? _selectedCaster.GetComponent<SkillCaster>() : null;
        Debug.Log($"  Skill: {(sc != null && sc.Skill != null ? sc.Skill.DisplayName : "none")} Charge: {(sc != null ? $"{sc.CurrentCharge}/{sc.MaxCharge}" : "N/A")}");
        Debug.Log($"Selected target: {(_selectedTarget != null ? $"{_selectedTarget.name} (HP={_selectedTarget.CurrentHealth})" : "null")}");
        Debug.Log($"Selected target cell: {_selectedTargetCell}");
        Debug.Log("[CreatureCombatDebugTool] == End ==");
    }

    [ContextMenu("Log Movement Audit")]
    public void LogMovementAudit()
    {
        if (_roomGrid == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Movement audit requires a RoomGrid.", this);
            return;
        }

        Debug.Log("[CreatureCombatDebugTool] == Movement Audit ==", this);
        for (int i = 0; i < _spawnedUnits.Count; i++)
        {
            Unit unit = _spawnedUnits[i];
            if (unit == null)
                continue;

            LogUnitMovementAudit(unit);
        }
        Debug.Log("[CreatureCombatDebugTool] == End Movement Audit ==", this);
    }

    private void LogUnitMovementAudit(Unit unit)
    {
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        TargetingStrategy targeting = unit.GetComponent<TargetingStrategy>();
        SkillCaster skillCaster = unit.GetComponent<SkillCaster>();
        IBasicAction action = unit.Action;

        if (movement == null || !movement.TryGetLogicalCell(out Vector3Int unitCell))
        {
            Debug.Log($"[CreatureCombatDebugTool] {unit.name}: no logical movement cell.", unit);
            return;
        }

        Unit basicTarget = targeting != null
            ? targeting.SelectBasicActionTarget(unit, action, null)
            : null;
        Unit moveTarget = SpacingEvaluator.GetNearestVisibleHostile(unit);

        string basicTargetInfo = FormatTargetAudit(unit, unitCell, basicTarget, action != null ? action.RangeInCells : 0);
        string moveTargetInfo = FormatTargetAudit(unit, unitCell, moveTarget, action != null ? action.PreferredDistanceInCells : 0);
        SkillData skill = skillCaster != null ? skillCaster.Skill : null;
        string skillInfo = skill != null
            ? $"{skill.DisplayName} range={skill.RangeInCells} charge={skillCaster.CurrentCharge:F0}/{skillCaster.MaxCharge:F0}"
            : "none";

        string pathInfo = BuildPathAudit(unit, unitCell, moveTarget, action);

        Debug.Log(
            $"[CreatureCombatDebugTool] {unit.name} cell={unitCell} basicRange={action?.RangeInCells ?? 0} " +
            $"preferredDistance={action?.PreferredDistanceInCells ?? 0} moving={movement.IsMoving} | " +
            $"basicTarget={basicTargetInfo} | moveTarget={moveTargetInfo} | skill={skillInfo} | {pathInfo}",
            unit);
    }

    private string FormatTargetAudit(Unit source, Vector3Int sourceCell, Unit target, int rangeInCells)
    {
        if (target == null)
            return "none";

        Vector3Int targetCell = GridUnitCellUtility.ResolveUnitCell(_roomGrid, target);
        int distance = GridNavigationUtility.GetCellDistance(sourceCell, targetCell);
        bool inRange = GridNavigationUtility.IsWithinCellRange(sourceCell, targetCell, rangeInCells);
        return $"{target.name} cell={targetCell} distance={distance} range={rangeInCells} inRange={inRange}";
    }

    private string BuildPathAudit(Unit unit, Vector3Int originCell, Unit moveTarget, IBasicAction action)
    {
        if (moveTarget == null || action == null)
            return "path=not-requested";

        Vector3Int targetCell = GridUnitCellUtility.ResolveUnitCell(_roomGrid, moveTarget);
        int preferredDistance = unit.GetPreferredDistance(action);
        if (GridNavigationUtility.IsWithinCellRange(originCell, targetCell, action.RangeInCells))
            return "path=not-needed";

        if (!_roomGrid.TryFindWalkableCellInRange(
                targetCell,
                originCell,
                preferredDistance,
                unit,
                out Vector3Int desiredAttackCell))
        {
            return $"path=failed reason=no-desired-cell targetCell={targetCell}";
        }

        List<Vector3Int> path = GridPathfinder.FindPath(_roomGrid, originCell, desiredAttackCell, unit);
        return path.Count > 1
            ? $"path=found desiredCell={desiredAttackCell} cells={path.Count} next={path[1]}"
            : $"path=failed desiredCell={desiredAttackCell}";
    }

    private bool AssertSelectedCaster(out SkillCaster caster)
    {
        caster = null;
        if (_selectedCaster == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] No caster selected.");
            return false;
        }

        caster = _selectedCaster.GetComponent<SkillCaster>();
        if (caster == null)
        {
            Debug.LogWarning($"[CreatureCombatDebugTool] '{_selectedCaster.name}' has no SkillCaster.");
            return false;
        }

        return true;
    }

    private void ClearDeadSelection()
    {
        _spawnedUnits.RemoveAll(u => u == null || u.gameObject == null);
        if (_selectedCaster == null || _selectedCaster.gameObject == null)
            _selectedCaster = null;
        if (_selectedTarget == null || _selectedTarget.gameObject == null)
            _selectedTarget = null;
    }
}

#if UNITY_EDITOR
internal static class GeneratedVariantCache
{
    private static List<UnitData> _cached;
    private static string[] _cachedGuids;

    public static IReadOnlyList<UnitData> Load()
    {
        if (_cached == null)
            Refresh();
        return _cached;
    }

    public static void Refresh()
    {
        _cachedGuids = AssetDatabase.FindAssets("t:UnitData", new[] { "Assets/Core/Data/Scriptable Objects/Creatures/Generated" });
        var excludedGuids = new HashSet<string>();
        string excludedPath = "Assets/Core/Data/Scriptable Objects/Creatures/Generated/_Excluded";
        string[] excludedAssets = AssetDatabase.FindAssets("t:UnitData", new[] { excludedPath });
        foreach (string g in excludedAssets)
            excludedGuids.Add(g);

        _cached = new List<UnitData>();
        foreach (string guid in _cachedGuids)
        {
            if (excludedGuids.Contains(guid))
                continue;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnitData data = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data != null)
                _cached.Add(data);
        }
    }

    public static UnitFaction ParseFaction(UnitData data)
    {
        string path = AssetDatabase.GetAssetPath(data);
        if (path.Contains("/Human/", System.StringComparison.OrdinalIgnoreCase))
            return UnitFaction.Human;
        if (path.Contains("/Orc/", System.StringComparison.OrdinalIgnoreCase))
            return UnitFaction.Orc;
        return UnitFaction.None;
    }
}

[CustomEditor(typeof(CreatureCombatDebugTool))]
internal class CreatureCombatDebugToolEditor : Editor
{
    private SerializedProperty _roomContextProp;
    private SerializedProperty _roomGridProp;
    private SerializedProperty _walkableTilemapProp;
    private SerializedProperty _blockedTilemapProp;
    private SerializedProperty _configureGridOnEnableProp;
    private SerializedProperty _startCombatOnEnableProp;
    private SerializedProperty _spawnEntriesProp;
    private SerializedProperty _selectedCasterProp;
    private SerializedProperty _selectedTargetProp;
    private SerializedProperty _selectedTargetCellProp;
    private ReorderableList _spawnEntriesList;
    private bool _showSpawnedUnits = true;

    private void OnEnable()
    {
        _roomContextProp = serializedObject.FindProperty("_roomContext");
        _roomGridProp = serializedObject.FindProperty("_roomGrid");
        _walkableTilemapProp = serializedObject.FindProperty("_walkableTilemap");
        _blockedTilemapProp = serializedObject.FindProperty("_blockedTilemap");
        _configureGridOnEnableProp = serializedObject.FindProperty("_configureGridOnEnable");
        _startCombatOnEnableProp = serializedObject.FindProperty("_startCombatOnEnable");
        _spawnEntriesProp = serializedObject.FindProperty("_spawnEntries");
        _selectedCasterProp = serializedObject.FindProperty("_selectedCaster");
        _selectedTargetProp = serializedObject.FindProperty("_selectedTarget");
        _selectedTargetCellProp = serializedObject.FindProperty("_selectedTargetCell");

        _spawnEntriesList = new ReorderableList(serializedObject, _spawnEntriesProp, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawn Entries"),
            drawElementCallback = DrawSpawnEntryElement,
            elementHeightCallback = CalculateSpawnEntryHeight
        };
    }

    public override void OnInspectorGUI()
    {
        CreatureCombatDebugTool tool = (CreatureCombatDebugTool)target;
        serializedObject.Update();

        DrawContextSection(tool);
        EditorGUILayout.Space(4);
        DrawSpawnEntriesSection(tool);
        EditorGUILayout.Space(4);
        DrawSelectionSection(tool);
        EditorGUILayout.Space(4);
        DrawManualActionsSection(tool);
        EditorGUILayout.Space(4);
        DrawSpawnedUnitsSection(tool);
        EditorGUILayout.Space(4);
        DrawGeneratedVariantBrowser(tool);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawContextSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("1. Context", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_roomContextProp);
        EditorGUILayout.PropertyField(_roomGridProp);
        EditorGUILayout.PropertyField(_walkableTilemapProp);
        EditorGUILayout.PropertyField(_blockedTilemapProp);
        EditorGUILayout.PropertyField(_configureGridOnEnableProp);
        EditorGUILayout.PropertyField(_startCombatOnEnableProp);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto Find Context", GUILayout.Height(22)))
        {
            tool.AutoFindContext();
            EditorUtility.SetDirty(target);
            serializedObject.Update();
        }
        if (GUILayout.Button("Validate Setup", GUILayout.Height(22)))
        {
            tool.ValidateSetup();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawSpawnEntriesSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("2. Spawn Entries", EditorStyles.boldLabel);
        _spawnEntriesList.DoLayoutList();

        EditorGUILayout.BeginHorizontal();
        bool inPlayMode = EditorApplication.isPlaying;
        EditorGUI.BeginDisabledGroup(!inPlayMode);
        if (GUILayout.Button("Spawn All", GUILayout.Height(24)))
        {
            tool.SpawnAll();
            serializedObject.Update();
        }
        if (GUILayout.Button("Clear Spawned", GUILayout.Height(24)))
        {
            tool.ClearSpawned();
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private float CalculateSpawnEntryHeight(int index)
    {
        return EditorGUIUtility.singleLineHeight * 4 + 8f;
    }

    private void DrawSpawnEntryElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = _spawnEntriesProp.GetArrayElementAtIndex(index);
        if (element == null)
            return;

        CreatureCombatDebugTool tool = (CreatureCombatDebugTool)target;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        float y = rect.y;

        SerializedProperty prefabProp = element.FindPropertyRelative("prefab");
        SerializedProperty teamProp = element.FindPropertyRelative("team");
        SerializedProperty spawnCellMarkerProp = element.FindPropertyRelative("spawnCellMarker");
        SerializedProperty cellProp = element.FindPropertyRelative("cell");
        SerializedProperty cellX = cellProp.FindPropertyRelative("x");
        SerializedProperty cellY = cellProp.FindPropertyRelative("y");

        float indexLabelWidth = 60f;
        EditorGUI.LabelField(new Rect(rect.x, y, indexLabelWidth, lineHeight), $"Entry [{index}]", EditorStyles.miniBoldLabel);
        EditorGUI.PropertyField(new Rect(rect.x + indexLabelWidth + 4f, y, rect.width - indexLabelWidth - 4f, lineHeight), prefabProp, GUIContent.none);
        y += lineHeight + spacing;

        EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, lineHeight), teamProp, GUIContent.none);
        y += lineHeight + spacing;

        float markerWidth = rect.width * 0.6f;
        EditorGUI.PropertyField(new Rect(rect.x, y, markerWidth, lineHeight), spawnCellMarkerProp, GUIContent.none);
        float cellXPos = rect.x + markerWidth + 6f;
        float cellFieldWidth = (rect.width - markerWidth - 16f) * 0.5f;
        EditorGUI.LabelField(new Rect(cellXPos, y, 16f, lineHeight), "X", EditorStyles.miniLabel);
        cellX.intValue = EditorGUI.IntField(new Rect(cellXPos + 14f, y, cellFieldWidth, lineHeight), cellX.intValue);
        EditorGUI.LabelField(new Rect(cellXPos + 14f + cellFieldWidth + 2f, y, 16f, lineHeight), "Y", EditorStyles.miniLabel);
        cellY.intValue = EditorGUI.IntField(new Rect(cellXPos + 14f + cellFieldWidth + 16f, y, cellFieldWidth, lineHeight), cellY.intValue);
        y += lineHeight + spacing;

        bool inPlayMode = EditorApplication.isPlaying;
        EditorGUI.BeginDisabledGroup(!inPlayMode);
        if (GUI.Button(new Rect(rect.x, y, rect.width, lineHeight), "Spawn"))
        {
            serializedObject.ApplyModifiedProperties();
            tool.SpawnSingle(index);
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
    }

    private void DrawSelectionSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("3. Selection", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_selectedCasterProp);
        EditorGUILayout.PropertyField(_selectedTargetProp);
        EditorGUILayout.PropertyField(_selectedTargetCellProp);

        EditorGUILayout.BeginHorizontal();
        bool inPlayMode = EditorApplication.isPlaying;
        if (GUILayout.Button("Clear Selection", GUILayout.Height(22)))
        {
            tool.ClearSelection();
            serializedObject.Update();
        }
        EditorGUI.BeginDisabledGroup(!inPlayMode);
        if (GUILayout.Button("First Ally as Caster", GUILayout.Height(22)))
        {
            tool.UseFirstAllyAsCaster();
            serializedObject.Update();
        }
        if (GUILayout.Button("First Enemy as Target", GUILayout.Height(22)))
        {
            tool.UseFirstEnemyAsTarget();
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawManualActionsSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("4. Manual Actions", EditorStyles.boldLabel);
        bool inPlayMode = EditorApplication.isPlaying;
        EditorGUI.BeginDisabledGroup(!inPlayMode);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Charge", GUILayout.Height(24)))
        {
            tool.ForceFullSkillChargeOnCaster();
        }
        if (GUILayout.Button("Cast Skill on Target", GUILayout.Height(24)))
        {
            tool.CastSelectedSkillOnTarget();
        }
        if (GUILayout.Button("Cast Skill on Cell", GUILayout.Height(24)))
        {
            tool.CastSelectedSkillOnCell();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Basic Attack Target", GUILayout.Height(24)))
        {
            tool.BasicAttackOnTarget();
        }
        if (GUILayout.Button("Kill Selected Target", GUILayout.Height(24)))
        {
            tool.KillTarget(tool.SelectedTarget);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Log Current State", GUILayout.Height(22)))
        {
            tool.LogCurrentState();
        }
        if (GUILayout.Button("Log Movement Audit", GUILayout.Height(22)))
        {
            tool.LogMovementAudit();
        }

        EditorGUI.EndDisabledGroup();
    }

    private void DrawSpawnedUnitsSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("5. Spawned Units", EditorStyles.boldLabel);

        int count = tool.SpawnedUnits.Count;
        if (count == 0)
        {
            EditorGUILayout.HelpBox("No spawned units. Use Spawn All or individual Spawn buttons.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        _showSpawnedUnits = EditorGUILayout.Foldout(_showSpawnedUnits, $"Units ({count})", true);
        EditorGUILayout.EndHorizontal();
        if (!_showSpawnedUnits)
            return;

        for (int i = 0; i < count; i++)
        {
            Unit u = tool.SpawnedUnits[i];
            if (u == null)
            {
                EditorGUILayout.HelpBox($"[{i}] (destroyed)", MessageType.Warning);
                continue;
            }

            DrawSpawnedUnitRow(tool, u, i);
        }
    }

    private void DrawSpawnedUnitRow(CreatureCombatDebugTool tool, Unit unit, int index)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float btnWidth = 80f;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        string teamStr = unit.Team == UnitTeam.Ally ? "Ally" : "Enemy";
        string hpStr = $"{unit.CurrentHealth}/{unit.MaxHealth}";

        EditorGUILayout.LabelField($"[{index}] {unit.name}  |  {teamStr}  |  HP: {hpStr}", EditorStyles.miniBoldLabel);

        string cellStr = "?";
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null && movement.TryGetLogicalCell(out Vector3Int cell))
            cellStr = $"({cell.x}, {cell.y})";
        string lifecycleStr = unit.LifecycleState.ToString();
        EditorGUILayout.LabelField($"  Cell: {cellStr}  |  State: {lifecycleStr}", EditorStyles.miniLabel);

        bool inPlayMode = EditorApplication.isPlaying;
        EditorGUI.BeginDisabledGroup(!inPlayMode);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set as Caster", GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.AssignSelectedCaster(unit);
            serializedObject.Update();
        }
        if (GUILayout.Button("Set as Target", GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.AssignSelectedTarget(unit);
            serializedObject.Update();
        }
        if (GUILayout.Button("Kill", GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.KillTarget(unit);
            serializedObject.Update();
        }
        if (GUILayout.Button("Destroy", GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.DestroySpawnedUnit(unit);
            serializedObject.Update();
            EditorUtility.SetDirty(target);
            return;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    #region Generated Variant Browser

    private enum VariantFactionFilter { All, Human, Orc }
    private enum VariantRoleFilter { All, DPS, Tank, Support }

    private VariantFactionFilter _variantFactionFilter = VariantFactionFilter.All;
    private VariantRoleFilter _variantRoleFilter = VariantRoleFilter.All;
    private string _variantSearchFilter = string.Empty;
    private Vector2 _variantScrollPos;
    private bool _showVariantBrowser = true;

    private void DrawGeneratedVariantBrowser(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("6. Generated Variant Browser", EditorStyles.boldLabel);

        List<UnitData> allVariants;
        try
        {
            allVariants = GeneratedVariantCache.Load() as List<UnitData>;
        }
        catch
        {
            EditorGUILayout.HelpBox("Failed to load generated variants. Check Console.", MessageType.Warning);
            return;
        }

        if (allVariants == null || allVariants.Count == 0)
        {
            EditorGUILayout.HelpBox("No generated variants found.", MessageType.Info);
            if (GUILayout.Button("Refresh Cache", GUILayout.Height(20)))
                GeneratedVariantCache.Refresh();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        _showVariantBrowser = EditorGUILayout.Foldout(_showVariantBrowser, $"Variants ({allVariants.Count})", true);
        if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(18)))
            GeneratedVariantCache.Refresh();
        EditorGUILayout.EndHorizontal();

        if (!_showVariantBrowser)
            return;

        EditorGUILayout.Space(2);

        _variantFactionFilter = (VariantFactionFilter)EditorGUILayout.EnumPopup("Faction", _variantFactionFilter);
        _variantRoleFilter = (VariantRoleFilter)EditorGUILayout.EnumPopup("Role", _variantRoleFilter);
        _variantSearchFilter = EditorGUILayout.TextField("Search", _variantSearchFilter);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Results", EditorStyles.miniBoldLabel);

        var filtered = new List<UnitData>();
        foreach (UnitData v in allVariants)
        {
            if (v == null)
                continue;

            UnitFaction faction = GeneratedVariantCache.ParseFaction(v);
            if (_variantFactionFilter == VariantFactionFilter.Human && faction != UnitFaction.Human)
                continue;
            if (_variantFactionFilter == VariantFactionFilter.Orc && faction != UnitFaction.Orc)
                continue;

            if (_variantRoleFilter == VariantRoleFilter.DPS && v.role != UnitRole.DPS)
                continue;
            if (_variantRoleFilter == VariantRoleFilter.Tank && v.role != UnitRole.Tank)
                continue;
            if (_variantRoleFilter == VariantRoleFilter.Support && v.role != UnitRole.Support)
                continue;

            string search = _variantSearchFilter.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                bool nameMatch = v.name.ToLowerInvariant().Contains(search);
                bool skillMatch = v.skill != null && v.skill.DisplayName.ToLowerInvariant().Contains(search);
                if (!nameMatch && !skillMatch)
                    continue;
            }

            filtered.Add(v);
        }

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox("No variants match the current filters.", MessageType.Info);
            return;
        }

        float rowHeight = EditorGUIUtility.singleLineHeight * 3 + 10f;
        _variantScrollPos = EditorGUILayout.BeginScrollView(_variantScrollPos, GUILayout.MaxHeight(400));

        for (int i = 0; i < filtered.Count; i++)
        {
            UnitData v = filtered[i];
            DrawVariantRow(tool, v, i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawVariantRow(CreatureCombatDebugTool tool, UnitData variant, int index)
    {
        UnitFaction faction = GeneratedVariantCache.ParseFaction(variant);
        string factionStr = faction == UnitFaction.Human ? "Human" : faction == UnitFaction.Orc ? "Orc" : "?";
        string roleStr = variant.role.ToString();
        string skillStr = variant.skill != null ? variant.skill.DisplayName : "No Skill";
        string prefabStr = variant.unitPrefab != null ? variant.unitPrefab.name : "No Prefab";

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField($"[{index}] {variant.name}", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Faction: {factionStr}  |  Role: {roleStr}  |  Prefab: {prefabStr}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Skill: {skillStr}", EditorStyles.miniLabel);

        bool inPlayMode = EditorApplication.isPlaying;
        EditorGUI.BeginDisabledGroup(!inPlayMode);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn as Ally", GUILayout.Height(22)))
        {
            SpawnGeneratedVariant(tool, variant, UnitTeam.Ally);
        }
        if (GUILayout.Button("Spawn as Enemy", GUILayout.Height(22)))
        {
            SpawnGeneratedVariant(tool, variant, UnitTeam.Enemy);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndVertical();
    }

    private void SpawnGeneratedVariant(CreatureCombatDebugTool tool, UnitData variant, UnitTeam team)
    {
        if (variant == null || variant.unitPrefab == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Variant has no prefab assigned.");
            return;
        }

        RoomGrid grid = tool.ContextGrid;
        RoomContext ctx = tool.ContextRoom;
        if (grid == null || ctx == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] RoomGrid or RoomContext not assigned.");
            return;
        }

        Vector3 spawnPos = grid.CellToWorld(Vector3Int.zero);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(variant.unitPrefab, ctx.transform);
        if (instance == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Failed to instantiate prefab.");
            return;
        }

        instance.name = $"{variant.name}_Spawned";
        instance.transform.position = spawnPos;

        Unit unit = instance.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Prefab has no Unit component.");
            Object.DestroyImmediate(instance);
            return;
        }

        var unitSO = new SerializedObject(unit);
        unitSO.FindProperty("_unitData").objectReferenceValue = variant;
        unitSO.ApplyModifiedProperties();

        SkillCaster skillCaster = unit.GetComponent<SkillCaster>();
        if (skillCaster != null)
        {
            var skillSO = new SerializedObject(skillCaster);
            skillSO.FindProperty("_overrideSkill").objectReferenceValue = variant.skill;
            skillSO.ApplyModifiedProperties();

            typeof(SkillCaster).GetField("_resolvedSkill",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(skillCaster, null);
        }

        typeof(Creature).GetMethod("Initialize",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(unit, new object[] { variant });

        unit.SetAffiliation(team, unit.Faction);

        RecruitableUnitState lifeState = instance.GetComponent<RecruitableUnitState>();
        if (lifeState != null && lifeState.CurrentState != UnitLifecycleState.Alive)
            lifeState.SetState(UnitLifecycleState.Alive);

        int spawnCount = tool.SpawnedUnits.Count;
        if (!TrySnapToGrid(instance, grid, out Vector3Int cell))
        {
            cell = FindClosestEmptyCell(grid, unit);
            if (!TrySnapToCell(instance, grid, cell))
            {
                Debug.LogWarning($"[CreatureCombatDebugTool] Could not place '{variant.name}' on grid.");
                Object.DestroyImmediate(instance);
                return;
            }
        }

        ctx.RegisterUnit(unit);
        var spawnedList = (List<Unit>)typeof(CreatureCombatDebugTool)
            .GetField("_spawnedUnits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(tool);
        spawnedList?.Add(unit);

        Debug.Log($"[CreatureCombatDebugTool] Spawned generated variant '{variant.name}' ({team}) at cell ({cell.x},{cell.y}).");
    }

    private static bool TrySnapToGrid(GameObject instance, RoomGrid grid, out Vector3Int cell)
    {
        cell = Vector3Int.zero;
        UnitMovement movement = instance.GetComponent<UnitMovement>();
        if (movement == null)
            return false;

        movement.SetGrid(grid);
        movement.ForceSyncToWorldPosition(instance.transform.position);

        if (!movement.TryGetLogicalCell(out cell))
        {
            Vector3Int nearest = grid.WorldToCell(instance.transform.position);
            if (!movement.AttachToGridAtCell(grid, nearest))
                return false;
            cell = nearest;
        }
        return true;
    }

    private static bool TrySnapToCell(GameObject instance, RoomGrid grid, Vector3Int cell)
    {
        UnitMovement movement = instance.GetComponent<UnitMovement>();
        if (movement == null)
            return false;

        movement.SetGrid(grid);
        return movement.AttachToGridAtCell(grid, cell);
    }

    private static Vector3Int FindClosestEmptyCell(RoomGrid grid, Unit unit)
    {
        for (int r = 0; r < 10; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        continue;
                    Vector3Int candidate = new Vector3Int(dx, dy, 0);
                    if (grid.IsCellWalkable(candidate, unit))
                        return candidate;
                }
            }
        }
        return Vector3Int.zero;
    }

    #endregion
}
#endif
