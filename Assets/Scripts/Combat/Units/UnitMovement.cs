using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private float _repathInterval = 0.2f;
    [SerializeField] private bool _drawPathGizmos = true;

    private Unit _unit;
    private readonly List<Vector3Int> _currentPath = new();
    private int _pathIndex;
    private bool _hasDestination;
    private Vector3Int _destinationCell;
    private Vector3Int _currentCell;
    private bool _hasCurrentCell;
    private Unit _targetUnit;
    private int _targetRangeInCells;
    private float _repathTimer;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        if (_grid != null)
            SnapToCurrentCell();
    }

    private void Start()
    {
        if (_grid != null)
            SnapToCurrentCell();
    }

    private void Update()
    {
        if (_grid == null || _unit == null)
            return;

        if (_targetUnit != null)
            UpdateTargetPath();

        if (!_hasDestination)
            return;

        FollowPath();
    }

    public bool SetDestinationCell(Vector3Int destinationCell)
    {
        if (_grid == null || _unit == null)
            return false;

        Vector3Int startCell = GetCurrentCell();
        if (!_grid.IsCellWalkable(destinationCell, _unit))
            return false;

        if (startCell == destinationCell)
        {
            _currentPath.Clear();
            _pathIndex = 0;
            _destinationCell = destinationCell;
            _hasDestination = false;
            return true;
        }

        List<Vector3Int> path = GridPathfinder.FindPath(_grid, startCell, destinationCell, _unit);
        if (path.Count <= 1)
            return false;

        _currentPath.Clear();
        _currentPath.AddRange(path);
        _pathIndex = 1;
        _destinationCell = destinationCell;
        _targetUnit = null;
        _hasDestination = true;
        return true;
    }

    public bool SetTarget(Unit targetUnit, int rangeInCells)
    {
        if (_grid == null || _unit == null || targetUnit == null)
            return false;

        _targetUnit = targetUnit;
        _targetRangeInCells = Mathf.Max(0, rangeInCells);
        _repathTimer = 0f;
        return true;
    }

    public bool IsWithinRange(Unit targetUnit, int rangeInCells)
    {
        if (_grid == null || _unit == null || targetUnit == null)
            return false;

        Vector3Int startCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        int distance = Mathf.Abs(startCell.x - targetCell.x) + Mathf.Abs(startCell.y - targetCell.y);
        return distance <= Mathf.Max(0, rangeInCells);
    }

    public void ClearDestination()
    {
        _currentPath.Clear();
        _pathIndex = 0;
        _hasDestination = false;
        _targetUnit = null;
    }

    public void ClearPath()
    {
        _currentPath.Clear();
        _pathIndex = 0;
        _hasDestination = false;
    }

    private void UpdateTargetPath()
    {
        if (_targetUnit == null)
            return;

        _repathTimer -= Time.deltaTime;
        if (_repathTimer > 0f)
            return;

        _repathTimer = _repathInterval;

        Vector3Int startCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(_targetUnit.Position);
        int distanceToTarget = Mathf.Abs(startCell.x - targetCell.x) + Mathf.Abs(startCell.y - targetCell.y);

        if (distanceToTarget <= _targetRangeInCells)
        {
            SnapToCurrentCell();
            ClearPath();
            return;
        }

        if (!_grid.TryFindWalkableCellInRange(targetCell, startCell, _targetRangeInCells, _unit, out Vector3Int destinationCell))
        {
            ClearPath();
            return;
        }

        if (startCell == destinationCell)
        {
            ClearPath();
            return;
        }

        List<Vector3Int> path = GridPathfinder.FindPath(_grid, startCell, destinationCell, _unit);
        if (path.Count <= 1)
        {
            ClearPath();
            return;
        }

        _currentPath.Clear();
        _currentPath.AddRange(path);
        _pathIndex = 1;
        _destinationCell = destinationCell;
        _hasDestination = true;
    }

    private void FollowPath()
    {
        if (_currentPath.Count == 0 || _pathIndex >= _currentPath.Count)
        {
            ClearPath();
            return;
        }

        Vector3 targetWorld = _grid.CellToWorld(_currentPath[_pathIndex]);
        transform.position = Vector3.MoveTowards(transform.position, targetWorld, _unit.MoveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorld) > 0.01f)
            return;

        transform.position = targetWorld;
        _currentCell = _currentPath[_pathIndex];
        _hasCurrentCell = true;
        _pathIndex++;

        if (_pathIndex >= _currentPath.Count)
        {
            SnapToCurrentCell();
            ClearPath();
        }
    }

    private void SnapToCurrentCell()
    {
        if (_grid == null)
            return;

        _currentCell = _grid.WorldToCell(transform.position);
        _hasCurrentCell = true;
        transform.position = _grid.CellToWorld(_currentCell);
    }

    private Vector3Int GetCurrentCell()
    {
        if (_grid == null)
            return Vector3Int.zero;

        if (!_hasCurrentCell)
            _currentCell = _grid.WorldToCell(transform.position);
        _hasCurrentCell = true;

        return _currentCell;
    }

    private void OnDrawGizmos()
    {
        if (!_drawPathGizmos || _grid == null || _currentPath.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _currentPath.Count; i++)
        {
            Vector3 worldPoint = _grid.CellToWorld(_currentPath[i]);
            Gizmos.DrawSphere(worldPoint, 0.08f);

            if (i + 1 < _currentPath.Count)
                Gizmos.DrawLine(worldPoint, _grid.CellToWorld(_currentPath[i + 1]));
        }

        Vector3 destinationWorld = _grid.CellToWorld(_destinationCell);
        Gizmos.color = new Color(0.2f, 1f, 1f, 0.35f);
        Gizmos.DrawCube(destinationWorld, new Vector3(_grid.CellWorldSize.x, _grid.CellWorldSize.y, 0.05f));
        Gizmos.color = new Color(0.2f, 1f, 1f, 1f);
        Gizmos.DrawWireCube(destinationWorld, new Vector3(_grid.CellWorldSize.x, _grid.CellWorldSize.y, 0.05f));
    }
}
