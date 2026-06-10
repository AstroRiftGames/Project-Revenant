#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CreatureCombatDebugTool : MonoBehaviour
{
    [SerializeField] private RoomContext _roomContext;
    [SerializeField] private RoomGrid _roomGrid;
    [SerializeField] private Tilemap _walkableTilemap;
    [SerializeField] private Tilemap _blockedTilemap;
    [SerializeField] private bool _configureGridOnEnable;
    [SerializeField] private bool _startCombatOnEnable;

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
[CustomEditor(typeof(CreatureCombatDebugTool))]
internal class CreatureCombatDebugToolEditor : Editor
{
    private SerializedProperty _roomContextProp;
    private SerializedProperty _roomGridProp;
    private SerializedProperty _walkableTilemapProp;
    private SerializedProperty _blockedTilemapProp;
    private SerializedProperty _configureGridOnEnableProp;
    private SerializedProperty _startCombatOnEnableProp;
    private SerializedProperty _selectedCasterProp;
    private SerializedProperty _selectedTargetProp;
    private SerializedProperty _selectedTargetCellProp;
    private bool _showSpawnedUnits = true;

    private void OnEnable()
    {
        _roomContextProp = serializedObject.FindProperty("_roomContext");
        _roomGridProp = serializedObject.FindProperty("_roomGrid");
        _walkableTilemapProp = serializedObject.FindProperty("_walkableTilemap");
        _blockedTilemapProp = serializedObject.FindProperty("_blockedTilemap");
        _configureGridOnEnableProp = serializedObject.FindProperty("_configureGridOnEnable");
        _startCombatOnEnableProp = serializedObject.FindProperty("_startCombatOnEnable");
        _selectedCasterProp = serializedObject.FindProperty("_selectedCaster");
        _selectedTargetProp = serializedObject.FindProperty("_selectedTarget");
        _selectedTargetCellProp = serializedObject.FindProperty("_selectedTargetCell");
    }

    public override void OnInspectorGUI()
    {
        CreatureCombatDebugTool tool = (CreatureCombatDebugTool)target;
        serializedObject.Update();

        DrawContextSection(tool);
        EditorGUILayout.Space(4);
        DrawSelectionSection(tool);
        EditorGUILayout.Space(4);
        DrawManualActionsSection(tool);
        EditorGUILayout.Space(4);
        DrawSpawnedUnitsSection(tool);

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

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Log Current State", GUILayout.Height(22)))
        {
            tool.LogCurrentState();
        }
        if (GUILayout.Button("Log Movement Audit", GUILayout.Height(22)))
        {
            tool.LogMovementAudit();
        }
        if (GUILayout.Button("Clear Spawned Units", GUILayout.Height(22)))
        {
            tool.ClearSpawned();
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
    }

    private void DrawSpawnedUnitsSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("5. Spawned Units", EditorStyles.boldLabel);

        int count = tool.SpawnedUnits.Count;
        if (count == 0)
        {
            EditorGUILayout.HelpBox("No spawned units. Use the Creature Variant Lab to spawn units.", MessageType.Info);
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
}
#endif
