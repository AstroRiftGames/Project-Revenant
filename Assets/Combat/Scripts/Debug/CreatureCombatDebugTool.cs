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

        TryStartCombatIfReady();
    }

    public bool TryStartCombatIfReady()
    {
        CombatRoomController combatController = _roomContext != null ? _roomContext.CombatController : null;
        if (combatController == null || _roomContext == null)
        {
            Debug.LogWarning("[CreatureCombatDebugTool] Cannot start debug combat because no CombatRoomController is resolved.", this);
            return false;
        }

        bool hasAlly = false;
        bool hasEnemy = false;
        IReadOnlyList<Unit> roomUnits = _roomContext.Units;
        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit unit = roomUnits[i];
            if (unit == null || !unit.IsAlive || unit.LifecycleState != UnitLifecycleState.Alive)
                continue;

            if (unit.Team == UnitTeam.Ally)
                hasAlly = true;
            else if (unit.Team == UnitTeam.Enemy)
                hasEnemy = true;
        }

        if (!hasAlly || !hasEnemy)
            return false;

        if (combatController.IsResolved)
            combatController.ResetEncounter();

        return combatController.IsDeploymentActive && combatController.TryStartCombat();
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

    public void RegisterSpawnedUnit(Unit unit)
    {
        if (unit != null && !_spawnedUnits.Contains(unit))
            _spawnedUnits.Add(unit);
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
#if UNITY_EDITOR
        bool result = caster.TryUseForDebug(_selectedTarget, out string rejectReason);
        if (result)
            Debug.Log($"[CreatureCombatDebugTool] Cast skill '{skillName}' from '{_selectedCaster.name}' on '{_selectedTarget.name}': started.");
        else
            Debug.LogWarning($"[CreatureCombatDebugTool] Cast skill '{skillName}' from '{_selectedCaster.name}' on '{_selectedTarget.name}' rejected: {rejectReason}");
#else
        bool result = caster.TryUse(_selectedTarget);
        Debug.Log($"[CreatureCombatDebugTool] Cast skill '{skillName}' from '{_selectedCaster.name}' on '{_selectedTarget.name}': {(result ? "started" : "rejected")}.");
#endif
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
        life.TakeDamage(int.MaxValue, null, DamageSourceKind.Debug);
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
    private static class Tips
    {
        public const string RoomContext = "Room registry used to discover and control existing runtime units.";
        public const string RoomGrid = "Grid used for logical cells, movement and cell-targeted skills.";
        public const string Walkable = "Optional walkable Tilemap used only when Configure Grid On Enable is active.";
        public const string Blocked = "Optional blocked Tilemap used only for debug grid configuration.";
        public const string ConfigureGrid = "Reconfigures RoomGrid from the assigned Tilemaps when this component enables.";
        public const string StartCombat = "Attempts to start combat after both an alive Ally and Enemy are registered.";
        public const string AutoFind = "Finds RoomContext and RoomGrid in the active scene. It does not create units.";
        public const string Validate = "Logs missing context, grid or combat setup without changing gameplay.";
        public const string Caster = "Existing runtime unit that will cast skills or basic attacks.";
        public const string Target = "Existing runtime unit that receives target-based debug actions.";
        public const string TargetCell = "Grid cell used by Cast Skill on Cell.";
        public const string ClearSelection = "Clears caster and target references only.";
        public const string FirstAlly = "Uses the first alive Ally spawned by Creature Variant Lab as caster.";
        public const string FirstEnemy = "Uses the first alive Enemy or dummy spawned by Creature Variant Lab as target.";
        public const string ForceCharge = "Fills the selected caster's current skill charge for runtime testing.";
        public const string CastTarget = "Forces full charge and asks SkillCaster to cast its current skill on the selected target.";
        public const string CastCell = "Forces full charge and asks SkillCaster to cast its current skill on Selected Target Cell.";
        public const string BasicAttack = "Requests a debug basic attack from the selected caster to the selected target.";
        public const string KillTarget = "Applies lethal debug damage to the selected target.";
        public const string LogState = "Logs context, spawned units, health, selection and skill charge.";
        public const string LogMovement = "Logs logical cells, targeting ranges and pathfinding diagnostics.";
        public const string ClearUnits = "Removes only units registered as spawned by Creature Variant Lab.";
    }

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
    private bool _showAdvancedDebug;

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
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.hierarchyChanged += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.hierarchyChanged -= Repaint;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        Repaint();
    }

    public override void OnInspectorGUI()
    {
        CreatureCombatDebugTool tool = (CreatureCombatDebugTool)target;
        serializedObject.Update();

        if (tool.SpawnedUnits.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No test units found.\nSpawn units from Creature Variant Lab first.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.LabelField($"Test Units: {tool.SpawnedUnits.Count}", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_selectedCasterProp, new GUIContent("Caster", Tips.Caster));
            EditorGUILayout.PropertyField(_selectedTargetProp, new GUIContent("Target", Tips.Target));
            EditorGUILayout.PropertyField(_selectedTargetCellProp, new GUIContent("Target Cell", Tips.TargetCell));

            DrawQuickSelection(tool);
            DrawPrimaryCombatActions(tool);
        }

        _showAdvancedDebug = EditorGUILayout.Foldout(_showAdvancedDebug, "Advanced Debug", true);
        if (_showAdvancedDebug)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            DrawContextSection(tool);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Log Movement Audit", Tips.LogMovement)))
                tool.LogMovementAudit();
            if (GUILayout.Button(new GUIContent("Clear Spawned Units", Tips.ClearUnits)))
            {
                tool.ClearSpawned();
                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawQuickSelection(CreatureCombatDebugTool tool)
    {
        bool inPlayMode = EditorApplication.isPlaying;
        bool hasAlly = HasAliveUnit(tool, UnitTeam.Ally);
        bool hasEnemy = HasAliveUnit(tool, UnitTeam.Enemy);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!inPlayMode || !hasAlly);
        if (GUILayout.Button(new GUIContent("First Ally", Tips.FirstAlly)))
        {
            tool.UseFirstAllyAsCaster();
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!inPlayMode || !hasEnemy);
        if (GUILayout.Button(new GUIContent("First Enemy", Tips.FirstEnemy)))
        {
            tool.UseFirstEnemyAsTarget();
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button(new GUIContent("Clear", Tips.ClearSelection)))
        {
            tool.ClearSelection();
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPrimaryCombatActions(CreatureCombatDebugTool tool)
    {
        string casterReason = GetCasterBlockReason(tool);
        string targetReason = GetTargetBlockReason(tool);
        string contextReason = GetContextBlockReason(tool);
        string targetActionReason = FirstReason(contextReason, casterReason, targetReason);

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(casterReason));
        if (GUILayout.Button(new GUIContent("Force Charge", DisabledTip(Tips.ForceCharge, casterReason)), GUILayout.Height(26)))
            tool.ForceFullSkillChargeOnCaster();
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(targetActionReason));
        if (GUILayout.Button(new GUIContent("Cast Skill", DisabledTip(Tips.CastTarget, targetActionReason)), GUILayout.Height(26)))
            tool.CastSelectedSkillOnTarget();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(targetActionReason));
        if (GUILayout.Button(new GUIContent("Basic Attack", DisabledTip(Tips.BasicAttack, targetActionReason))))
            tool.BasicAttackOnTarget();
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(targetReason));
        if (GUILayout.Button(new GUIContent("Kill Target", DisabledTip(Tips.KillTarget, targetReason))))
            tool.KillTarget(tool.SelectedTarget);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button(new GUIContent("Log State", Tips.LogState)))
            tool.LogCurrentState();
        EditorGUILayout.EndHorizontal();

        DrawReason(targetActionReason);
    }

    private void DrawLegacyInspector()
    {
        CreatureCombatDebugTool tool = (CreatureCombatDebugTool)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox("This tool controls spawned runtime units. Use Creature Variant Lab to create them.", MessageType.Info);
        DrawRuntimeSummary(tool);
        EditorGUILayout.Space(6);
        DrawContextSection(tool);
        EditorGUILayout.Space(4);
        DrawSelectionSection(tool);
        EditorGUILayout.Space(4);
        DrawManualActionsSection(tool);
        EditorGUILayout.Space(4);
        DrawSpawnedUnitsSection(tool);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRuntimeSummary(CreatureCombatDebugTool tool)
    {
        int allies = 0;
        int enemies = 0;
        for (int i = 0; i < tool.SpawnedUnits.Count; i++)
        {
            Unit unit = tool.SpawnedUnits[i];
            if (unit == null)
                continue;
            if (unit.Team == UnitTeam.Ally)
                allies++;
            else if (unit.Team == UnitTeam.Enemy)
                enemies++;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Runtime Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Play Mode", EditorApplication.isPlaying ? "Active" : "Not active");
        EditorGUILayout.LabelField("Units", $"Allies: {allies} | Enemies: {enemies}");
        EditorGUILayout.LabelField("Selected caster", tool.SelectedCaster != null ? tool.SelectedCaster.name : "None");
        EditorGUILayout.LabelField("Selected target", tool.SelectedTarget != null ? tool.SelectedTarget.name : "None");
        EditorGUILayout.EndVertical();
    }

    private static bool HasAliveUnit(CreatureCombatDebugTool tool, UnitTeam team)
    {
        for (int i = 0; i < tool.SpawnedUnits.Count; i++)
        {
            Unit unit = tool.SpawnedUnits[i];
            if (unit != null && unit.Team == team && unit.IsAlive)
                return true;
        }
        return false;
    }

    private static string GetContextBlockReason(CreatureCombatDebugTool tool)
    {
        if (!EditorApplication.isPlaying)
            return "Blocked: not in Play Mode.";
        if (tool.ContextRoom == null)
            return "Blocked: RoomContext is missing.";
        if (tool.ContextGrid == null)
            return "Blocked: RoomGrid is missing.";
        if (tool.ContextRoom.CombatController == null)
            return "Blocked: CombatRoomController is missing.";
        return null;
    }

    private static string GetCasterBlockReason(CreatureCombatDebugTool tool)
    {
        if (!EditorApplication.isPlaying)
            return "Blocked: not in Play Mode.";
        if (tool.SelectedCaster == null)
            return "Blocked: no caster selected.";
        if (!tool.SelectedCaster.IsAlive)
            return "Blocked: selected caster is dead.";

        SkillCaster caster = tool.SelectedCaster.GetComponent<SkillCaster>();
        if (caster == null || caster.Skill == null)
            return "Blocked: selected caster has no available skill.";
        return null;
    }

    private static string GetTargetBlockReason(CreatureCombatDebugTool tool)
    {
        if (!EditorApplication.isPlaying)
            return "Blocked: not in Play Mode.";
        if (tool.SelectedTarget == null)
            return "Blocked: no target selected.";
        if (!tool.SelectedTarget.IsAlive)
            return "Blocked: selected target is dead.";
        return null;
    }

    private static string FirstReason(params string[] reasons)
    {
        for (int i = 0; i < reasons.Length; i++)
        {
            if (!string.IsNullOrEmpty(reasons[i]))
                return reasons[i];
        }
        return null;
    }

    private static string DisabledTip(string tooltip, string reason)
    {
        return string.IsNullOrEmpty(reason) ? tooltip : $"{tooltip}\n\n{reason}";
    }

    private static void DrawReason(string reason)
    {
        if (!string.IsNullOrEmpty(reason))
            EditorGUILayout.LabelField(reason, EditorStyles.wordWrappedMiniLabel);
    }

    private void DrawContextSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("1. Context", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_roomContextProp, new GUIContent("Room Context", Tips.RoomContext));
        EditorGUILayout.PropertyField(_roomGridProp, new GUIContent("Room Grid", Tips.RoomGrid));
        EditorGUILayout.PropertyField(_walkableTilemapProp, new GUIContent("Walkable Tilemap", Tips.Walkable));
        EditorGUILayout.PropertyField(_blockedTilemapProp, new GUIContent("Blocked Tilemap", Tips.Blocked));
        EditorGUILayout.PropertyField(_configureGridOnEnableProp, new GUIContent("Configure Grid On Enable", Tips.ConfigureGrid));
        EditorGUILayout.PropertyField(_startCombatOnEnableProp, new GUIContent("Start Combat On Enable", Tips.StartCombat));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Auto Find Context", Tips.AutoFind), GUILayout.Height(22)))
        {
            tool.AutoFindContext();
            EditorUtility.SetDirty(target);
            serializedObject.Update();
        }
        if (GUILayout.Button(new GUIContent("Validate Setup", Tips.Validate), GUILayout.Height(22)))
        {
            tool.ValidateSetup();
        }
        EditorGUILayout.EndHorizontal();
    }



    private void DrawSelectionSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("3. Selection", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_selectedCasterProp, new GUIContent("Selected Caster", Tips.Caster));
        EditorGUILayout.PropertyField(_selectedTargetProp, new GUIContent("Selected Target", Tips.Target));
        EditorGUILayout.PropertyField(_selectedTargetCellProp, new GUIContent("Selected Target Cell", Tips.TargetCell));

        EditorGUILayout.BeginHorizontal();
        bool inPlayMode = EditorApplication.isPlaying;
        if (GUILayout.Button(new GUIContent("Clear Selection", Tips.ClearSelection), GUILayout.Height(22)))
        {
            tool.ClearSelection();
            serializedObject.Update();
        }
        bool hasAlly = HasAliveUnit(tool, UnitTeam.Ally);
        bool hasEnemy = HasAliveUnit(tool, UnitTeam.Enemy);
        EditorGUI.BeginDisabledGroup(!inPlayMode || !hasAlly);
        if (GUILayout.Button(new GUIContent("First Ally as Caster", DisabledTip(Tips.FirstAlly, !inPlayMode ? "Blocked: not in Play Mode." : !hasAlly ? "Blocked: no alive Ally is available." : null)), GUILayout.Height(22)))
        {
            tool.UseFirstAllyAsCaster();
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!inPlayMode || !hasEnemy);
        if (GUILayout.Button(new GUIContent("First Enemy as Target", DisabledTip(Tips.FirstEnemy, !inPlayMode ? "Blocked: not in Play Mode." : !hasEnemy ? "Blocked: no alive Enemy is available." : null)), GUILayout.Height(22)))
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
        string casterReason = GetCasterBlockReason(tool);
        string targetReason = GetTargetBlockReason(tool);
        string contextReason = GetContextBlockReason(tool);

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(casterReason));
        if (GUILayout.Button(new GUIContent("Force Charge", DisabledTip(Tips.ForceCharge, casterReason)), GUILayout.Height(24)))
        {
            tool.ForceFullSkillChargeOnCaster();
        }
        EditorGUI.EndDisabledGroup();

        string castTargetReason = FirstReason(!inPlayMode ? "Blocked: not in Play Mode." : null, contextReason, casterReason, targetReason);
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(castTargetReason));
        if (GUILayout.Button(new GUIContent("Cast Skill on Target", DisabledTip(Tips.CastTarget, castTargetReason)), GUILayout.Height(24)))
        {
            tool.CastSelectedSkillOnTarget();
        }
        EditorGUI.EndDisabledGroup();

        string castCellReason = FirstReason(!inPlayMode ? "Blocked: not in Play Mode." : null, contextReason, casterReason);
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(castCellReason));
        if (GUILayout.Button(new GUIContent("Cast Skill on Cell", DisabledTip(Tips.CastCell, castCellReason)), GUILayout.Height(24)))
        {
            tool.CastSelectedSkillOnCell();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(castTargetReason));
        if (GUILayout.Button(new GUIContent("Basic Attack Target", DisabledTip(Tips.BasicAttack, castTargetReason)), GUILayout.Height(24)))
        {
            tool.BasicAttackOnTarget();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(targetReason));
        if (GUILayout.Button(new GUIContent("Kill Selected Target", DisabledTip(Tips.KillTarget, targetReason)), GUILayout.Height(24)))
        {
            tool.KillTarget(tool.SelectedTarget);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(new GUIContent("Log Current State", Tips.LogState), GUILayout.Height(22)))
        {
            tool.LogCurrentState();
        }
        EditorGUI.BeginDisabledGroup(!inPlayMode || tool.ContextGrid == null);
        if (GUILayout.Button(new GUIContent("Log Movement Audit", DisabledTip(Tips.LogMovement, !inPlayMode ? "Blocked: not in Play Mode." : tool.ContextGrid == null ? "Blocked: RoomGrid is missing." : null)), GUILayout.Height(22)))
        {
            tool.LogMovementAudit();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUI.BeginDisabledGroup(!inPlayMode || tool.SpawnedUnits.Count == 0);
        if (GUILayout.Button(new GUIContent("Clear Spawned Units", DisabledTip(Tips.ClearUnits, !inPlayMode ? "Blocked: not in Play Mode." : tool.SpawnedUnits.Count == 0 ? "Blocked: no lab-spawned units exist." : null)), GUILayout.Height(22)))
        {
            tool.ClearSpawned();
            serializedObject.Update();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        DrawReason(castTargetReason);
    }

    private void DrawSpawnedUnitsSection(CreatureCombatDebugTool tool)
    {
        EditorGUILayout.LabelField("5. Spawned Units", EditorStyles.boldLabel);

        int count = tool.SpawnedUnits.Count;
        if (count == 0)
        {
            EditorGUILayout.HelpBox(
                "No spawned units.\n1. Open Creature Variant Lab.\n2. Enter Play Mode.\n3. Spawn Test Unit.\n4. Spawn Opponent Dummy.",
                MessageType.Info);
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
        if (GUILayout.Button(new GUIContent("Set as Caster", Tips.Caster), GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.AssignSelectedCaster(unit);
            serializedObject.Update();
        }
        if (GUILayout.Button(new GUIContent("Set as Target", Tips.Target), GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.AssignSelectedTarget(unit);
            serializedObject.Update();
        }
        if (GUILayout.Button(new GUIContent("Kill", Tips.KillTarget), GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
        {
            tool.KillTarget(unit);
            serializedObject.Update();
        }
        if (GUILayout.Button(new GUIContent("Destroy", "Removes this lab-spawned runtime unit and unregisters it from RoomContext."), GUILayout.Width(btnWidth), GUILayout.Height(lineHeight)))
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
