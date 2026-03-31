using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LifeController))]
public class Unit : Creature
{
    [SerializeField] private UnitData _unitData;
    [SerializeField] private bool _initializeOnAwake = true;
    [SerializeField] private bool _drawDetectionGizmos = true;

    private readonly List<IUnit> _detectionCandidates = new();

    protected override void Awake()
    {
        base.Awake();

        if (!_initializeOnAwake || _unitData == null || _data != null)
            return;

        Initialize(_unitData);
    }

    public List<IUnit> GetVisibleUnitsInScene()
    {
        if (_data == null)
            return new List<IUnit>();

        RefreshDetectionCandidates();
        return GetVisibleUnits(_detectionCandidates);
    }

    public List<IUnit> GetVisiblecriaturaeUnitsInScene()
    {
        if (_data == null)
            return new List<IUnit>();

        RefreshDetectionCandidates();
        return GetVisiblecriaturaeUnits(_detectionCandidates);
    }

    public List<Unit> GetcriaturaeUnitsInScene()
    {
        if (_data == null)
            return new List<Unit>();

        RefreshDetectionCandidates();

        var criaturas = new List<Unit>();
        for (int i = 0; i < _detectionCandidates.Count; i++)
        {
            if (_detectionCandidates[i] is not Unit candidate)
                continue;

            if (!candidate.IsAlive || !IscriaturaeTo(candidate))
                continue;

            criaturas.Add(candidate);
        }

        return criaturas;
    }

    public Unit GetNearestVisiblecriaturaeUnitInScene()
    {
        if (_data == null)
            return null;

        RefreshDetectionCandidates();
        return GetNearestVisiblecriaturaeUnit(_detectionCandidates) as Unit;
    }

    public Unit GetNearestcriaturaeUnitInScene()
    {
        List<Unit> criaturas = GetcriaturaeUnitsInScene();
        Unit nearest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < criaturas.Count; i++)
        {
            float distance = Vector3.Distance(Position, criaturas[i].Position);
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            nearest = criaturas[i];
        }

        return nearest;
    }

    private void RefreshDetectionCandidates()
    {
        _detectionCandidates.Clear();

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null && !ReferenceEquals(units[i], this))
                _detectionCandidates.Add(units[i]);
        }
    }

    [ContextMenu("Snap To Grid")]
    public void SnapToGrid()
    {
        BattleGrid grid = BattleGrid.Instance != null
            ? BattleGrid.Instance
            : FindAnyObjectByType<BattleGrid>();

        if (grid == null)
            return;

        Vector3Int desiredCell = grid.WorldToCell(transform.position);
        Vector3Int targetCell = grid.IsCellWalkable(desiredCell, this)
            ? desiredCell
            : grid.FindClosestWalkableCell(desiredCell, this);

        transform.position = grid.CellToWorld(targetCell);
    }

    private void OnDrawGizmos()
    {
        if (!_drawDetectionGizmos || _unitData == null)
            return;

        RefreshDetectionCandidates();

        bool hasBlockedCandidateInRange = false;
        for (int i = 0; i < _detectionCandidates.Count; i++)
        {
            bool inRange = Vector3.Distance(transform.position, _detectionCandidates[i].Position) <= _unitData.stats.visionRange;
            bool blocked = _data != null && inRange && IsDetectionBlocked(_detectionCandidates[i]);

            if (blocked)
            {
                hasBlockedCandidateInRange = true;
                break;
            }
        }

        Color rangeFill = hasBlockedCandidateInRange
            ? new Color(1f, 0.4f, 0f, 0.15f)
            : new Color(1f, 1f, 0f, 0.15f);
        Color rangeWire = hasBlockedCandidateInRange ? new Color(1f, 0.4f, 0f) : Color.yellow;

        Gizmos.color = rangeFill;
        Gizmos.DrawSphere(transform.position, _unitData.stats.visionRange);
        Gizmos.color = rangeWire;
        Gizmos.DrawWireSphere(transform.position, _unitData.stats.visionRange);

        for (int i = 0; i < _detectionCandidates.Count; i++)
        {
            bool inRange = Vector3.Distance(transform.position, _detectionCandidates[i].Position) <= _unitData.stats.visionRange;
            bool blocked = _data != null && inRange && IsDetectionBlocked(_detectionCandidates[i]);
            bool detected = _data != null
                ? CanDetect(_detectionCandidates[i])
                : inRange;

            Gizmos.color = detected
                ? Color.green
                : blocked
                    ? new Color(1f, 0.4f, 0f)
                    : Color.red;
            Gizmos.DrawLine(transform.position, _detectionCandidates[i].Position);
        }
    }
}
