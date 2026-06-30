using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MovementDebugTool : MonoBehaviour
{
    [Header("Required Context & Grid References")]
    [Tooltip("The RoomContext component managing unit registry and combat lifecycle in the current scene room.")]
    [SerializeField] private RoomContext _roomContext;

    [Tooltip("The RoomGrid component managing the grid traversal, coordinates, and cell status.")]
    [SerializeField] private RoomGrid _roomGrid;

    [Tooltip("The walkable tilemap (e.g. FloorTilemap or WalkableTilemap) used to define statically navigable cells.")]
    [SerializeField] private Tilemap _walkableTilemap;

    [Tooltip("The blocked tilemap (e.g. WallTilemap or BlockedTilemap) used to define statically impassable cells.")]
    [SerializeField] private Tilemap _blockedTilemap;

    [Header("Unit Prefab References")]
    [Tooltip("The template prefab used to spawn ally units. Must have a Unit component.")]
    [SerializeField] private Unit _allyPrefab;

    [Tooltip("The template prefab used to spawn enemy units. Must have a Unit component.")]
    [SerializeField] private Unit _enemyPrefab;

    [Header("Runtime State")]
    [SerializeField] private List<Unit> _spawnedDebugUnits = new();
    [SerializeField] private bool _gridConfiguredByDebugTool = false;

    // Runtime state logs for Inspector display
    [HideInInspector] public string lastScenarioRun = "None";
    [HideInInspector] public string expectedBehaviorText = "N/A";
    [HideInInspector] public bool lastScenarioNeedsCombatStart = false;

    public List<Unit> SpawnedDebugUnits => _spawnedDebugUnits;
    public bool GridConfiguredByDebugTool => _gridConfiguredByDebugTool;

    [ContextMenu("Initialize Test Grid")]
    public void InitializeTestGrid()
    {
        if (_roomGrid == null)
        {
            Debug.LogError("[MovementDebugTool] Missing field: _roomGrid reference must be assigned.");
            return;
        }

        if (_walkableTilemap == null)
        {
            Debug.LogError("[MovementDebugTool] Missing field: _walkableTilemap reference must be assigned.");
            return;
        }

        if (_blockedTilemap == null)
        {
            Debug.LogError("[MovementDebugTool] Missing field: _blockedTilemap reference must be assigned.");
            return;
        }

        Debug.Log($"[MovementDebugTool] Explicitly configuring RoomGrid using Walkable='{_walkableTilemap.name}', Blocked='{_blockedTilemap.name}'");
        _roomGrid.Configure(_walkableTilemap, _blockedTilemap);
        _gridConfiguredByDebugTool = true;
        Debug.Log("[MovementDebugTool] RoomGrid configured successfully.");

        DiagnoseGrid();
    }

    [ContextMenu("Diagnose Grid")]
    public void DiagnoseGrid()
    {
        Debug.Log("[MovementDebugTool] === Grid Diagnostics ===");
        Debug.Log($"Application.isPlaying: {Application.isPlaying}");
        Debug.Log($"RoomContext reference: {(_roomContext != null ? _roomContext.name : "null")}");
        Debug.Log($"RoomGrid reference: {(_roomGrid != null ? _roomGrid.name : "null")}");
        Debug.Log($"Walkable Tilemap reference: {(_walkableTilemap != null ? _walkableTilemap.name : "null")}");
        Debug.Log($"Blocked Tilemap reference: {(_blockedTilemap != null ? _blockedTilemap.name : "null")}");
        Debug.Log($"Grid Configured by Debug Tool: {_gridConfiguredByDebugTool}");

        bool isInitialized = false;
        BoundsInt scanBounds = new BoundsInt(-15, -15, 0, 30, 30, 1);
        string boundsSource = "Default Range";

        if (_roomGrid != null)
        {
            Bounds worldBounds;
            if (_roomGrid.TryGetWorldBounds(out worldBounds))
            {
                Vector3Int cellMin = _roomGrid.WorldToCell(worldBounds.min);
                Vector3Int cellMax = _roomGrid.WorldToCell(worldBounds.max);
                cellMin.z = 0;
                cellMax.z = 0;
                Vector3Int size = cellMax - cellMin + new Vector3Int(1, 1, 1);
                scanBounds = new BoundsInt(cellMin, size);
                boundsSource = "RoomGrid.TryGetWorldBounds()";
                isInitialized = true;
            }
        }

        Debug.Log($"RoomGrid Appears Initialized: {isInitialized}");
        Debug.Log($"Grid Scan Bounds Used: {scanBounds} (Source: {boundsSource})");

        List<Vector3Int> walkableCells = new();
        List<string> adjacentPairs = new();

        if (_roomGrid != null)
        {
            foreach (var pos in scanBounds.allPositionsWithin)
            {
                Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
                if (_roomGrid.IsCellWalkable(cell))
                {
                    walkableCells.Add(cell);

                    Vector3Int rightNeighbor = cell + Vector3Int.right;
                    if (_roomGrid.IsCellWalkable(rightNeighbor))
                    {
                        adjacentPairs.Add($"({cell.x}, {cell.y}) <-> ({rightNeighbor.x}, {rightNeighbor.y})");
                    }
                    Vector3Int upNeighbor = cell + Vector3Int.up;
                    if (_roomGrid.IsCellWalkable(upNeighbor))
                    {
                        adjacentPairs.Add($"({cell.x}, {cell.y}) <-> ({upNeighbor.x}, {upNeighbor.y})");
                    }
                }
            }
        }

        Debug.Log($"Total Walkable Cells Found: {walkableCells.Count}");
        Debug.Log($"Total Adjacent Walkable Pairs Found: {adjacentPairs.Count}");

        int cellsToLog = Mathf.Min(10, walkableCells.Count);
        for (int i = 0; i < cellsToLog; i++)
        {
            Debug.Log($"Walkable Cell [{i}]: {walkableCells[i]}");
        }

        int pairsToLog = Mathf.Min(10, adjacentPairs.Count);
        for (int i = 0; i < pairsToLog; i++)
        {
            Debug.Log($"Adjacent Pair [{i}]: {adjacentPairs[i]}");
        }

        if (!isInitialized)
        {
            Debug.LogWarning("[MovementDebugTool] RECOMMENDED ACTION: Please assign the Walkable and Blocked Tilemap references and click 'Initialize Test Grid'.");
        }

        Debug.Log("[MovementDebugTool] === End Grid Diagnostics ===");
    }

    [ContextMenu("Diagnose Encounter")]
    public void DiagnoseEncounter()
    {
        Debug.Log("[MovementDebugTool] === Encounter Diagnostics ===");
        if (_roomContext == null)
        {
            Debug.LogError("RoomContext reference is missing.");
            return;
        }

        CombatRoomController controller = _roomContext.CombatController;
        Debug.Log($"Current Combat State: {(controller != null ? controller.State.ToString() : "N/A (Missing CombatRoomController)")}");
        Debug.Log($"Spawned Debug Units Count: {_spawnedDebugUnits.Count}");

        int allyCount = 0;
        int enemyCount = 0;
        int aliveAllyCount = 0;
        int aliveEnemyCount = 0;

        foreach (var unit in _spawnedDebugUnits)
        {
            if (unit == null) continue;
            if (unit.Team == UnitTeam.Ally)
            {
                allyCount++;
                if (unit.IsAlive) aliveAllyCount++;
            }
            else if (unit.Team == UnitTeam.Enemy)
            {
                enemyCount++;
                if (unit.IsAlive) aliveEnemyCount++;
            }
        }

        Debug.Log($"Ally count: {allyCount} (Alive: {aliveAllyCount})");
        Debug.Log($"Enemy count: {enemyCount} (Alive: {aliveEnemyCount})");

        int contextUnitsCount = _roomContext.Units.Count;
        Debug.Log($"Total Registered Units in RoomContext: {contextUnitsCount}");
        foreach (var u in _roomContext.Units)
        {
            if (u != null)
            {
                Debug.Log($"  Registered Unit: '{u.name}' Team={u.Team} Alive={u.IsAlive}");
            }
        }

        bool safeToStart = (controller != null && controller.IsDeploymentActive && aliveAllyCount > 0 && aliveEnemyCount > 0);
        Debug.Log($"Is Start Test Encounter Safe to Call: {safeToStart}");
        if (!safeToStart && controller != null)
        {
            if (!controller.IsDeploymentActive)
                Debug.LogWarning("  Reason: Room is not in Deployment state.");
            if (aliveAllyCount == 0)
                Debug.LogWarning("  Reason: No alive friendly Ally units are registered.");
            if (aliveEnemyCount == 0)
                Debug.LogWarning("  Reason: No alive hostile Enemy units are registered (starts combat and immediately triggers PlayerVictory/Resolved state).");
        }

        Debug.Log("[MovementDebugTool] === End Encounter Diagnostics ===");
    }

    [ContextMenu("Start Test Encounter")]
    public void StartTestEncounter()
    {
        if (_roomContext == null)
        {
            Debug.LogError("[MovementDebugTool] RoomContext is null. Cannot start encounter.");
            return;
        }

        CombatRoomController controller = _roomContext.CombatController;
        if (controller == null)
        {
            Debug.LogWarning("[MovementDebugTool] No public test-safe combat start API found (CombatRoomController is missing on Context).");
            return;
        }

        int aliveEnemies = 0;
        foreach (var u in _roomContext.Units)
        {
            if (u != null && u.Team == UnitTeam.Enemy && u.IsAlive)
            {
                aliveEnemies++;
            }
        }

        if (aliveEnemies == 0)
        {
            Debug.LogError("[MovementDebugTool] Cannot start test encounter: no alive enemy units registered. (Starting would immediately end combat with PlayerVictory).");
            return;
        }

        Debug.Log($"[MovementDebugTool] Starting encounter. Pre-state: {controller.State}");
        if (controller.IsResolved)
        {
            controller.ResetEncounter();
        }

        bool result = controller.TryStartCombat();
        Debug.Log($"[MovementDebugTool] TryStartCombat returned: {result}. Post-state: {controller.State}");
    }

    [ContextMenu("Explain Scenarios")]
    public void ExplainScenarios()
    {
        Debug.Log(
            "[MovementDebugTool] === Scenario Explanations ===\n" +
            "1. Line Friendly Blocker:\n" +
            "   - Spawns: 2 Allies (Mover & Blocker) and 1 Enemy Target (if template assigned).\n" +
            "   - Validates: That a moving unit does not get blocked by a friendly ally. Pathfinder should apply a traversal penalty but allow crossing the friendly blocker.\n" +
            "   - Failure: Mover unit gets stuck permanently behind the blocker and refuses to move.\n\n" +
            "2. Alternative Route:\n" +
            "   - Spawns: 2 Allies (Mover & Blocker) and 1 Enemy Target (if template assigned).\n" +
            "   - Validates: That a unit detours around a friendly blocker when a longer path is free (path traversal cost of friendly block vs detours).\n" +
            "   - Failure: Mover unit paths through the friendly blocker anyway (ignoring the penalty) or refuses to move.\n\n" +
            "3. Three Allies vs One Enemy:\n" +
            "   - Spawns: 3 Allies in line and 1 Enemy unit.\n" +
            "   - Validates: Queuing/spacing behavior as multiple allies approach a single threat.\n" +
            "   - Failure: Units clip inside each other, stack on identical cells, or deadlock.\n\n" +
            "4. Occupied Destination:\n" +
            "   - Spawns: 2 Allies (Mover & Blocker occupying target cell) and 1 Enemy Target (if template assigned).\n" +
            "   - Validates: How a unit handles moving to a cell already physically occupied by an ally.\n" +
            "   - Failure: Units overlap on the same cell or fail to resolve to a nearby alternative.\n\n" +
            "5. Moving Friendly Recalculation:\n" +
            "   - Spawns: 2 Allies crossing intersecting paths.\n" +
            "   - Validates: Real-time path recalculation and deadlock recovery when two units block each other's next step mid-movement.\n" +
            "   - Failure: Units collide, deadlock indefinitely, or clip.\n\n" +
            "6. Enemy Proximity:\n" +
            "   - Spawns: 1 Ally and 1 Enemy in adjacent cells.\n" +
            "   - Validates: Direct melee combat targeting and proximity avoidance.\n" +
            "   - Failure: Units ignore each other or throw errors during step reservation.\n\n" +
            "7. Taunt Multi Enemy Layout:\n" +
            "   - Spawns: 1 Ally Source and 2-3 Enemy units (Target and Distractors).\n" +
            "   - Validates: Visual positioning and read of the Taunt loop chevron VFX and directional pointer pointing towards the Ally source.\n" +
            "   - Failure: The target does not show VFX, distractors show VFX, or the arrow points in an incorrect direction."
        );
    }

    [ContextMenu("Clear Spawned Units")]
    public void ClearSpawnedUnits()
    {
        for (int i = _spawnedDebugUnits.Count - 1; i >= 0; i--)
        {
            Unit unit = _spawnedDebugUnits[i];
            if (unit == null) continue;

            if (_roomContext != null)
            {
                _roomContext.UnregisterUnit(unit);
            }

            if (_roomGrid != null && _roomGrid.OccupancyService != null)
            {
                _roomGrid.OccupancyService.ReleaseOccupant(unit);
            }

            DestroyImmediate(unit.gameObject);
        }

        _spawnedDebugUnits.Clear();
        Debug.Log("[MovementDebugTool] Cleared all spawned debug units.");

        if (_roomContext != null && _roomContext.CombatController != null)
        {
            _roomContext.CombatController.ResetEncounter();
        }
    }

    private bool CheckGridInitialized()
    {
        if (_roomGrid == null)
        {
            Debug.LogError("[MovementDebugTool] RoomGrid reference is null. Please assign it in the Inspector.");
            return false;
        }

        Bounds worldBounds;
        if (!_roomGrid.TryGetWorldBounds(out worldBounds))
        {
            Debug.LogError("[MovementDebugTool] Grid not initialized. Please assign the tilemaps and click 'Initialize Test Grid' first.");
            return false;
        }

        return true;
    }

    private string GetCombatControllerStateString()
    {
        if (_roomContext != null && _roomContext.CombatController != null)
            return _roomContext.CombatController.State.ToString();
        return "N/A";
    }

    private void LogScenarioStart(string scenarioName)
    {
        Debug.Log(
            $"[MovementDebugTool] === Starting {scenarioName} ===\n" +
            $"  Initial Room Combat State: {GetCombatControllerStateString()}"
        );
    }

    private void LogScenarioEnd(string scenarioName, int spawnedCount, string teamsList, bool needsCombatStart)
    {
        lastScenarioNeedsCombatStart = needsCombatStart;
        string currentState = GetCombatControllerStateString();
        string nextStep = needsCombatStart ? "Click Start Test Encounter" : "Observe movement directly";
        Debug.Log(
            $"[MovementDebugTool] === {scenarioName} Spawned ===\n" +
            $"  Units Spawned: {spawnedCount} ({teamsList})\n" +
            $"  Final Room Combat State: {currentState}\n" +
            $"  Next step: {nextStep}"
        );
    }

    private Unit SpawnUnitAtCell(Unit prefab, Vector3Int cell, UnitTeam team)
    {
        if (_roomGrid == null || _roomContext == null)
        {
            Debug.LogError("[MovementDebugTool] RoomContext or RoomGrid is null. Cannot spawn unit.");
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError($"[MovementDebugTool] Prefab reference is null. Cannot spawn {team} unit.");
            return null;
        }

        GameObject instance = null;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, _roomContext.transform);
        }
        else
