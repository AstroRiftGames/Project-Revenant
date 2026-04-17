using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DoorInteractable : MonoBehaviour, IInteractable, IGridOccupant
{
    [SerializeField] private RoomDoor _structuralDoor;
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private bool _blocksMovement = true;

    private RoomContext _roomContext;
    private Necromancer _necromancer;
    private bool _isOccupancyRegistered;
    private bool _isInteractionAvailable;

    public event Action<bool> OnInteractionAvailabilityChanged;

    public bool IsInteractionAvailable => _isInteractionAvailable;
    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;

    private void OnEnable()
    {
        ResolveDependencies();
        TryRegisterOccupancy();
        RefreshInteractionAvailability(forceEvent: true);
    }

    private void Start()
    {
        ResolveDependencies();
        TryRegisterOccupancy();
        RefreshInteractionAvailability(forceEvent: true);
    }

    private void Update()
    {
        RefreshInteractionAvailability(forceEvent: false);
    }

    private void OnDisable()
    {
        ReleaseOccupancy();
        SetInteractionAvailability(false, forceEvent: true);
    }

    [ContextMenu("Interact")]
    public void Interact()
    {
        if (!CanInteract())
            return;

        _structuralDoor.TriggerStructuralInteraction();
    }

    private bool CanInteract()
    {
        return IsInteractionAvailable && _structuralDoor != null;
    }

    private void RefreshInteractionAvailability(bool forceEvent)
    {
        ResolveDependencies();
        _necromancer = GridInteractionAvailability.ResolveNecromancer(_necromancer);

        bool shouldBeAvailable =
            isActiveAndEnabled &&
            _structuralDoor != null &&
            !_structuralDoor.IsCombatLocked &&
            GridInteractionAvailability.IsNecromancerAdjacent(_grid, _necromancer, transform.position);

        SetInteractionAvailability(shouldBeAvailable, forceEvent);
    }

    private void SetInteractionAvailability(bool isAvailable, bool forceEvent)
    {
        if (!forceEvent && _isInteractionAvailable == isAvailable)
            return;

        _isInteractionAvailable = isAvailable;
        OnInteractionAvailabilityChanged?.Invoke(_isInteractionAvailable);
    }

    private void ResolveDependencies()
    {
        _structuralDoor ??= GetComponentInParent<RoomDoor>(includeInactive: true);
        _roomContext ??= GetComponentInParent<RoomContext>(includeInactive: true);
        _grid ??= _roomContext != null
            ? _roomContext.RoomGrid
            : GetComponentInParent<RoomGrid>(includeInactive: true);
    }

    private void TryRegisterOccupancy()
    {
        if (_isOccupancyRegistered)
            return;

        if (_grid == null)
            return;

        _grid.OccupancyService.RegisterOccupant(this);
        _isOccupancyRegistered = true;
    }

    private void ReleaseOccupancy()
    {
        if (!_isOccupancyRegistered || _grid == null)
            return;

        _grid.OccupancyService.ReleaseOccupant(this);
        _isOccupancyRegistered = false;
    }
}
