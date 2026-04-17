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
    }

    private void OnDisable()
    {
        LifeController.OnUnitDied -= HandleUnitDied;
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
