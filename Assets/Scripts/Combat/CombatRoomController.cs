using System;
using PrefabDungeonGeneration;
using UnityEngine;

public enum CombatRoomState
{
    PendingStart,
    CombatActive,
    Resolved
}

[DisallowMultipleComponent]
public class CombatRoomController : MonoBehaviour, IRoomContextComponent
{
    [SerializeField] private RoomContext _roomContext;
    [SerializeField] private RoomPrefabProfile _roomProfile;
    [SerializeField] private CombatRoomState _state = CombatRoomState.PendingStart;

    public RoomContext RoomContext => _roomContext;
    public RoomPrefabProfile RoomProfile => _roomProfile;
    public CombatRoomState State => _state;
    public bool IsCombatRoom => ResolveIsCombatRoom();
    public bool IsPendingStart => _state == CombatRoomState.PendingStart;
    public bool IsCombatActive => _state == CombatRoomState.CombatActive;
    public bool IsResolved => _state == CombatRoomState.Resolved;
    public bool CanUnitsAct => !IsCombatRoom || _state == CombatRoomState.CombatActive;

    public event Action<CombatRoomController> CombatStarted;
    public event Action<CombatRoomController> CombatResolved;
    public event Action<CombatRoomController, CombatRoomState> StateChanged;

    private void Awake()
    {
        ResolveReferences();
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

        SetState(CombatRoomState.CombatActive);
        CombatStarted?.Invoke(this);
        return true;
    }

    public bool TryResolveCombat()
    {
        if (!IsCombatRoom || IsResolved)
            return false;

        SetState(CombatRoomState.Resolved);
        CombatResolved?.Invoke(this);
        return true;
    }

    public bool ResetEncounter()
    {
        if (!IsCombatRoom)
            return false;

        SetState(CombatRoomState.PendingStart);
        return true;
    }

    private void ResolveReferences()
    {
        _roomContext ??= GetComponent<RoomContext>() ?? GetComponentInParent<RoomContext>(includeInactive: true);
        _roomProfile ??= GetComponent<RoomPrefabProfile>() ?? GetComponentInParent<RoomPrefabProfile>(includeInactive: true);
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
}
