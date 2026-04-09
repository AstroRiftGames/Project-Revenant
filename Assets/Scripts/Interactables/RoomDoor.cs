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

    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;

    private void OnEnable()
    {
        TryRegisterOccupancy();
    }

    private void Start()
    {
        // Fallback
        TryRegisterOccupancy();
    }

    private void OnDisable()
    {
        ReleaseOccupancy();
    }

    [ContextMenu("Interact")]
    public virtual void Interact()
    {
        OnDoorInteracted?.Invoke(this);
    }

    public void HandleTriggerEnter(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Interact();
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
