using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Unit))]
public class UnitMovement : MonoBehaviour
{
    [SerializeField] private BattleGrid _grid;
    [SerializeField] private bool _allowSerializedGridFallback;
    [SerializeField] private float _repathInterval = 0.2f;

    private Unit _unit;
    private Vector3Int _currentCell;
    private float _nextStepTime;
    private Unit _cachedTargetUnit;
    private Vector3Int _cachedTargetCell;
    private bool _hasCachedTargetCell;
    private int _cachedTargetRange;
    private float _nextRepathTime;
    private readonly List<Vector3Int> _cachedPath = new();

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
        InvalidatePathCache();
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

        RefreshPathCache(originCell, desiredCell, targetUnit, Mathf.Max(0, rangeInCells));

        Vector3Int nextStep = GetNextStepTowards(originCell, desiredCell);

        if (nextStep == originCell)
            return false;

        if (!SetDestinationCell(nextStep))
            return false;

        float moveSpeed = Mathf.Max(0.01f, _unit.MoveSpeed);
        _nextStepTime = Time.time + (1f / moveSpeed);
        return true;
    }

    public bool MoveTowards(Unit targetUnit, int desiredDistance)
    {
        return SetTarget(targetUnit, Mathf.Max(0, desiredDistance));
    }

    public bool MoveAway(Unit targetUnit, int desiredDistance)
    {
        if (_grid == null || _unit == null || targetUnit == null || !targetUnit.IsAlive)
            return false;

        if (Time.time < _nextStepTime)
            return false;

        Vector3Int originCell = GetCurrentCell();
        Vector3Int targetCell = _grid.WorldToCell(targetUnit.Position);
        int currentDistance = ManhattanDistance(originCell, targetCell);
        if (currentDistance >= desiredDistance)
            return false;

        Vector3Int nextStep = GetNextStepAway(originCell, targetCell, desiredDistance);
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
        InvalidatePathCache();
    }

    public void ClearPath()
    {
        InvalidatePathCache();
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

        if (_cachedPath.Count > 1 && _cachedPath[0] == originCell)
            return _cachedPath[1];

        int originDistance = ManhattanDistance(originCell, targetCell);
        Vector3Int bestImprovingStep = originCell;
        int bestImprovingDistance = originDistance;
        Vector3Int bestFallbackStep = originCell;
        int bestFallbackDistance = int.MaxValue;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell);
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

    private Vector3Int GetNextStepAway(Vector3Int originCell, Vector3Int targetCell, int desiredDistance)
    {
        if (_grid == null || _unit == null)
            return originCell;

        int currentDistance = ManhattanDistance(originCell, targetCell);
        Vector3Int bestCandidate = originCell;
        int bestDistanceGap = int.MaxValue;
        int bestCandidateDistance = currentDistance;
        Vector3Int fallbackCandidate = originCell;
        int fallbackDistance = currentDistance;

        List<Vector3Int> neighbors = _grid.GetNeighbors(originCell);
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int candidate = neighbors[i];
            if (!_grid.IsCellWalkable(candidate, _unit))
                continue;

            int candidateDistance = ManhattanDistance(candidate, targetCell);
            if (candidateDistance <= currentDistance)
                continue;

            int distanceGap = Mathf.Abs(desiredDistance - candidateDistance);
            if (distanceGap < bestDistanceGap ||
                (distanceGap == bestDistanceGap && candidateDistance > bestCandidateDistance))
            {
                bestCandidate = candidate;
                bestDistanceGap = distanceGap;
                bestCandidateDistance = candidateDistance;
            }

            if (candidateDistance > fallbackDistance)
            {
                fallbackCandidate = candidate;
                fallbackDistance = candidateDistance;
            }
        }

        if (bestCandidate != originCell)
            return bestCandidate;

        if (fallbackCandidate != originCell)
            return fallbackCandidate;

        return originCell;
    }

    private void RefreshPathCache(Vector3Int originCell, Vector3Int targetCell, Unit targetUnit, int rangeInCells)
    {
        bool targetChanged = _cachedTargetUnit != targetUnit;
        bool targetCellChanged = !_hasCachedTargetCell || _cachedTargetCell != targetCell;
        bool targetRangeChanged = _cachedTargetRange != rangeInCells;
        bool pathInvalid = !IsCachedPathStillValid(originCell);
        bool shouldRepath = targetChanged || targetCellChanged || targetRangeChanged || pathInvalid || Time.time >= _nextRepathTime;

        if (!shouldRepath)
            return;

        _cachedTargetUnit = targetUnit;
        _cachedTargetCell = targetCell;
        _hasCachedTargetCell = true;
        _cachedTargetRange = rangeInCells;
        _nextRepathTime = Time.time + _repathInterval;

        _cachedPath.Clear();
        _cachedPath.AddRange(FindPath(originCell, targetCell));
    }

    private bool IsCachedPathStillValid(Vector3Int originCell)
    {
        if (_cachedPath.Count <= 1)
            return false;

        if (_cachedPath[0] != originCell)
            return false;

        for (int i = 1; i < _cachedPath.Count; i++)
        {
            if (!_grid.IsCellWalkable(_cachedPath[i], _unit))
                return false;
        }

        return true;
    }

    private List<Vector3Int> FindPath(Vector3Int startCell, Vector3Int targetCell)
    {
        var path = new List<Vector3Int>();

        if (_grid == null || _unit == null)
            return path;

        if (startCell == targetCell)
        {
            path.Add(startCell);
            return path;
        }

        var openSet = new List<Vector3Int> { startCell };
        var closedSet = new HashSet<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int> { [startCell] = 0 };
        var fScore = new Dictionary<Vector3Int, int> { [startCell] = ManhattanDistance(startCell, targetCell) };

        while (openSet.Count > 0)
        {
            Vector3Int current = GetLowestScoreCell(openSet, fScore);
            if (current == targetCell)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            List<Vector3Int> neighbors = _grid.GetNeighbors(current);
            for (int i = 0; i < neighbors.Count; i++)
            {
                Vector3Int neighbor = neighbors[i];
                if (closedSet.Contains(neighbor))
                    continue;

                if (neighbor != targetCell && !_grid.IsCellWalkable(neighbor, _unit))
                    continue;

                int tentativeGScore = gScore[current] + 1;
                if (gScore.TryGetValue(neighbor, out int existingGScore) && tentativeGScore >= existingGScore)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = tentativeGScore + ManhattanDistance(neighbor, targetCell);

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
            }
        }

        return path;
    }

    private void InvalidatePathCache()
    {
        _cachedTargetUnit = null;
        _cachedTargetCell = Vector3Int.zero;
        _hasCachedTargetCell = false;
        _cachedTargetRange = 0;
        _nextRepathTime = 0f;
        _cachedPath.Clear();
    }

    private static Vector3Int GetLowestScoreCell(List<Vector3Int> openSet, Dictionary<Vector3Int, int> fScore)
    {
        Vector3Int bestCell = openSet[0];
        int bestScore = fScore.TryGetValue(bestCell, out int currentScore) ? currentScore : int.MaxValue;

        for (int i = 1; i < openSet.Count; i++)
        {
            Vector3Int candidate = openSet[i];
            int candidateScore = fScore.TryGetValue(candidate, out int score) ? score : int.MaxValue;
            if (candidateScore >= bestScore)
                continue;

            bestCell = candidate;
            bestScore = candidateScore;
        }

        return bestCell;
    }

    private static List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        var path = new List<Vector3Int> { current };

        while (cameFrom.TryGetValue(current, out Vector3Int previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private static int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
