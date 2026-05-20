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

    // Use this only for rules that belong to one specific skill.
    // Generic checks like self/friend/enemy/alive/injured are handled by the
    // skill targeting flow itself.
    public bool AreSkillSpecificRequirementsMet(Unit caster, Unit target)
    {
        return caster != null;
    }
}
