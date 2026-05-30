using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public abstract bool Apply(SkillContext context, SkillImpact impact);

    protected static Unit ResolveTargetUnit(SkillImpact impact)
    {
        return impact != null && impact.HasTargetUnit
            ? impact.TargetUnit
            : null;
    }

    protected static bool IsCombatAliveUnit(Unit unit)
    {
        return unit != null &&
               unit.IsAlive &&
               unit.LifecycleState == UnitLifecycleState.Alive;
    }
}
