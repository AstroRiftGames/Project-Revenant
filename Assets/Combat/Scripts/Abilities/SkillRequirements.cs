using System;
using UnityEngine;

[Serializable]
public class SkillRequirements
{
    public bool mustTargetInjured;

    public bool RequiresInjuredTarget => mustTargetInjured;

    // Use this only for rules that belong to one specific skill.
    // Declarative targeting rules now live directly on SkillData.
    public bool AreSkillSpecificRequirementsMet(Unit caster, Unit target)
    {
        return caster != null;
    }
}
