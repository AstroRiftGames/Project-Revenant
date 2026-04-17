using UnityEngine;

[DisallowMultipleComponent]
public class GridCellBlocker : MonoBehaviour, IGridOccupant
{
    [SerializeField] private bool _occupiesCell = true;
    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private RoomGrid _grid;
    private bool _isOccupancyRegistered;

    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => _occupiesCell && gameObject.activeInHierarchy;
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
            ? roomContext.RoomGrid
            : GetComponentInParent<RoomGrid>(includeInactive: true);
    }
}
