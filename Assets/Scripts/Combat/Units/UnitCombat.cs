using UnityEngine;

public abstract class UnitAction : MonoBehaviour, IAction
{
    public abstract int RangeInCells { get; }
    public abstract int PreferredDistanceInCells { get; }
    public abstract bool IsInRange(Unit self, Unit target);
    public abstract bool CanExecute(Unit self, Unit target);
    public abstract bool Execute(Unit self, Unit target);
}

[RequireComponent(typeof(UnitCombat))]
public class AttackAction : UnitAction
{
    private UnitCombat _combat;
    private Unit _unit;

    public override int RangeInCells => _combat != null ? _combat.AttackRangeInCells : 0;
    public override int PreferredDistanceInCells => _unit != null ? Mathf.Max(0, _unit.PreferredDistanceInCells) : RangeInCells;

    private void Awake()
    {
        _combat = GetComponent<UnitCombat>();
        _unit = GetComponent<Unit>();
    }

    public override bool IsInRange(Unit self, Unit target)
    {
        return _combat != null && _combat.IsTargetInRange(target);
    }

    public override bool CanExecute(Unit self, Unit target)
    {
        if (self == null || target == null)
            return false;

        return self.IsHostileTo(target) && _combat != null && _combat.CanUseOn(target);
    }

    public override bool Execute(Unit self, Unit target)
    {
        if (!CanExecute(self, target))
            return false;

        return _combat.TryAttack(target);
    }
}

[RequireComponent(typeof(UnitCombat))]
public class HealAction : UnitAction
{
    private UnitCombat _combat;
    private Unit _unit;

    public override int RangeInCells => _combat != null ? _combat.AttackRangeInCells : 0;
    public override int PreferredDistanceInCells => _unit != null ? Mathf.Max(0, _unit.PreferredDistanceInCells) : RangeInCells;

    private void Awake()
    {
        _combat = GetComponent<UnitCombat>();
        _unit = GetComponent<Unit>();
    }

    public override bool IsInRange(Unit self, Unit target)
    {
        return _combat != null && _combat.IsTargetInRange(target);
    }

    public override bool CanExecute(Unit self, Unit target)
    {
        if (self == null || target == null || _combat == null)
            return false;

        if (self.IsHostileTo(target))
            return false;

        if (target.CurrentHealth >= target.MaxHealth)
            return false;

        return _combat.CanUseOn(target);
    }

    public override bool Execute(Unit self, Unit target)
    {
        if (!CanExecute(self, target))
            return false;

        return _combat.TryExecute(target, candidate => candidate.Heal(self.AttackDamage, self));
    }
}

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitCombat : MonoBehaviour, IAction
{
    [SerializeField] private CombatProjectileVisual _projectileVisualPrefab;
    [SerializeField] private CombatProjectileVisual _supportProjectileVisualPrefab;

    private Unit _unit;
    private float _nextAttackTime;

    public int AttackRangeInCells => _unit != null ? Mathf.Max(0, _unit.AttackRangeInCells) : 0;
    public int RangeInCells => AttackRangeInCells;
    public int PreferredDistanceInCells => _unit != null ? Mathf.Max(0, _unit.PreferredDistanceInCells) : RangeInCells;

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

    public bool IsInRange(Unit self, Unit target)
    {
        return IsTargetInRange(target);
    }

    public bool CanUseOn(Unit target)
    {
        if (_unit == null || target == null || !target.IsAlive)
            return false;

        if (Time.time < _nextAttackTime)
            return false;

        return IsTargetInRange(target);
    }

    public bool TryExecute(Unit target, System.Action<Unit> effect)
    {
        if (effect == null || !CanUseOn(target))
            return false;

        effect(target);
        PlayAttackVisual(target);
        _nextAttackTime = Time.time + Mathf.Max(0f, _unit.AttackInterval);
        return true;
    }

    public bool CanExecute(Unit self, Unit target)
    {
        if (self == null || target == null)
            return false;

        return self.IsHostileTo(target) && CanUseOn(target);
    }

    public bool Execute(Unit self, Unit target)
    {
        return TryAttack(target);
    }

    public bool TryAttack(Unit target)
    {
        return TryExecute(target, candidate => candidate.TakeDamage(_unit.AttackDamage, _unit));
    }

    private void PlayAttackVisual(Unit target)
    {
        if (_unit == null || target == null)
            return;

        if (_unit.AttackPresentation == UnitAttackKind.Melee)
            return;

        CombatProjectileVisual projectilePrefab = ResolveProjectileVisualPrefab();
        if (projectilePrefab == null)
            return;

        CombatProjectileVisual projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.Launch(transform.position, target.transform, target.Position);
    }

    private CombatProjectileVisual ResolveProjectileVisualPrefab()
    {
        if (_unit.AttackPresentation == UnitAttackKind.SupportProjectile)
            return _supportProjectileVisualPrefab;

        return _projectileVisualPrefab;
    }
}
