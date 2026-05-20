using System;
using UnityEngine;

public enum SkillTargetRequirement
{
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
    [SerializeField] private SkillTargetRequirement _targetRequirement = SkillTargetRequirement.Hostile;
    public bool mustTargetInjured;

    public SkillTargetRequirement TargetRequirement => _targetRequirement;
    public bool RequiresInjuredTarget => mustTargetInjured;

    // Use this only for rules that belong to one specific skill.
    // Generic checks like self/friend/enemy/alive/injured are handled by the
    // skill targeting flow itself.
    public bool AreSkillSpecificRequirementsMet(Unit caster, Unit target)
    {
        return caster != null;
    }
}
