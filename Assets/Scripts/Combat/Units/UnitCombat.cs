using UnityEngine;

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitCombat : MonoBehaviour
{
    private Unit _unit;
    private float _nextAttackTime;

    public int AttackRangeInCells => _unit != null ? Mathf.Max(0, _unit.AttackRangeInCells) : 0;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    public bool IsTargetInRange(Unit target)
    {
        if (_unit == null || target == null || !target.IsAlive)
            return false;

        BattleGrid grid = _unit.RoomContext != null ? _unit.RoomContext.BattleGrid : null;
        if (grid == null)
        {
            float distance = Vector3.Distance(_unit.Position, target.Position);
            return distance <= Mathf.Max(0f, AttackRangeInCells);
        }

        Vector3Int selfCell = grid.WorldToCell(_unit.Position);
        Vector3Int targetCell = grid.WorldToCell(target.Position);
        int distanceInCells = Mathf.Abs(selfCell.x - targetCell.x) + Mathf.Abs(selfCell.y - targetCell.y);
        return distanceInCells <= AttackRangeInCells;
    }

    public bool TryAttack(Unit target)
    {
        if (_unit == null || target == null || !target.IsAlive)
            return false;

        if (Time.time < _nextAttackTime)
            return false;

        if (!IsTargetInRange(target))
            return false;

        target.TakeDamage(_unit.AttackDamage, _unit);
        _nextAttackTime = Time.time + Mathf.Max(0f, _unit.AttackInterval);
        return true;
    }
}
