using UnityEngine;

[CreateAssetMenu(fileName = "DamageSkillEffect", menuName = "Combat/Skills/Effects/Damage Skill Effect")]
public class DamageSkillEffect : SkillEffect
{
    [SerializeField] private int _damage = 1;

    public override bool Apply(SkillContext context, SkillImpact impact)
    {
        Unit hitUnit = ResolveTargetUnit(impact);
        Unit caster = context != null ? context.Caster : null;
        if (caster == null || hitUnit == null || !hitUnit.IsAlive)
            return false;

        int damageAmount = Mathf.Max(0, _damage);
        hitUnit.TakeDamage(damageAmount, caster);

        if (caster.StatusEffects != null && caster.StatusEffects.HasLifeSteal)
        {
            float healPercent = caster.StatusEffects.GetEffectStrength(StatusEffectType.LifeSteal);
            int healAmount = Mathf.RoundToInt(damageAmount * healPercent);
            LifeController casterLife = caster.GetComponent<LifeController>();
            if (healAmount > 0 && casterLife != null)
                casterLife.Heal(healAmount, hitUnit);
        }

        return true;
    }
}
