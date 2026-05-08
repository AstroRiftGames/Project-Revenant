using UnityEngine;

public abstract class BasicUnitAction : MonoBehaviour, IBasicAction
{
    protected UnitCombat Combat { get; private set; }
    protected Unit Owner { get; private set; }

    public abstract RequiredTargetRelationship RequiredTargetRelationship { get; }
    public bool RequiresInjuredTarget => RequiresInjuredTargetForAction;
    public int RangeInCells => Combat != null ? Combat.AttackRangeInCells : 0;
    public int PreferredDistanceInCells => Owner != null ? Mathf.Max(0, Owner.PreferredDistanceInCells) : RangeInCells;

    protected virtual bool RequiresInjuredTargetForAction => false;

    protected virtual void Awake()
    {
        Combat = GetComponent<UnitCombat>();
        Owner = GetComponent<Unit>();
    }

    public bool IsInRange(Unit self, Unit target)
    {
        return Combat != null && Combat.IsTargetInBasicActionRange(target);
    }

    public bool IsValidTarget(Unit self, Unit target)
    {
        return Combat != null && Combat.IsValidBasicActionTarget(self, target, TargetingPolicy.ForBasicAction(this));
    }

    public bool CanExecute(Unit self, Unit target)
    {
        return Combat != null && Combat.CanExecuteBasicAction(self, target, TargetingPolicy.ForBasicAction(this));
    }

    public bool Execute(Unit self, Unit target)
    {
        if (!CanExecute(self, target))
            return false;

        return ExecuteValidated(self, target);
    }

    protected abstract bool ExecuteValidated(Unit self, Unit target);
}

[RequireComponent(typeof(UnitCombat))]
public class AttackAction : BasicUnitAction
{
    public override RequiredTargetRelationship RequiredTargetRelationship => RequiredTargetRelationship.Hostile;

    protected override bool ExecuteValidated(Unit self, Unit target)
    {
        return Combat != null && Combat.TryExecuteAttackAction(self, target, RequiredTargetRelationship);
    }
}

[RequireComponent(typeof(UnitCombat))]
public class HealAction : BasicUnitAction
{
    public override RequiredTargetRelationship RequiredTargetRelationship => RequiredTargetRelationship.Ally;
    protected override bool RequiresInjuredTargetForAction => true;

    protected override bool ExecuteValidated(Unit self, Unit target)
    {
        return Combat != null && Combat.TryExecuteHealAction(self, target, RequiredTargetRelationship);
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

    public bool IsTargetInBasicActionRange(Unit target)
    {
        if (!UnitTargetValidator.IsTargetSelectable(_unit, target, TargetingPolicy.ForRelationship(RequiredTargetRelationship.Any)))
            return false;

        return UnitTargetValidator.IsTargetInRange(_unit, target, AttackRangeInCells);
    }

    public bool CanExecuteBasicAction(Unit self, Unit target, in TargetingPolicy policy)
    {
        if (!IsValidBasicActionTarget(self, target, policy))
            return false;

        if (!CanOwnerUseBasicAction())
            return false;

        if (!IsBasicActionOffCooldown())
            return false;

        return IsTargetInBasicActionRange(target);
    }

    public bool IsValidBasicActionTarget(Unit self, Unit target, in TargetingPolicy policy)
    {
        if (self == null || _unit == null)
            return false;

        if (!UnitTargetValidator.IsTargetSelectable(self, target, policy))
            return false;

        return true;
    }

    public bool TryExecuteAttackAction(Unit self, Unit target, RequiredTargetRelationship relationship)
    {
        return TryExecuteBasicAction(
            self,
            target,
            TargetingPolicy.ForBasicAction(relationship),
            candidate => candidate.TakeDamage(self.AttackDamage, self));
    }

    public bool TryExecuteHealAction(Unit self, Unit target, RequiredTargetRelationship relationship)
    {
        return TryExecuteBasicAction(
            self,
            target,
            TargetingPolicy.ForBasicAction(relationship, requiresInjuredTarget: true),
            candidate => candidate.Heal(self.AttackDamage, self));
    }

    public bool TryExecuteBasicAction(Unit self, Unit target, in TargetingPolicy policy, System.Action<Unit> effect)
    {
        if (self == null || effect == null)
            return false;

        if (!CanExecuteBasicAction(self, target, policy))
            return false;

        ApplyBasicActionEffect(target, effect);
        ConsumeBasicActionCooldown();
        TriggerBasicActionPresentation(target);
        return true;
    }

    private bool CanOwnerUseBasicAction()
    {
        return _unit == null || _unit.StatusEffects == null || _unit.StatusEffects.CanAttack;
    }

    private bool IsBasicActionOffCooldown()
    {
        return Time.time >= _nextAttackTime;
    }

    private static void ApplyBasicActionEffect(Unit target, System.Action<Unit> effect)
    {
        effect(target);
    }

    private void ConsumeBasicActionCooldown()
    {
        if (_unit == null)
            return;

        _nextAttackTime = Time.time + Mathf.Max(0f, _unit.AttackCooldown);
    }

    private void TriggerBasicActionPresentation(Unit target)
    {
        if (_unit == null || target == null)
            return;

        if (_unit.AttackPresentation == UnitAttackKind.Melee)
            return;

        CombatProjectileVisual projectilePrefab = ResolveBasicActionProjectilePrefab();
        if (projectilePrefab == null)
            return;

        Transform projectileParent = _unit.RoomContext != null ? _unit.RoomContext.transform : null;
        CombatProjectileVisual projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity, projectileParent);
        projectile.Launch(transform.position, target.transform, target.Position);
    }

    private CombatProjectileVisual ResolveBasicActionProjectilePrefab()
    {
        if (_unit.AttackPresentation == UnitAttackKind.SupportProjectile)
            return _supportProjectileVisualPrefab;

        return _projectileVisualPrefab;
    }
}
