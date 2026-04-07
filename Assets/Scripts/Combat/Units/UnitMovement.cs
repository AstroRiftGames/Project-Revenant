using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private bool _allowSerializedGridFallback;

    private Unit _unit;
    private Vector3Int _currentCell;
    private float _nextStepTime;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    private void Start()
    {
        if (!_allowSerializedGridFallback || _grid == null)
            return;

        SnapToCurrentCell();
    }

    public void SetGrid(BattleGrid grid)
    {
        _grid = grid;
        SnapToCurrentCell();
    }

    public bool SetDestinationCell(Vector3Int destinationCell)
    {
        if (_grid == null || _unit == null)
            return false;

        if (!_grid.IsCellWalkable(destinationCell, _unit))
            return false;

        _currentCell = destinationCell;
        transform.position = _grid.CellToWorld(destinationCell);
        return true;
    }

    public bool SetTarget(Unit targetUnit, int rangeInCells)
    {
        if (_grid == null || _unit == null || targetUnit == null || !targetUnit.IsAlive)
            return false;

        if (IsWithinRange(targetUnit, rangeInCells))
            return false;

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        Vector3Int desiredCell = targetCell;

        if (!_grid.TryFindWalkableCellInRange(targetCell, originCell, Mathf.Max(0, rangeInCells), _unit, out desiredCell))
            desiredCell = targetCell;

        Vector3Int nextStep = GetNextStepTowards(originCell, desiredCell);

        if (nextStep == originCell)
            return false;

        if (!SetDestinationCell(nextStep))
            return false;

        float moveSpeed = Mathf.Max(0.01f, _unit.MoveSpeed);
        _nextStepTime = Time.time + (1f / moveSpeed);
        return true;
    }

    public bool IsWithinRange(Unit targetUnit, int rangeInCells)
    {
        if (_grid == null || _unit == null || targetUnit == null || !targetUnit.IsAlive)
            return false;

        Vector3Int selfCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        return ManhattanDistance(selfCell, targetCell) <= Mathf.Max(0, rangeInCells);
    }

    public void ClearDestination()
    {
    }

    public void ClearPath()
    {
    }

    private void SnapToCurrentCell()
    {
        if (_grid == null)
            return;

        Vector3Int rawCell = _grid.WorldToCell(transform.position);
        _currentCell = _grid.IsCellWalkable(rawCell, _unit)
            ? rawCell
            : _grid.FindClosestWalkableCell(rawCell, _unit);
        transform.position = _grid.CellToWorld(_currentCell);
    }

    private Vector3Int GetCurrentCell()
    {
        if (_grid == null)
            return Vector3Int.zero;

        _currentCell = _grid.WorldToCell(transform.position);
        return _currentCell;
    }

    private Vector3Int GetNextStepTowards(Vector3Int originCell, Vector3Int targetCell)
    {
        if (_grid == null || _unit == null)
            return originCell;

        int originDistance = ManhattanDistance(originCell, targetCell);
        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistance;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        System.Collections.Generic.List<Vector3Int> neighbors = _grid.GetNeighbors(originCell);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            if (!_grid.IsCellWalkable(candidate, _unit))
                continue;

            int candidateDistance = ManhattanDistance(candidate, targetCell);

            if (candidateDistance < bestImprovingDistance)
            {
                bestImprovingStep = candidate;
                bestImprovingDistance = candidateDistance;
            }

            if (candidateDistance <= originDistance && candidateDistance < bestFallbackDistance)
            {
                bestFallbackStep = candidate;
                bestFallbackDistance = candidateDistance;
            }
        }

        if (bestImprovingStep != originCell)
            return bestImprovingStep;

        if (bestFallbackStep != originCell)
            return bestFallbackStep;

        return originCell;
    }

    private static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
