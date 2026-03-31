using UnityEngine;
using System.Collections.Generic;

public class Unit : Creature
{
    [SerializeField] private UnitData _unitData;
    [SerializeField] private bool _initializeOnAwake = true;
    [SerializeField] private bool _drawDetectionGizmos = true;

    private readonly List<IUnit> _detectionCandidates = new();

    private void Awake()
    {
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

    private void OnDrawGizmos()
    {
        if (!_drawDetectionGizmos || _unitData == null)
            return;

        RefreshDetectionCandidates();

        bool hasBlockedCandidateInRange = false;
        for (int i = 0; i < _detectionCandidates.Count; i++)
        {
            bool inRange = Vector3.Distance(transform.position, _detectionCandidates[i].Position) <= _unitData.visionRange;
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
        Gizmos.DrawSphere(transform.position, _unitData.visionRange);
        Gizmos.color = rangeWire;
        Gizmos.DrawWireSphere(transform.position, _unitData.visionRange);

        for (int i = 0; i < _detectionCandidates.Count; i++)
        {
            bool inRange = Vector3.Distance(transform.position, _detectionCandidates[i].Position) <= _unitData.visionRange;
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
