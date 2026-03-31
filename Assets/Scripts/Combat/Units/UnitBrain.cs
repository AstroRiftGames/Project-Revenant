using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitCombat))]
public class UnitBrain : MonoBehaviour
{
    [SerializeField] private float _retargetInterval = 0.25f;
    [SerializeField] private bool _runOnStart = true;

    private Unit _unit;
    private UnitMovement _unitMovement;
    private UnitCombat _unitCombat;
    private Unit _currentTarget;
    private float _retargetTimer;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _unitMovement = GetComponent<UnitMovement>();
        _unitCombat = GetComponent<UnitCombat>();
    }

    private void Update()
    {
        if (!_runOnStart || _unit == null || _unitMovement == null || _unitCombat == null)
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
        if (_currentTarget != null && _currentTarget.IsAlive && _unit.IsHostileTo(_currentTarget))
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

        if (_unitCombat.IsTargetInRange(_currentTarget))
        {
            _unitMovement.ClearPath();
            _unitCombat.TryAttack(_currentTarget);
            return;
        }

        _unitMovement.SetTarget(_currentTarget, _unitCombat.AttackRangeInCells);
    }

    private void OnDrawGizmosSelected()
    {
        if (_currentTarget == null)
            return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawLine(transform.position, _currentTarget.Position);
    }
}
