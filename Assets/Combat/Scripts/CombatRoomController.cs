using System;
using System.Collections.Generic;
using PrefabDungeonGeneration;
using UnityEngine;

public enum CombatRoomState
{
    Deployment,
    Combat,
    Resolved
}

public enum CombatRoomOutcome
{
    None,
    PlayerVictory,
    PlayerDefeat,
    MutualDefeat
}

[DisallowMultipleComponent]
public class CombatRoomController : MonoBehaviour, IRoomContextComponent
{
    [SerializeField] private RoomContext _roomContext;
    [SerializeField] private RoomPrefabProfile _roomProfile;
    [SerializeField] private CombatRoomState _state = CombatRoomState.Deployment;
    [SerializeField] private CombatRoomOutcome _outcome = CombatRoomOutcome.None;
    [SerializeField] private int _enemyFormationEdgePadding = 1;
    [SerializeField] private int _deploymentMinForwardDistance = 1;
    [SerializeField] private int _deploymentFallbackMaxForwardDistance = 3;
    [SerializeField] private Necromancer _necromancer;
    [SerializeField] private bool _debugLogs = true;

    public RoomContext RoomContext => _roomContext;
    public RoomPrefabProfile RoomProfile => _roomProfile;
    public CombatRoomState State => _state;
    public CombatRoomOutcome Outcome => _outcome;
    public bool IsCombatRoom => ResolveIsCombatRoom();
    public bool IsDeploymentActive => _state == CombatRoomState.Deployment;
    public bool IsCombatActive => _state == CombatRoomState.Combat;
    public bool IsResolved => _state == CombatRoomState.Resolved;
    public bool CanUnitsAct => !IsCombatRoom || _state == CombatRoomState.Combat;
    public bool CanDeployUnits => IsCombatRoom && _state == CombatRoomState.Deployment;

