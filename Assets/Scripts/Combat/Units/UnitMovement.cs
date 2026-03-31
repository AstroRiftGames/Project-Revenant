using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private Unit _targetOverride;
    [SerializeField] private float _moveSpeed = 2.5f;
    [SerializeField] private float _repathInterval = 0.25f;
    [SerializeField] private float _stopDistanceInCells = 1f;
    [SerializeField] private bool _moveOnStart = true;
    [SerializeField] private bool _drawPathGizmos = true;

    private Unit _unit;
    private Unit _currentTarget;
    private List<Vector3Int> _currentPath = new();
    private int _pathIndex;
    private float _repathTimer;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    private void Update()
    {
        if (_grid == null || _unit == null)
            return;

        if (!_moveOnStart)
            return;

        _repathTimer -= Time.deltaTime;
        AcquireTarget();

        if (_currentTarget == null)
            return;

        if (_repathTimer <= 0f || _currentPath.Count == 0)
        {
            RebuildPath();
            _repathTimer = _repathInterval;
        }

        FollowPath();
    }

    private void AcquireTarget()
    {
        if (_targetOverride != null)
        {
            _currentTarget = _targetOverride;
            return;
        }

        List<IUnit> visibleUnits = _unit.GetVisibleUnitsInScene();
        _currentTarget = GetNearestVisibleUnit(visibleUnits);
    }

    private Unit GetNearestVisibleUnit(List<IUnit> visibleUnits)
    {
        Unit nearest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < visibleUnits.Count; i++)
        {
            if (visibleUnits[i] is not Unit candidate)
                continue;

            float distance = Vector3.Distance(_unit.Position, candidate.Position);
            if (distance < bestDistance)
            {
                nearest = candidate;
                bestDistance = distance;
            }
        }

        return nearest;
    }

    private void RebuildPath()
    {
        Vector3Int startCell = _grid.WorldToCell(_unit.Position);
        Vector3Int targetCell = _grid.WorldToCell(_currentTarget.Position);
        Vector3Int destinationCell = ResolveDestinationCell(startCell, targetCell);

        if (Vector3Int.Distance(startCell, destinationCell) <= _stopDistanceInCells)
        {
            _currentPath.Clear();
            _pathIndex = 0;
            return;
        }

        _currentPath = GridPathfinder.FindPath(_grid, startCell, destinationCell, _unit);
        _pathIndex = _currentPath.Count > 1 ? 1 : 0;
    }

    private Vector3Int ResolveDestinationCell(Vector3Int startCell, Vector3Int targetCell)
    {
        if (_grid.IsCellWalkable(targetCell, _unit))
            return targetCell;

        List<Vector3Int> neighbors = _grid.GetNeighbors(targetCell);
        Vector3Int bestCell = startCell;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int neighbor = neighbors[i];
            if (!_grid.IsCellWalkable(neighbor, _unit))
                continue;

            float distance = Vector3Int.Distance(startCell, neighbor);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCell = neighbor;
            }
        }

        return bestCell;
    }

    private void FollowPath()
    {
        if (_currentPath.Count == 0 || _pathIndex >= _currentPath.Count)
            return;

        Vector3 targetWorld = _grid.CellToWorld(_currentPath[_pathIndex]);
        transform.position = Vector3.MoveTowards(transform.position, targetWorld, _moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorld) <= 0.01f)
        {
            transform.position = targetWorld;
            _pathIndex++;
        }
    }

    private void OnDrawGizmos()
    {
        if (!_drawPathGizmos || _grid == null || _currentPath == null || _currentPath.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _currentPath.Count; i++)
        {
            Vector3 worldPoint = _grid.CellToWorld(_currentPath[i]);
            Gizmos.DrawSphere(worldPoint, 0.08f);

            if (i + 1 < _currentPath.Count)
                Gizmos.DrawLine(worldPoint, _grid.CellToWorld(_currentPath[i + 1]));
        }
    }
}
