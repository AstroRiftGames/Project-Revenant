using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private int _attackRangeInCells = 1;
    [SerializeField] private float _attackInterval = 0.75f;
    [SerializeField] private int _attackDamage = 1;

    private Unit _unit;
    private UnitMovement _unitMovement;
    private float _attackTimer;

    public int AttackRangeInCells => _attackRangeInCells;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _unitMovement = GetComponent<UnitMovement>();
    }

    private void Update()
    {
        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;
    }

    public bool IsTargetInRange(Unit target)
    {
        if (_unitMovement == null || target == null)
            return false;

        return _unitMovement.IsWithinRange(target, _attackRangeInCells);
    }

    public bool TryAttack(Unit target)
    {
        if (_unit == null || target == null)
            return false;

        if (!_unit.IsAlive || !target.IsAlive)
            return false;

        if (!_unit.IsHostileTo(target))
            return false;

        if (!IsTargetInRange(target))
            return false;

        if (_attackTimer > 0f)
            return false;

        _attackTimer = _attackInterval;
        target.TakeDamage(_attackDamage);
        return true;
    }
}
