using UnityEngine;

public sealed class CombatAction : IAction
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

    public int RangeInCells => _combat != null ? _combat.AttackRangeInCells : 0;
    public int PreferredDistanceInCells => _owner != null ? Mathf.Max(0, _owner.PreferredDistanceInCells) : RangeInCells;

    public bool IsInRange(Unit self, Unit target)
    {
        return _combat != null && _combat.IsTargetInRange(target);
    }

    public bool CanExecute(Unit self, Unit target)
    {
        if (self == null || target == null || _combat == null)
            return false;

        if (_supportsAllies)
        {
            if (self.IsHostileTo(target))
                return false;

            if (target.CurrentHealth >= target.MaxHealth)
                return false;

            return _combat.CanUseOn(target);
        }

        return self.IsHostileTo(target) && _combat.CanUseOn(target);
    }

    public bool Execute(Unit self, Unit target)
    {
        if (!CanExecute(self, target))
            return false;

        if (_supportsAllies)
            return _combat.TryExecute(target, candidate => candidate.Heal(self.AttackDamage, self));

        return _combat.TryAttack(target);
    }
}
