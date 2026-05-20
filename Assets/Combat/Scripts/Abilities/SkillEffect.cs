using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public abstract bool Apply(SkillContext context, Unit hitUnit);
}
