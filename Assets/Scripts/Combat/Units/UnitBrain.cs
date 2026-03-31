using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitBrain : MonoBehaviour
{
    [SerializeField] private int _engageRangeInCells = 1;
    [SerializeField] private float _retargetInterval = 0.25f;
    [SerializeField] private bool _runOnStart = true;

    private Unit _unit;
    private UnitMovement _unitMovement;
    private Unit _currentTarget;
    private float _retargetTimer;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _unitMovement = GetComponent<UnitMovement>();
    }

    private void Update()
    {
        if (!_runOnStart || _unit == null || _unitMovement == null)
            return;

        _retargetTimer -= Time.deltaTime;
        if (_retargetTimer > 0f)
            return;

        _retargetTimer = _retargetInterval;
        UpdateTarget();
        UpdateMovement();
    }

    private void UpdateTarget()
    {
        if (_currentTarget != null && _unit.IsHostileTo(_currentTarget))
            return;

        _currentTarget = _unit.GetNearestVisibleHostileUnitInScene();
    }

    private void UpdateMovement()
    {
        if (_currentTarget == null)
        {
            _unitMovement.ClearDestination();
            return;
        }

        _unitMovement.SetTarget(_currentTarget, _engageRangeInCells);
    }

    private void OnDrawGizmosSelected()
    {
        if (_currentTarget == null)
            return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, _currentTarget.Position);
    }
}
