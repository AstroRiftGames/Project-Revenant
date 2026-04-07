using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(UnitCombat))]
[RequireComponent(typeof(TargetingStrategy))]
public class UnitBrain : MonoBehaviour
{
    private Unit _unit;
    private UnitMovement _movement;
    private UnitCombat _combat;
    private TargetingStrategy _targeting;
    private Unit _currentTarget;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _movement = GetComponent<UnitMovement>();
        _combat = GetComponent<UnitCombat>();
        _targeting = GetComponent<TargetingStrategy>();
    }

    private void Update()
    {
        if (_unit == null || _movement == null || _combat == null || _targeting == null || !_unit.IsAlive)
            return;

        _currentTarget = _targeting.SelectTarget(_unit, _currentTarget);
        if (_currentTarget == null)
            return;

        if (_combat.IsTargetInRange(_currentTarget))
        {
            _combat.TryAttack(_currentTarget);
            return;
        }

        _movement.SetTarget(_currentTarget, _combat.AttackRangeInCells);
    }
}
