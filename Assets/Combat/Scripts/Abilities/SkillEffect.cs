using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public abstract bool Apply(Unit caster, SkillData skill, Unit chosenTarget, Unit target);
}
