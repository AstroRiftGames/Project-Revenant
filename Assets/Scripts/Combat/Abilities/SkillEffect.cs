using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public abstract bool Apply(SkillCastContext context, Unit target);
}
