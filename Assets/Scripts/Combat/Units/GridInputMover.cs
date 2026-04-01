using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class GridInputMover : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private bool _drawHoveredCellGizmo = true;
    [SerializeField] private bool _drawClickedCellGizmo = true;

    private Unit _unit;
    private UnitMovement _unitMovement;
    private Camera _mainCamera;
    private Vector3Int _hoveredCell;
    private bool _hasHoveredCell;
    private Vector3Int _clickedCell;
    private bool _hasClickedCell;
    private bool _clickedCellWasWalkable;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _unitMovement = GetComponent<UnitMovement>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (_grid == null)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        _hoveredCell = _grid.WorldToCell(mouseWorldPosition);
        _hasHoveredCell = true;

        if (!Input.GetMouseButtonDown(0))
            return;

        _clickedCell = _hoveredCell;
        _hasClickedCell = true;
        _clickedCellWasWalkable = _grid.IsCellWalkable(_hoveredCell, _unit);

        if (_clickedCellWasWalkable)
            _unitMovement.SetDestinationCell(_hoveredCell);
    }

    private void OnDrawGizmos()
    {
        if (_grid == null)
            return;

        if (_drawHoveredCellGizmo && _hasHoveredCell)
            DrawCellGizmo(_hoveredCell, _unit != null && _grid.IsCellWalkable(_hoveredCell, _unit), 0.05f);

        if (_drawClickedCellGizmo && _hasClickedCell)
            DrawCellGizmo(_clickedCell, _clickedCellWasWalkable, 0.08f);
    }

    private void DrawCellGizmo(Vector3Int cell, bool isWalkable, float depth)
    {
        Vector3 center = _grid.CellToWorld(cell);
        Vector2 size = _grid.CellWorldSize;

        Gizmos.color = isWalkable
            ? new Color(0.2f, 1f, 0.2f, 0.35f)
            : new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawCube(center, new Vector3(size.x, size.y, depth));
        Gizmos.color = isWalkable ? Color.green : Color.red;
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, depth));
    }
}
