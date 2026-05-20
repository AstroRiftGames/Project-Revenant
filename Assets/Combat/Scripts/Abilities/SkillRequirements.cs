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
    #region Serialized Contract

    // Compatibility note:
    // SkillData now exposes the primary target contract semantically, but the
    // serialized target requirement still lives here to preserve existing
    // assets and avoid widening this refactor.
    public bool requiresTarget = true;
    [SerializeField] private SkillTargetRequirement _targetRequirement = SkillTargetRequirement.Legacy;
    [FormerlySerializedAs("mustTargetHostile")]
    [SerializeField, HideInInspector] private bool _legacyMustTargetHostile = true;
    // Additional condition layered on top of the target type/relationship.
    public bool mustTargetInjured;

    #endregion

    #region Semantic Properties

    public SkillTargetRequirement TargetRequirement => ResolveTargetRequirement();
    public bool RequiresTarget => requiresTarget;
    public bool RequiresInjuredTarget => mustTargetInjured;

    #endregion

    #region Resolution

    public SkillTargetRequirement ResolveTargetRequirement()
    {
        if (_targetRequirement != SkillTargetRequirement.Legacy)
            return _targetRequirement;

        return _legacyMustTargetHostile
            ? SkillTargetRequirement.Hostile
            : SkillTargetRequirement.Ally;
    }

    #endregion

    #region Legacy Combined Validation

    // Legacy combined validation kept for compatibility with older callers.
    // The semantic split is:
    // - TargetRequirement: primary target relation/type
    // - SkillRequirements: additional target conditions
    // - TargetingPolicy / UnitTargetValidator: runtime validation
    public bool AreMet(Unit caster, Unit target)
    {
        if (caster == null)
            return false;

        SkillTargetRequirement targetRequirement = TargetRequirement;
        if (targetRequirement == SkillTargetRequirement.GroundCell)
            return false;

        if (RequiresTarget && target == null)
            return false;

        if (target == null)
            return !RequiresTarget || targetRequirement == SkillTargetRequirement.NoTarget;

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

        if (RequiresInjuredTarget && target.CurrentHealth >= target.MaxHealth)
            return false;

        return AreSkillSpecificRequirementsMet(caster, target);
    }

    // Use this only for rules that belong to one specific skill.
    // Generic checks like self/friend/enemy/alive/injured are handled by the
    // skill targeting flow itself.
    public bool AreSkillSpecificRequirementsMet(Unit caster, Unit target)
    {
        return caster != null;
    }

    #endregion
}
