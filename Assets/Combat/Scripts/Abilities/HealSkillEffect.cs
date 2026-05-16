using UnityEngine;

[CreateAssetMenu(fileName = "HealSkillEffect", menuName = "Combat/Skills/Effects/Heal Skill Effect")]
public class HealSkillEffect : SkillEffect
{
    [SerializeField] private int _heal = 1;

    public override bool Apply(Unit caster, SkillData skill, Unit chosenTarget, Unit target)
    {
        if (caster == null || target == null || !target.IsAlive)
            return false;

        if (target.CurrentHealth >= target.MaxHealth)
            return false;

        target.Heal(Mathf.Max(0, _heal), caster);
        return true;
    }
}
