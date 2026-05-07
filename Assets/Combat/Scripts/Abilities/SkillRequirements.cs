using System;
using UnityEngine;
using UnityEngine.Serialization;

public enum SkillTargetRequirement
{
    Legacy = 0,
    Hostile = 1,
    Ally = 2,
    Self = 3,
    Any = 4,
    NoTarget = 5,
    GroundCell = 6
}

[Serializable]
public class SkillRequirements
{
    public bool requiresTarget = true;
    [SerializeField] private SkillTargetRequirement _targetRequirement = SkillTargetRequirement.Legacy;
    [FormerlySerializedAs("mustTargetHostile")]
    [SerializeField, HideInInspector] private bool _legacyMustTargetHostile = true;
    public bool mustTargetInjured;

    public SkillTargetRequirement TargetRequirement => ResolveTargetRequirement();

    public SkillTargetRequirement ResolveTargetRequirement()
    {
        if (_targetRequirement != SkillTargetRequirement.Legacy)
            return _targetRequirement;

        return _legacyMustTargetHostile
            ? SkillTargetRequirement.Hostile
            : SkillTargetRequirement.Ally;
    }

    public bool AreMet(Unit caster, Unit target)
    {
        if (caster == null)
            return false;

        SkillTargetRequirement targetRequirement = ResolveTargetRequirement();
        if (targetRequirement == SkillTargetRequirement.GroundCell)
            return false;

        if (requiresTarget && target == null)
            return false;

        if (target == null)
            return !requiresTarget || targetRequirement == SkillTargetRequirement.NoTarget;

        switch (targetRequirement)
        {
            case SkillTargetRequirement.Hostile:
                if (!caster.IsHostileTo(target))
                    return false;
                break;
            case SkillTargetRequirement.Ally:
                if (caster.IsHostileTo(target))
                    return false;
                break;
            case SkillTargetRequirement.Self:
                if (!ReferenceEquals(caster, target))
                    return false;
                break;
            case SkillTargetRequirement.NoTarget:
                return false;
            case SkillTargetRequirement.GroundCell:
                return false;
        }

        if (mustTargetInjured && target.CurrentHealth >= target.MaxHealth)
            return false;

        return true;
    }
}
