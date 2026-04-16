using System;
using UnityEngine;

public class RoomDoor : MonoBehaviour, IInteractable, IGridOccupant
{
    [Header("Connected Rooms")]
    public GameObject roomA;
    public GameObject roomB;

    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private RoomGrid _grid;
    private bool _isOccupancyRegistered;

    public static event Action<RoomDoor> OnDoorInteracted;
    public event Action<bool> OnInteractionAvailabilityChanged;

    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;
    public bool IsInteractionAvailable => isActiveAndEnabled;

    private void OnEnable()
    {
        TryRegisterOccupancy();
        OnInteractionAvailabilityChanged?.Invoke(IsInteractionAvailable);
    }

    private void Start()
    {
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
        return IsInteractionAvailable;
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

        RoomContext roomContext = GetComponentInParent<RoomContext>(includeInactive: true);
        _grid = roomContext != null
            ? roomContext.BattleGrid
            : GetComponentInParent<RoomGrid>(includeInactive: true);
    }
}