#endif
        {
            instance = Instantiate(prefab.gameObject, _roomContext.transform);
        }

        if (instance == null) return null;

        Unit unit = instance.GetComponent<Unit>();
        if (unit == null)
        {
            Debug.LogError($"[MovementDebugTool] Prefab {prefab.name} does not have a Unit component.");
            DestroyImmediate(instance);
            return null;
        }

        unit.gameObject.name = $"Debug_{team}_{_spawnedDebugUnits.Count}";
        
        UnitMovement movement = unit.GetComponent<UnitMovement>();
        if (movement != null)
        {
            movement.AttachToGridAtCell(_roomGrid, cell);
        }

        _roomContext.RegisterUnit(unit);
        _spawnedDebugUnits.Add(unit);
        return unit;
    }

    [ContextMenu("Scenario 1: Line Friendly Blocker")]
    public void SetupScenario1()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Line Friendly Blocker requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 1 (Line Friendly Blocker)");

        Vector3Int moverCell = Vector3Int.zero;
        Vector3Int blockerCell = Vector3Int.zero;
        Vector3Int targetCell = Vector3Int.zero;
        bool layoutFound = false;

        BoundsInt bounds = GetGridBounds();

        // 1. Try to find a straight line of 5 walkable cells
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            
            if (_roomGrid.IsCellWalkable(cell) &&
                _roomGrid.IsCellWalkable(cell + Vector3Int.right) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(2, 0, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(3, 0, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(4, 0, 0)))
            {
                moverCell = cell;
                blockerCell = cell + new Vector3Int(2, 0, 0);
                targetCell = cell + new Vector3Int(4, 0, 0);
                layoutFound = true;
                break;
            }

            if (_roomGrid.IsCellWalkable(cell) &&
                _roomGrid.IsCellWalkable(cell + Vector3Int.up) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(0, 2, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(0, 3, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(0, 4, 0)))
            {
                moverCell = cell;
                blockerCell = cell + new Vector3Int(0, 2, 0);
                targetCell = cell + new Vector3Int(0, 4, 0);
                layoutFound = true;
                break;
            }
        }

        // 2. Fallback to straight line of 3 walkable cells
        if (!layoutFound)
        {
            foreach (var pos in bounds.allPositionsWithin)
            {
                Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
                
                if (_roomGrid.IsCellWalkable(cell) &&
                    _roomGrid.IsCellWalkable(cell + Vector3Int.right) &&
                    _roomGrid.IsCellWalkable(cell + new Vector3Int(2, 0, 0)))
                {
                    moverCell = cell;
                    blockerCell = cell + Vector3Int.right;
                    targetCell = cell + new Vector3Int(2, 0, 0);
                    layoutFound = true;
                    break;
                }

                if (_roomGrid.IsCellWalkable(cell) &&
                    _roomGrid.IsCellWalkable(cell + Vector3Int.up) &&
                    _roomGrid.IsCellWalkable(cell + new Vector3Int(0, 2, 0)))
                {
                    moverCell = cell;
                    blockerCell = cell + Vector3Int.up;
                    targetCell = cell + new Vector3Int(0, 2, 0);
                    layoutFound = true;
                    break;
                }
            }
        }

        // 3. Absolute Fallback: any connected 3-cell path
        if (!layoutFound)
        {
            foreach (var pos in bounds.allPositionsWithin)
            {
                Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
                if (!_roomGrid.IsCellWalkable(cell)) continue;

                List<Vector3Int> neighbors = _roomGrid.Topology.GetNeighbors(cell, includeDiagonals: false);
                Vector3Int firstNeighbor = Vector3Int.zero;
                bool foundFirst = false;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (_roomGrid.IsCellWalkable(neighbors[i]))
                    {
                        firstNeighbor = neighbors[i];
                        foundFirst = true;
                        break;
                    }
                }

                if (foundFirst)
                {
                    List<Vector3Int> nextNeighbors = _roomGrid.Topology.GetNeighbors(firstNeighbor, includeDiagonals: false);
                    for (int j = 0; j < nextNeighbors.Count; j++)
                    {
                        if (nextNeighbors[j] != cell && _roomGrid.IsCellWalkable(nextNeighbors[j]))
                        {
                            moverCell = cell;
                            blockerCell = firstNeighbor;
                            targetCell = nextNeighbors[j];
                            layoutFound = true;
                            break;
                        }
                    }
                }

                if (layoutFound) break;
            }
        }

        if (!layoutFound)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 1: No suitable 3-cell sequence or connected path found in grid.");
            return;
        }

        ClearSpawnedUnits();

        Debug.Log($"[MovementDebugTool] Scenario 1 Line Setup:\n  Mover Cell: {moverCell}\n  Blocker Cell: {blockerCell}\n  Target Cell: {targetCell}");

        Unit movingUnit = SpawnUnitAtCell(_allyPrefab, moverCell, UnitTeam.Ally);
        Unit blockerUnit = SpawnUnitAtCell(_allyPrefab, blockerCell, UnitTeam.Ally);

        int spawnedCount = 2;
        string teamsList = "2 Allies";
        bool needsCombatStart = false;

        if (_enemyPrefab != null)
        {
            SpawnUnitAtCell(_enemyPrefab, targetCell, UnitTeam.Enemy);
            spawnedCount++;
            teamsList = "2 Allies, 1 Enemy Target";
            needsCombatStart = true;
        }

        if (movingUnit != null && blockerUnit != null)
        {
            DisableUnitAI(blockerUnit);
            UnitMovement movement = movingUnit.GetComponent<UnitMovement>();
            if (movement != null && Application.isPlaying)
            {
                movement.SetDestinationCell(targetCell);
            }
        }

        lastScenarioRun = "1: Line Friendly Blocker";
        expectedBehaviorText = "Mover debug ally should walk past/cross the static friendly blocker ally to reach the target cell. Mover pathfinding cost applies penalty (5) when planning over blocker.";
        LogScenarioEnd("Scenario 1 (Line Friendly Blocker)", spawnedCount, teamsList, needsCombatStart);
    }

    [ContextMenu("Scenario 2: Alternative Route around Friendly Blocker")]
    public void SetupScenario2()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Alternative Route requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 2 (Alternative Route)");

        Vector3Int start = Vector3Int.zero;
        Vector3Int target = Vector3Int.zero;
        Vector3Int shortBlock = Vector3Int.zero;
        bool found = false;

        BoundsInt bounds = GetGridBounds();
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            if (!_roomGrid.IsCellWalkable(cell)) continue;

            if (_roomGrid.IsCellWalkable(cell + Vector3Int.right) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(2, 0, 0)) &&
                _roomGrid.IsCellWalkable(cell + Vector3Int.up) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(1, 1, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(2, 1, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(2, 0, 0)))
            {
                start = cell;
                shortBlock = cell + Vector3Int.right;
                target = cell + new Vector3Int(2, 0, 0);
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 2: No suitable branching routes found.");
            return;
        }

        ClearSpawnedUnits();

        Debug.Log($"[MovementDebugTool] Scenario 2 Setup: Start={start}, Blocker={shortBlock}, Target={target}");

        Unit movingUnit = SpawnUnitAtCell(_allyPrefab, start, UnitTeam.Ally);
        Unit blockerUnit = SpawnUnitAtCell(_allyPrefab, shortBlock, UnitTeam.Ally);

        int spawnedCount = 2;
        string teamsList = "2 Allies";
        bool needsCombatStart = false;

        if (_enemyPrefab != null)
        {
            SpawnUnitAtCell(_enemyPrefab, target, UnitTeam.Enemy);
            spawnedCount++;
            teamsList = "2 Allies, 1 Enemy Target";
            needsCombatStart = true;
        }

        if (movingUnit != null && blockerUnit != null)
        {
            DisableUnitAI(blockerUnit);
            UnitMovement movement = movingUnit.GetComponent<UnitMovement>();
            if (movement != null && Application.isPlaying)
            {
                movement.SetDestinationCell(target);
            }
        }

        lastScenarioRun = "2: Alternative Route";
        expectedBehaviorText = "Mover debug ally should walk around the friendly blocker using the longer/alternative route instead of passing directly over it because the path traversal penalty on the blocker exceeds detour length.";
        LogScenarioEnd("Scenario 2 (Alternative Route)", spawnedCount, teamsList, needsCombatStart);
    }

    [ContextMenu("Scenario 3: Three Allies approaching One Enemy")]
    public void SetupScenario3()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Three Allies vs One Enemy requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }
        if (_enemyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Three Allies vs One Enemy requires the 'Enemy Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 3 (Three Allies approaching One Enemy)");

        Vector3Int baseCell = Vector3Int.zero;
        bool found = false;

        BoundsInt bounds = GetGridBounds();
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            if (_roomGrid.IsCellWalkable(cell) &&
                _roomGrid.IsCellWalkable(cell + Vector3Int.right) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(2, 0, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(3, 0, 0)) &&
                _roomGrid.IsCellWalkable(cell + new Vector3Int(4, 0, 0)))
            {
                baseCell = cell;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 3: No horizontal line of 5 walkable cells found.");
            return;
        }

        ClearSpawnedUnits();

        Debug.Log($"[MovementDebugTool] Scenario 3 Setup: Allies at {baseCell}, {baseCell + Vector3Int.right}, {baseCell + new Vector3Int(2, 0, 0)}. Enemy at {baseCell + new Vector3Int(4, 0, 0)}");

        Unit a1 = SpawnUnitAtCell(_allyPrefab, baseCell, UnitTeam.Ally);
        Unit a2 = SpawnUnitAtCell(_allyPrefab, baseCell + Vector3Int.right, UnitTeam.Ally);
        Unit a3 = SpawnUnitAtCell(_allyPrefab, baseCell + new Vector3Int(2, 0, 0), UnitTeam.Ally);
        Unit e1 = SpawnUnitAtCell(_enemyPrefab, baseCell + new Vector3Int(4, 0, 0), UnitTeam.Enemy);

        DisableUnitAI(e1);

        lastScenarioRun = "3: Three Allies vs One Enemy";
        expectedBehaviorText = "Allies should move toward the enemy unit. They should queue behind one another or spread out rather than stacking on the same cell or clipping.";
        LogScenarioEnd("Scenario 3 (Three Allies approaching One Enemy)", 4, "3 Allies, 1 Enemy", needsCombatStart: true);
    }

    [ContextMenu("Scenario 4: Friendly Unit Occupying Desired Destination")]
    public void SetupScenario4()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Occupied Destination requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 4 (Occupied Destination)");

        Vector3Int start = Vector3Int.zero;
        Vector3Int dest = Vector3Int.zero;
        bool found = false;

        BoundsInt bounds = GetGridBounds();
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            if (_roomGrid.IsCellWalkable(cell) && _roomGrid.IsCellWalkable(cell + Vector3Int.right))
            {
                start = cell;
                dest = cell + Vector3Int.right;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 4: No 2 adjacent walkable cells found.");
            return;
        }

        ClearSpawnedUnits();

        Debug.Log($"[MovementDebugTool] Scenario 4 Setup: Start={start}, Occupied Dest={dest}");

        Unit movingUnit = SpawnUnitAtCell(_allyPrefab, start, UnitTeam.Ally);
        Unit blockerUnit = SpawnUnitAtCell(_allyPrefab, dest, UnitTeam.Ally);

        int spawnedCount = 2;
        string teamsList = "2 Allies";
        bool needsCombatStart = false;

        if (_enemyPrefab != null)
        {
            Vector3Int offset = dest + Vector3Int.up;
            if (_roomGrid.IsCellWalkable(offset))
            {
                Unit enemy = SpawnUnitAtCell(_enemyPrefab, offset, UnitTeam.Enemy);
                DisableUnitAI(enemy);
                spawnedCount++;
                teamsList = "2 Allies, 1 Enemy Target";
                needsCombatStart = true;
            }
        }

        if (movingUnit != null && blockerUnit != null)
        {
            DisableUnitAI(blockerUnit);
            UnitMovement movement = movingUnit.GetComponent<UnitMovement>();
            if (movement != null && Application.isPlaying)
            {
                movement.SetDestinationCell(dest);
            }
        }

        lastScenarioRun = "4: Occupied Destination";
        expectedBehaviorText = "Mover ally should find the destination occupied by an ally, and should either path to a nearby alternative cell or wait, rather than sitting on the same cell.";
        LogScenarioEnd("Scenario 4 (Occupied Destination)", spawnedCount, teamsList, needsCombatStart);
    }

    [ContextMenu("Scenario 5: Moving Friendly Obstacle during Path Recalculation")]
    public void SetupScenario5()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Intersecting Moving Paths requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 5 (Intersecting Moving Paths)");

        Vector3Int center = Vector3Int.zero;
        bool found = false;

        BoundsInt bounds = GetGridBounds();
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            bool squareWalkable = true;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!_roomGrid.IsCellWalkable(cell + new Vector3Int(dx, dy, 0)))
                    {
                        squareWalkable = false;
                        break;
                    }
                }
                if (!squareWalkable) break;
            }

            if (squareWalkable)
            {
                center = cell;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 5: No 3x3 walkable area found.");
            return;
        }

        ClearSpawnedUnits();

        Vector3Int u1Start = center + Vector3Int.left;
        Vector3Int u1Target = center + Vector3Int.right;
        Vector3Int u2Start = center + Vector3Int.down;
        Vector3Int u2Target = center + Vector3Int.up;

        Debug.Log($"[MovementDebugTool] Scenario 5 Setup: Unit1 from {u1Start} to {u1Target}, Unit2 from {u2Start} to {u2Target}");

        Unit movingUnit1 = SpawnUnitAtCell(_allyPrefab, u1Start, UnitTeam.Ally);
        Unit movingUnit2 = SpawnUnitAtCell(_allyPrefab, u2Start, UnitTeam.Ally);

        int spawnedCount = 2;
        string teamsList = "2 Allies";
        bool needsCombatStart = false;

        if (_enemyPrefab != null)
        {
            Vector3Int offset = center + new Vector3Int(1, 1, 0);
            if (_roomGrid.IsCellWalkable(offset))
            {
                Unit enemy = SpawnUnitAtCell(_enemyPrefab, offset, UnitTeam.Enemy);
                DisableUnitAI(enemy);
                spawnedCount++;
                teamsList = "2 Allies, 1 Enemy Target";
                needsCombatStart = true;
            }
        }

        if (movingUnit1 != null && movingUnit2 != null)
        {
            UnitMovement m1 = movingUnit1.GetComponent<UnitMovement>();
            UnitMovement m2 = movingUnit2.GetComponent<UnitMovement>();
            if (m1 != null && m2 != null && Application.isPlaying)
            {
                m1.SetDestinationCell(u1Target);
                m2.SetDestinationCell(u2Target);
            }
        }

        lastScenarioRun = "5: Moving Friendly Recalculation";
        expectedBehaviorText = "Two allies should cross intersecting paths simultaneously. They should detect each other's presence, recalculate paths dynamically, or resolve deadlocks without collision.";
        LogScenarioEnd("Scenario 5 (Intersecting Moving Paths)", spawnedCount, teamsList, needsCombatStart);
    }

    [ContextMenu("Scenario 6: Enemy occupying nearby cell")]
    public void SetupScenario6()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Enemy Proximity requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }
        if (_enemyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Enemy Proximity requires the 'Enemy Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 6 (Enemy Proximity)");

        Vector3Int allyCell = Vector3Int.zero;
        Vector3Int enemyCell = Vector3Int.zero;
        bool found = false;

        BoundsInt bounds = GetGridBounds();
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);
            if (_roomGrid.IsCellWalkable(cell) && _roomGrid.IsCellWalkable(cell + Vector3Int.right))
            {
                allyCell = cell;
                enemyCell = cell + Vector3Int.right;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 6: No 2 adjacent walkable cells found.");
            return;
        }

        ClearSpawnedUnits();

        Debug.Log($"[MovementDebugTool] Scenario 6 Setup: Ally={allyCell}, Enemy={enemyCell}");

        SpawnUnitAtCell(_allyPrefab, allyCell, UnitTeam.Ally);
        Unit enemyUnit = SpawnUnitAtCell(_enemyPrefab, enemyCell, UnitTeam.Enemy);
        DisableUnitAI(enemyUnit);

        lastScenarioRun = "6: Enemy Proximity";
        expectedBehaviorText = "Ally unit should engage in melee combat targeting. Proximity, range, and obstacle parameters are validated.";
        LogScenarioEnd("Scenario 6 (Enemy Proximity)", 2, "1 Ally, 1 Enemy", needsCombatStart: true);
    }

    [ContextMenu("Scenario 7: Taunt Multi Enemy Layout")]
    public void SetupScenario7()
    {
        if (!CheckGridInitialized()) return;
        if (_allyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Taunt Multi Enemy Layout requires the 'Ally Spawn Template' field to be assigned.");
            return;
        }
        if (_enemyPrefab == null)
        {
            Debug.LogError("[MovementDebugTool] Setup aborted: Taunt Multi Enemy Layout requires the 'Enemy Spawn Template' field to be assigned.");
            return;
        }

        LogScenarioStart("Scenario 7 (Taunt Multi Enemy Layout)");

        BoundsInt bounds = GetGridBounds();
        Vector3Int center = new Vector3Int((bounds.xMin + bounds.xMax) / 2, (bounds.yMin + bounds.yMax) / 2, 0);

        Vector3Int sourceCell = center;
        Vector3Int targetCell = center + new Vector3Int(3, 0, 0);
        Vector3Int distractor1Cell = center + new Vector3Int(3, 2, 0);
        Vector3Int distractor2Cell = center + new Vector3Int(3, -2, 0);

        bool found = false;
        for (int radius = 0; radius < 15 && !found; radius++)
        {
            for (int dx = -radius; dx <= radius && !found; dx++)
            {
                for (int dy = -radius; dy <= radius && !found; dy++)
                {
                    Vector3Int candidate = center + new Vector3Int(dx, dy, 0);
                    if (_roomGrid.IsCellWalkable(candidate) &&
                        _roomGrid.IsCellWalkable(candidate + new Vector3Int(3, 0, 0)) &&
                        _roomGrid.IsCellWalkable(candidate + new Vector3Int(3, 2, 0)) &&
                        _roomGrid.IsCellWalkable(candidate + new Vector3Int(3, -2, 0)))
                    {
                        sourceCell = candidate;
                        targetCell = candidate + new Vector3Int(3, 0, 0);
                        distractor1Cell = candidate + new Vector3Int(3, 2, 0);
                        distractor2Cell = candidate + new Vector3Int(3, -2, 0);
                        found = true;
                    }
                }
            }
        }

        if (!found)
        {
            for (int radius = 0; radius < 15 && !found; radius++)
            {
                for (int dx = -radius; dx <= radius && !found; dx++)
                {
                    for (int dy = -radius; dy <= radius && !found; dy++)
                    {
                        Vector3Int candidate = center + new Vector3Int(dx, dy, 0);
                        if (_roomGrid.IsCellWalkable(candidate) &&
                            _roomGrid.IsCellWalkable(candidate + new Vector3Int(2, 0, 0)) &&
                            _roomGrid.IsCellWalkable(candidate + new Vector3Int(2, 1, 0)) &&
                            _roomGrid.IsCellWalkable(candidate + new Vector3Int(2, -1, 0)))
                        {
                            sourceCell = candidate;
                            targetCell = candidate + new Vector3Int(2, 0, 0);
                            distractor1Cell = candidate + new Vector3Int(2, 1, 0);
                            distractor2Cell = candidate + new Vector3Int(2, -1, 0);
                            found = true;
                        }
                    }
                }
            }
        }

        if (!found)
        {
            for (int radius = 0; radius < 15 && !found; radius++)
            {
                for (int dx = -radius; dx <= radius && !found; dx++)
                {
                    for (int dy = -radius; dy <= radius && !found; dy++)
                    {
                        Vector3Int candidate = center + new Vector3Int(dx, dy, 0);
                        if (_roomGrid.IsCellWalkable(candidate) &&
                            _roomGrid.IsCellWalkable(candidate + Vector3Int.left) &&
                            _roomGrid.IsCellWalkable(candidate + Vector3Int.right))
                        {
                            targetCell = candidate;
                            sourceCell = candidate + Vector3Int.left;
                            distractor1Cell = candidate + Vector3Int.right;
                            distractor2Cell = candidate;
                            found = true;
                        }
                    }
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning("[MovementDebugTool] Aborted Scenario 7: Could not find 3 adjacent walkable cells.");
            return;
        }

        ClearSpawnedUnits();

        Unit allySource = SpawnUnitAtCell(_allyPrefab, sourceCell, UnitTeam.Ally);
        if (allySource != null)
        {
            allySource.gameObject.name = "Debug_Taunt_Source";
            DisableUnitAI(allySource);
        }

        Unit enemyTarget = SpawnUnitAtCell(_enemyPrefab, targetCell, UnitTeam.Enemy);
        if (enemyTarget != null)
        {
            enemyTarget.gameObject.name = "Debug_Taunt_Target";
            DisableUnitAI(enemyTarget);
        }

        Unit enemyDistractor1 = SpawnUnitAtCell(_enemyPrefab, distractor1Cell, UnitTeam.Enemy);
        if (enemyDistractor1 != null)
        {
            enemyDistractor1.gameObject.name = "Debug_Taunt_Distractor_1";
            DisableUnitAI(enemyDistractor1);
        }

        if (distractor2Cell != targetCell)
        {
            Unit enemyDistractor2 = SpawnUnitAtCell(_enemyPrefab, distractor2Cell, UnitTeam.Enemy);
            if (enemyDistractor2 != null)
            {
                enemyDistractor2.gameObject.name = "Debug_Taunt_Distractor_2";
                DisableUnitAI(enemyDistractor2);
            }
        }

        lastScenarioRun = "7: Taunt Multi Enemy Layout";
        expectedBehaviorText = "Layout setup for visual Taunt loop verification. Use CombatVfxContextMenuTester > Test Effect Taunt or Test Effect Taunt Multi Enemy. The target should show a red nervous Chevron effect pointing towards the source ally, while distractors remain clean.";
        lastScenarioNeedsCombatStart = false;

        LogScenarioEnd("Scenario 7 (Taunt Multi Enemy Layout)", 3, "1 Ally, 2+ Enemies", needsCombatStart: false);
    }

    private BoundsInt GetGridBounds()
    {
        if (_roomGrid != null && _roomGrid.TryGetWorldBounds(out Bounds worldBounds))
        {
            Vector3Int cellMin = _roomGrid.WorldToCell(worldBounds.min);
            Vector3Int cellMax = _roomGrid.WorldToCell(worldBounds.max);
            cellMin.z = 0;
            cellMax.z = 0;
            Vector3Int size = cellMax - cellMin + new Vector3Int(1, 1, 1);
            return new BoundsInt(cellMin, size);
        }
        return new BoundsInt(-15, -15, 0, 30, 30, 1);
    }

    private void DisableUnitAI(Unit unit)
    {
        var brain = unit.GetComponent("UnitBrain");
        if (brain != null && brain is MonoBehaviour mb)
        {
            mb.enabled = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (_roomGrid == null) return;

        for (int i = 0; i < _spawnedDebugUnits.Count; i++)
        {
            Unit unit = _spawnedDebugUnits[i];
            if (unit == null) continue;

            UnitMovement movement = unit.GetComponent<UnitMovement>();
            if (movement != null && movement.TryGetLogicalCell(out Vector3Int cell))
            {
                Vector3 worldPos = _roomGrid.CellToWorld(cell);
                Gizmos.color = unit.Team == UnitTeam.Ally ? Color.green : Color.red;
                Gizmos.DrawWireCube(worldPos, new Vector3(_roomGrid.CellWorldSize.x, _roomGrid.CellWorldSize.y, 0.1f));

                if (unit.Team == UnitTeam.Ally)
                {
                    Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
                    Gizmos.DrawCube(worldPos, new Vector3(_roomGrid.CellWorldSize.x * 0.8f, _roomGrid.CellWorldSize.y * 0.8f, 0.05f));
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MovementDebugTool))]
public class MovementDebugToolEditor : Editor
{
    private bool _showDiagnostics = true;
    private bool _showScenarios = true;

    public override void OnInspectorGUI()
    {
        MovementDebugTool tool = (MovementDebugTool)target;

        serializedObject.Update();

        // 1. Help Box for Templates
        EditorGUILayout.HelpBox(
            "These prefabs are used as templates to spawn temporary debug units. The spawned units are renamed Debug_Ally_X / Debug_Enemy_X at runtime.", 
            MessageType.Info);

        // 2. Custom Labels for Prefabs
        SerializedProperty allyProp = serializedObject.FindProperty("_allyPrefab");
        SerializedProperty enemyProp = serializedObject.FindProperty("_enemyPrefab");
        EditorGUILayout.PropertyField(allyProp, new GUIContent("Ally Spawn Template", "The template prefab used to spawn ally units."));
        EditorGUILayout.PropertyField(enemyProp, new GUIContent("Enemy Spawn Template", "The template prefab used to spawn enemy units."));

        // 3. Context & Grid fields
        SerializedProperty roomContextProp = serializedObject.FindProperty("_roomContext");
        SerializedProperty roomGridProp = serializedObject.FindProperty("_roomGrid");
        SerializedProperty walkableTilemapProp = serializedObject.FindProperty("_walkableTilemap");
        SerializedProperty blockedTilemapProp = serializedObject.FindProperty("_blockedTilemap");
        
        EditorGUILayout.PropertyField(roomContextProp);
        EditorGUILayout.PropertyField(roomGridProp);
        EditorGUILayout.PropertyField(walkableTilemapProp);
        EditorGUILayout.PropertyField(blockedTilemapProp);

        // 4. Status Panel
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Status Board", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Last Scenario Run: {tool.lastScenarioRun}", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("Expected Behavior:", EditorStyles.miniLabel);
        EditorGUILayout.HelpBox(tool.expectedBehaviorText, MessageType.None);
        EditorGUILayout.LabelField($"Spawned Units Count: {tool.SpawnedDebugUnits.Count}", EditorStyles.miniLabel);

        string activeState = "N/A";
        if (tool.SpawnedDebugUnits.Count > 0 && tool.GetComponent<RoomContext>() != null)
        {
            var controller = tool.GetComponent<RoomContext>().CombatController;
            if (controller != null) activeState = controller.State.ToString();
        }
        else
        {
            var context = Object.FindFirstObjectByType<RoomContext>();
            if (context != null && context.CombatController != null)
                activeState = context.CombatController.State.ToString();
        }
        EditorGUILayout.LabelField($"Current Room State: {activeState}", EditorStyles.miniLabel);
        
        string nextStep = tool.lastScenarioNeedsCombatStart ? "Click Start Test Encounter" : "Observe movement directly";
        EditorGUILayout.LabelField($"Next Step: {nextStep}", EditorStyles.miniBoldLabel);
        EditorGUILayout.EndVertical();

        // 5. Template Validation Checks
        if (allyProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Ally Spawn Template is missing. Scenarios 1, 2, 3, 4, 5, and 6 require it.", MessageType.Error);
        }
        if (enemyProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("Enemy Spawn Template is missing. Scenarios 3 and 6 require it to setup. Scenarios 1, 2, 4, and 5 require it if you want to test active combat AI.", MessageType.Warning);
        }

        // 6. Diagnostics Foldout
        _showDiagnostics = EditorGUILayout.Foldout(_showDiagnostics, "Diagnostics & Grid Controls", true);
        if (_showDiagnostics)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Initialize Test Grid"))
            {
                tool.InitializeTestGrid();
            }
            if (GUILayout.Button("Start Test Encounter"))
            {
                tool.StartTestEncounter();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Diagnose Grid"))
            {
                tool.DiagnoseGrid();
            }
            if (GUILayout.Button("Diagnose Encounter"))
            {
                tool.DiagnoseEncounter();
            }
            if (GUILayout.Button("Explain Scenarios"))
            {
                tool.ExplainScenarios();
            }
            EditorGUILayout.EndHorizontal();
        }

        // 7. Scenarios Foldout with Descriptions
        EditorGUILayout.Space();
        _showScenarios = EditorGUILayout.Foldout(_showScenarios, "Scenario Setup Controls", true);
        if (_showScenarios)
        {
            DrawScenarioSection("Scenario 1: Line Friendly Blocker", 
                "Spawn & Setup creates the test layout (Mover + friendly Blocker). If Enemy Spawn Template is assigned, spawns an enemy at target cell.\n" +
                "• Flow: Click Spawn & Setup to create layout. After Spawn & Setup, click Start Test Encounter if the status board says it is required.\n" +
                "• Expect: Ally walks through blocker cell to destination.\n" +
                "• Fail condition: Mover gets stuck permanently behind blocker.",
                tool.SetupScenario1);

            DrawScenarioSection("Scenario 2: Alternative Route", 
                "Spawn & Setup creates the test layout (two routes: shorter blocked, longer clear). If Enemy Spawn Template is assigned, spawns an enemy at target cell.\n" +
                "• Flow: Click Spawn & Setup to create layout. After Spawn & Setup, click Start Test Encounter if the status board says it is required.\n" +
                "• Expect: Ally detours around the friendly blocker.\n" +
                "• Fail condition: Mover walks through blocker cell or gets stuck.",
                tool.SetupScenario2);

            DrawScenarioSection("Scenario 3: Three Allies vs One Enemy", 
                "Spawn & Setup creates the test layout (three allies and one enemy target).\n" +
                "• Flow: Click Spawn & Setup to create layout. After Spawn & Setup, click Start Test Encounter if the status board says it is required.\n" +
                "• Expect: Allies spread out/queue without clipping or stacking.\n" +
                "• Fail condition: Units stack on identical cells or clip.",
                tool.SetupScenario3);

            DrawScenarioSection("Scenario 4: Occupied Destination", 
                "Spawn & Setup creates the test layout (mover and blocker occupying desired target cell). If Enemy Spawn Template is assigned, spawns an enemy target nearby.\n" +
                "• Flow: Click Spawn & Setup to create layout. After Spawn & Setup, click Start Test Encounter if the status board says it is required.\n" +
                "• Expect: Ally resolves path to a adjacent alternative cell or waits.\n" +
                "• Fail condition: Mover overlaps the blocker on the same cell.",
                tool.SetupScenario4);

            DrawScenarioSection("Scenario 5: Moving Friendly Recalculation", 
                "Spawn & Setup creates the test layout (two moving allies with intersecting paths). If Enemy Spawn Template is assigned, spawns an enemy target nearby.\n" +
                "• Flow: Click Spawn & Setup to create layout. After Spawn & Setup, click Start Test Encounter if the status board says it is required.\n" +
                "• Expect: Allies recalculate path mid-route without colliding.\n" +
                "• Fail condition: Units collide, clip, or deadlock indefinitely.",
                tool.SetupScenario5);

            DrawScenarioSection("Scenario 6: Enemy Proximity", 
                "Spawn & Setup creates the test layout (ally and enemy in adjacent cells).\n" +
                "• Flow: Click Spawn & Setup to create layout. After Spawn & Setup, click Start Test Encounter if the status board says it is required.\n" +
                "• Expect: Ally engages enemy unit directly.\n" +
                "• Fail condition: Units ignore proximity target.",
                tool.SetupScenario6);

            DrawScenarioSection("Scenario 7: Taunt Multi Enemy Layout",
                "Spawn & Setup creates a layout with one Ally (Source) and two or three Enemy units (Target and Distractors) positioned side-by-side to visually verify Taunt redirection.\n" +
                "• Flow: Click Spawn & Setup to create layout. Then use CombatVfxContextMenuTester > Test Effect Taunt or Test Effect Taunt Multi Enemy to trigger VFX.\n" +
                "• Expect: The target unit displays the chevron Taunt VFX pointing towards the Ally Source, and distractors remain clean.",
                tool.SetupScenario7);

            EditorGUILayout.Space();
            if (GUILayout.Button("Clear Spawned Units", GUILayout.Height(30)))
            {
                tool.ClearSpawnedUnits();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScenarioSection(string title, string description, System.Action setupAction)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(description, MessageType.None);
        if (GUILayout.Button("Spawn & Setup"))
        {
            setupAction?.Invoke();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
}
#endif
