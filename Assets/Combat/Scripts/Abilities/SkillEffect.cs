using UnityEngine;

public abstract class SkillEffect : ScriptableObject
{
    public virtual bool Apply(Unit caster, SkillData skill, Unit selectedTarget, Unit hitUnit)
    {
        if (caster == null || skill == null)
            return false;

        RoomContext roomContext = caster.RoomContext;
        RoomGrid roomGrid = roomContext != null ? roomContext.RoomGrid : null;
        Unit impactCenterUnit = selectedTarget;
        Vector3 impactCenterWorld = impactCenterUnit != null ? impactCenterUnit.Position : caster.Position;
        SkillContext context = new(
            caster,
            skill,
            selectedTarget,
            default,
            false,
            impactCenterWorld,
            impactCenterUnit,
            roomContext,
            roomGrid);
        return Apply(context, hitUnit);
    }

    public abstract bool Apply(SkillContext context, Unit hitUnit);
}
