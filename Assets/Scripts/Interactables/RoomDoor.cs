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
    private bool _isOccupancyRegistered;
    private bool _hasLoggedCombatLock;

    public static event Action<RoomDoor> OnDoorInteracted;
    public event Action<bool> OnInteractionAvailabilityChanged;

    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;
    public bool IsInteractionAvailable => isActiveAndEnabled && !IsLockedByActiveCombat();

    private void OnEnable()
    {
        ResolveRoomDependencies();
        TryRegisterOccupancy();
        OnInteractionAvailabilityChanged?.Invoke(IsInteractionAvailable);
    }

    private void Start()
    {
        ResolveRoomDependencies();
        // Fallback
        TryRegisterOccupancy();
    }

    private void OnDisable()
    {
        ReleaseOccupancy();
        OnInteractionAvailabilityChanged?.Invoke(false);
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

    protected virtual bool CanInteract()
    {
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

    private void TryRegisterOccupancy()
    {
        ResolveGrid();
        if (_grid == null)
            return;

        if (_isOccupancyRegistered)
            return;

        _grid.OccupancyService.RegisterOccupant(this);
        _isOccupancyRegistered = true;
    }

    private void ReleaseOccupancy()
    {
        if (_grid == null || !_isOccupancyRegistered)
            return;

        _grid.OccupancyService.ReleaseOccupant(this);
        _isOccupancyRegistered = false;
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
}
