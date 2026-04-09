using System.Collections.Generic;
using UnityEngine;

public class Necromancer : MonoBehaviour
{
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private bool _drawHoveredCellGizmo = true;
    [SerializeField] private bool _drawClickedCellGizmo = true;

    private Camera _mainCamera;
    private readonly Queue<Vector3Int> _remainingPathCells = new();
    private Vector3Int _currentStepCell;
    private Vector3Int _destinationCell;
    private bool _hasDestinationCell;
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

        if (_grid == null)
        {
            StopMovement();
            return;
        }

        if (!EnsureCurrentStepIsEnterable())
            return;

        Vector3 currentTarget = _grid.CellToWorld(_currentStepCell);
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, currentTarget) > 0.001f)
            return;

        transform.position = currentTarget;
        AdvanceToNextStep();
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
        Vector3Int resolvedDestination = GridNavigationUtility.ResolvePlacementCell(_grid, _grid.CellToWorld(_hoveredCell));
        _clickedCellWasWalkable = resolvedDestination == _hoveredCell;

        Vector3Int currentCell = _grid.WorldToCell(transform.position);
        SetDestination(currentCell, resolvedDestination);
    }

    private void SetDestination(Vector3Int from, Vector3Int to)
    {
        if (_grid == null)
            return;

        _destinationCell = to;
        _hasDestinationCell = true;

        if (!TryBuildPath(from, _destinationCell))
        {
            StopMovement();
            return;
        }

        BeginNextStep();
    }

    private bool EnsureCurrentStepIsEnterable()
    {
        if (_grid == null)
            return false;

        if (_grid.IsCellEnterable(_currentStepCell))
            return true;

        if (!_hasDestinationCell)
        {
            StopMovement();
            return false;
        }

        Vector3Int currentCell = _grid.WorldToCell(transform.position);
        if (!TryBuildPath(currentCell, _destinationCell))
        {
            StopMovement();
            return false;
        }

        BeginNextStep();
        return _grid.IsCellEnterable(_currentStepCell);
    }

    private bool TryBuildPath(Vector3Int from, Vector3Int to)
    {
        _remainingPathCells.Clear();

        List<Vector3Int> path = GridPathfinder.FindPath(_grid, from, to);
        if (path.Count <= 1)
            return false;

        for (int i = 1; i < path.Count; i++)
            _remainingPathCells.Enqueue(path[i]);

        return _remainingPathCells.Count > 0;
    }

    private void BeginNextStep()
    {
        if (_remainingPathCells.Count == 0)
        {
            StopMovement();
            return;
        }

        _currentStepCell = _remainingPathCells.Dequeue();
        _isMoving = true;
    }

    private void AdvanceToNextStep()
    {
        if (_remainingPathCells.Count > 0)
        {
            BeginNextStep();
            return;
        }

        StopMovement();
    }

    private void SnapToGrid()
    {
        transform.position = GridNavigationUtility.SnapWorldPositionToCell(_grid, transform.position);
    }

    public void SetGrid(RoomGrid grid)
    {
        _grid = grid;
    }

    public void Teleport(Vector3 worldPosition)
    {
        StopMovement();
        transform.position = worldPosition;
    }

    private void StopMovement()
    {
        _remainingPathCells.Clear();
        _isMoving = false;
        _hasDestinationCell = false;
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
