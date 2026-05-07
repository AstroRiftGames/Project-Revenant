using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class GridAvoidanceObstacle : MonoBehaviour
{
    private static readonly Vector3[] SharedGizmoCellCorners = new Vector3[4];

    [SerializeField] private int _radiusInCells = 1;
    [SerializeField] private int _avoidanceCost = 5;
    [SerializeField] private bool _blocksCenterCell;
    [SerializeField] private bool _includeDiagonals = true;
    [SerializeField] private bool _isActive = true;
    [SerializeField] private bool _drawGizmos = true;
    [SerializeField] private Transform _cellAnchor;
    [SerializeField] private Vector3 _cellAnchorWorldOffset;

    private readonly List<Vector3Int> _gizmoCellBuffer = new();

    private RoomGrid _grid;
    private GridAvoidanceObstacleRegistry _registry;
    private Vector3Int _lastCenterCell;
    private bool _hasLastCenterCell;
    private bool _isRegistered;
    private bool _lastRuntimeActive;

    public int RadiusInCells => Mathf.Max(0, _radiusInCells);
    public int AvoidanceCost => Mathf.Max(0, _avoidanceCost);
    public bool BlocksCenterCell => _blocksCenterCell;
    public bool IncludeDiagonals => _includeDiagonals;
    public bool IsRuntimeActive => _isActive && isActiveAndEnabled;
    public bool IsRegisteredAndActive => _isRegistered && IsRuntimeActive;

    private void OnEnable()
    {
        RefreshRegistration();
    }

    private void Start()
    {
        RefreshRegistration();
    }

    private void Update()
    {
        MonitorRegistrationContext();
    }

    private void OnDisable()
    {
        UnregisterFromRegistry();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _radiusInCells = Mathf.Max(0, _radiusInCells);
        _avoidanceCost = Mathf.Max(0, _avoidanceCost);
    }
#endif

    public bool TryGetCenterCell(out Vector3Int centerCell)
    {
        centerCell = Vector3Int.zero;

        if (_grid == null)
            return false;

        centerCell = _grid.WorldToCell(GetAnchorWorldPosition());
        return true;
    }

    public void GetAffectedCells(List<Vector3Int> results)
    {
        if (results == null)
            return;

        results.Clear();

        if (!IsRuntimeActive || !TryGetCenterCell(out Vector3Int centerCell))
            return;

        int radius = RadiusInCells;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (!IsOffsetIncluded(dx, dy, radius))
                    continue;

                Vector3Int cell = centerCell + new Vector3Int(dx, dy, 0);
                if (!_grid.HasCell(cell))
                    continue;

                results.Add(cell);
            }
        }
    }

    private void MonitorRegistrationContext()
    {
        bool runtimeActive = IsRuntimeActive;
        if (runtimeActive != _lastRuntimeActive)
        {
            RefreshRegistration();
            _lastRuntimeActive = runtimeActive;
            transform.hasChanged = false;
            return;
        }

        if (transform.hasChanged)
        {
            RefreshRegistration();
            transform.hasChanged = false;
            if (_cellAnchor != null)
                _cellAnchor.hasChanged = false;
            return;
        }

        if (_cellAnchor != null && _cellAnchor.hasChanged)
        {
            RefreshRegistration();
            _cellAnchor.hasChanged = false;
            return;
        }

        RoomGrid resolvedGrid = RoomGridResolver.ResolveInParents(this);
        if (!ReferenceEquals(resolvedGrid, _grid))
            RefreshRegistration();
    }

    private void RefreshRegistration()
    {
        RoomGrid nextGrid = RoomGridResolver.ResolveInParents(this);
        GridAvoidanceObstacleRegistry nextRegistry = nextGrid != null ? nextGrid.AvoidanceObstacleRegistry : null;

        bool hasCenterCell = nextGrid != null;
        Vector3Int nextCenterCell = hasCenterCell ? nextGrid.WorldToCell(GetAnchorWorldPosition()) : Vector3Int.zero;
        bool registrationChanged =
            !ReferenceEquals(nextGrid, _grid) ||
            !ReferenceEquals(nextRegistry, _registry) ||
            _hasLastCenterCell != hasCenterCell ||
            (_hasLastCenterCell && hasCenterCell && nextCenterCell != _lastCenterCell);

        if (registrationChanged || !IsRuntimeActive)
            UnregisterFromRegistry();

        _grid = nextGrid;
        _registry = nextRegistry;
        _lastRuntimeActive = IsRuntimeActive;
        _hasLastCenterCell = hasCenterCell;
        _lastCenterCell = nextCenterCell;

        if (!IsRuntimeActive || _registry == null)
            return;

        if (!_isRegistered)
        {
            _registry.RegisterObstacle(this);
            _isRegistered = true;
        }

        if (_cellAnchor != null)
            _cellAnchor.hasChanged = false;
    }

    private void UnregisterFromRegistry()
    {
        if (_registry != null)
            _registry.UnregisterObstacle(this);

        _isRegistered = false;
    }

    private bool IsOffsetIncluded(int dx, int dy, int radius)
    {
        if (_includeDiagonals)
            return Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) <= radius;

        return Mathf.Abs(dx) + Mathf.Abs(dy) <= radius;
    }

    private void OnDrawGizmosSelected()
    {
        if (!_drawGizmos)
            return;

        RoomGrid grid = Application.isPlaying ? _grid : RoomGridResolver.ResolveInParents(this);
        if (grid == null)
            return;

        _gizmoCellBuffer.Clear();
        GridAvoidanceObstacleRegistry previousRegistry = _registry;
        RoomGrid previousGrid = _grid;
        _grid = grid;
        GetAffectedCells(_gizmoCellBuffer);
        _grid = previousGrid;
        _registry = previousRegistry;

        if (!TryResolveGizmoCenter(grid, out Vector3Int centerCell))
            return;

        DrawCenterGizmo(grid, centerCell);
        DrawAffectedCellsGizmos(grid, centerCell);
        DrawAnchorGizmo();
    }

    private bool TryResolveGizmoCenter(RoomGrid grid, out Vector3Int centerCell)
    {
        centerCell = Vector3Int.zero;
        if (grid == null)
            return false;

        centerCell = grid.WorldToCell(GetAnchorWorldPosition());
        return true;
    }

    private Vector3 GetAnchorWorldPosition()
    {
        if (_cellAnchor != null)
            return _cellAnchor.position;

        return transform.position + _cellAnchorWorldOffset;
    }

    private void DrawCenterGizmo(RoomGrid grid, Vector3Int centerCell)
    {
        if (!grid.TryGetCellVisualCornersWorld(centerCell, SharedGizmoCellCorners))
            return;

        DrawCellGizmo(
            SharedGizmoCellCorners,
            _blocksCenterCell
                ? new Color(1f, 0.2f, 0.2f, 0.45f)
                : new Color(1f, 0.75f, 0.2f, 0.25f),
            _blocksCenterCell
                ? Color.red
                : new Color(1f, 0.75f, 0.2f, 1f));
    }

    private void DrawAffectedCellsGizmos(RoomGrid grid, Vector3Int centerCell)
    {
        for (int i = 0; i < _gizmoCellBuffer.Count; i++)
        {
            Vector3Int cell = _gizmoCellBuffer[i];
            if (cell == centerCell)
                continue;

            if (!grid.TryGetCellVisualCornersWorld(cell, SharedGizmoCellCorners))
                continue;

            DrawCellGizmo(
                SharedGizmoCellCorners,
                new Color(1f, 0.85f, 0.1f, 0.18f),
                new Color(1f, 0.85f, 0.1f, 0.9f));
        }
    }

    private void DrawAnchorGizmo()
    {
        Vector3 anchorWorld = GetAnchorWorldPosition();
        float crossHalfSize = 0.08f;

        Gizmos.color = new Color(0.2f, 1f, 1f, 0.95f);
        Gizmos.DrawSphere(anchorWorld, 0.04f);
        Gizmos.DrawLine(anchorWorld + Vector3.left * crossHalfSize, anchorWorld + Vector3.right * crossHalfSize);
        Gizmos.DrawLine(anchorWorld + Vector3.up * crossHalfSize, anchorWorld + Vector3.down * crossHalfSize);
    }

    private static void DrawCellGizmo(Vector3[] corners, Color fillColor, Color outlineColor)
    {
        if (corners == null || corners.Length < 4)
            return;

#if UNITY_EDITOR
        Handles.DrawSolidRectangleWithOutline(corners, fillColor, outlineColor);
#else
        Gizmos.color = outlineColor;
        for (int i = 0; i < 4; i++)
        {
            Vector3 from = corners[i];
            Vector3 to = corners[(i + 1) % 4];
            Gizmos.DrawLine(from, to);
        }
#endif
    }
}
