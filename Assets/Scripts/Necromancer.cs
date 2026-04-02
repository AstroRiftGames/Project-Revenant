using System.Collections.Generic;
using UnityEngine;

public class Necromancer : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private bool _drawHoveredCellGizmo = true;
    [SerializeField] private bool _drawClickedCellGizmo = true;

    private Camera _mainCamera;
    private readonly Queue<Vector3> _waypoints = new();
    private Vector3 _currentTarget;
    private bool _isMoving;
    private Vector3Int _hoveredCell;
    private bool _hasHoveredCell;
    private Vector3Int _clickedCell;
    private bool _hasClickedCell;
    private bool _clickedCellWasWalkable;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        if (_grid != null)
            SnapToGrid();
    }

    private void Update()
    {
        HandleMovement();
        HandleInput();
    }

    private void HandleMovement()
    {
        if (!_isMoving)
            return;

        transform.position = Vector3.MoveTowards(transform.position, _currentTarget, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, _currentTarget) > 0.001f)
            return;

        transform.position = _currentTarget;

        if (_waypoints.Count > 0)
            _currentTarget = _waypoints.Dequeue();
        else
            _isMoving = false;
    }

    private void HandleInput()
    {
        if (_grid == null)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera == null)
            return;

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        _hoveredCell = _grid.WorldToCell(mouseWorld);
        _hasHoveredCell = true;

        if (!Input.GetMouseButtonDown(0))
            return;

        _clickedCell = _hoveredCell;
        _hasClickedCell = true;
        _clickedCellWasWalkable = _grid.IsCellWalkable(_hoveredCell);

        if (!_clickedCellWasWalkable)
            return;

        Vector3Int currentCell = _grid.WorldToCell(transform.position);
        SetDestination(currentCell, _hoveredCell);
    }

    private void SetDestination(Vector3Int from, Vector3Int to)
    {
        _waypoints.Clear();

        List<Vector3Int> path = GridPathfinder.FindPath(_grid, from, to);

        if (path.Count <= 1)
            return;

        for (int i = 1; i < path.Count; i++)
            _waypoints.Enqueue(_grid.CellToWorld(path[i]));

        _currentTarget = _waypoints.Dequeue();
        _isMoving = true;
    }

    private void SnapToGrid()
    {
        Vector3Int cell = _grid.WorldToCell(transform.position);
        transform.position = _grid.CellToWorld(cell);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_grid == null)
            return;

        if (_drawHoveredCellGizmo && _hasHoveredCell)
            DrawCellGizmo(_hoveredCell, _grid.IsCellWalkable(_hoveredCell), 0.05f);

        if (_drawClickedCellGizmo && _hasClickedCell)
            DrawCellGizmo(_clickedCell, _clickedCellWasWalkable, 0.08f);
    }

    private void DrawCellGizmo(Vector3Int cell, bool isWalkable, float depth)
    {
        Vector3 center = _grid.CellToWorld(cell);
        Vector2 size = _grid.CellWorldSize;

        Gizmos.color = isWalkable
            ? new Color(0.8f, 0.2f, 1f, 0.35f)
            : new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.DrawCube(center, new Vector3(size.x, size.y, depth));
        Gizmos.color = isWalkable ? new Color(0.8f, 0.2f, 1f) : Color.red;
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, depth));
    }
#endif
}