    public static event Action<CombatRoomController, CombatRoomOutcome> AnyCombatResolved;
    public event Action<CombatRoomController> CombatStarted;
    public event Action<CombatRoomController, CombatRoomOutcome> CombatResolved;
    public event Action<CombatRoomController, CombatRoomState> StateChanged;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        LifeController.OnUnitDied += HandleUnitDied;
        FloorManager.OnRoomEntered += HandleRoomEntered;
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleUnitDied;
        FloorManager.OnRoomEntered -= HandleRoomEntered;
    }

    public void IntegrateWithRoom(RoomContext roomContext)
    {
        if (roomContext != null)
            _roomContext = roomContext;

        ResolveReferences();
    }

    public bool TryStartCombat()
    {
        if (!IsCombatRoom || !IsDeploymentActive)
            return false;

        _outcome = CombatRoomOutcome.None;
        SetState(CombatRoomState.Combat);
        EvaluateEncounterOutcome();
        CombatStarted?.Invoke(this);
        return true;
    }

    public bool TryResolveCombat(CombatRoomOutcome outcome)
    {
        if (!IsCombatRoom || IsResolved || outcome == CombatRoomOutcome.None)
            return false;

        _outcome = outcome;
        SetState(CombatRoomState.Resolved);
        CleanupResolvedCombatRuntime();
        LogDebug($"[{nameof(CombatRoomController)}] Room '{name}' resolved with outcome: {_outcome}.");
        CombatResolved?.Invoke(this, _outcome);
        AnyCombatResolved?.Invoke(this, _outcome);
        return true;
    }

    public bool ResetEncounter()
    {
        if (!IsCombatRoom)
            return false;

        _outcome = CombatRoomOutcome.None;
        SetState(CombatRoomState.Deployment);
        return true;
    }

    public bool TryMoveDeployedAlly(Unit unit, Vector3Int destinationCell)
    {
        if (!CanDeployUnits || unit == null || _roomContext == null)
            return false;

        if (!ReferenceEquals(unit.RoomContext, _roomContext) || unit.Team != UnitTeam.NecromancerAlly)
            return false;

        RoomGrid grid = _roomContext.RoomGrid;
        if (grid == null || !IsDeploymentCellValid(unit, destinationCell))
            return false;

        UnitMovement movement = unit.GetComponent<UnitMovement>();
        return movement != null && movement.SetDestinationCell(destinationCell);
    }

    public bool IsDeploymentCellValid(Unit unit, Vector3Int cell)
    {
        if (!CanDeployUnits || unit == null || _roomContext == null)
            return false;

        RoomGrid grid = _roomContext.RoomGrid;
        if (grid == null || !grid.HasCell(cell) || !grid.IsCellEnterable(cell, unit))
            return false;

        if (!RoomFormationPlacementUtility.TryResolveDeploymentFrame(grid, _roomContext, ResolveNecromancer(), out RoomPlacementFrame placementFrame))
            return false;

        int forwardDistance = Mathf.RoundToInt(Vector3.Dot((Vector3)(cell - placementFrame.AnchorCell), (Vector3)placementFrame.ForwardDirection));
        if (forwardDistance < Mathf.Max(0, _deploymentMinForwardDistance))
            return false;

        int maxForwardDistance = ResolveDeploymentMaxForwardDistance(placementFrame.AnchorCell, placementFrame.ForwardDirection);
        return forwardDistance <= maxForwardDistance;
    }

    private void ResolveReferences()
    {
        _roomContext ??= GetComponent<RoomContext>() ?? GetComponentInParent<RoomContext>(includeInactive: true);
        _roomProfile ??= GetComponent<RoomPrefabProfile>() ?? GetComponentInParent<RoomPrefabProfile>(includeInactive: true);
    }

    private Necromancer ResolveNecromancer()
    {
        _necromancer = NecromancerReferenceUtility.Resolve(_necromancer);
        return _necromancer;
    }

    private void HandleUnitDied(Unit unit)
    {
        if (!IsCombatActive || unit == null || _roomContext == null)
            return;

        if (!ReferenceEquals(unit.RoomContext, _roomContext))
            return;

        EvaluateEncounterOutcome();
    }

    private void EvaluateEncounterOutcome()
    {
        if (!IsCombatActive || _roomContext == null)
            return;

        bool hasAliveAllies = false;
        bool hasAliveEnemies = false;
        IReadOnlyList<Unit> roomUnits = _roomContext.Units;

        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit unit = roomUnits[i];
            if (!IsValidCombatant(unit))
                continue;

            if (unit.Team == UnitTeam.NecromancerAlly)
                hasAliveAllies = true;
            else if (unit.Team == UnitTeam.Enemy)
                hasAliveEnemies = true;

            if (hasAliveAllies && hasAliveEnemies)
                return;
        }

        CombatRoomOutcome outcome = ResolveOutcome(hasAliveAllies, hasAliveEnemies);
        if (outcome == CombatRoomOutcome.None)
            return;

        TryResolveCombat(outcome);
    }

    private void HandleRoomEntered(RoomDoor enteredDoor, GameObject newRoom)
    {
        if (!IsCombatRoom || _roomContext == null || !ReferenceEquals(newRoom, _roomContext.gameObject))
            return;

        ArrangeEnemiesForRoomEntry(enteredDoor);
    }

    private void ArrangeEnemiesForRoomEntry(RoomDoor enteredDoor)
    {
        RoomGrid grid = _roomContext.RoomGrid;
        if (grid == null)
            return;

        List<Unit> enemies = GetActiveEnemies();
        if (enemies.Count == 0)
            return;

        if (!RoomFormationPlacementUtility.TryResolveEntryFrame(grid, _roomContext, enteredDoor, ResolveNecromancer(), out RoomPlacementFrame placementFrame))
            return;

        List<UnitMovement> enemyMovements = ReleaseEnemyOccupancy(enemies);
        List<Vector3Int> candidateCells = _roomContext.GetAvailableSpawnCells(_enemyFormationEdgePadding);
        RoomPlacementCellSelectionUtility.SortByEntryPriority(candidateCells, placementFrame);

        int assignedCount = Mathf.Min(enemies.Count, candidateCells.Count);
        for (int i = 0; i < assignedCount; i++)
        {
            Unit enemy = enemies[i];
            UnitMovement movement = enemyMovements[i];
            if (movement != null)
            {
                if (!movement.AttachToGridAtCell(grid, candidateCells[i]))
                    enemy.SnapToGrid();
            }
            else
            {
                enemy.transform.position = grid.CellToWorld(candidateCells[i]);
                enemy.SnapToGrid();
            }
        }

        for (int i = assignedCount; i < enemies.Count; i++)
        {
            if (enemyMovements[i] != null)
            {
                Vector3Int currentCell = GridNavigationUtility.ResolvePlacementCell(grid, enemies[i].transform.position, enemies[i]);
                if (!enemyMovements[i].AttachToGridAtCell(grid, currentCell))
                    enemies[i].SnapToGrid();
            }
            else
                enemies[i].SnapToGrid();
        }
    }

    private List<Unit> GetActiveEnemies()
    {
        var enemies = new List<Unit>();
        if (_roomContext == null)
            return enemies;

        IReadOnlyList<Unit> roomUnits = _roomContext.Units;
        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit unit = roomUnits[i];
            if (unit == null || !unit.gameObject.activeInHierarchy || !unit.IsAlive || unit.Team != UnitTeam.Enemy)
                continue;

            enemies.Add(unit);
        }

        enemies.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return enemies;
    }

    private List<UnitMovement> ReleaseEnemyOccupancy(List<Unit> enemies)
    {
        var movements = new List<UnitMovement>(enemies.Count);

        for (int i = 0; i < enemies.Count; i++)
        {
            Unit enemy = enemies[i];
            UnitMovement movement = enemy != null ? enemy.GetComponent<UnitMovement>() : null;
            movements.Add(movement);
            movement?.SetGrid(null);
        }

        return movements;
    }

    private static bool IsValidCombatant(Unit unit)
    {
        return unit != null &&
               unit.gameObject.activeInHierarchy &&
               unit.IsAlive;
    }

    private static CombatRoomOutcome ResolveOutcome(bool hasAliveAllies, bool hasAliveEnemies)
    {
        if (hasAliveAllies && !hasAliveEnemies)
            return CombatRoomOutcome.PlayerVictory;

        if (!hasAliveAllies && hasAliveEnemies)
            return CombatRoomOutcome.PlayerDefeat;

        if (!hasAliveAllies && !hasAliveEnemies)
            return CombatRoomOutcome.MutualDefeat;

        return CombatRoomOutcome.None;
    }

    private bool ResolveIsCombatRoom()
    {
        return RoomCombatUtility.IsCombatRoom(_roomProfile);
    }

    private int ResolveDeploymentMaxForwardDistance(Vector3Int anchorCell, Vector3Int forwardDirection)
    {
        List<Vector3Int> candidateCells = _roomContext.GetAvailableSpawnCells();
        int furthestForwardDistance = 0;

        for (int i = 0; i < candidateCells.Count; i++)
        {
            Vector3Int candidateCell = candidateCells[i];
            int forwardDistance = RoomPlacementCellSelectionUtility.ResolveForwardDistance(candidateCell, anchorCell, forwardDirection);
            if (forwardDistance > furthestForwardDistance)
                furthestForwardDistance = forwardDistance;
        }

        int halfDepth = Mathf.FloorToInt(furthestForwardDistance * 0.5f);
        return Mathf.Max(_deploymentFallbackMaxForwardDistance, halfDepth, _deploymentMinForwardDistance);
    }

    private void SetState(CombatRoomState nextState)
    {
        if (_state == nextState)
            return;

        _state = nextState;
        StateChanged?.Invoke(this, _state);
    }

    private void CleanupResolvedCombatRuntime()
    {
        if (_roomContext == null)
            return;

        CleanupRoomStatusEffects();

        if (_outcome != CombatRoomOutcome.PlayerVictory)
            return;

        CleanupSummonedMinionRuntime();
        CleanupRoomProjectileRuntime();
    }

    private void CleanupRoomStatusEffects()
    {
        IReadOnlyList<Unit> roomUnits = _roomContext.Units;
        for (int i = 0; i < roomUnits.Count; i++)
        {
            Unit unit = roomUnits[i];
            if (unit == null || unit.StatusEffects == null)
                continue;

            unit.StatusEffects.ClearCombatEffects();
        }
    }

    private void CleanupSummonedMinionRuntime()
    {
        CombatSummonedUnitRuntimeMarker[] summonedUnits =
            _roomContext.GetComponentsInChildren<CombatSummonedUnitRuntimeMarker>(includeInactive: true);

        for (int i = 0; i < summonedUnits.Length; i++)
        {
            CombatSummonedUnitRuntimeMarker summonedUnit = summonedUnits[i];
            if (summonedUnit == null)
                continue;

            Destroy(summonedUnit.gameObject);
        }
    }

    private void CleanupRoomProjectileRuntime()
    {
        CombatProjectileVisual[] roomProjectiles =
            _roomContext.GetComponentsInChildren<CombatProjectileVisual>(includeInactive: true);

        for (int i = 0; i < roomProjectiles.Length; i++)
        {
            CombatProjectileVisual projectile = roomProjectiles[i];
            if (projectile == null)
                continue;

            Destroy(projectile.gameObject);
        }
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
