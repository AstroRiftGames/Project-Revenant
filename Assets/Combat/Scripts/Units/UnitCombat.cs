using UnityEngine;

public abstract class BasicUnitAction : MonoBehaviour, IBasicAction
{
    protected UnitCombat Combat { get; private set; }
    protected Unit Owner { get; private set; }

    public abstract TargetRelation TargetRelation { get; }
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
        return Combat != null && Combat.IsBasicActionTargetInRange(target);
    }

    public bool IsValidTarget(Unit self, Unit target)
    {
        return Combat != null && Combat.CanPickBasicActionTarget(self, target, TargetRelation, RequiresInjuredTarget);
    }

    public bool CanExecute(Unit self, Unit target)
    {
        return Combat != null && Combat.CanUseBasicActionOn(self, target, TargetRelation, RequiresInjuredTarget);
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
    public override TargetRelation TargetRelation => TargetRelation.Hostile;

    protected override bool ExecuteValidated(Unit self, Unit target)
    {
        return Combat != null && Combat.TryUseAttack(self, target, TargetRelation);
    }
}

[RequireComponent(typeof(UnitCombat))]
public class HealAction : BasicUnitAction
{
    public override TargetRelation TargetRelation => TargetRelation.Ally;
    protected override bool RequiresInjuredTargetForAction => true;

    protected override bool ExecuteValidated(Unit self, Unit target)
    {
        return Combat != null && Combat.TryUseHeal(self, target, TargetRelation);
    }
}

[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(UnitMovement))]
public class UnitCombat : MonoBehaviour
{
    [SerializeField] private CombatProjectileVisual _projectileVisualPrefab;
    [SerializeField] private CombatProjectileVisual _supportProjectileVisualPrefab;

    private Unit _unit;
    private SkillCaster _skillCaster;
    private float _nextAttackTime;

    public int AttackRangeInCells => _unit != null ? Mathf.Max(0, _unit.AttackRangeInCells) : 0;

    private void Awake()
    {
        _unit = GetComponent<Unit>();
        _skillCaster = GetComponent<SkillCaster>();
    }

    public bool IsBasicActionTargetInRange(Unit target)
    {
        if (!CanOwnerParticipateInCombat() ||
            target == null ||
            !target.gameObject.activeInHierarchy ||
            !target.IsAlive ||
            target.LifecycleState != UnitLifecycleState.Alive)
        {
            return false;
        }

        return UnitTargetValidator.IsTargetInRange(_unit, target, AttackRangeInCells);
    }

    public bool CanUseBasicActionOn(Unit self, Unit target, TargetRelation targetRelation, bool needsInjuredTarget)
    {
        if (!CanPickBasicActionTarget(self, target, targetRelation, needsInjuredTarget))
            return false;

        if (!CanOwnerUseBasicAction())
            return false;

        if (!IsBasicActionReady())
            return false;

        return IsBasicActionTargetInRange(target);
    }

    public bool CanPickBasicActionTarget(Unit self, Unit target, TargetRelation targetRelation, bool needsInjuredTarget)
    {
        if (self == null || _unit == null)
            return false;

        return UnitTargetValidator.IsBasicActionTargetSelectable(self, target, targetRelation, needsInjuredTarget);
    }

    public bool TryUseAttack(Unit self, Unit target, TargetRelation targetRelation)
    {
        return TryUseBasicActionOn(
            self,
            target,
            targetRelation,
            needsInjuredTarget: false,
            candidate => candidate.TakeDamage(self.AttackDamage, self));
    }

    public bool TryUseHeal(Unit self, Unit target, TargetRelation targetRelation)
    {
        return TryUseBasicActionOn(
            self,
            target,
            targetRelation,
            needsInjuredTarget: true,
            candidate => candidate.Heal(self.AttackDamage, self));
    }

    public bool TryUseBasicActionOn(
        Unit self,
        Unit target,
        TargetRelation targetRelation,
        bool needsInjuredTarget,
        System.Action<Unit> effect)
    {
        if (self == null || effect == null)
            return false;

        if (!CanUseBasicActionOn(self, target, targetRelation, needsInjuredTarget))
            return false;

        int targetHealthBefore = target != null ? target.CurrentHealth : 0;
        ApplyBasicActionToTarget(target, effect);
        bool appliedEffect = DidBasicActionApplyEffect(target, targetHealthBefore);
        NotifySuccessfulBasicAction(targetRelation, appliedEffect);
        ConsumeBasicActionCooldown();
        ShowBasicActionPresentation(target);
        return true;
    }

    private bool CanOwnerUseBasicAction()
    {
        return CanOwnerParticipateInCombat() &&
               (_unit.StatusEffects == null || _unit.StatusEffects.CanAttack);
    }

    private bool CanOwnerParticipateInCombat()
    {
        return _unit != null &&
               _unit.IsAlive &&
               _unit.LifecycleState == UnitLifecycleState.Alive;
    }

    private bool IsBasicActionReady()
    {
        return Time.time >= _nextAttackTime;
    }

    private static void ApplyBasicActionToTarget(Unit target, System.Action<Unit> effect)
    {
        effect(target);
    }

    private static bool DidBasicActionApplyEffect(Unit target, int targetHealthBefore)
    {
        if (target == null)
            return false;

        return target.CurrentHealth != targetHealthBefore;
    }

    private void NotifySuccessfulBasicAction(TargetRelation targetRelation, bool appliedEffect)
    {
        if (_skillCaster == null || !appliedEffect)
            return;

        _skillCaster.GrantChargeFromBasicAction(targetRelation);
    }

    private void ConsumeBasicActionCooldown()
    {
        if (_unit == null)
            return;

        _nextAttackTime = Time.time + Mathf.Max(0f, _unit.AttackCooldown);
    }

    private void ShowBasicActionPresentation(Unit target)
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
