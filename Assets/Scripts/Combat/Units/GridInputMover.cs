using UnityEngine;

[RequireComponent(typeof(Unit))]
public class GridInputMover : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private float _moveSpeed = 4f;
    [SerializeField] private bool _drawTargetCellGizmo = true;

    private Unit _unit;
    private bool _isMoving;
    private Vector3 _targetWorldPosition;
    private Vector3Int _targetCell;
    private Camera _mainCamera;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _mainCamera = Camera.main;
        _targetWorldPosition = transform.position;
        _targetCell = _grid != null ? _grid.WorldToCell(transform.position) : Vector3Int.zero;
    }

    private void Update()
    {
        if (_grid == null)
            return;

        if (_isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetWorldPosition, _moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetWorldPosition) <= 0.001f)
            {
                transform.position = _targetWorldPosition;
                _isMoving = false;
            }

            return;
        }

        if (!Input.GetMouseButtonDown(0))
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;

        Vector3Int clickedCell = _grid.WorldToCell(mouseWorldPosition);
        if (!_grid.IsCellWalkable(clickedCell, _unit))
            return;

        _targetCell = clickedCell;
        _targetWorldPosition = _grid.CellToWorld(clickedCell);
        _isMoving = true;
    }

    private void OnDrawGizmos()
    {
        if (!_drawTargetCellGizmo || _grid == null)
            return;

        Vector3Int cell = Application.isPlaying ? _targetCell : _grid.WorldToCell(transform.position);
        Vector3 center = _grid.CellToWorld(cell);
        Vector2 size = _grid.CellWorldSize;

        Gizmos.color = new Color(0.2f, 1f, 1f, 0.35f);
        Gizmos.DrawCube(center, new Vector3(size.x, size.y, 0.05f));
        Gizmos.color = new Color(0.2f, 1f, 1f, 1f);
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.05f));
    }
}
