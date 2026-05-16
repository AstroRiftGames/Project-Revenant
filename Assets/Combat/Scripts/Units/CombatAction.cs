using UnityEngine;

public sealed class CombatAction : IBasicAction
{
    private readonly UnitCombat _combat;
    private readonly Unit _owner;
    private readonly bool _supportsAllies;

    public CombatAction(Unit owner, UnitCombat combat, bool supportsAllies)
    {
        _owner = owner;
        _combat = combat;
        _supportsAllies = supportsAllies;
    }

    public TargetRelation TargetRelation => _supportsAllies
        ? TargetRelation.Ally
        : TargetRelation.Hostile;
    public bool RequiresInjuredTarget => _supportsAllies;
    public int RangeInCells => _combat != null ? _combat.AttackRangeInCells : 0;
    public int PreferredDistanceInCells => _owner != null ? Mathf.Max(0, _owner.PreferredDistanceInCells) : RangeInCells;

    public bool IsValidTarget(Unit self, Unit target)
    {
        return _combat != null && _combat.CanPickBasicActionTarget(self, target, TargetRelation, RequiresInjuredTarget);
    }

    public bool IsInRange(Unit self, Unit target)
    {
        return _combat != null && _combat.IsBasicActionTargetInRange(target);
    }

    public bool CanExecute(Unit self, Unit target)
    {
        return _combat != null && _combat.CanUseBasicActionOn(self, target, TargetRelation, RequiresInjuredTarget);
    }

    public bool Execute(Unit self, Unit target)
    {
        if (!CanExecute(self, target))
            return false;

        return _combat.TryUseBasicActionOn(
            self,
            target,
            TargetRelation,
            RequiresInjuredTarget,
            candidate =>
            {
                if (_supportsAllies)
                    candidate.Heal(self.AttackDamage, self);
                else
                    candidate.TakeDamage(self.AttackDamage, self);
            });
    }
}
