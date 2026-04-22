using UnityEngine;

[CreateAssetMenu(fileName = "DamageSkillEffect", menuName = "Combat/Skills/Effects/Damage Skill Effect")]
public class DamageSkillEffect : SkillEffect
{
    [SerializeField] private int _damage = 1;

    public override bool Apply(SkillCastContext context, Unit target)
    {
        if (context == null || context.Caster == null || target == null || !target.IsAlive)
            return false;

        target.TakeDamage(Mathf.Max(0, _damage), context.Caster);
        return true;
    }
}
