using System;

[Serializable]
public class SkillRequirements
{
    public bool requiresTarget = true;
    public bool mustTargetHostile = true;
    public bool mustTargetInjured;

    public bool AreMet(Unit caster, Unit target)
    {
        if (caster == null)
            return false;

        if (requiresTarget && target == null)
            return false;

        if (target == null)
            return !requiresTarget;

        if (!target.gameObject.activeInHierarchy || !target.IsAlive)
            return false;

        if (mustTargetHostile && !caster.IsHostileTo(target))
            return false;

        if (mustTargetInjured && target.CurrentHealth >= target.MaxHealth)
            return false;

        return true;
    }
}
