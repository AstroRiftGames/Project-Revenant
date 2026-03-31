using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private float _moveSpeed = 2.5f;
    [SerializeField] private bool _drawPathGizmos = true;

    private Unit _unit;
    private readonly List<Vector3Int> _currentPath = new();
    private int _pathIndex;
    private bool _hasDestination;
    private Vector3Int _destinationCell;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    private void Update()
    {
        if (_grid == null || _unit == null || !_hasDestination)
            return;

        FollowPath();
    }

    public bool SetDestinationCell(Vector3Int destinationCell)
    {
        if (_grid == null || _unit == null)
            return false;

        Vector3Int startCell = _grid.WorldToCell(_unit.Position);
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
        _hasDestination = true;
        return true;
    }

    public void ClearDestination()
    {
        _currentPath.Clear();
        _pathIndex = 0;
        _hasDestination = false;
    }

    private void FollowPath()
    {
        if (_currentPath.Count == 0 || _pathIndex >= _currentPath.Count)
        {
            ClearDestination();
            return;
        }

        Vector3 targetWorld = _grid.CellToWorld(_currentPath[_pathIndex]);
        transform.position = Vector3.MoveTowards(transform.position, targetWorld, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorld) > 0.01f)
            return;

        transform.position = targetWorld;
        _pathIndex++;

        if (_pathIndex >= _currentPath.Count)
            ClearDestination();
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
