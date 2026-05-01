public sealed class SkillCastContext
{
    public SkillCastContext(Unit caster, SkillData skill, Unit primaryTarget)
    {
        Caster = caster;
        Skill = skill;
        PrimaryTarget = primaryTarget;
    }

    public Unit Caster { get; }
    public SkillData Skill { get; }
    public Unit PrimaryTarget { get; }
}
