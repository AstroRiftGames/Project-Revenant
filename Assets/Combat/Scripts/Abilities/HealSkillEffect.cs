using UnityEngine;

[CreateAssetMenu(fileName = "HealSkillEffect", menuName = "Combat/Skills/Effects/Heal Skill Effect")]
public class HealSkillEffect : SkillEffect
{
    [SerializeField] private int _heal = 1;

    public override bool Apply(SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || hitUnit == null || !hitUnit.IsAlive)
            return false;

        if (hitUnit.CurrentHealth >= hitUnit.MaxHealth)
            return false;

        hitUnit.Heal(Mathf.Max(0, _heal), caster);
        return true;
    }
}
