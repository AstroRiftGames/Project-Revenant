using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private bool _debugCombat;
    [SerializeField] [Range(0f, 1f)] private float _minHitChance = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float _maxHitChance = 0.95f;

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

        float hitChance = GetHitChance(target);
        float roll = Random.value;

        _attackTimer = _unit.AttackInterval;

        if (roll > hitChance)
        {
            if (_debugCombat)
                Debug.Log($"[Attack] {_unit.Id} missed {target.Id}. Roll: {roll:0.00}, HitChance: {hitChance:0.00}", this);

            return false;
        }

        target.TakeDamage(_unit.AttackDamage);

        if (_debugCombat)
            Debug.Log($"[Attack] {_unit.Id} hit {target.Id} for {_unit.AttackDamage}. Roll: {roll:0.00}, HitChance: {hitChance:0.00}", this);

        return true;
    }

    private float GetHitChance(Unit target)
    {
        if (_unit == null || target == null)
            return 0f;

        float rawHitChance = _unit.Accuracy - target.Evasion;
        return Mathf.Clamp(rawHitChance, _minHitChance, _maxHitChance);
    }
}
