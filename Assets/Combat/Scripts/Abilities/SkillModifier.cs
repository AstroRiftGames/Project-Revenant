using System.Collections.Generic;
using UnityEngine;

public abstract class SkillModifier : ScriptableObject
{
    public virtual void ModifyImpacts(SkillContext context, SkillData skill, List<SkillImpact> impacts)
    {
    }

    protected static bool IsCombatAliveUnit(Unit unit)
    {
        return unit != null &&
               unit.IsAlive &&
               unit.LifecycleState == UnitLifecycleState.Alive;
    }
}
