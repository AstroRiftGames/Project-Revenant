using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementTileFeedbackController))]
public class Necromancer : MonoBehaviour
{
    [SerializeField] private RoomGrid _grid;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private MovementTileFeedbackController _movementTileFeedback;
    [SerializeField] private Camera _inputCamera;
    [SerializeField] private bool _cancelSelectionOnRightClick = true;
    [SerializeField] private bool _cancelSelectionOnEscape = true;
    [SerializeField] private bool _drawHoveredCellGizmo = true;
    [SerializeField] private bool _drawClickedCellGizmo = true;

    private Camera _mainCamera;
    private readonly Queue<Vector3Int> _remainingPathCells = new();
    private Vector3Int _currentCell;
    private bool _hasCurrentCell;
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
        _mainCamera = _inputCamera != null ? _inputCamera : Camera.main;

        if (_movementTileFeedback == null)
            _movementTileFeedback = GetComponent<MovementTileFeedbackController>();
    }

    private void Start()
    {
        if (_grid != null)
        {
            _movementTileFeedback?.SetGrid(_grid);
            SnapToGrid();
        }
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
            ResetMovementContext();
            return;
        }

        if (!IsDestinationContextValid())
        {
            ResetMovementContext();
            return;
        }

        if (!EnsureCurrentStepIsTraversable())
            return;

        Vector3 currentTarget = _grid.CellToWorld(_currentStepCell);
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, currentTarget) > 0.001f)
            return;

        SetCurrentCell(_currentStepCell);
        AdvanceToNextStep();
    }

    private void HandleInput()
    {
        if (_grid == null)
        {
            ResetMovementContext();
            return;
        }

        if (_mainCamera == null)
            _mainCamera = _inputCamera != null ? _inputCamera : Camera.main;

        if (_mainCamera == null)
        {
            _movementTileFeedback?.HideHover();
            return;
        }

        Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector3Int hoveredCell = _grid.WorldToCell(mouseWorld);
        if (!_grid.HasCell(hoveredCell))
        {
            _hasHoveredCell = false;
            _movementTileFeedback?.HideHover();
            HandleCancelInput();
            return;
        }

        _hoveredCell = hoveredCell;
        _hasHoveredCell = true;
        bool isHoveredCellEnterable = _grid.IsCellEnterable(_hoveredCell);
        _movementTileFeedback?.ShowHover(_hoveredCell, isHoveredCellEnterable);

        HandleCancelInput();

        if (!Input.GetMouseButtonDown(0))
            return;

        if (!isHoveredCellEnterable)
            return;

        _clickedCell = _hoveredCell;
        _hasClickedCell = true;
        _clickedCellWasWalkable = true;
        _movementTileFeedback?.SetSelection(_clickedCell);

        Vector3Int currentCell = ResolveCurrentCell();
        SetDestination(currentCell, _hoveredCell);
    }

    private void SetDestination(Vector3Int from, Vector3Int to)
    {
        if (_grid == null)
            return;

        _destinationCell = to;
        _hasDestinationCell = true;

        if (!TryBuildPath(from, _destinationCell))
        {
            ResetMovementContext();
            return;
        }

        BeginNextStep();
    }

    private bool EnsureCurrentStepIsTraversable()
    {
        if (_grid == null)
            return false;

        Vector3Int currentCell = ResolveCurrentCell();
        if (_grid.IsStepAllowed(currentCell, _currentStepCell))
            return true;

        if (!_hasDestinationCell)
        {
            ResetMovementContext();
            return false;
        }

        if (!TryBuildPath(currentCell, _destinationCell))
        {
            ResetMovementContext();
            return false;
        }

        BeginNextStep();
        return _grid.IsStepAllowed(currentCell, _currentStepCell);
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
            ResetMovementContext();
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
        SnapToGrid();
        ClearSelectedDestinationState();
        _movementTileFeedback?.ClearSelection();
    }

    private void SnapToGrid()
    {
        if (_grid == null)
            return;

        Vector3Int snappedCell = GridNavigationUtility.ResolvePlacementCell(_grid, transform.position);
        SetCurrentCell(snappedCell);
    }

    public void SetGrid(RoomGrid grid)
    {
        if (ReferenceEquals(_grid, grid))
        {
            _movementTileFeedback?.SetGrid(grid);
            return;
        }

        ResetMovementContext();
        _grid = grid;
        _movementTileFeedback?.SetGrid(grid);
        SnapToGrid();
    }

    public void Teleport(Vector3 worldPosition)
    {
        ResetMovementContext();
        if (_grid == null)
        {
            transform.position = worldPosition;
            _hasCurrentCell = false;
            return;
        }

        Vector3Int destinationCell = GridNavigationUtility.ResolvePlacementCell(_grid, worldPosition);
        SetCurrentCell(destinationCell);
    }

    private void OnDisable()
    {
        ResetMovementContext();
    }

    private void HandleCancelInput()
    {
        bool cancelRequested =
            (_cancelSelectionOnRightClick && Input.GetMouseButtonDown(1)) ||
            (_cancelSelectionOnEscape && Input.GetKeyDown(KeyCode.Escape));

        if (!cancelRequested)
            return;

        ResetMovementContext();
    }

    private void StopMovement()
    {
        _remainingPathCells.Clear();
        _isMoving = false;
        _hasDestinationCell = false;
        _currentStepCell = Vector3Int.zero;
    }

    private void ResetMovementContext()
    {
        StopMovement();
        ClearHoverState();
        ClearSelectedDestinationState();
        _movementTileFeedback?.HideAll();
    }

    private void ClearHoverState()
    {
        _hoveredCell = Vector3Int.zero;
        _hasHoveredCell = false;
    }

    private void ClearSelectedDestinationState()
    {
        _destinationCell = Vector3Int.zero;
        _clickedCell = Vector3Int.zero;
        _hasClickedCell = false;
        _clickedCellWasWalkable = false;
    }

    private Vector3Int ResolveCurrentCell()
    {
        if (_grid == null)
            return Vector3Int.zero;

        if (_hasCurrentCell)
            return _currentCell;

        Vector3Int resolvedCell = GridNavigationUtility.ResolvePlacementCell(_grid, transform.position);
        SetCurrentCell(resolvedCell);
        return resolvedCell;
    }

    private void SetCurrentCell(Vector3Int cell)
    {
        _currentCell = cell;
        _hasCurrentCell = true;
        transform.position = _grid != null ? _grid.CellToWorld(cell) : transform.position;
    }

    private bool IsDestinationContextValid()
    {
        if (_grid == null)
            return false;

        if (_hasDestinationCell && !_grid.HasCell(_destinationCell))
            return false;

        if (_isMoving && !_grid.HasCell(_currentStepCell))
            return false;

        return true;
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
