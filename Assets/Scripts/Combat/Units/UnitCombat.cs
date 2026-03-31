using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private bool _debugCombat;

    private Unit _unit;
    private UnitMovement _unitMovement;
    private float _attackTimer;

    public int AttackRangeInCells => _unit != null ? _unit.AttackRangeInCells : 0;

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

        return _unitMovement.IsWithinRange(target, AttackRangeInCells);
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

        _attackTimer = _unit.AttackInterval;
        target.TakeDamage(_unit.AttackDamage);

        if (_debugCombat)
            Debug.Log($"[Attack] {_unit.Id} hit {target.Id} for {_unit.AttackDamage}.", this);

        return true;
    }
}
