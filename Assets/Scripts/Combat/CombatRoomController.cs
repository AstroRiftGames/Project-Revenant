using System;
using System.Collections.Generic;
using PrefabDungeonGeneration;
using UnityEngine;

public enum CombatRoomState
{
    PendingStart,
    CombatActive,
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
    [SerializeField] private CombatRoomState _state = CombatRoomState.PendingStart;
    [SerializeField] private CombatRoomOutcome _outcome = CombatRoomOutcome.None;
    [SerializeField] private int _enemyFormationEdgePadding = 1;
    [SerializeField] private bool _debugLogs = true;

    public RoomContext RoomContext => _roomContext;
    public RoomPrefabProfile RoomProfile => _roomProfile;
    public CombatRoomState State => _state;
    public CombatRoomOutcome Outcome => _outcome;
    public bool IsCombatRoom => ResolveIsCombatRoom();
    public bool IsPendingStart => _state == CombatRoomState.PendingStart;
    public bool IsCombatActive => _state == CombatRoomState.CombatActive;
    public bool IsResolved => _state == CombatRoomState.Resolved;
    public bool CanUnitsAct => !IsCombatRoom || _state == CombatRoomState.CombatActive;

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
        if (!IsCombatRoom || !IsPendingStart)
            return false;

        _outcome = CombatRoomOutcome.None;
        SetState(CombatRoomState.CombatActive);
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
        LogDebug($"[{nameof(CombatRoomController)}] Room '{name}' resolved with outcome: {_outcome}.");
        CombatResolved?.Invoke(this, _outcome);
        return true;
    }

    public bool ResetEncounter()
    {
        if (!IsCombatRoom)
            return false;

        _outcome = CombatRoomOutcome.None;
        SetState(CombatRoomState.PendingStart);
        return true;
    }

    private void ResolveReferences()
    {
        _roomContext ??= GetComponent<RoomContext>() ?? GetComponentInParent<RoomContext>(includeInactive: true);
        _roomProfile ??= GetComponent<RoomPrefabProfile>() ?? GetComponentInParent<RoomPrefabProfile>(includeInactive: true);
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

        Vector3Int playerAnchorCell = ResolvePlayerAnchorCell(grid, enteredDoor, out Vector3Int forwardDirection);
        Vector3Int lateralDirection = new(-forwardDirection.y, forwardDirection.x, 0);

        List<UnitMovement> enemyMovements = ReleaseEnemyOccupancy(enemies);
        List<Vector3Int> candidateCells = _roomContext.GetAvailableSpawnCells(_enemyFormationEdgePadding);
        candidateCells.Sort((left, right) => CompareEnemyCells(left, right, playerAnchorCell, forwardDirection, lateralDirection));

        int assignedCount = Mathf.Min(enemies.Count, candidateCells.Count);
        for (int i = 0; i < assignedCount; i++)
        {
            Unit enemy = enemies[i];
            UnitMovement movement = enemyMovements[i];
            enemy.transform.position = grid.CellToWorld(candidateCells[i]);

            if (movement != null)
                movement.SetGrid(grid);
            else
                enemy.SnapToGrid();
        }

        for (int i = assignedCount; i < enemies.Count; i++)
        {
            if (enemyMovements[i] != null)
                enemyMovements[i].SetGrid(grid);
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

    private Vector3Int ResolvePlayerAnchorCell(RoomGrid grid, RoomDoor enteredDoor, out Vector3Int forwardDirection)
    {
        Vector3Int roomCenterCell = grid.WorldToCell(_roomContext.transform.position);
        Vector3Int resolvedArrivalCell =
            RoomTransitionPlacementUtility.ResolveArrivalCell(grid, _roomContext, enteredDoor, out forwardDirection);

        Necromancer necromancer = FindAnyObjectByType<Necromancer>();
        if (necromancer == null || !necromancer.TryGetGrid(out RoomGrid necromancerGrid) || !ReferenceEquals(necromancerGrid, grid))
            return resolvedArrivalCell;

        Vector3Int necromancerCell = grid.WorldToCell(necromancer.transform.position);
        if (!grid.HasCell(necromancerCell))
            return resolvedArrivalCell;

        if (enteredDoor == null)
            forwardDirection = RoomTransitionPlacementUtility.GetBestInwardDirection(necromancerCell, roomCenterCell);

        return necromancerCell;
    }

    private static int CompareEnemyCells(
        Vector3Int left,
        Vector3Int right,
        Vector3Int playerAnchorCell,
        Vector3Int forwardDirection,
        Vector3Int lateralDirection)
    {
        int leftForward = Mathf.RoundToInt(Vector3.Dot((Vector3)(left - playerAnchorCell), (Vector3)forwardDirection));
        int rightForward = Mathf.RoundToInt(Vector3.Dot((Vector3)(right - playerAnchorCell), (Vector3)forwardDirection));
        int forwardComparison = rightForward.CompareTo(leftForward);
        if (forwardComparison != 0)
            return forwardComparison;

        int leftLateral = Mathf.RoundToInt(Vector3.Dot((Vector3)(left - playerAnchorCell), (Vector3)lateralDirection));
        int rightLateral = Mathf.RoundToInt(Vector3.Dot((Vector3)(right - playerAnchorCell), (Vector3)lateralDirection));
        int lateralMagnitudeComparison = Mathf.Abs(leftLateral).CompareTo(Mathf.Abs(rightLateral));
        if (lateralMagnitudeComparison != 0)
            return lateralMagnitudeComparison;

        return rightLateral.CompareTo(leftLateral);
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
        if (_roomProfile == null)
            return false;

        return _roomProfile.RoomType == PDRoomType.Combat ||
               _roomProfile.RoomType == PDRoomType.MiniBoss ||
               _roomProfile.RoomType == PDRoomType.Boss;
    }

    private void SetState(CombatRoomState nextState)
    {
        if (_state == nextState)
            return;

        _state = nextState;
        StateChanged?.Invoke(this, _state);
    }

    private void LogDebug(string message)
    {
        if (_debugLogs)
            Debug.Log(message, this);
    }
}
