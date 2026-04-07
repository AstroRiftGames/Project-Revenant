using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
[RequireComponent(typeof(TargetingStrategy))]
public class UnitBrain : MonoBehaviour
{
    private Unit _unit;
    private UnitMovement _movement;
    private TargetingStrategy _targeting;
    private IAction _action;
    private Unit _currentTarget;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _movement = GetComponent<UnitMovement>();
        _targeting = GetComponent<TargetingStrategy>();
        _action = _unit != null ? _unit.Action : null;
    }

    private void Update()
    {
        if (_unit == null || _movement == null || _targeting == null || _action == null || !_unit.IsAlive)
            return;

        _currentTarget = _targeting.SelectTarget(_unit, _currentTarget);
        if (_currentTarget == null)
            return;

        int preferredDistance = Mathf.Max(0, Mathf.Min(_action.PreferredDistanceInCells, _action.RangeInCells));

        if (!_action.IsInRange(_unit, _currentTarget))
        {
            _movement.MoveTowards(_currentTarget, preferredDistance);
            return;
        }

        if (preferredDistance > 0 && _movement.IsWithinRange(_currentTarget, preferredDistance - 1))
        {
            _movement.MoveAway(_currentTarget, preferredDistance);
            return;
        }

        if (_action.CanExecute(_unit, _currentTarget))
            _action.Execute(_unit, _currentTarget);
    }
}
