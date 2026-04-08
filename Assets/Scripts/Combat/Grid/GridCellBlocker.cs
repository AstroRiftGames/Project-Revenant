using UnityEngine;

[DisallowMultipleComponent]
public class GridCellBlocker : MonoBehaviour, IGridOccupant
{
    [SerializeField] private bool _occupiesCell = true;
    [SerializeField] private bool _blocksMovement = true;
    [SerializeField] private RoomGrid _grid;

    public Vector3 OccupancyWorldPosition => transform.position;
    public bool OccupiesCell => _occupiesCell && gameObject.activeInHierarchy;
    public bool BlocksMovement => _blocksMovement;

    private void OnEnable()
    {
        RefreshOccupancy();
    }

    private void Start()
    {
        RefreshOccupancy();
    }

    private void OnDisable()
    {
        _grid?.OccupancyService.ReleaseOccupant(this);
    }

    private void RefreshOccupancy()
    {
        ResolveGrid();
        _grid?.OccupancyService.RegisterOccupant(this);
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
