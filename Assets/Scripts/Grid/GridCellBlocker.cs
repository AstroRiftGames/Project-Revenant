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
        _isOccupancyRegistered = StaticGridOccupancyUtility.TryRegister(_grid, this, _isOccupancyRegistered);
    }

    private void ReleaseOccupancy()
    {
        _isOccupancyRegistered = StaticGridOccupancyUtility.Release(_grid, this, _isOccupancyRegistered);
    }

    private void ResolveGrid()
    {
        if (_grid != null)
            return;

        _grid = RoomGridResolver.ResolveInParents(this);
    }
}
