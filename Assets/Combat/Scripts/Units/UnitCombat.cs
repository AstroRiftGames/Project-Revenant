using UnityEngine;

public abstract class UnitAction : MonoBehaviour, IAction
{
    public abstract RequiredTargetRelationship PreferredTargetRelationship { get; }
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

    public override RequiredTargetRelationship PreferredTargetRelationship => RequiredTargetRelationship.Hostile;
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
        if (!UnitTargetValidator.IsTargetSelectableForBasicAction(self, target, PreferredTargetRelationship))
            return false;

        return _combat != null && _combat.CanUseOn(target, PreferredTargetRelationship);
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

    public override RequiredTargetRelationship PreferredTargetRelationship => RequiredTargetRelationship.Ally;
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
        if (_combat == null)
            return false;

        if (!UnitTargetValidator.IsTargetSelectableForBasicAction(self, target, PreferredTargetRelationship))
            return false;

        if (target.CurrentHealth >= target.MaxHealth)
            return false;

        return _combat.CanUseOn(target, PreferredTargetRelationship);
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
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private CombatProjectileVisual _projectileVisualPrefab;
    [SerializeField] private CombatProjectileVisual _supportProjectileVisualPrefab;

    private Unit _unit;
    private float _nextAttackTime;

    public int AttackRangeInCells => _unit != null ? Mathf.Max(0, _unit.AttackRangeInCells) : 0;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
    }

    public bool IsTargetInRange(Unit target)
    {
        if (!UnitTargetValidator.IsTargetSelectable(_unit, target, RequiredTargetRelationship.Any, allowInvisible: false))
            return false;

        return UnitTargetValidator.IsTargetInRange(_unit, target, AttackRangeInCells);
    }

    public bool CanUseOn(Unit target)
    {
        return CanUseOn(target, RequiredTargetRelationship.Any);
    }

    public bool CanUseOn(Unit target, RequiredTargetRelationship relationship)
    {
        if (!UnitTargetValidator.IsTargetSelectableForBasicAction(_unit, target, relationship))
            return false;

        if (_unit.StatusEffects != null && !_unit.StatusEffects.CanAttack)
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
        _nextAttackTime = Time.time + Mathf.Max(0f, _unit.AttackCooldown);
        return true;
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

        Transform projectileParent = _unit.RoomContext != null ? _unit.RoomContext.transform : null;
        CombatProjectileVisual projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity, projectileParent);
        projectile.Launch(transform.position, target.transform, target.Position);
    }

    private CombatProjectileVisual ResolveProjectileVisualPrefab()
    {
        if (_unit.AttackPresentation == UnitAttackKind.SupportProjectile)
            return _supportProjectileVisualPrefab;

        return _projectileVisualPrefab;
    }
}
