using System;
using UnityEngine;

public class RoomDoor : MonoBehaviour, IInteractable, IGridOccupant
{
    [Header("Connected Rooms")]
    public GameObject roomA;
    public GameObject roomB;

    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private bool _debugCombatLockLogs = true;

    private RoomContext _roomContext;
    private CombatRoomController _combatRoomController;
    private DoorInteractable _gameplayDoor;
    private Necromancer _necromancer;
    private bool _isCompatibilityOccupancyRegistered;
    private bool _hasLoggedCombatLock;
    private bool _isCompatibilityInteractionAvailable;

    public static event Action<RoomDoor> OnDoorInteracted;
    public event Action<bool> OnInteractionAvailabilityChanged;

    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => OwnsGameplayInteraction() && gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;
    public bool IsInteractionAvailable => OwnsGameplayInteraction() && _isCompatibilityInteractionAvailable;
    public bool UsesChildGameplayInteractable => !OwnsGameplayInteraction();

    private void OnEnable()
    {
        ResolveRoomDependencies();
        SyncCompatibilityGameplay(forceEvent: true);
    }

    private void Start()
    {
        ResolveRoomDependencies();
        SyncCompatibilityGameplay(forceEvent: true);
    }

    private void Update()
    {
        SyncCompatibilityGameplay(forceEvent: false);
    }

    private void OnDisable()
    {
        ReleaseCompatibilityOccupancy();
        SetCompatibilityInteractionAvailability(false, forceEvent: true);
    }

    [ContextMenu("Interact")]
    public void Interact()
    {
        if (!CanInteract())
            return;

        PerformInteraction();
    }

    public void TryInteractFromTrigger(Collider2D other)
    {
        if (!CanTriggerInteraction(other))
            return;

        Interact();
    }

    public bool IsCombatLocked => IsLockedByActiveCombat();

    public void TriggerStructuralInteraction()
    {
        PerformInteraction();
    }

    protected virtual bool CanInteract()
    {
        if (!OwnsGameplayInteraction())
            return false;

        if (!IsInteractionAvailable)
        {
            if (isActiveAndEnabled && IsLockedByActiveCombat())
                LogCombatLock();

            return false;
        }

        _hasLoggedCombatLock = false;
        return true;
    }

    protected virtual void PerformInteraction()
    {
        OnDoorInteracted?.Invoke(this);
    }

    protected virtual bool CanTriggerInteraction(Collider2D other)
    {
        return other != null && other.CompareTag("Player");
    }

    private void ResolveGrid()
    {
        if (_grid != null)
            return;

        ResolveRoomDependencies();
        _grid = _roomContext != null
            ? _roomContext.RoomGrid
            : GetComponentInParent<RoomGrid>(includeInactive: true);
    }

    private void ResolveRoomDependencies()
    {
        _roomContext ??= GetComponentInParent<RoomContext>(includeInactive: true);
        _combatRoomController ??= _roomContext != null ? _roomContext.CombatController : null;
        ResolveGameplayDoor();
    }

    private bool IsLockedByActiveCombat()
    {
        ResolveRoomDependencies();

        return _combatRoomController != null &&
               _combatRoomController.IsCombatRoom &&
               _combatRoomController.State == CombatRoomState.CombatActive;
    }

    private void LogCombatLock()
    {
        if (!_debugCombatLockLogs || _hasLoggedCombatLock)
            return;

        _hasLoggedCombatLock = true;
        Debug.Log($"[RoomDoor] '{name}' locked during combat in room '{_roomContext?.name ?? "Unknown"}'.", this);
    }

    private void SyncCompatibilityGameplay(bool forceEvent)
    {
        if (!OwnsGameplayInteraction())
        {
            ReleaseCompatibilityOccupancy();
            SetCompatibilityInteractionAvailability(false, forceEvent: true);
            return;
        }

        TryRegisterCompatibilityOccupancy();
        RefreshCompatibilityInteractionAvailability(forceEvent);
    }

    private void TryRegisterCompatibilityOccupancy()
    {
        ResolveGrid();
        if (_grid == null || _isCompatibilityOccupancyRegistered)
            return;

        _grid.OccupancyService.RegisterOccupant(this);
        _isCompatibilityOccupancyRegistered = true;
    }

    private void ReleaseCompatibilityOccupancy()
    {
        if (_grid == null || !_isCompatibilityOccupancyRegistered)
            return;

        _grid.OccupancyService.ReleaseOccupant(this);
        _isCompatibilityOccupancyRegistered = false;
    }

    private void RefreshCompatibilityInteractionAvailability(bool forceEvent)
    {
        ResolveGrid();
        _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);

        bool shouldBeAvailable =
            isActiveAndEnabled &&
            !IsLockedByActiveCombat() &&
            GridInteractionAvailability.IsNecromancerAdjacent(_grid, _necromancer, transform.position);

        SetCompatibilityInteractionAvailability(shouldBeAvailable, forceEvent);
    }

    private void SetCompatibilityInteractionAvailability(bool isAvailable, bool forceEvent)
    {
        if (!forceEvent && _isCompatibilityInteractionAvailable == isAvailable)
            return;

        _isCompatibilityInteractionAvailable = isAvailable;
        OnInteractionAvailabilityChanged?.Invoke(_isCompatibilityInteractionAvailable);
    }

    private void ResolveGameplayDoor()
    {
        if (_gameplayDoor != null)
            return;

        DoorInteractable[] gameplayDoors = GetComponentsInChildren<DoorInteractable>(includeInactive: true);
        for (int i = 0; i < gameplayDoors.Length; i++)
        {
            DoorInteractable candidate = gameplayDoors[i];
            if (candidate == null || candidate.gameObject == gameObject)
                continue;

            _gameplayDoor = candidate;
            return;
        }
    }

    private bool OwnsGameplayInteraction()
    {
        ResolveGameplayDoor();
        return _gameplayDoor == null;
    }
}
