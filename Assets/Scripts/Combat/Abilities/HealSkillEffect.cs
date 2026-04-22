using UnityEngine;

[CreateAssetMenu(fileName = "HealSkillEffect", menuName = "Combat/Skills/Effects/Heal Skill Effect")]
public class HealSkillEffect : SkillEffect
{
    [SerializeField] private int _heal = 1;

    public override bool Apply(SkillCastContext context, Unit target)
    {
        if (context == null || context.Caster == null || target == null || !target.IsAlive)
            return false;

        if (target.CurrentHealth >= target.MaxHealth)
            return false;

        target.Heal(Mathf.Max(0, _heal), context.Caster);
        return true;
    }
}
